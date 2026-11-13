#!/usr/bin/env python3
"""ابزار مشترک ساخت صحنه از روی `Sandbox.unity`.

چرا مشترک شد؟ چای‌خانه و هتل هر دو همان کار را می‌کنند: یک صحنه‌ی مبدأ را
باز می‌کنند، چند آبجکت را برمی‌دارند، جعبه و کپسول و کاغذ و نور اضافه
می‌کنند، و یک فایل YAML می‌نویسند. منطقِ کپی‌کردن شناسه‌ها اگر دو جا باشد،
دو جا هم باگ می‌گیرد. این فایل تنها جایی است که به شناسه‌های عددی دست می‌زند.

قاعده‌ی طلایی: هر شناسه‌ی تازه از یک شمارنده می‌آید و هر ارجاع داخلی با
همان نقشه بازنویسی می‌شود؛ هیچ شناسه‌ای حدس زده نمی‌شود.
"""

import hashlib
import io
import os
import re

WARM = "{r: 1, g: 0.82, b: 0.6, a: 1}"


def guid_of(path):
    """همان قاعده‌ی قطعی برای همه‌ی فایل‌های تازه: هش مسیر."""
    return hashlib.md5(("tehran-noir:" + path).encode("utf-8")).hexdigest()


def yaml_text(text):
    """رشته را مثل خود Unity به کد یونیکد تبدیل می‌کند تا فایل صحنه ASCII بماند."""
    out = []

    for char in text:
        if ord(char) < 128:
            out.append(char)
        else:
            out.append("\\u%04X" % ord(char))

    return "".join(out)


# ---------------------------------------------------------------- خواندن صحنه

def split_scene(text):
    pattern = re.compile(r"^--- !u!(\d+) &(-?\d+)\n", re.M)
    marks = list(pattern.finditer(text))
    header = text[: marks[0].start()] if marks else text
    docs = []

    for index, mark in enumerate(marks):
        end = marks[index + 1].start() if index + 1 < len(marks) else len(text)
        docs.append({
            "cls": mark.group(1),
            "id": mark.group(2),
            "body": text[mark.end():end],
        })

    return header, docs


def go_components(docs_by_id, go_id):
    body = docs_by_id[go_id]["body"]
    return re.findall(r"- component: \{fileID: (-?\d+)\}", body)


def collect_subtree(docs, root_go):
    """همه‌ی شناسه‌های یک GameObject و بچه‌هایش."""
    by_id = {doc["id"]: doc for doc in docs}
    children = {}

    for doc in docs:
        if doc["cls"] != "4":
            continue
        owner = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", doc["body"])
        father = re.search(r"m_Father: \{fileID: (-?\d+)\}", doc["body"])
        if not owner:
            continue
        father_id = father.group(1) if father else "0"
        children.setdefault(father_id, []).append(doc["id"])

    drop = set()
    stack = [root_go]

    while stack:
        go = stack.pop()
        if go in drop:
            continue
        drop.add(go)

        for component in go_components(by_id, go):
            drop.add(component)

            if by_id[component]["cls"] == "4":
                for child in children.get(component, []):
                    owner = re.search(r"m_GameObject: \{fileID: (-?\d+)\}",
                                      by_id[child]["body"])
                    if owner:
                        stack.append(owner.group(1))

    return drop


# ---------------------------------------------------------------- ساخت اجزا

