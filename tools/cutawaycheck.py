#!/usr/bin/env python3
"""بررسی برش ساختمان در فایل صحنه، بیرون از یونیتی.

چه چیزی را می‌بیند؟
  ۱. هر `BuildingCutaway` باید روی آبجکتی باشد که همه‌ی `CutawayPiece`ها
     زیرش‌اند. اگر روی یک آبجکت خالیِ دیگر باشد، در بازی هیچ کاری نمی‌کند.
  ۲. برای نقطه‌ی شروع بازیکن، باید مشخص شود «داخل» است یا «بیرون»، و کدام
     تکه‌ها در همان لحظه پنهان می‌شوند.
  ۳. اگر بازیکن داخل باشد و سقفِ طبقه‌ی خودش پنهان نشود، هشدار می‌دهد —
     همان تخته‌ی قهوه‌ای.

این ابزار موتور نیست؛ همان دو خط قاعده را روی داده‌ی صحنه اجرا می‌کند تا
مشکلی که فقط در Play دیده می‌شود، در preflight دیده شود.

اجرا:
    python3 tools/cutawaycheck.py Assets/_Project/Scenes/Hilton.unity
    python3 tools/cutawaycheck.py Assets/_Project/Scenes/Hilton.unity --at 0,1,0
"""

import io
import math
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import scenekit as kit

CUTAWAY = "716b6c9cd7df75ec7018eb366bc368f8"
PIECE = "7bf126e49e49cec06616925990812a03"


def vec(text):
    match = re.search(r"\{x: (-?[\d.]+), y: (-?[\d.]+), z: (-?[\d.]+)\}", text)
    return tuple(float(match.group(i)) for i in (1, 2, 3)) if match else None


def inside(local, center, size, margin):
    return all(abs(local[i] - center[i]) <= size[i] * 0.5 + margin for i in range(3))


def main(path, at=None):
    with io.open(path, encoding="utf-8") as handle:
        header, docs = kit.split_scene(handle.read())

    by_id = {doc["id"]: doc for doc in docs}
    problems = 0
    managers = []

    for doc in docs:
        if doc["cls"] != "114" or CUTAWAY not in doc["body"]:
            continue

        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", doc["body"]).group(1)
        managers.append((go, doc))

    if not managers:
        print("  برشی ندارد: " + os.path.basename(path))
        return 0

    # همه‌ی تکه‌ها با این‌که زیرِ کدام آبجکت‌اند
    pieces = []
    for doc in docs:
        if doc["cls"] != "114" or PIECE not in doc["body"]:
            continue
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", doc["body"]).group(1)
        role = int(re.search(r"role: (\d+)", doc["body"]).group(1))
        level = int(re.search(r"level: (\d+)", doc["body"]).group(1))
        name = re.search(r"m_Name: (.*)", by_id[go]["body"]).group(1)
        pieces.append((go, name, role, level))

    for manager_go, doc in managers:
        viewer = re.search(r"viewer: \{fileID: (-?\d+)\}", doc["body"]).group(1)
        center = vec(re.search(r"interiorCenter: (\{[^}]*\})", doc["body"]).group(1))
        size = vec(re.search(r"interiorSize: (\{[^}]*\})", doc["body"]).group(1))
        margin = float(re.search(r"exitMargin: ([\d.]+)", doc["body"]).group(1))
        floor_height = float(re.search(r"floorHeight: ([\d.]+)", doc["body"]).group(1))
        ground = float(re.search(r"groundLevel: ([\d.]+)", doc["body"]).group(1))

        # ۱) همه‌ی تکه‌ها باید فرزندِ همین آبجکت باشند.
        manager_transform = kit.Builder({}, {}, "0", "0").transform_id(manager_go)
        manager_transform = manager_transform if manager_transform != "0" else None

        if manager_transform is None:
            for candidate in docs:
                if candidate["cls"] == "4" and re.search(
                        r"m_GameObject: \{fileID: " + manager_go + r"\}", candidate["body"]):
                    manager_transform = candidate["id"]

        orphans = []

        for piece_go, name, role, level in pieces:
            parent = None
            for candidate in docs:
                if candidate["cls"] == "4" and re.search(
                        r"m_GameObject: \{fileID: " + piece_go + r"\}", candidate["body"]):
                    parent = candidate
                    break

            ok = False
            while parent is not None:
                father = re.search(r"m_Father: \{fileID: (-?\d+)\}", parent["body"]).group(1)
                if father == "0":
                    break
                if father == manager_transform:
                    ok = True
                    break
                parent = by_id.get(father)

            if not ok:
                orphans.append(name)

        if orphans:
            problems += 1
            print("  ✗ %s: این تکه‌ها زیر آبجکت برش نیستند: %s" %
                  (os.path.basename(path), ", ".join(orphans)))
        else:
            print("  ✓ %s: %d تکه، همه زیر آبجکت برش" %
                  (os.path.basename(path), len(pieces)))

        # ۲) نقطه‌ی بازیکن
        viewer_doc = by_id.get(viewer)

        if viewer_doc is None:
            print("  ! بیننده (viewer) در صحنه نیست")
            continue

        position = at or vec(
            re.search(r"m_LocalPosition: (\{[^}]*\})", viewer_doc["body"]).group(1))
        parent = re.search(r"m_Father: \{fileID: (-?\d+)\}", viewer_doc["body"]).group(1)

        if parent != "0":
            # بازیکن فرزندِ ریشه است؛ مکان جهانی = مجموع مکان‌های محلی
            position = tuple(position[i] + vec(
                re.search(r"m_LocalPosition: (\{[^}]*\})", by_id[parent]["body"]).group(1))[i]
                for i in range(3))

        inside_now = inside(position, center, size, margin)
        level = int(math.floor((position[1] - ground) / max(0.1, floor_height)))

        print("  شروع بازیکن: %s → %s، طبقه‌ی %d" %
              (tuple(round(v, 2) for v in position),
               "داخل" if inside_now else "بیرون", level))

        if not inside_now:
            print("  ✓ بیرون: همه‌ی تکه‌ها دیده می‌شوند")
            continue

        hidden = [name for _, name, role, lvl in pieces
                  if not (lvl > level if role == 1 else lvl <= level)]

        if not hidden:
            print("  ✓ هیچ تکه‌ای لازم نیست پنهان شود")

        for name in hidden:
            print("    پنهان می‌شود: " + name)

        ceilings = [name for _, name, role, lvl in pieces if role == 1 and lvl == level]

        if ceilings and not any(name in hidden for name in ceilings):
            problems += 1
            print("  ✗ سقفِ طبقه‌ی %d پنهان نمی‌شود — همان تخته‌ی قهوه‌ای" % level)

    return 1 if problems else 0


if __name__ == "__main__":
    argv = sys.argv[1:]

    if not argv:
        print(__doc__)
        sys.exit(1)

    at = None

    if "--at" in argv:
        index = argv.index("--at")
        at = tuple(float(part) for part in argv[index + 1].split(","))
        del argv[index:index + 2]

    status = 0

    for scene in argv:
        status |= main(scene, at)

    print("  problems: %d" % (1 if status else 0))
    sys.exit(status)
