#!/usr/bin/env python3
"""بررسی ساختار فایل‌های دیالوگ، بدون باز کردن Unity.

پارسر دیالوگ در زمان اجرا خطا می‌دهد، ولی نه برای همه‌چیز: یک `->` که به
گره‌ی نبوده اشاره کند، یا یک `[say]` که یک لوله‌ی جداکننده کم دارد، ممکن
است تا وسط بازی تازه معلوم شود. این ابزار همان اشتباه‌ها را قبل از کامیت
می‌گیرد:

  ۱) تگ ناشناخته (پارسر فقط مجموعه‌ی بسته‌ی زیر را می‌شناسد).
  ۲) گره‌ی تکراری یا بدون گره‌ی شروع.
  ۳) پرش به گره‌ای که در فایل نیست ([opt]، [if]، [go]).
  ۴) خط [say] بدون سه بخش «گوینده | فارسی | انگلیسی».
  ۵) خط [opt] بدون «|» یا بدون «->».
  ۶) گره‌ی بی‌راه (هیچ مسیری از گره‌ی اول به آن نمی‌رسد).

اجرا:  python3 tools/dialoguecheck.py
"""

import glob
import io
import os
import re
import sys

FOLDER = "Assets/_Project/Dialogue"
TAGS = set("person name role cast node say opt go if else do end read".split())
ARROW = "->"

# گره‌هایی که از بیرون فایل صدا زده می‌شوند: `TalkInteractable.handoverNode`.
# این‌ها عمداً هیچ پرشی به‌شان نیست و نباید «بی‌راه» شمرده شوند.
EXTERNAL = set(["handover", "handover_broken"])


def split_target(text):
    index = text.rfind(ARROW)
    return text[:index].strip(), text[index + 2:].strip().split(" ")[0]


def check_file(path, problems):
    name = os.path.basename(path)
    nodes = []
    links = []
    issues = []

    # نگهبان باز روی انتخاب‌ها: انتخاب زیر [if] تا وقتی [else] نیامده پنهان
    # می‌شود. اگر همه‌ی انتخاب‌های یک گره همین‌طور باشند، بازیکن هیچ‌وقت
    # نمی‌بیندشان و دیالوگ بی‌صدا تمام می‌شود — همان باگ بی‌صدایی که ابزار
    # برای گرفتنش نوشته شد.
    guarded_options = {}
    plain_options = {}
    open_guard = False
    current_node = None

    with io.open(path, encoding="utf-8") as handle:
        for number, raw in enumerate(handle, 1):
            line = raw.strip()

            if not line or line.startswith("#"):
                continue

            match = re.match(r"^\[([a-z]+)\]\s*(.*)$", line)

            if not match:
                issues.append((number, "خط بدون تگ: " + line))
                continue

            tag, rest = match.group(1), match.group(2).strip()

            if tag not in TAGS:
                issues.append((number, "تگ ناشناخته [" + tag + "]"))
                continue

            if tag == "node":
                if not rest:
                    issues.append((number, "[node] بی‌نام"))
                elif rest in nodes:
                    issues.append((number, "گره‌ی تکراری: " + rest))
                else:
                    nodes.append(rest)
                    current_node = rest
                    open_guard = False
                    guarded_options[rest] = 0
                    plain_options[rest] = 0

            elif tag == "say":
                if rest.count("|") < 2:
                    issues.append((number, "say بدون سه بخش: " + rest))

            elif tag == "else":
                open_guard = False

            elif tag == "opt":
                if current_node is not None:
                    if open_guard:
                        guarded_options[current_node] += 1
                    else:
                        plain_options[current_node] += 1

                if ARROW not in rest:
                    issues.append((number, "opt بدون " + ARROW))
                    continue

                label, target = split_target(rest)
                if label.count("|") < 1:
                    issues.append((number, "opt بدون ترجمه: " + label))
                if target:
                    links.append((number, target))
                else:
                    issues.append((number, "opt با مقصد خالی"))

            elif tag == "if":
                # دو شکل مجاز است: نگهبان `[if] شرط` و پرش `[if] شرط -> مقصد`.
                if ARROW not in rest:
                    if not rest:
                        issues.append((number, "if بدون شرط"))
                    else:
                        open_guard = True
                    continue

                condition, target = split_target(rest)
                if not condition:
                    issues.append((number, "if بدون شرط"))
                if target:
                    links.append((number, target))
                else:
                    issues.append((number, "if با مقصد خالی"))

            elif tag == "go":
                if rest:
                    links.append((number, rest))
                else:
                    issues.append((number, "go بدون مقصد"))

            elif tag == "do":
                if "=" not in rest and not re.match(r"^[A-Za-z0-9_.]+\s*[+-]\s*\S+$", rest):
                    issues.append((number, "do بدون عملگر: " + rest))

    for node in nodes:
        if guarded_options.get(node) and not plain_options.get(node):
            problems.append((name, 0,
                             "همه‌ی انتخاب‌های گره «" + node +
                             "» داخل نگهبان‌اند؛ ممکن است هیچ‌وقت دیده نشوند"))

    for number, message in issues:
        problems.append((name, number, message))

    if not nodes:
        problems.append((name, 0, "هیچ گره‌ای ندارد"))
        return 0

    for number, target in links:
        if target not in nodes:
            problems.append((name, number, "پرش به گره‌ی نبوده: " + target))

    # گرهای که از گره‌ی اول هیچ مسیری به آن نیست
    reachable = set([nodes[0]])
    queue = [nodes[0]]
    edges = {}

    with io.open(path, encoding="utf-8") as handle:
        current = None

        for raw in handle:
            line = raw.strip()
            match = re.match(r"^\[([a-z]+)\]\s*(.*)$", line)

            if not match:
                continue

            tag, rest = match.group(1), match.group(2).strip()

            if tag == "node":
                current = rest
                edges.setdefault(current, set())

            elif current and tag in ("opt", "if") and ARROW in rest:
                edges[current].add(split_target(rest)[1])

            elif current and tag == "go" and rest:
                edges[current].add(rest)

    while queue:
        node = queue.pop()

        for target in edges.get(node, ()):
            if target in reachable:
                continue
            reachable.add(target)
            queue.append(target)

    for node in nodes:
        if node in reachable or node in EXTERNAL:
            continue

        problems.append((name, 0, "گره‌ی بی‌راه: " + node))

    return len(nodes)


def main():
    problems = []
    files = sorted(glob.glob(FOLDER + "/*.txt"))
    total = 0
    nodes = 0

    for path in files:
        nodes += check_file(path, problems)

    print("دیالوگ: %d فایل، %d گره" % (len(files), nodes))

    if problems:
        for name, number, message in problems:
            print("  ✗ %s:%d  %s" % (name, number, message))
        print("  problems: %d" % len(problems))
        return 1

    print("  problems: 0")
    return 0


if __name__ == "__main__":
    sys.exit(main())