class Builder:
    def __init__(self, docs, templates, wall, light, next_id=700000000):
        self.docs = docs
        self.templates = templates
        self.by_id = templates
        self.wall = wall
        self.light_source = light
        self.next_id = next_id

    def take(self, count):
        start = self.next_id
        self.next_id += 10 * count
        return [str(start + 10 * index) for index in range(count)]

    def add(self, cls, new_id, body):
        self.docs.append({"cls": cls, "id": new_id, "body": body})

    def remap(self, body, mapping):
        def swap(match):
            old = match.group(1)
            return "{fileID: " + mapping.get(old, old) + "}"

        return re.sub(r"\{fileID: (-?\d+)\}", swap, body)

    def clone(self, source_go, name, position, scale, material, parent, rotation=None):
        """یک GameObject کامل را با شناسه‌های تازه کپی می‌کند."""
        components = go_components(self.by_id, source_go)
        ids = self.take(len(components) + 1)
        new_go = ids[0]
        mapping = {source_go: new_go}

        for old, new in zip(components, ids[1:]):
            mapping[old] = new

        go_body = self.remap(self.by_id[source_go]["body"], mapping)
        go_body = re.sub(r"m_Name: .*", "m_Name: " + name, go_body, count=1)
        self.add("1", new_go, go_body)

        transform_id = None

        for old, new in zip(components, ids[1:]):
            doc = self.by_id[old]
            body = self.remap(doc["body"], mapping)

            if doc["cls"] == "4":
                body = self.set_transform(body, position, scale, parent, rotation)
                transform_id = new
            elif doc["cls"] == "23" and material:
                body = re.sub(r"m_Materials:\n  - \{fileID: \d+, guid: [0-9a-f]{32}",
                              "m_Materials:\n  - {fileID: 2100000, guid: " + material,
                              body, count=1)

            self.add(doc["cls"], new, body)

        return new_go, transform_id

    @staticmethod
    def set_transform(body, position, scale, parent, rotation):
        body = re.sub(r"m_LocalPosition: \{[^}]*\}",
                      "m_LocalPosition: {x: %s, y: %s, z: %s}" % position, body, count=1)
        body = re.sub(r"m_LocalScale: \{[^}]*\}",
                      "m_LocalScale: {x: %s, y: %s, z: %s}" % scale, body, count=1)
        body = re.sub(r"m_Father: \{fileID: -?\d+\}",
                      "m_Father: {fileID: " + parent + "}", body, count=1)

        if rotation:
            body = re.sub(r"m_LocalRotation: \{[^}]*\}",
                          "m_LocalRotation: {x: 0, y: %s, z: 0, w: %s}" % rotation,
                          body, count=1)

        return body

    def transform_id(self, go_or_transform):
        """شناسه‌ی Transform یک GameObject، یا خودش اگر از قبل Transform باشد."""
        # اول در اسنادِ زنده: آبجکت‌هایی که همین حالا ساخته‌ایم در قالب نیستند.
        for candidate in self.docs:
            if candidate["id"] == go_or_transform and candidate["cls"] == "4":
                return go_or_transform

        doc = self.by_id.get(go_or_transform)

        if doc is not None and doc["cls"] == "4":
            return go_or_transform

        for candidate in self.docs:
            if candidate["cls"] != "4":
                continue
            if re.search(r"m_GameObject: \{fileID: " + go_or_transform + r"\}",
                         candidate["body"]):
                return candidate["id"]

        return "0"

    def box(self, name, position, scale, material, parent, rotation=None):
        return self.clone(self.wall, name, position, scale, material, parent, rotation)

    def light(self, name, position, intensity, light_range, parent):
        go, transform = self.clone(self.light_source, name, position, (1, 1, 1), None, parent)

        for doc in self.docs:
            if doc["cls"] == "108" and re.search(
                    r"m_GameObject: \{fileID: " + go + r"\}", doc["body"]):
                doc["body"] = re.sub(r"m_Type: \d+", "m_Type: 2", doc["body"], count=1)
                doc["body"] = re.sub(r"m_Color: \{[^}]*\}",
                                     "m_Color: " + WARM, doc["body"], count=1)
                doc["body"] = re.sub(r"m_Intensity: [0-9.]+",
                                     "m_Intensity: " + str(intensity), doc["body"], count=1)
                doc["body"] = re.sub(r"m_Range: [0-9.]+",
                                     "m_Range: " + str(light_range), doc["body"], count=1)
                doc["body"] = re.sub(r"m_Shadows:\n    m_Type: \d+",
                                     "m_Shadows:\n    m_Type: 0", doc["body"], count=1)

        return go, transform


def rebuild_children(docs):
    """m_Children هر Transform را از روی m_Father دوباره می‌سازد."""
    children = {}

    for doc in docs:
        if doc["cls"] != "4":
            continue
        father = re.search(r"m_Father: \{fileID: (-?\d+)\}", doc["body"])
        if father:
            children.setdefault(father.group(1), []).append(doc["id"])

    for doc in docs:
        if doc["cls"] != "4":
            continue
        if children.get(doc["id"]):
            block = "m_Children:\n" + "".join(
                "  - {fileID: " + child + "}\n" for child in children[doc["id"]])
        else:
            block = "m_Children: []\n"
        doc["body"] = re.sub(r"m_Children:.*\n(  - \{fileID: -?\d+\}\n)*", block,
                             doc["body"], count=1)


