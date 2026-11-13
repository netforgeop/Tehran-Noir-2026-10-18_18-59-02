"""Offline symbol check for this project (catches CS0103-style mistakes).

Reads every script once and collects:
  * every member name declared anywhere in the project (methods, properties,
    fields, constants, nested types), and
  * every name called as a bare method (`Foo(`) or referenced as a private
    field (`_foo`).
A bare call or field that no type in the project declares — and that is not a
known Unity/BCL name — is reported. That is exactly the failure that happens
when a helper is deleted or renamed but its call sites stay behind.

It is deliberately repository-wide rather than per-class: no false positives
from inheritance, and it still catches the real mistake (a name that exists
nowhere any more).

A second pass catches the sibling failure: a name that exists only with the
other capitalisation (`NotePath` where the field is `notePath`). It is kept
narrow on purpose — only a PascalCase use, with no such member anywhere, whose
lowercase form is a field declared in the same file, is reported. A wide
version of this check produced a thousand false hits (`Image`/`image`,
`Bool`/`bool`), so do not loosen it.

    python3 tools/symcheck.py Assets/_Project/Scripts
"""
import glob
import os
import re
import sys

KEYWORDS = {
    'if', 'for', 'foreach', 'while', 'switch', 'catch', 'lock', 'using', 'return',
    'new', 'typeof', 'sizeof', 'nameof', 'base', 'this', 'get', 'set', 'add',
    'remove', 'when', 'checked', 'unchecked', 'default', 'throw', 'await', 'yield',
    'fixed', 'stackalloc', 'delegate', 'var', 'is', 'as', 'in', 'out', 'ref',
    'params', 'do', 'else', 'case', 'operator', 'implicit', 'explicit', 'where',
    'assert',
}

# Names a MonoBehaviour / UI / collection class may use without declaring them.
EXTERNAL = {
    'Destroy', 'DestroyImmediate', 'Instantiate', 'DontDestroyOnLoad',
    'FindFirstObjectByType', 'FindAnyObjectByType', 'FindObjectsByType',
    'Invoke', 'InvokeRepeating', 'CancelInvoke', 'IsInvoking',
    'StartCoroutine', 'StopCoroutine', 'StopAllCoroutines', 'print',
    'GetComponent', 'GetComponents', 'GetComponentInChildren', 'GetComponentsInChildren',
    'GetComponentInParent', 'GetComponentsInParent', 'TryGetComponent', 'AddComponent',
    'SendMessage', 'BroadcastMessage', 'CompareTag', 'Equals', 'GetHashCode',
    'ToString', 'ReferenceEquals', 'MemberwiseClone', 'Finalize', 'OnValidate',
    'Awake', 'Start', 'Update', 'LateUpdate', 'FixedUpdate', 'OnEnable', 'OnDisable',
    'OnDestroy', 'OnGUI', 'OnDrawGizmos', 'Reset', 'OnPopulateMesh', 'Name',
    'PropertyName', 'Component', 'Sprite', 'Texture2D', 'Color', 'Vector2',
    'SetVerticesDirty', 'SetMaterialDirty', 'SetAllDirty', 'SetLayoutDirty',
    'Vector3', 'Vector4', 'Rect', 'Mathf', 'Time', 'Resources', 'Debug',
}

DECL_METHOD = re.compile(r'([A-Za-z_~][\w<>\[\],\.\?]*)\s+([A-Za-z_]\w*)\s*(?:<[^>\n]*>)?\s*\([^;]*$')
DECL_PROPERTY = re.compile(r'([A-Za-z_][\w<>\[\],\.\?]*)\s+([A-Za-z_]\w*)\s*\{')
DECL_FIELD = re.compile(r'(?:^|[;{}])\s*(?:\[[^\]]*\]\s*)*'
                        r'(?:(?:public|private|protected|internal|static|readonly|const|volatile|new|event)\s+)*'
                        r'[A-Za-z_][\w<>\[\],\.\?]*\s+([A-Za-z_]\w*)\s*(?:=[^;]*)?;')

# A delegate-typed field is called exactly like a method (`Hook(x)`), so it must
# count as a callable. The types here may contain spaces after commas
# (`Func<string, WorldState, bool>`), which DECL_FIELD cannot match.
DECL_DELEGATE = re.compile(
    r'(?:^|[;{}])\s*(?:\[[^\]]*\]\s*)*'
    r'(?:(?:public|private|protected|internal|static|readonly|volatile|new|event)\s+)*'
    r'(?:System\s*\.\s*)?(?:Func|Action|Predicate|Comparison|Converter)\s*<[^;\n]*>\s+'
    r'([A-Za-z_]\w*)\s*(?:=[^;]*)?;', re.MULTILINE)

