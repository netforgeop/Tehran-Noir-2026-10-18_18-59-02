"""Offline argument-count check (catches CS1501/CS7036-style mistakes).

Indexes every method declared in the project together with how many arguments it
accepts (minimum counts parameters with a default, maximum counts `params T[]`),
then checks calls of the form `Type.Method(...)` and bare calls inside the class
that declares them.

    python3 tools/argcheck.py Assets/_Project/Scripts
"""
import glob
import os
import re
import sys

TYPE_RE = re.compile(r'\b(?:class|struct|interface)\s+(\w+)[^{]*\{')
METHOD_RE = re.compile(
    r'^\s*(?:(?:public|private|protected|internal|static|virtual|override|sealed|abstract|'
    r'async|partial|extern|unsafe|new)\s+)+'
    r'[A-Za-z_~][\w<>\[\],\.\?]*\s+([A-Za-z_]\w*)\s*\(([^)]*)\)\s*$')

KEYWORDS = {
    'if', 'for', 'foreach', 'while', 'switch', 'catch', 'lock', 'using', 'return',
    'new', 'typeof', 'sizeof', 'nameof', 'base', 'this', 'get', 'set', 'add',
    'remove', 'when', 'checked', 'unchecked', 'default', 'throw', 'await', 'yield',
    'fixed', 'stackalloc', 'delegate', 'var', 'is', 'as', 'in', 'out', 'ref',
    'params', 'do', 'else', 'case', 'operator', 'implicit', 'explicit', 'where',
}


def blank(text):
    out = []
    i = 0
    n = len(text)
    in_str = False
    in_chr = False
    while i < n:
        ch = text[i]
        if in_str:
            if ch == '\\':
                i += 2
                continue
            if ch == '"':
                in_str = False
                out.append('"')
            i += 1
            continue
        if in_chr:
            if ch == '\\':
                i += 2
                continue
            if ch == "'":
                in_chr = False
                out.append("'")
            i += 1
            continue
        if ch == '/' and i + 1 < n and text[i + 1] == '/':
            i += 2
            while i < n and text[i] != '\n':
                i += 1
            continue
        if ch == '/' and i + 1 < n and text[i + 1] == '*':
            i += 2
            while i + 1 < n and not (text[i] == '*' and text[i + 1] == '/'):
                if text[i] == '\n':
                    out.append('\n')
                i += 1
            i += 2
            continue
        if ch == '"':
            in_str = True
            out.append('"')
            i += 1
            continue
        if ch == "'":
            in_chr = True
            out.append("'")
            i += 1
            continue
        out.append(ch)
        i += 1
    while True:
        joined = ''.join(out)
        stripped = re.sub(r'\[[^\[\]\n]*\]', ' ', joined)
        if stripped == joined:
            break
        out = list(stripped)
    return ''.join(out)


def param_range(raw):
    if not raw.strip():
        return 0, 0

    parts = []
    depth = 0
    current = ''
    for ch in raw:
        if ch in '<([{':
            depth += 1
        elif ch in '>)]}':
            depth -= 1
        if ch == ',' and depth == 0:
            parts.append(current)
            current = ''
            continue
        current += ch
    parts.append(current)

    minimum = 0
    variadic = False
    for part in parts:
        if 'params ' in part:
            variadic = True
            continue
        if '=' not in part:
            minimum += 1
    maximum = 10 ** 6 if variadic else len(parts)
    return minimum, maximum