# ---------------------------------------------------------------- تغییر صحنه

def set_environment(docs, sky_color, intensity=1.0, fog=False):
    """نور محیط صحنه را از «آسمان» به «رنگ ثابت» عوض می‌کند.

    چرا؟ صحنه‌های داخلی ما سقف ندارند و پنجره‌شان هم واقعی نیست. با ambient
    از آسمان، شبِ بیرون یعنی اتاقِ سیاه: هرچه نور نقطه‌ای اضافه کنی، گوشه‌ها
    باز هم می‌میرند. رنگ ثابت، کف و دیوار را می‌خواند و نورهای نقطه‌ای کارِ
    سایه و گرمی را می‌کنند.
    """
    for doc in docs:
        if doc["cls"] != "104":
            continue

        body = doc["body"]
        body = re.sub(r"m_AmbientMode: \d+", "m_AmbientMode: 3", body, count=1)
        body = re.sub(r"m_AmbientSkyColor: \{[^}]*\}",
                      "m_AmbientSkyColor: " + sky_color, body, count=1)
        body = re.sub(r"m_AmbientIntensity: [0-9.]+",
                      "m_AmbientIntensity: " + str(intensity), body, count=1)

        if fog:
            body = re.sub(r"m_Fog: \d+", "m_Fog: 1", body, count=1)
            body = re.sub(r"m_FogDensity: [0-9.]+", "m_FogDensity: 0.012", body, count=1)
            body = re.sub(r"m_FogColor: \{[^}]*\}",
                          "m_FogColor: " + sky_color, body, count=1)

        doc["body"] = body


SCENE_DOOR_GUID = "43a4294763fcaa9343fb41e8b0c944ec"
SCENE_ENTRY_GUID = "f581e2c7c009eb342181474601fc47ca"

CUTAWAY_SCRIPT_GUID = "716b6c9cd7df75ec7018eb366bc368f8"
CUTAWAY_PIECE_GUID = "7bf126e49e49cec06616925990812a03"


def empty(builder, name, parent):
    """یک GameObject خالی، به‌عنوان ریشه‌ی یک گروه (مثلاً یک طبقه)."""
    go, transform = builder.take(2)

    builder.add("1", go,
                "GameObject:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  serializedVersion: 6\n"
                "  m_Component:\n"
                "  - component: {fileID: " + transform + "}\n"
                "  m_Layer: 0\n"
                "  m_Name: " + name + "\n"
                "  m_TagString: Untagged\n"
                "  m_Icon: {fileID: 0}\n"
                "  m_NavMeshLayer: 0\n"
                "  m_StaticEditorFlags: 0\n"
                "  m_IsActive: 1\n\n")

    builder.add("4", transform, transform_body(go, builder.transform_id(parent)))

    return go, transform


def transform_body(go, parent, position=(0, 0, 0)):
    return (
        "Transform:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: " + go + "}\n"
        "  serializedVersion: 2\n"
        "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
        "  m_LocalPosition: {x: %s, y: %s, z: %s}\n" % position +
        "  m_LocalScale: {x: 1, y: 1, z: 1}\n"
        "  m_ConstrainProportionsScale: 0\n"
        "  m_Children: []\n"
        "  m_Father: {fileID: " + parent + "}\n"
        "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n\n")


def attach_component(builder, go, script_guid, class_identifier, fields):
    """یک MonoBehaviour به GameObject موجود اضافه می‌کند."""
    (mono,) = builder.take(1)

    builder.add("114", mono,
                "MonoBehaviour:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: " + go + "}\n"
                "  m_Enabled: 1\n"
                "  m_EditorHideFlags: 0\n"
                "  m_Script: {fileID: 11500000, guid: " + script_guid + ", type: 3}\n"
                "  m_Name: \n"
                "  m_EditorClassIdentifier: Assembly-CSharp::" + class_identifier + "\n"
                + fields + "\n")

    for doc in builder.docs:
        if doc["cls"] == "1" and doc["id"] == go:
            doc["body"] = doc["body"].replace(
                "  m_Layer: 0",
                "  - component: {fileID: " + mono + "}\n  m_Layer: 0", 1)
            break

    return mono


