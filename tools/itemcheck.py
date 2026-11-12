#!/usr/bin/env python3
"""بررسی فایل آیتم‌ها — همان قواعدی که ItemCatalog.Parse در بازی اجرا می‌کند.

    python3 tools/itemcheck.py
    python3 tools/itemcheck.py Assets/_Project/Resources/Items/items.txt

چه چیزی را می‌گیرد:
  * شناسه‌ی خالی، دارای فاصله یا تکراری
  * دسته‌ی ناشناس (در بازی بی‌صدا Miscellaneous می‌شود، یکجا همیشه ناشناس می‌ماند)
  * بیشترین تعداد نامعتبر
  * گزینه‌ی بی‌شکل (بدون =) یا کلید ناشناس
  * وزن/ارزش/پرچم نامعتبر
  * آیکون یا Prefab که مسیرش وجود ندارد

خروجی: یک خط برای هر مشکل، و در آخر شمارش. کد خروج ۱ یعنی مشکل هست.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT = os.path.join(ROOT, "Assets/_Project/Resources/Items/items.txt")
RESOURCES = os.path.join(ROOT, "Assets/_Project/Resources")

CATEGORIES = {
    "document", "key", "weapon", "ammo", "evidence",
    "consumable", "tool", "quest", "item", "misc",
}

FLAGS = {"use", "drop", "trade", "store", "examine", "destroy"}
FLAG_ALIASES = {"all", "none", "-", "*"}
OPTION_KEYS = {"weight", "value", "flags", "icon", "prefab", "sub", "desc.fa", "desc.en"}
ICON_EXTENSIONS = ("", ".png", ".jpg", ".jpeg")


def normalize(text):
    return (text or "").strip().lower()


def check_icon(path, problems, line_no):
    """icon=Icons/Keys/archive  →  Resources/Icons/Keys/archive.png"""
    clean = path.strip()
    base = os.path.join(RESOURCES, clean)

    for ext in ICON_EXTENSIONS:
        candidate = base + ext
        if ext == "" and os.path.isdir(candidate):
            return
        if os.path.isfile(candidate):
            return

    problems.append((line_no, "آیکون پیدا نشد: " + clean))


def check_prefab(path, problems, line_no):
    clean = path.strip()
    candidate = os.path.join(RESOURCES, clean + ".prefab")

    if not os.path.isfile(candidate):
        problems.append((line_no, "Prefab پیدا نشد: " + clean))


def parse_float(text):
    try:
        return float(text)
    except ValueError:
        return None


def parse_int(text):
    try:
        return int(text)
    except ValueError:
        return None


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT

    if not os.path.isfile(path):
        print("فایل پیدا نشد: " + path)
        return 1

    body = io.open(path, encoding="utf-8-sig").read()
    lines = body.splitlines()

    problems = []
    seen = {}
    items = 0

    for index, raw in enumerate(lines, start=1):
        text = raw.strip()

        if not text or text.startswith("#"):
            continue

        parts = text.split("|")

        if len(parts) < 4:
            problems.append((index, "کمتر از ۴ فیلد: " + text))
            continue

        item_id = normalize(parts[0])

        if not item_id:
            problems.append((index, "شناسه خالی است"))
            continue

        if " " in item_id or "\t" in item_id:
            problems.append((index, "شناسه فاصله دارد: " + item_id))
            continue

        if item_id in seen:
            problems.append((index, "شناسه تکراری «%s»؛ قبلاً خط %d" % (item_id, seen[item_id])))
            continue

        seen[item_id] = index

        category = normalize(parts[3])

        if category not in CATEGORIES:
            problems.append((index, "دسته‌ی ناشناس «%s» (یکی از: %s)" % (category, ", ".join(sorted(CATEGORIES)))))

        if len(parts) >= 5 and parts[4].strip():
            stack = parse_int(parts[4].strip())

            if stack is None or stack < 1:
                problems.append((index, "بیشترین تعداد نامعتبر: " + parts[4].strip()))

        for token in parts[5:]:
            token = token.strip()

            if not token:
                continue

            if "=" not in token:
                problems.append((index, "گزینه بی‌شکل (باید key=value باشد): " + token))
                continue

            key, value = token.split("=", 1)
            key = normalize(key)
            value = value.strip()

            if key not in OPTION_KEYS:
                problems.append((index, "کلید ناشناس: " + key))
                continue

            if key == "weight":
                number = parse_float(value)
                if number is None or number < 0:
                    problems.append((index, "وزن نامعتبر: " + value))

            elif key == "value":
                number = parse_int(value)
                if number is None or number < 0:
                    problems.append((index, "ارزش نامعتبر: " + value))

            elif key == "flags":
                cleaned = normalize(value)
                if cleaned not in FLAG_ALIASES:
                    for name in cleaned.split(","):
                        name = name.strip()
                        if name and name not in FLAGS:
                            problems.append((index, "پرچم ناشناس: " + name))

            elif key == "icon":
                if value:
                    check_icon(value, problems, index)

            elif key == "prefab":
                if value:
                    check_prefab(value, problems, index)

        items += 1

    for line_no, message in problems:
        print("  %s | خط %d | %s" % (os.path.basename(path), line_no, message))

    print("lines: %d items: %d problems: %d" % (len(lines), items, len(problems)))

    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
