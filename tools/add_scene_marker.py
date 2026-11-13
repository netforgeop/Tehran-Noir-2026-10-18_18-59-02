#!/usr/bin/env python3
"""گذاشتن `SceneMarker` در یک صحنه‌ی موجود.

چرا ابزار؟ چون افزودن یک آبجکت به فایل YAML با دست یعنی دست‌زدن به شناسه‌ها،
و قاعده‌ی پروژه این است که فقط `scenekit` شناسه بسازد.

هر صحنه یک کلید می‌گیرد: `place.<id>`. بعد از این، هر کاری که فقط در یک
مکان معنی دارد با همان کلید شرط‌گذاری می‌شود؛ وگرنه کارِ «اولین روز» در
هتل هم در گوشه‌ی صفحه می‌آید.

اجرا:
    python3 tools/add_scene_marker.py Assets/_Project/Scenes/Sandbox.unity office
    python3 tools/add_scene_marker.py Assets/_Project/Scenes/Teahouse.unity teahouse
    python3 tools/add_scene_marker.py Assets/_Project/Scenes/Hilton.unity hotel
"""

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import scenekit as kit

MARKER_GUID = "7d32222d12ba24edcd0410ce3e475a72"


def has_marker(docs, place):
    for doc in docs:
        if doc["cls"] == "114" and ("placeId: " + place) in doc["body"]:
            return True
    return False


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 1

    path = sys.argv[1]
    place = sys.argv[2]

    with io.open(path, encoding="utf-8") as handle:
        header, docs = kit.split_scene(handle.read())

    if has_marker(docs, place):
        print("از قبل هست: " + path + " → place." + place)
        return 0

    builder = kit.Builder(docs, {doc["id"]: doc for doc in docs},
                          "0", "0", next_id=max(int(doc["id"]) for doc in docs) + 1000)

    kit.component_object(
        builder, "0", "SceneMarker_" + place, MARKER_GUID,
        "TehranNoir.World.SceneMarker",
        "  placeId: " + place + "\n  logMarker: 1")

    kit.write_scene(path, header, builder.docs)

    print("گذاشته شد: " + path + " → place." + place)
    return 0


if __name__ == "__main__":
    sys.exit(main())