def scene_door(builder, cube, root, name, position, scale, material, display_name,
                subtype, target_scene, entry_id, effects, fade_seconds=0.45,
                rotation=None):
    """در یا آسانسوری که صحنه را عوض می‌کند.

    کپیِ یک مکعب با `BoxCollider`، بعد کامپوننتش با `SceneDoor` عوض می‌شود.
    مکعب نامرئی است: خودِ درها و چارچوب جدا ساخته می‌شوند.
    """
    go, _ = builder.clone(cube, name, position, scale, material, root, rotation)

    # خودِ ناحیه‌ی تعامل نامرئی است؛ درها و چارچوب جدا ساخته می‌شوند. اگر
    # رندرر روشن بماند، یک مکعب برنجی روی آسانسور می‌نشیند.
    for doc in builder.docs:
        if doc["cls"] == "23" and re.search(r"m_GameObject: \{fileID: " + go + r"\}",
                                            doc["body"]):
            doc["body"] = re.sub(r"m_Enabled: 1", "m_Enabled: 0", doc["body"], count=1)

    for doc in builder.docs:
        if doc["cls"] != "114":
            continue
        if not re.search(r"m_GameObject: \{fileID: " + go + r"\}", doc["body"]):
            continue

        lines = "".join("  - " + effect + "\n" for effect in effects)

        doc["body"] = (
            "MonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n"
            "  m_GameObject: {fileID: " + go + "}\n"
            "  m_Enabled: 1\n"
            "  m_EditorHideFlags: 0\n"
            "  m_Script: {fileID: 11500000, guid: " + SCENE_DOOR_GUID + ", type: 3}\n"
            "  m_Name: \n"
            "  m_EditorClassIdentifier: Assembly-CSharp::TehranNoir.World.SceneDoor\n"
            "  action: 0\n"
            "  interactionPoint: {fileID: 0}\n"
            "  maxDistance: 2.6\n"
            "  maxAngle: 360\n"
            '  displayName: "' + yaml_text(display_name) + '"\n'
            '  subtype: "' + yaml_text(subtype) + '"\n'
            "  outline: {fileID: 0}\n"
            "  targetScene: " + target_scene + "\n"
            "  entryId: " + entry_id + "\n"
            "  effects: " + ("\n" + lines.rstrip("\n") if effects else "[]") + "\n"
            "  fadeSeconds: " + str(fade_seconds) + "\n"
            "  logDeparture: 1\n\n")

    return go


def scene_entry(builder, player_go, entry_id, point_transform, log_arrival=True):
    """روی خودِ Player: اگر از این در آمده‌ای، همان‌جا پیاده شو."""
    return attach_component(builder, player_go, SCENE_ENTRY_GUID,
                            "TehranNoir.World.SceneEntryPoint",
                            "  entryId: " + entry_id + "\n"
                            "  entryPoint: {fileID: " + point_transform + "}\n"
                            "  lookAt: {fileID: 0}\n"
                            "  logArrival: %d" % (1 if log_arrival else 0))


def set_local(builder, transform, position, rotation_y=None):
    """جابه‌جا کردن یک Transform که همین حالا ساخته‌ایم."""
    for doc in builder.docs:
        if doc["cls"] != "4" or doc["id"] != transform:
            continue

        doc["body"] = re.sub(r"m_LocalPosition: \{[^}]*\}",
                             "m_LocalPosition: {x: %s, y: %s, z: %s}" % position,
                             doc["body"], count=1)

        if rotation_y is not None:
            import math
            half = math.radians(rotation_y) * 0.5
            doc["body"] = re.sub(r"m_LocalRotation: \{[^}]*\}",
                                 "m_LocalRotation: {x: 0, y: %.6f, z: 0, w: %.6f}"
                                 % (math.sin(half), math.cos(half)), doc["body"], count=1)
        break


