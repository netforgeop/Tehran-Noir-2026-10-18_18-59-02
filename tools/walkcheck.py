#!/usr/bin/env python3
"""شبیه‌ساز مسیر — آیا گره‌های یک فایل دیالوگ از `start` در دسترس‌اند؟

این ابزار موتور نیست؛ فقط نمودار را می‌بیند. برای هر فایل:
  ۱) هر گره باید از `start` (یا از یک گره‌ی ورودی) قابل رسیدن باشد؛
  ۲) هر گره باید راهی به `[end]` یا به گره‌ی دیگر داشته باشد؛
  ۳) گره‌های بی‌مصرف (بدون پرش ورودی) گزارش می‌شوند.

اجرا:  python3 tools/walkcheck.py
"""

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

DIALOGUE = "Assets/_Project/Dialogue"


def parse(path):
    nodes = {}
    current = None

    for raw in io.open(path, encoding="utf-8").read().splitlines():
        line = raw.strip()
        if not line or line[0] == "#":
            continue

        if line.startswith("[node]"):
            current = line[6:].strip()
            nodes[current] = []
            continue

        if current is None:
            continue

        if line.startswith("[go]"):
            nodes[current].append(line[4:].strip().rstrip("]"))
            continue

        match = re.match(r"\[(opt|if)\](.*?)->\s*([A-Za-z0-9_.]+)\s*\]?$", line)
        if match:
            nodes[current].append(match.group(3))
            continue

        if line.startswith("[end]"):
            nodes[current].append("<end>")

    return nodes


def main():
    problems = 0
    files = sorted(name for name in os.listdir(DIALOGUE) if name.endswith(".txt"))

    for name in files:
        nodes = parse(os.path.join(DIALOGUE, name))

        if not nodes:
            continue

        entry = "start" if "start" in nodes else next(iter(nodes))

        seen = set()
        stack = [entry]

        while stack:
            node = stack.pop()
            if node in seen or node not in nodes:
                continue
            seen.add(node)
            stack.extend(target for target in nodes[node] if target != "<end>")

        dead = [node for node in nodes if node not in seen]
        if dead:
            print("  ? %s: گره‌ی بدون ورودی %s" % (name, ", ".join(sorted(dead))))

        for node, targets in nodes.items():
            if not targets:
                print("  ! %s: گره‌ی %s هیچ پایانی ندارد" % (name, node))
                problems += 1

        if "<end>" not in set(sum(nodes.values(), [])):
            print("  ! %s: هیچ گره‌ای به [end] نمی‌رسد" % name)
            problems += 1

    print("فایل‌ها: %d  problems: %d" % (len(files), problems))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
