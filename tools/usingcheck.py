#!/usr/bin/env python3
"""بررسی «using» جاافتاده — همان چیزی که کامپایلر با CS0103 روی نام یک نوع می‌دهد.

    python3 tools/usingcheck.py Assets/_Project/Scripts

چرا لازم شد: `PickupInteractable` از `Localization` استفاده می‌کرد ولی
`using TehranNoir.Core;` نداشت. symcheck آن را نگرفت، چون نام در کل پروژه
تعریف شده بود؛ چیزی که نداشت، فضا‌ی نامش بود.

کار این ابزار:
  * فضا‌ی نام هر نوعِ پروژه را جمع می‌کند.
  * در هر فایل، استفاده‌های همان نوع‌ها را می‌بیند و می‌گوید
    آیا `using` لازم نوشته شده یا نه.

کامنت‌ها و رشته‌ها پاک می‌شوند تا واژه‌های فارسی داخل توضیح، نوع حساب نشوند.
"""

import glob
import os
import re
import sys

NAMESPACE_RE = re.compile(r'\bnamespace\s+([\w\.]+)')
USING_RE = re.compile(r'\busing\s+([\w\.]+)\s*;')
TYPE_RE = re.compile(r'\b(?:class|struct|interface|enum)\s+([A-Za-z_]\w*)')


def blank(text):
    """کامنت و رشته را با فاصله عوض می‌کند تا آفست خط‌ها یکی بماند."""
    out = []
    i = 0
    n = len(text)
    state = 0  # 0 code, 1 string, 2 char, 3 line comment, 4 block comment

    while i < n:
        ch = text[i]

        if state == 0:
            if ch == '/' and i + 1 < n and text[i + 1] == '/':
                state = 3
                i += 2
                continue
            if ch == '/' and i + 1 < n and text[i + 1] == '*':
                state = 4
                i += 2
                continue
            if ch == '"':
                state = 1
                out.append(' ')
                i += 1
                continue
            if ch == "'":
                state = 2
                out.append(' ')
                i += 1
                continue
            out.append(ch)
            i += 1
            continue

        if state == 1:
            if ch == '\\':
                i += 2
                continue
            if ch == '"':
                state = 0
            i += 1
            out.append(' ')
            continue

        if state == 2:
            if ch == '\\':
                i += 2
                continue
            if ch == "'":
                state = 0
            i += 1
            out.append(' ')
            continue

        if state == 3:
            if ch == '\n':
                state = 0
                out.append('\n')
            i += 1
            continue

        if state == 4:
            if ch == '*' and i + 1 < n and text[i + 1] == '/':
                state = 0
                i += 2
                continue
            out.append('\n' if ch == '\n' else ' ')
            i += 1
            continue

    return ''.join(out)


def files(root):
    return [p for p in sorted(glob.glob(os.path.join(root, '**', '*.cs'), recursive=True))
            if 'RTLTMPro' not in p]


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else 'Assets/_Project/Scripts'
    paths = files(root)

    codes = {path: blank(open(path, encoding='utf-8-sig').read()) for path in paths}

    owner = {}
    for path, code in codes.items():
        namespace = NAMESPACE_RE.search(code)
        space = namespace.group(1) if namespace else ''
        for name in TYPE_RE.findall(code):
            owner.setdefault(name, space)

    problems = 0

    for path, code in codes.items():
        namespace = NAMESPACE_RE.search(code)
        space = namespace.group(1) if namespace else ''
        usings = set(USING_RE.findall(code))
        own = set(TYPE_RE.findall(code))

        for name, home in owner.items():
            if name in own or home == space or home in usings:
                continue

            if any(home == u or home.startswith(u + '.') for u in usings):
                continue

            # اگر همان نام در همین فایل به‌عنوان عضو (فیلد/ثابت/متد) تعریف شده،
            # استفاده‌اش نوع نیست؛ نادیده بگیر.
            member = re.search(
                r'(?:\bconst\b|\breadonly\b|\bstatic\b|\bprivate\b|\bpublic\b|'
                r'\bprotected\b|\binternal\b)[^\n;{}]*\b' + re.escape(name) + r'\b\s*(?:=|;|\()',
                code)

            if member:
                continue

            for match in re.finditer(r'(?<![\w.])' + re.escape(name) + r'\b', code):
                line = code[code.rfind('\n', 0, match.start()) + 1:code.find('\n', match.start())]

                if re.search(r'\b(namespace|class|struct|enum|interface)\s+' + re.escape(name), line):
                    break

                number = code[:match.start()].count('\n') + 1
                print('  %s | line %d | نوع «%s» از «%s» است ولی using ندارد'
                      % (os.path.basename(path), number, name, home))
                problems += 1
                break

    print('files:', len(paths), 'types:', len(owner), 'problems:', problems)
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