def world_initial(docs, lines):
    """مقدارهای اولیه‌ی جهان؛ با همان نحو `[do]`.

    برای چیزی که «از اول همین است»: طبقه‌ای که در آن هستی، فصلی که باز است.
    اگر بازیکن مستقیم صحنه را Play کند، این‌ها نوشته می‌شوند؛ اگر از در
    بیاید، جهانِ مانده حرف اول را می‌زند.
    """
    for doc in docs:
        if doc["cls"] != "114":
            continue
        if "TehranNoir.Dialogue.WorldStateProvider" not in doc["body"]:
            continue

        block = "initialVariables:\n" + "".join("  - " + line + "\n" for line in lines) \
            if lines else "initialVariables: []\n"

        doc["body"] = re.sub(r"initialVariables:( \[\])?\n(  - .*\n)*", block,
                             doc["body"], count=1)


def persist_world(docs):
    """جهان بین صحنه‌ها بماند. بدون این، هر بار که از در رد شوی همه‌ی پرچم‌ها صفر می‌شوند."""
    for doc in docs:
        if doc["cls"] != "114":
            continue
        if "TehranNoir.Dialogue.WorldStateProvider" in doc["body"]:
            doc["body"] = re.sub(r"persistBetweenScenes: \d",
                                 "persistBetweenScenes: 1", doc["body"], count=1)


def cutaway_piece(builder, go, role, level):
    """تکه‌ی برش: role ۰ = طبقه، ۱ = سقف."""
    return attach_component(builder, go, CUTAWAY_PIECE_GUID,
                            "TehranNoir.World.CutawayPiece",
                            "  role: %d\n  level: %d" % (role, level))


def building_cutaway(builder, go, viewer_transform, center, size,
                     floor_height=3.0, ground_level=0.0, exit_margin=0.7,
                     log_changes=True):
    """مدیر برش، روی خودِ ریشه‌ی ساختمان.

    عمداً روی ریشه و نه روی یک آبجکت تازه: کامپوننت با
    `GetComponentsInChildren` تکه‌ها را پیدا می‌کند، و یک آبجکت تازه فرزندی
    ندارد. یک بار همین اشتباه باعث شد برش هیچ کاری نکند و هیچ خطایی هم
    ندهد.
    """
    fields = (
        "  viewer: {fileID: " + viewer_transform + "}\n"
        "  interiorCenter: {x: %s, y: %s, z: %s}\n" % center +
        "  interiorSize: {x: %s, y: %s, z: %s}\n" % size +
        "  exitMargin: %s\n" % exit_margin +
        "  floorHeight: %s\n" % floor_height +
        "  groundLevel: %s\n" % ground_level +
        "  checkInterval: 0.1\n"
        "  logChanges: %d" % (1 if log_changes else 0))

    return attach_component(builder, go, CUTAWAY_SCRIPT_GUID,
                            "TehranNoir.World.BuildingCutaway", fields)


def component_object(builder, parent, name, script_guid, class_identifier, fields):
    """یک GameObject خالی با یک MonoBehaviour و فیلدهای داده‌شده.

    `fields` رشته‌ی YAML خام است، بدون تورفتگی. برای کامپوننت‌های کوچکی مثل
    `SceneMarker` که فقط یک شناسه می‌خواهند.
    """
    go, transform, mono = builder.take(3)

    builder.add("1", go,
                "GameObject:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  serializedVersion: 6\n"
                "  m_Component:\n"
                "  - component: {fileID: " + transform + "}\n"
                "  - component: {fileID: " + mono + "}\n"
                "  m_Layer: 0\n"
                "  m_Name: " + name + "\n"
                "  m_TagString: Untagged\n"
                "  m_Icon: {fileID: 0}\n"
                "  m_NavMeshLayer: 0\n"
                "  m_StaticEditorFlags: 0\n"
                "  m_IsActive: 1\n\n")

    builder.add("4", transform,
                "Transform:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: " + go + "}\n"
                "  serializedVersion: 2\n"
                "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
                "  m_LocalPosition: {x: 0, y: 0, z: 0}\n"
                "  m_LocalScale: {x: 1, y: 1, z: 1}\n"
                "  m_ConstrainProportionsScale: 0\n"
                "  m_Children: []\n"
                "  m_Father: {fileID: " + builder.transform_id(parent) + "}\n"
                "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n\n")

    builder.add("114", mono,
                "MonoBehaviour:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: " + go + "}\n"
                "  m_Enabled: 1\n"
                "  m_EditorHideFlags: 0\n"
                "  m_Script: {fileID: 11500000, guid: " + script_guid + ", type: 3}\n"
                "  m_Name: \n"
                "  m_EditorClassIdentifier: Assembly-CSharp::" + class_identifier + "\n"
                + fields + "\n")

    return go, transform