# A method signature may carry a default argument or sit on its own line, so it
# is matched over the whole file instead of per line.
DECL_SIGNATURE = re.compile(
    r'(?:(?:public|private|protected|internal|static|virtual|override|sealed|abstract|'
    r'async|partial|extern|unsafe|new)\s+)+'
    r'[A-Za-z_][\w<>\[\],\.\?]*\s+([A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\(')

# Names introduced by a parameter list (they are locals to the method).
DECL_PARAMETER = re.compile(
    r'[A-Za-z_][\w<>\[\],\.\?]*\s+([A-Za-z_]\w*)\s*(?:=[^,()]*)?\s*(?:,|\))')
DECL_TYPE = re.compile(r'\b(?:class|struct|interface|enum)\s+([A-Za-z_]\w*)')

CALL_RE = re.compile(r'(?<![\w.])([A-Za-z_]\w*)\s*\(')
FIELD_USE_RE = re.compile(r'(?<![\w.])(_[A-Za-z]\w*)\b')

# Every bare identifier, for the case-mismatch check.
IDENT_RE = re.compile(r'(?<![\w.])([A-Za-z_]\w*)')

# Fields declared with an access level, i.e. real members, not locals or
# parameters. Only these can be the "other half" of a case-mismatch typo.
FIELDS_WITH_MODIFIER = re.compile(
    r'(?:public|private|protected|internal)\s+'
    r'(?:(?:static|readonly|const|volatile|new)\s+)*'
    r'[A-Za-z_][\w<>\[\],\.\?]*\s+([A-Za-z_]\w*)\s*(?:=[^;]*)?;')

# Same, for a property whose opening brace sits on the next line.
PROPERTIES_WITH_MODIFIER = re.compile(
    r'(?:public|private|protected|internal)\s+'
    r'(?:(?:static|readonly|virtual|override|sealed|new)\s+)*'
    r'[A-Za-z_][\w<>\[\],\.\?]*\s+([A-Za-z_]\w*)\s*\r?\n\s*\{')


def blank(text):
    """Drop comments and literal contents so braces, parentheses and words
    inside them cannot confuse the scan."""
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
    while '[' in out:
        joined = ''.join(out)
        stripped = re.sub(r'\[[^\[\]\n]*\]', ' ', joined)
        if stripped == joined:
            break
        out = list(stripped)
    return ''.join(out)


def declared_names(code):
    names = set()

    for m in DECL_TYPE.finditer(code):
        names.add(m.group(1))

    for line in code.split('\n'):
        s = line.strip()
        if not s or s.startswith('//'):
            continue

        # a declaration never assigns straight into a call on the same line
        if '=' in s and 'new ' not in s and '=>' not in s:
            continue

        m = DECL_METHOD.search(s)
        if m:
            names.add(m.group(2))

        for m in DECL_PROPERTY.finditer(s):
            names.add(m.group(2))

    for m in DECL_SIGNATURE.finditer(code):
        names.add(m.group(1))

    for m in DECL_PARAMETER.finditer(code):
        names.add(m.group(1))

    for m in re.finditer(DECL_FIELD.pattern, code, re.MULTILINE):
        names.add(m.group(1))

    for m in DECL_DELEGATE.finditer(code):
        names.add(m.group(1))

    return names


def member_names(code):
    """اعلام‌های واقعیِ سطح کلاس: نوع، متد، خاصیت، فیلد با سطح دسترسی.

    چرا جدا از `declared_names`؟ چون آن تابع از پارامترها و آرگومان‌ها هم نام
    برمی‌دارد (`Check(..., NotePath)` را «اعلام» می‌شمرد) و بررسی تفاوت
    بزرگ/کوچک را بی‌اثر می‌کرد.
    """
    names = set()

    for m in DECL_TYPE.finditer(code):
        names.add(m.group(1))

    for m in DECL_SIGNATURE.finditer(code):
        names.add(m.group(1))

    for line in code.split('\n'):
        text = line.strip()
        if not text or text.startswith('//'):
            continue

        m = DECL_METHOD.search(text)
        if m:
            names.add(m.group(2))

        for m in DECL_PROPERTY.finditer(text):
            names.add(m.group(2))

    for m in DECL_DELEGATE.finditer(code):
        names.add(m.group(1))

    for m in FIELDS_WITH_MODIFIER.finditer(code):
        names.add(m.group(1))

    for m in PROPERTIES_WITH_MODIFIER.finditer(code):
        names.add(m.group(1))

    return names


