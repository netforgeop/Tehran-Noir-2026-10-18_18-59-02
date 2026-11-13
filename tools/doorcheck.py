#!/usr/bin/env python3
"""بررسی درهای بین صحنه‌ها.

`SceneManager.LoadScene("Hilton814")` اگر نام غلط باشد یا صحنه در Build
Settings نباشد، هیچ خطای واضحی نمی‌دهد؛ فقط بازیکن روی همان صحنه گیر می‌کند.
این ابزار پیش از بازی می‌گوید کدام در به کجا می‌رود و کدام مقصد وجود ندارد.

چه چیزی را چک می‌کند؟
  ۱. صحنه‌ی مقصد در `Assets/_Project/Scenes/` هست.
  ۲. `entryId` هر در، در صحنه‌ی مقصد یک `SceneEntryPoint` دارد — یا اگر
     ندارد، صریحاً گزارش می‌شود (بازیکن سر جای پیش‌فرض صحنه می‌آید).
  ۳. خط‌های `effects` شکل درست دارند: `name = value` یا `name + n`.
  ۴. مسیر رفت‌وبرگشت: هر دری که به صحنه‌ی دیگر می‌رود، در آن صحنه راه برگشت
     دارد.

اجرا:  python3 tools/doorcheck.py
"""

import glob
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import scenekit as kit

SCENES = "Assets/_Project/Scenes"
DOOR = "43a4294763fcaa9343fb41e8b0c944ec"
ENTRY = "f581e2c7c009eb342181474601fc47ca"

EFFECT = re.compile(r"^[A-Za-z0-9_.]+\s*(=\s*\S+|[+-]\s*\d+(\.\d+)?)$")


def read_scene(path):
    with io.open(path, encoding="utf-8") as handle:
        return kit.split_scene(handle.read())


def value(body, field):
    match = re.search(field + r": (.*)", body)
    return match.group(1).strip() if match else ""


def effects(body):
    block = re.search(r"effects:\s*\n((?:  - .*\n)+)", body)
    if not block:
        return []
    return [line.strip()[2:].strip() for line in block.group(1).splitlines() if line.strip()]


def main():
    names = {os.path.splitext(os.path.basename(p))[0] for p in glob.glob(SCENES + "/*.unity")}
    problems = 0
    doors = {}

    for path in sorted(glob.glob(SCENES + "/*.unity")):
        scene = os.path.splitext(os.path.basename(path))[0]
        _, docs = read_scene(path)

        entries = set()

        for doc in docs:
            if doc["cls"] == "114" and ENTRY in doc["body"]:
                entries.add(value(doc["body"], "entryId"))

        for doc in docs:
            if doc["cls"] != "114" or DOOR not in doc["body"]:
                continue

            target = value(doc["body"], "targetScene")
            entry = value(doc["body"], "entryId")
            go = re.search(r"m_GameObject: \{fileID: (\d+)\}", doc["body"]).group(1)
            label = re.search(r"m_Name: (.*)", next(d["body"] for d in docs if d["id"] == go)).group(1)

            doors.setdefault(scene, []).append((label, target, entry))

            if target not in names:
                problems += 1
                print("  ✗ %s/%s: صحنه‌ی مقصد «%s» وجود ندارد" % (scene, label, target))
                continue

            for effect in effects(doc["body"]):
                if not EFFECT.match(effect):
                    problems += 1
                    print("  ✗ %s/%s: اثر بدشکل «%s»" % (scene, label, effect))

    for scene, links in sorted(doors.items()):
        for label, target, entry in links:
            _, target_docs = read_scene(os.path.join(SCENES, target + ".unity"))
            target_entries = {value(d["body"], "entryId") for d in target_docs
                              if d["cls"] == "114" and ENTRY in d["body"]}

            if entry in target_entries:
                note = "entry «%s»" % entry
            elif target_entries:
                note = "بدون entry «%s» — سر جای پیش‌فرض می‌آید" % entry
            else:
                note = "بدون entry — سر جای پیش‌فرض می‌آید"

            print("  %s → %s  (%s)" % (scene, target, note))

            if not any(other_target == scene for _, other_target, _ in doors.get(target, [])):
                problems += 1
                print("  ✗ %s/%s: راه برگشت از %s نیست" % (scene, label, target))

    print("  درها: %d  problems: %d" %
          (sum(len(v) for v in doors.values()), problems))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