def ensure_folder_meta(folder):
    meta = folder + ".meta"

    if os.path.exists(meta):
        return

    with io.open(meta, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("fileFormatVersion: 2\nguid: " + guid_of(folder) +
                     "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n" +
                     "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def set_clock(docs, clock_id, year, month, day, hour, minute):
    """تاریخ شروع صحنه را می‌نویسد؛ تنها جای «زمان» در یک صحنه."""
    for doc in docs:
        if doc["id"] != clock_id:
            continue

        doc["body"] = re.sub(
            r"startDate:\n    Year: \d+\n    Month: \d+\n    Day: \d+\n    Hour: \d+\n    Minute: \d+",
            "startDate:\n    Year: %d\n    Month: %d\n    Day: %d\n    Hour: %d\n    Minute: %d"
            % (year, month, day, hour, minute),
            doc["body"], count=1)


def set_player(docs, player_id, position):
    for doc in docs:
        if doc["id"] == player_id:
            body = re.sub(r"m_LocalPosition: \{[^}]*\}",
                          "m_LocalPosition: {x: %s, y: %s, z: %s}" % position,
                          doc["body"], count=1)
            body = re.sub(r"m_LocalRotation: \{[^}]*\}",
                          "m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}", body, count=1)
            doc["body"] = body


def write_materials(folder, materials):
    """هر ماده از یک ماده‌ی موجود کلون می‌شود؛ فقط نام و رنگ عوض می‌شود."""
    for name, (color, source) in materials.items():
        with io.open(source, encoding="utf-8") as handle:
            text = handle.read()

        text = re.sub(r"m_Name: .*", "m_Name: " + name, text, count=1)

        pair = tuple(("%.6f" % value).rstrip("0").rstrip(".") for value in color)
        text = re.sub(r"_BaseColor: \{[^}]*\}",
                      "_BaseColor: {r: %s, g: %s, b: %s, a: %s}" % pair, text)
        text = re.sub(r"_Color: \{[^}]*\}",
                      "_Color: {r: %s, g: %s, b: %s, a: %s}" % pair, text)

        path = folder + "/" + name + ".mat"
        os.makedirs(folder, exist_ok=True)

        with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)

        with io.open(path + ".meta", "w", encoding="utf-8", newline="\n") as handle:
            handle.write("fileFormatVersion: 2\nguid: " + guid_of(path) +
                         "\nNativeFormatImporter:\n  externalObjects: {}\n" +
                         "  mainObjectFileID: 2100000\n  userData: \n" +
                         "  assetBundleName: \n  assetBundleVariant: \n")


def write_scene(target, header, docs):
    """بچه‌ها را بازمی‌سازد، ریشه‌ها را می‌نویسد و فایل را خروجی می‌دهد."""
    rebuild_children(docs)

    roots = [doc for doc in docs if doc["cls"] in ("4", "224") and
             re.search(r"m_Father: \{fileID: 0\}", doc["body"])]

    for doc in docs:
        if doc["cls"] != "1660057539":
            continue

        block = "m_Roots:\n" + "".join(
            "  - {fileID: " + root["id"] + "}\n" for root in roots)
        doc["body"] = re.sub(r"m_Roots:\n(  - \{fileID: -?\d+\}\n)*", block,
                             doc["body"], count=1)

    # SceneRoots همیشه آخرین سند باشد؛ همان جایی که خود Unity می‌نویسد.
    docs.sort(key=lambda doc: doc["cls"] == "1660057539")

    output = header + "".join(
        "--- !u!" + doc["cls"] + " &" + doc["id"] + "\n" + doc["body"]
        for doc in docs)

    with io.open(target, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(output)

    return output
