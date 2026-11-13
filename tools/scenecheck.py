#!/usr/bin/env python3
"""بررسی سلامتی فایل صحنه، بدون باز کردن Unity.

چرا لازم است؟ صحنه یک فایل YAML با شناسه‌های عددی است. اگر یک `fileID`
اشتباه باشد، Unity صحنه را باز می‌کند ولی یک کامپوننت «Missing» می‌شود یا
آبجکت از سلسله‌مراتب می‌افتد — و هیچ خطای کامپایلی هم نمی‌دهد. این ابزار
همان خطاهای بی‌صدا را می‌گیرد:

  ۱) شناسه‌ی تکراری یا ارجاع به شناسه‌ای که در فایل نیست.
  ۲) ارجاع به فایلی که guid آن در پروژه نیست (مثل دیالوگ یا سند جاافتاده).
  ۳) GameObject بدون Transform، یا Transform با پدر/فرزند ناهمخوان.
  ۴) SceneRoots که با واقعیت نمی‌خواند.
  ۵) ریشه‌ی سلسله‌مراتب آبجکت‌ها و کامپوننت‌ها که دوطرفه نیست.

اجرا:  python3 tools/scenecheck.py [مسیر صحنه]
"""

import glob
import io
import os
import re
import sys

DEFAULT = "Assets/_Project/Scenes/Teahouse.unity"
BUILTIN = "0000000000000000e000000000000000"

DOC_RE = re.compile(r"^--- !u!(\d+) &(-?\d+)\n", re.M)
REF_RE = re.compile(r"\{fileID: (-?\d+)(, guid: ([0-9a-f]{32}), type: \d+)?\}")
GO_RE = re.compile(r"m_GameObject: \{fileID: (-?\d+)\}")
COMPONENT_RE = re.compile(r"- component: \{fileID: (-?\d+)\}")
FATHER_RE = re.compile(r"m_Father: \{fileID: (-?\d+)\}")
CHILDREN_RE = re.compile(r"- \{fileID: (-?\d+)\}")


def load_guids():
    """همه‌ی guidهای شناخته‌شده.

    دو منبع دارد: متای فایل‌های خود پروژه، و هر guidی که در صحنه‌ی سالم
    دیگری استفاده شده است. دومی برای اسکریپت‌های پکیج است (URP،
    TextMeshPro، Cinemachine) که متای‌شان داخل Assets نیست ولی معتبرند.
    """
    known = set([BUILTIN])

    for meta in glob.glob("Assets/**/*.meta", recursive=True):
        with io.open(meta, encoding="utf-8", errors="ignore") as handle:
            match = re.search(r"guid: ([0-9a-f]{32})", handle.read())

        if match:
            known.add(match.group(1))

    for scene in glob.glob("Assets/**/Scenes/*.unity", recursive=True):
        with io.open(scene, encoding="utf-8", errors="ignore") as handle:
            known |= set(re.findall(r"guid: ([0-9a-f]{32})", handle.read()))

    return known


def split_scene(text):
    marks = list(DOC_RE.finditer(text))
    docs = []

    for index, mark in enumerate(marks):
        end = marks[index + 1].start() if index + 1 < len(marks) else len(text)
        docs.append({
            "cls": mark.group(1),
            "id": mark.group(2),
            "body": text[mark.end():end],
        })

    return docs


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT
    problems = []

    if not os.path.exists(path):
        print("صحنه پیدا نشد: " + path)
        return 1

    with io.open(path, encoding="utf-8") as handle:
        text = handle.read()

    # Unity serializes scene IDs as signed 64-bit integers, including references.
    for match in re.finditer(r"(?m)^--- !u!\d+ &(-?\d+)|\{fileID: (-?\d+)", text):
        value = match.group(1) or match.group(2)
        if not -(1 << 63) <= int(value) <= (1 << 63) - 1:
            problems.append("fileID outside signed 64-bit range: " + value)

    docs = split_scene(text)
    by_id = {}

    for doc in docs:
        if doc["id"] in by_id:
            problems.append("شناسه‌ی تکراری: " + doc["id"])
        by_id[doc["id"]] = doc

    known_guids = load_guids()
    refs = 0

    for doc in docs:
        for match in REF_RE.finditer(doc["body"]):
            target = match.group(1)
            guid = match.group(3)

            if guid:
                refs += 1
                if guid not in known_guids:
                    problems.append("ارجاع به فایل ناشناس در " + doc["id"] + ": " + guid)
                continue

            if target == "0":
                continue

            refs += 1
            if target not in by_id:
                problems.append("ارجاع به شناسه‌ی نبوده در " + doc["id"] + ": " + target)

    # نگاشت دوطرفه‌ی GameObject و کامپوننت
    gameobjects = {doc["id"]: doc for doc in docs if doc["cls"] == "1"}
    transforms = {doc["id"]: doc for doc in docs
                  if doc["cls"] in ("4", "224")}
    owned = {}

    for doc in docs:
        match = GO_RE.search(doc["body"])
        if not match or doc["cls"] == "1":
            continue
        owned.setdefault(match.group(1), []).append(doc["id"])

    for go_id, doc in gameobjects.items():
        listed = COMPONENT_RE.findall(doc["body"])

        if not listed:
            problems.append("GameObject بدون کامپوننت: " + go_id)
            continue

        for component in listed:
            if component not in by_id:
                problems.append("کامپوننت ناموجود در " + go_id + ": " + component)

        actual = set(owned.get(go_id, []))

        if set(listed) != actual:
            problems.append("فهرست کامپوننت‌های " + go_id + " با واقعیت نمی‌خواند")

        if not any(by_id.get(component, {}).get("cls") in ("4", "224")
                   for component in listed):
            problems.append("GameObject بدون Transform: " + go_id)

    for go_id in owned:
        if go_id not in gameobjects:
            problems.append("کامپوننتِ بدون GameObject: " + go_id)

    # پدر و فرزند
    children = {}

    for doc in transforms.values():
        father = FATHER_RE.search(doc["body"])
        if not father:
            problems.append("Transform بدون m_Father: " + doc["id"])
            continue

        if father.group(1) != "0":
            if father.group(1) not in transforms:
                problems.append("پدر نبوده برای " + doc["id"] + ": " + father.group(1))
            children.setdefault(father.group(1), []).append(doc["id"])

    for transform_id, doc in transforms.items():
        declared = set(CHILDREN_RE.findall(doc["body"].split("m_Father:")[0]))
        actual = set(children.get(transform_id, []))

        if declared != actual:
            problems.append("فرزندان " + transform_id + " با m_Father نمی‌خواند")

    # SceneRoots
    roots = set()

    for transform_id, doc in transforms.items():
        father = FATHER_RE.search(doc["body"])
        if father and father.group(1) == "0":
            roots.add(transform_id)

    for doc in docs:
        if doc["cls"] != "1660057539":
            continue

        listed = set(CHILDREN_RE.findall(doc["body"].split("m_Roots:")[1]))

        if listed != roots:
            missing = sorted(roots - listed)
            extra = sorted(listed - roots)
            if missing:
                problems.append("ریشه‌های جاافتاده در SceneRoots: " + ", ".join(missing))
            if extra:
                problems.append("ریشه‌های اضافه در SceneRoots: " + ", ".join(extra))

    print("صحنه: " + path)
    print("  سند: %d، آبجکت: %d، ارجاع: %d" % (len(docs), len(gameobjects), refs))

    if problems:
        for problem in problems:
            print("  ✗ " + problem)
        print("  problems: %d" % len(problems))
        return 1

    print("  problems: 0")
    return 0


if __name__ == "__main__":
    sys.exit(main())