def split_args(call):
    """Count top-level commas of the argument list that starts at call[0] == '('."""
    depth = 0
    count = 0
    empty = True
    i = 0
    while i < len(call):
        ch = call[i]
        if ch in '([{':
            depth += 1
            if depth == 1:
                i += 1
                continue
        elif ch in ')]}':
            depth -= 1
            if depth == 0:
                return (0 if empty else count + 1)
        elif ch == ',' and depth == 1:
            count += 1
            empty = False
        elif depth >= 1 and not ch.isspace():
            empty = False
        i += 1
    return 0 if empty else count + 1


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else 'Assets/_Project/Scripts'
    paths = [p for p in sorted(glob.glob(os.path.join(root, '**', '*.cs'), recursive=True))
             if 'RTLTMPro' not in p]

    codes = {p: blank(open(p, encoding='utf-8').read()) for p in paths}

    # type name -> list of (method, min, max)
    api = {}
    # type name -> list of (min, max) declared constructors
    ctors = {}
    spans_by_file = {}
    struct_names = set()
    # (type, method) pairs, so calls can be resolved
    for path, code in codes.items():
        spans = []
        for m in TYPE_RE.finditer(code):
            brace = code.find('{', m.end() - 1)
            depth = 0
            i = brace
            while i < len(code):
                if code[i] == '{':
                    depth += 1
                elif code[i] == '}':
                    depth -= 1
                    if depth == 0:
                        break
                i += 1
            if 'struct' in m.group(0):
                struct_names.add(m.group(1))

            spans.append((m.group(1), brace + 1, i))

        spans_by_file[path] = spans

        for name, start, end in spans:
            for line in code[start:end].split('\n'):
                m = METHOD_RE.match(line)
                if not m:
                    continue
                lo, hi = param_range(m.group(2))
                api.setdefault(name, []).append((m.group(1), lo, hi))

    problems = 0
    for path, code in codes.items():
        for m in re.finditer(r'\b([A-Za-z_]\w*)\.([A-Za-z_]\w*)\s*\(', code):
            type_name, method = m.group(1), m.group(2)
            entries = api.get(type_name)
            if not entries:
                continue
            candidates = [(lo, hi) for n, lo, hi in entries if n == method]
            if not candidates:
                continue          # unknown member: symcheck's job
            count = split_args(code[m.end() - 1:])
            if any(lo <= count <= hi for lo, hi in candidates):
                continue
            line = code[:m.start()].count('\n') + 1
            print(f'  {os.path.basename(path)} | line {line} | {type_name}.{method}() '
                  f'called with {count} argument(s); expected {candidates}')
            problems += 1

    # --- constructors: collect declarations, then check `new Type(...)` -------
    for path, code in codes.items():
        for type_name, start, end in spans_by_file.get(path, []):
            body = code[start:end]
            pattern = re.compile(r'\b' + re.escape(type_name) + r'\s*\(')

            for m in pattern.finditer(body):
                line_start = body.rfind('\n', 0, m.start()) + 1
                prefix = body[line_start:m.start()].strip()

                if not re.match(r'^(?:public|private|protected|internal)(?:\s+static)?$', prefix):
                    continue

                open_paren = body.find('(', m.start())
                depth = 0
                close = -1

                for index in range(open_paren, len(body)):
                    if body[index] == '(':
                        depth += 1
                    elif body[index] == ')':
                        depth -= 1
                        if depth == 0:
                            close = index
                            break

                if close < 0:
                    continue

                lo, hi = param_range(body[open_paren + 1:close])
                ctors.setdefault(type_name, []).append((lo, hi))

    for path, code in codes.items():
        for m in re.finditer(r'\bnew\s+([A-Za-z_]\w*)\s*\(', code):
            type_name = m.group(1)
            entries = ctors.get(type_name)

            if not entries:
                continue

            count = split_args(code[m.end() - 1:])

            # struct همیشه سازنده‌ی بی‌پارامتر دارد، حتی اگر در کد نوشته نشده باشد
            if count == 0 and type_name in struct_names:
                continue

            if any(lo <= count <= hi for lo, hi in entries):
                continue

            line = code[:m.start()].count('\n') + 1
            print(f'  {os.path.basename(path)} | line {line} | new {type_name}() '
                  f'with {count} argument(s); expected {sorted(set(entries))}')
            problems += 1

    print('files:', len(paths), 'problems:', problems)


if __name__ == '__main__':
    main()