def used_calls(code):
    for m in CALL_RE.finditer(code):
        name = m.group(1)
        if name in KEYWORDS:
            continue
        before = code[max(0, m.start() - 6):m.start()]
        if re.search(r'\bnew\s*$', before):
            continue          # constructor: the type itself is declared above
        yield name, code[:m.start()].count('\n') + 1


def case_mismatches(code, members):
    """CS0103 از تفاوت بزرگ/کوچک: `NotePath` استفاده شد، `notePath` اعلام شده بود.

    چرا این‌قدر باریک؟ چون یک بررسی عمومی روی همه‌ی نام‌ها هزار هشدار بی‌ربط
    می‌دهد: `Image` نوع است و `image` متغیر محلی، `bool` کلیدواژه است و `Bool`
    پارامتر. پس فقط این حالت گزارش می‌شود:

      * فیلدی با سطح دسترسی در همین فایل اعلام شده (`private string notePath`)،
      * نامی با حرف بزرگ شروع می‌شود و **هیچ‌جا** اعلام نشده،
      * و شکل کوچکِ همان نام، همان فیلد است.
    """
    fields = {}

    for m in FIELDS_WITH_MODIFIER.finditer(code):
        fields[m.group(1).lower()] = m.group(1)

    if not fields:
        return []

    found = []

    for m in IDENT_RE.finditer(code):
        name = m.group(1)

        if not name[0].isupper():
            continue

        if name in members or name in EXTERNAL or name in KEYWORDS:
            continue

        if looks_like_type(code, m):
            continue

        twin = fields.get(name.lower())

        if twin is None or twin == name:
            continue

        found.append((name, twin, code[:m.start()].count('\n') + 1))

    return found


def looks_like_type(code, match):
    """آیا این کلمه در جایگاه «نوع» نشسته، نه در جایگاه «مقدار»؟

    مثال دردسرساز: `CharacterController characterController;` — فیلد کوچک‌شروع
    `characterController` هست و کلمه‌ی `CharacterController` نوعِ یونیتی؛
    بدون این نگهبان، هر اعلام فیلد یک خطای بی‌ربط می‌شد.
    """
    before = code[:match.start()].rstrip()
    after = code[match.end():].lstrip()

    if before.endswith('<') or before.endswith('new '):
        return True

    if after.startswith('>'):
        return True

    # نام بعدی: یعنی همین کلمه نوعِ یک اعلام یا یک تبدیل (cast) است
    if after and (after[0].isalpha() or after[0] == '_'):
        return True

    return False


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else 'Assets/_Project/Scripts'
    paths = [p for p in sorted(glob.glob(os.path.join(root, '**', '*.cs'), recursive=True))
             if 'RTLTMPro' not in p]

    codes = {}
    declared = set()

    members = set()

    for path in paths:
        code = blank(open(path, encoding='utf-8').read())
        codes[path] = code
        declared |= declared_names(code)
        members |= member_names(code)

    problems = 0

    for path, code in codes.items():
        seen = set()

        for name, line in used_calls(code):
            if name in declared or name in EXTERNAL:
                continue
            if (name, line) in seen:
                continue
            seen.add((name, line))
            print(f'  {os.path.basename(path)} | line {line} | unknown call "{name}("')
            problems += 1

        for m in FIELD_USE_RE.finditer(code):
            name = m.group(1)
            if name in declared:
                continue
            line = code[:m.start()].count('\n') + 1
            if (name, line) in seen:
                continue
            seen.add((name, line))
            print(f'  {os.path.basename(path)} | line {line} | unknown field "{name}"')
            problems += 1

        mismatches = case_mismatches(code, members)

        for name, twin, line in mismatches:
            if (name, line) in seen:
                continue

            seen.add((name, line))
            print(f'  {os.path.basename(path)} | line {line} | '
                  f'case mismatch "{name}" (declared "{twin}")')
            problems += 1

    print('files:', len(paths), 'problems:', problems)

    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
