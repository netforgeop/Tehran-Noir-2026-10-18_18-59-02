#!/usr/bin/env python3
"""بررسی ارجاع‌های داستان به آیتم‌ها.

چرا این ابزار؟ یک اشتباه تایپی در شناسه‌ی آیتم هیچ خطای کامپایلی نمی‌دهد:
`[if] item.file.seal >= 1` همیشه نادرست است، دیالوگ بی‌صدا مسیر دیگری
می‌رود و کسی نمی‌فهمد. همان کلاس خطای «بی‌صدا» که ابزار ایستا برایش داریم.

چه چیزی بررسی می‌شود:
  ۱) هر `item.<id>` در شرط‌ها ([if] / [when] / [start] / [done]) باید
     شناسه‌ی یک آیتم موجود در items.txt باشد.
  ۲) هر `[do] item.give.<id> = <ظرف>` باید آیتم موجود و ظرف غیرخالی داشته باشد.
  ۳) نوشتن مستقیم `[do] item.<id> = ...` ممنوع است: پروجکشن `item.*` فقط
     دست InventoryManager است و هر نوشتن دیگری با SyncProjection پاک می‌شود.

اجرا:  python3 tools/storycheck.py
"""

import glob
import io
import os
import re
import sys

ITEMS = "Assets/_Project/Resources/Items/items.txt"
SOURCES = ["Assets/_Project/Dialogue/*.txt", "Assets/_Project/Resources/Quests/*.txt"]
DOCUMENTS = "Assets/_Project/Resources/Documents/*.txt"

TAG_RE = re.compile(r"^\[(do|if|when|start|done)\]\s*(.*)$")
ITEM_RE = re.compile(r"\bitem\.([A-Za-z0-9_.]+)")
CONTAINER_RE = re.compile(r"^[A-Za-z0-9_.]+$")
GIVE_PREFIX = "item.give."


def load_ids():
    ids = set()
    if not os.path.exists(ITEMS):
        print("items file not found: " + ITEMS)
        return ids
    with io.open(ITEMS, encoding="utf-8") as handle:
        for raw in handle:
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            item_id = line.split("|", 1)[0].strip().lower()
            if item_id:
                ids.add(item_id)
    return ids


def check_file(path, ids, problems):
    refs = 0
    with io.open(path, encoding="utf-8") as handle:
        for number, raw in enumerate(handle, 1):
            line = raw.strip()
            if not line or line.startswith("#"):
                continue

            match = TAG_RE.match(line)
            if not match:
                continue

            tag, body = match.group(1), match.group(2).strip()

            if tag == "do":
                if body.startswith(GIVE_PREFIX):
                    refs += 1
                    if "=" not in body:
                        problems.append((path, number, "تحویل بی‌علامت تساوی: " + body))
                        continue
                    item_id, container = body[len(GIVE_PREFIX):].split("=", 1)
                    item_id = item_id.strip().lower()
                    container = container.strip()

                    if item_id not in ids:
                        problems.append((path, number, "آیتم ناشناس در تحویل: item." + item_id))
                    if not container or not CONTAINER_RE.match(container):
                        problems.append((path, number, "ظرف مقصد نامعتبر: «" + container + "»"))
                    if container.lower() == "world":
                        problems.append((path, number,
                                         "تحویل به world یعنی برگرداندن به زمین؛ برای تحویل داستانی ظرف مشخص بنویس."))
                elif re.match(r"^item\.", body):
                    problems.append((path, number,
                                     "نوشتن مستقیم پروجکشن ممنوع: " + body +
                                     " — برای جابه‌جایی از item.give.<id> = <ظرف> استفاده کن."))
                continue

            # [if] / [when] / [start] / [done]
            for token in ITEM_RE.findall(body):
                refs += 1
                item_id = token.lower()

                if item_id.startswith("give."):
                    problems.append((path, number,
                                     "item.give فقط در [do] معنی دارد، نه در شرط: item." + token))
                    continue

                if item_id not in ids:
                    problems.append((path, number, "آیتم ناشناس در شرط: item." + token))

    return refs


def check_document(path, ids, problems):
    """سندها هم شرط دارند: [when] ... و جدول [row] ... .

    دو خطای بی‌صدا اینجا گرفته می‌شود:
      * شناسه‌ی آیتمِ غلط در [when] → ردیف یا خط هرگز دیده نمی‌شود.
      * تعداد خانه‌های [row] با [columns] نمی‌خواند → جدول جابه‌جا چاپ می‌شود.
    """
    refs = 0
    column_count = 0
    row_count = 0

    with io.open(path, encoding="utf-8") as handle:
        for number, raw in enumerate(handle, 1):
            line = raw.strip()

            if not line or line.startswith("#") or not line.startswith("["):
                continue

            close = line.find("]")
            if close < 0:
                continue

            tag = line[1:close].strip().lower()
            value = line[close + 1:].strip()

            if tag == "when":
                refs += 1
                for token in ITEM_RE.findall(value):
                    item_id = token.lower()
                    if item_id not in ids:
                        problems.append((path, number, "آیتم ناشناس در شرط سند: item." + token))
                continue

            if tag == "columns":
                column_count = len([c for c in value.split("؛") if c.strip()])
                continue

            if tag == "row":
                row_count += 1
                cells = [c for c in value.split("؛")]
                if column_count and len(cells) != column_count:
                    problems.append((path, number,
                                     "تعداد خانه‌های ردیف " + str(len(cells)) +
                                     " است، ولی جدول " + str(column_count) + " ستون دارد."))
                continue

    if row_count and not column_count:
        problems.append((path, 0, "سند ردیف دارد ولی [columns] ندارد."))

    return refs


def main():
    ids = load_ids()
    if not ids:
        return 1

    problems = []
    files = 0
    refs = 0

    for pattern in SOURCES:
        for path in sorted(glob.glob(pattern)):
            files += 1
            refs += check_file(path, ids, problems)

    documents = 0

    for path in sorted(glob.glob(DOCUMENTS)):
        documents += 1
        refs += check_document(path, ids, problems)

    for path, number, message in problems:
        print("%s:%d: %s" % (path, number, message))

    print("story files: %d documents: %d refs: %d items: %d problems: %d" %
          (files, documents, refs, len(ids), len(problems)))

    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
