#!/usr/bin/env python3
"""بررسی فهرست مدل‌ها.

مدل‌ها را من نمی‌توانم دانلود کنم؛ تو می‌گذاری. این ابزار قبل از بازی می‌گوید
کدام خط فهرست بدشکل است و کدام جای هنوز خالی است — تا «جعبه‌ی خاکستری» یک
تصمیم باشد، نه یک فراموشی.

خطای واقعی: خط بدشکل، تغییردهنده‌ی ناشناخته، یا نام آبجکتی که در هیچ صحنه‌ای
نیست. فایل نبودن خطا نیست؛ فقط «باز» است.

اجرا:  python3 tools/modelcheck.py
"""

import glob
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

MANIFEST = "Assets/_Project/Art/Models/model-slots.txt"
ROOT = "Assets/_Project/Art/Models"
SCENES = "Assets/_Project/Scenes/*.unity"
KNOWN = {"rotY", "stretch", "lift"}


def scene_names():
    names = set()
    for path in glob.glob(SCENES):
        with io.open(path, encoding="utf-8") as handle:
            names |= set(re.findall(r"m_Name: (\S+)", handle.read()))
    return names


def main():
    if not os.path.exists(MANIFEST):
        print("  ✗ فهرست پیدا نشد: " + MANIFEST)
        return 1

    names = scene_names()
    problems = 0
    filled = 0
    empty = 0
    slots = []

    with io.open(MANIFEST, encoding="utf-8") as handle:
        for number, raw in enumerate(handle, start=1):
            line = raw.strip()

            if not line or line.startswith("#"):
                continue

            if "=" not in line:
                print("  ✗ خط %d: علامت = ندارد" % number)
                problems += 1
                continue

            name, _, rest = line.partition("=")
            name = name.strip()
            parts = [p.strip() for p in rest.split("|")]
            path = parts[0]

            if not path:
                print("  ✗ خط %d: مسیر خالی است" % number)
                problems += 1
                continue

            full = path if path.startswith("Assets/") else os.path.join(ROOT, path)

            for modifier in parts[1:]:
                key = modifier.split("=")[0].strip()
                if key not in KNOWN:
                    print("  ✗ خط %d: تغییردهنده‌ی ناشناخته «%s»" % (number, key))
                    problems += 1

            if name not in names:
                print("  ✗ خط %d: آبجکتی به نام «%s» در هیچ صحنه‌ای نیست" % (number, name))
                problems += 1

            slots.append((name, full))

            if os.path.exists(full):
                filled += 1
            else:
                empty += 1

    print("  جای مدل: %d پُر، %d خالی از %d" % (filled, empty, len(slots)))

    if empty:
        print("  (فایل‌های نبوده خطا نیستند؛ صحنه همان جعبه‌ی ساده را نشان می‌دهد.)")

    print("  problems: %d" % problems)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
