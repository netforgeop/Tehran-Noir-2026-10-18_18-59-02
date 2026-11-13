#!/usr/bin/env python3
"""بررسی APIهای حذف‌شده یا منسوخ (CS0619 / CS0618).

چرا این ابزار؟ کامپایلر فقط وقتی به این‌ها می‌رسد که پروژه در Unity باز شود.
در جلسه‌ی ۳۹، `TMP_Text.enableWordWrapping` در Unity 6 به «خطای منسوخ»
تبدیل شده بود و چهار خطا ساخت؛ هیچ‌کدام از چهار ابزار قبلی این را نمی‌دید،
چون همه‌شان فقط داخل کد خودِ پروژه را می‌گشتند.

فهرست پایین عمداً کوتاه و دستی است: فقط چیزهایی که در نسخه‌ی فعلی موتور
**خطا** یا **هشدار** می‌دهند و جانشین روشن دارند. با هر ارتقای موتور،
یک خط به همین فهرست اضافه می‌شود؛ لازم نیست ابزار عمومی‌تر شود.

اجرا:  python3 tools/apicheck.py [مسیر]
"""

import glob
import io
import os
import re
import sys

# (الگو، سطح، جانشین، توضیح)
# level: error = کامپایل نمی‌شود · warn = کامپایل می‌شود ولی منسوخ است
# عمداً هیچ قاعده‌ای برای `.velocity` نیست: `CharacterController.velocity` و
# `Rigidbody.velocity` هنوز درست‌اند و فیلد خودِ پروژه هم همین نام را دارد.
RULES = [
    (r"\.enableWordWrapping\b", "error",
     "textWrappingMode = TextWrappingModes.Normal",
     "TMP 3.2 / Unity 6: enableWordWrapping خطای منسوخ است"),
    (r"\.enableAutoSizing\b", "warn",
     "fontSizeAutoSize = true",
     "TMP: نام تازه‌ی همین تنظیم"),
    (r"\bFindObjectOfType\s*<", "warn",
     "FindFirstObjectByType<T>()",
     "Unity 6: FindObjectOfType منسوخ شد"),
    (r"\bFindObjectsOfType\s*<", "warn",
     "FindObjectsByType<T>(FindObjectsSortMode.None)",
     "Unity 6: FindObjectsOfType منسوخ شد"),
]

ROOT = "Assets/_Project/Scripts"


def strip_comments(text):
    """کامنت‌ها و رشته‌ها با فاصله جایگزین می‌شوند تا شماره‌ی خط حفظ شود."""
    out = []
    i = 0
    n = len(text)
    in_str = False
    in_chr = False

    while i < n:
        ch = text[i]

        if in_str:
            if ch == "\\":
                out.append("  ")
                i += 2
                continue
            if ch == '"':
                in_str = False
                out.append('"')
            else:
                out.append("\n" if ch == "\n" else " ")
            i += 1
            continue

        if in_chr:
            if ch == "\\":
                out.append("  ")
                i += 2
                continue
            if ch == "'":
                in_chr = False
            out.append(" " if ch != "\n" else "\n")
            i += 1
            continue

        if ch == '"':
            in_str = True
            out.append('"')
            i += 1
            continue

        if ch == "'":
            in_chr = True
            out.append(" ")
            i += 1
            continue

        if ch == "/" and i + 1 < n and text[i + 1] == "/":
            while i < n and text[i] != "\n":
                out.append(" ")
                i += 1
            continue

        if ch == "/" and i + 1 < n and text[i + 1] == "*":
            i += 2
            while i < n and not (text[i] == "*" and i + 1 < n and text[i + 1] == "/"):
                out.append("\n" if text[i] == "\n" else " ")
                i += 1
            i += 2
            continue

        out.append(ch)
        i += 1

    return "".join(out)


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else ROOT

    paths = [p for p in sorted(glob.glob(os.path.join(root, "**", "*.cs"), recursive=True))
             if "RTLTMPro" not in p]

    errors = 0
    warnings = 0

    for path in paths:
        code = strip_comments(io.open(path, encoding="utf-8").read())

        for pattern, level, replacement, why in RULES:
            for match in re.finditer(pattern, code):
                line = code[:match.start()].count("\n") + 1
                text = match.group(0)

                if level == "error":
                    errors += 1
                    print("  %s | line %d | %s → %s  (%s)"
                          % (os.path.basename(path), line, text, replacement, why))
                else:
                    warnings += 1
                    print("  ~ %s | line %d | %s → %s  (%s)"
                          % (os.path.basename(path), line, text, replacement, why))

    print("files: %d errors: %d warnings: %d" % (len(paths), errors, warnings))

    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
