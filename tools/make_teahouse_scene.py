#!/usr/bin/env python3
"""ساخت صحنه‌ی چای‌خانه از روی صحنه‌ی Sandbox.

چرا اسکریپت و نه دست؟ چون صحنه یک فایل YAML با شناسه‌های عددی است؛ اگر
آبجکت‌ها را دستی اضافه کنیم، هر اشتباه در یک `fileID` فقط موقع باز کردن
صحنه در Unity دیده می‌شود. این ابزار همان کار را تکرارپذیر انجام می‌دهد و
`tools/scenecheck.py` بعدش همه‌ی ارجاع‌ها را چک می‌کند.

چه چیزی از Sandbox می‌ماند؟
  بازیکن، دوربین و ریگ Cinemachine، HUD، منوی توقف، EventSystem،
  DialogueSystem و World (ساعت و خورشید).
چه چیزی برداشته می‌شود؟
  دفتر (Office و بچه‌هایش)، رضا و پرونده‌ی روی میز.

اجرا:  python3 tools/make_teahouse_scene.py
"""

import hashlib
import io
import os
import re

SOURCE = "Assets/_Project/Scenes/Sandbox.unity"
TARGET = "Assets/_Project/Scenes/Teahouse.unity"

# شناسه‌های صحنه‌ی مبدأ
OFFICE = "1184640865"          # دفتر و همه‌ی بچه‌هایش
REZA = "978304783"             # NPC دفتر (فقط به‌عنوان الگوی کپسول)
CUBE = "1537828487"            # پرونده‌ی روی میز (الگوی کاغذ)
WALL = "1124334549"            # یک دیوار، الگوی جعبه
PLAYER = "1182697546"
CLOCK_COMPONENT = "445466633"  # WorldClock
LIGHT = "1107200289"           # Directional Light (الگوی نور)

DIALOGUE_GUID = "c1a53b8b2b5f4ec9a8a7a3f8c0d61e42"
CHIT_GUID = "7d2f0c1e6b8a4f3d9e5c2b1a0f8d7e63"
DOCUMENT_SCRIPT_GUID = "4f8b2d1c9a7e4b3d8c6f0a2e5b9d7c14"

WARM = "{r: 1, g: 0.82, b: 0.6, a: 1}"

# ماده‌ها: نام → (رنگ، الگوی مبدأ)
MATERIALS = {
    "M_TeaWall": ((0.62, 0.55, 0.45, 1.0), "Assets/_Project/Art/Office/M_Wall.mat"),
    "M_TeaFloor": ((0.28, 0.20, 0.15, 1.0), "Assets/_Project/Art/Office/M_Floor.mat"),
    "M_TeaWood": ((0.24, 0.16, 0.10, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
    "M_TeaCloth": ((0.22, 0.27, 0.22, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
}

PAPER_GUID = "4a8603e6b0d4eed4e8778f6120261937"
NPC_GUID = "5952c87a01e696a45a3c76677c2b3f30"


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


def material_guid(name):
    return guid_of("Assets/_Project/Art/TeaHouse/" + name + ".mat")


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
    transforms = {}
    children = {}

    for doc in docs:
        if doc["cls"] != "4":
            continue
        owner = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", doc["body"])
        father = re.search(r"m_Father: \{fileID: (-?\d+)\}", doc["body"])
        if not owner:
            continue
        transforms[owner.group(1)] = doc["id"]
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


# ---------------------------------------------------------------- نوشتن اجزا

class Builder:
    def __init__(self, docs, templates, next_id=700000000):
        self.docs = docs
        self.templates = templates
        self.by_id = templates
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

    def clone(self, source_go, name, position, scale, material, parent, rotation=None,
              extra_scale=None):
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
            elif doc["cls"] == "23":
                if material:
                    body = re.sub(r"m_Materials:\n  - \{fileID: \d+, guid: [0-9a-f]{32}",
                                  "m_Materials:\n  - {fileID: 2100000, guid: " + material,
                                  body, count=1)
            if doc["cls"] == "4":
                transform_id = new

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

    def box(self, name, position, scale, material, parent, rotation=None):
        return self.clone(WALL, name, position, scale, material, parent, rotation)

    def light(self, name, position, intensity, light_range, parent):
        go, transform = self.clone(LIGHT, name, position, (1, 1, 1), None, parent)

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


def set_clock(docs):
    for doc in docs:
        if doc["id"] != CLOCK_COMPONENT:
            continue
        # شب سیزدهم مرداد ۱۳۵۲ — همان شبی که شریفی از چای‌خانه بیرون رفت.
        doc["body"] = re.sub(
            r"startDate:\n    Year: \d+\n    Month: \d+\n    Day: \d+\n    Hour: \d+\n    Minute: \d+",
            "startDate:\n    Year: 1352\n    Month: 5\n    Day: 13\n    Hour: 21\n    Minute: 10",
            doc["body"], count=1)


def set_player(docs):
    for doc in docs:
        if doc["id"] == PLAYER:
            body = re.sub(r"m_LocalPosition: \{[^}]*\}",
                          "m_LocalPosition: {x: 0, y: 1, z: -3.2}", doc["body"], count=1)
            body = re.sub(r"m_LocalRotation: \{[^}]*\}",
                          "m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}", body, count=1)
            doc["body"] = body


def write_materials():
    for name, (color, source) in MATERIALS.items():
        with io.open(source, encoding="utf-8") as handle:
            text = handle.read()

        text = re.sub(r"m_Name: .*", "m_Name: " + name, text, count=1)
        text = re.sub(r"_BaseColor: \{[^}]*\}",
                      "_BaseColor: {r: %s, g: %s, b: %s, a: %s}" % tuple(
                          ("%.6f" % value).rstrip("0").rstrip(".") for value in color),
                      text)
        text = re.sub(r"_Color: \{[^}]*\}",
                      "_Color: {r: %s, g: %s, b: %s, a: %s}" % tuple(
                          ("%.6f" % value).rstrip("0").rstrip(".") for value in color),
                      text)

        path = "Assets/_Project/Art/TeaHouse/" + name + ".mat"
        os.makedirs(os.path.dirname(path), exist_ok=True)

        with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)

        with io.open(path + ".meta", "w", encoding="utf-8", newline="\n") as handle:
            handle.write("fileFormatVersion: 2\nguid: " + guid_of(path) +
                         "\nNativeFormatImporter:\n  externalObjects: {}\n" +
                         "  mainObjectFileID: 2100000\n  userData: \n" +
                         "  assetBundleName: \n  assetBundleVariant: \n")


def ensure_folder_meta(folder):
    meta = folder + ".meta"

    if os.path.exists(meta):
        return

    with io.open(meta, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("fileFormatVersion: 2\nguid: " + guid_of(folder) +
                     "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n" +
                     "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def main():
    os.makedirs("Assets/_Project/Art/TeaHouse", exist_ok=True)
    ensure_folder_meta("Assets/_Project/Art/TeaHouse")
    write_materials()

    with io.open(SOURCE, encoding="utf-8") as handle:
        header, docs = split_scene(handle.read())

    # ۱) برداشتن دفتر، رضا و پرونده
    drop = set()
    for root in (OFFICE, REZA, CUBE):
        drop |= collect_subtree(docs, root)

    templates = {doc["id"]: doc for doc in docs}
    docs = [doc for doc in docs if doc["id"] not in drop]

    builder = Builder(docs, templates)
    set_clock(docs)
    set_player(docs)

    # ۲) ریشه‌ی چای‌خانه
    root_id, root_transform = builder.take(2)
    builder.add("1", root_id,
                "GameObject:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  serializedVersion: 6\n"
                "  m_Component:\n"
                "  - component: {fileID: " + root_transform + "}\n"
                "  m_Layer: 0\n"
                "  m_Name: TeaHouse\n"
                "  m_TagString: Untagged\n"
                "  m_Icon: {fileID: 0}\n"
                "  m_NavMeshLayer: 0\n"
                "  m_StaticEditorFlags: 0\n"
                "  m_IsActive: 1\n\n")
    builder.add("4", root_transform,
                "Transform:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: " + root_id + "}\n"
                "  serializedVersion: 2\n"
                "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
                "  m_LocalPosition: {x: 0, y: 0, z: 0}\n"
                "  m_LocalScale: {x: 1, y: 1, z: 1}\n"
                "  m_ConstrainProportionsScale: 0\n"
                "  m_Children: []\n"
                "  m_Father: {fileID: 0}\n"
                "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n\n")

    wall = material_guid("M_TeaWall")
    floor = material_guid("M_TeaFloor")
    wood = material_guid("M_TeaWood")
    cloth = material_guid("M_TeaCloth")

    # ۳) کف و دیوارها — درِ ورودی جنوبی، درِ آشپزخانه شمالی
    builder.box("Floor", (0, -0.05, 0), (10, 0.1, 8), floor, root_transform)
    builder.box("Wall_S_West", (-3.0, 0.6, -4.0), (4.0, 1.2, 0.2), wall, root_transform)
    builder.box("Wall_S_East", (3.0, 0.6, -4.0), (4.0, 1.2, 0.2), wall, root_transform)
    builder.box("Wall_N_West", (-1.2, 0.6, 4.0), (7.6, 1.2, 0.2), wall, root_transform)
    builder.box("Wall_N_East", (4.5, 0.6, 4.0), (1.0, 1.2, 0.2), wall, root_transform)
    builder.box("Wall_West", (-5.0, 0.6, 0.0), (0.2, 1.2, 8.0), wall, root_transform)
    builder.box("Wall_East", (5.0, 0.6, 0.0), (0.2, 1.2, 8.0), wall, root_transform)

    builder.box("Window", (-4.88, 0.75, 0.4), (0.06, 0.5, 1.6), wood, root_transform)
    builder.box("KitchenDoor", (3.3, 0.6, 3.94), (1.3, 1.15, 0.06), wood, root_transform)

    # ۴) پیشخوان و پشت پیشخوان
    builder.box("Counter", (3.55, 0.5, 0.6), (0.6, 1.0, 3.0), wood, root_transform)
    builder.box("Counter_Top", (3.55, 1.04, 0.6), (0.75, 0.08, 3.2), wood, root_transform)
    builder.box("Cupboard", (4.55, 0.3, 2.9), (0.5, 0.6, 1.2), wood, root_transform)
    builder.box("Samovar", (4.55, 0.85, 2.9), (0.28, 0.5, 0.28), wood, root_transform)
    builder.box("Samovar_Pipe", (4.55, 1.18, 2.9), (0.07, 0.16, 0.07), wood, root_transform)
    builder.box("Tray", (3.55, 1.11, 1.7), (0.26, 0.06, 0.4), wood, root_transform)
    builder.box("Spike_Base", (3.62, 1.07, 0.5), (0.14, 0.03, 0.14), wood, root_transform)
    builder.box("Spike", (3.62, 1.21, 0.5), (0.03, 0.28, 0.03), wood, root_transform)

    # ۵) میزها، صندلی‌ها، رومیزی و چراغ آویز
    for index, (x, z, width) in enumerate(((-2.2, 0.4, 1.3), (-2.6, 2.9, 1.1)), start=1):
        builder.box("Table_%d_Top" % index, (x, 0.72, z), (width, 0.06, 0.9), wood, root_transform)
        builder.box("Table_%d_Leg" % index, (x, 0.35, z), (0.18, 0.7, 0.18), wood, root_transform)

    builder.box("Table_1_Cloth", (-2.2, 0.76, 0.4), (1.36, 0.012, 0.96), cloth, root_transform)

    stools = ((-1.35, 0.4), (-3.05, 0.4), (-1.85, 2.9), (-3.35, 2.9))

    for index, (x, z) in enumerate(stools, start=1):
        builder.box("Stool_%d_Seat" % index, (x, 0.44, z), (0.38, 0.06, 0.38), wood, root_transform)
        builder.box("Stool_%d_Leg" % index, (x, 0.21, z), (0.1, 0.42, 0.1), wood, root_transform)

    builder.box("Lamp_Table", (-2.2, 2.02, 0.4), (0.36, 0.04, 0.36), wood, root_transform)
    builder.box("Lamp_Counter", (3.5, 2.02, 0.9), (0.4, 0.04, 0.4), wood, root_transform)

    # ۶) نور گرم داخل
    builder.light("Light_Counter", (3.2, 2.0, 1.0), 1.9, 7.0, root_transform)
    builder.light("Light_Table", (-2.2, 2.0, 0.6), 1.4, 6.0, root_transform)
    builder.light("Light_Door", (0.0, 2.0, -3.0), 0.9, 5.0, root_transform)

    # ۷) مسعود — همان کپسول رضا، با دیالوگ تازه
    masoud, _ = builder.clone(REZA, "Masoud", (4.4, 1.0, 0.9), (0.8, 0.95, 0.8),
                              NPC_GUID, root_transform)

    for doc in builder.docs:
        if re.search(r"m_GameObject: \{fileID: " + masoud + r"\}", doc["body"]):
            if doc["cls"] == "114" and "TalkInteractable" in doc["body"]:
                doc["body"] = re.sub(
                    r'displayName: ".*"',
                    lambda _: 'displayName: "' + yaml_text("مسعود|Masoud") + '"',
                    doc["body"], count=1)
                doc["body"] = re.sub(
                    r'subtype: ".*"',
                    lambda _: 'subtype: "' + yaml_text("پادوی چای‌خانه | Tea-house boy") + '"',
                    doc["body"], count=1)
            elif doc["cls"] == "114" and "DialogueStarter" in doc["body"]:
                doc["body"] = re.sub(
                    r"dialogueFile: \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\}",
                    "dialogueFile: {fileID: 4900000, guid: " + DIALOGUE_GUID + ", type: 3}",
                    doc["body"], count=1)

    # ۸) قبض شب سیزدهم — کاغذ روی پیشخوان
    chit, _ = builder.clone(CUBE, "Chit", (3.5, 1.09, 0.1), (0.24, 0.012, 0.32),
                            PAPER_GUID, root_transform)

    for doc in builder.docs:
        if not re.search(r"m_GameObject: \{fileID: " + chit + r"\}", doc["body"]):
            continue
        if doc["cls"] != "114":
            continue
        doc["body"] = (
            "MonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n"
            "  m_GameObject: {fileID: " + chit + "}\n"
            "  m_Enabled: 1\n"
            "  m_EditorHideFlags: 0\n"
            "  m_Script: {fileID: 11500000, guid: " + DOCUMENT_SCRIPT_GUID + ", type: 3}\n"
            "  m_Name: \n"
            "  m_EditorClassIdentifier: Assembly-CSharp::TehranNoir.Documents.DocumentInteractable\n"
            "  action: 5\n"
            "  interactionPoint: {fileID: 0}\n"
            "  maxDistance: 2.5\n"
            "  maxAngle: 360\n"
            "  displayName: \"" + yaml_text("قبض شب سیزدهم|Slip, thirteenth night") + "\"\n"
            "  subtype: \"" + yaml_text("دست‌نویس|Handwritten") + "\"\n"
            "  outline: {fileID: 0}\n"
            "  documentPath: Documents/teahouse_chit\n"
            "  onReadVariable: flag.clue.chit\n"
            "  hideAfterRead: 0\n"
            "  logMissing: 1\n\n")

    # ۹) بازسازی بچه‌ها، ریشه‌ها و خروجی
    rebuild_children(builder.docs)

    roots = [doc for doc in builder.docs if doc["cls"] in ("4", "224") and
             re.search(r"m_Father: \{fileID: 0\}", doc["body"])]

    for doc in builder.docs:
        if doc["cls"] == "1660057539":
            block = "m_Roots:\n" + "".join(
                "  - {fileID: " + root["id"] + "}\n" for root in roots)
            doc["body"] = re.sub(r"m_Roots:\n(  - \{fileID: -?\d+\}\n)*", block,
                                 doc["body"], count=1)

    # SceneRoots همیشه آخرین سند باشد؛ همان جایی که خود Unity می‌نویسد.
    builder.docs.sort(key=lambda doc: doc["cls"] == "1660057539")

    output = header + "".join(
        "--- !u!" + doc["cls"] + " &" + doc["id"] + "\n" + doc["body"]
        for doc in builder.docs)

    with io.open(TARGET, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(output)

    print("نوشته شد: " + TARGET)
    print("  آبجکت‌ها: %d" % sum(1 for doc in builder.docs if doc["cls"] == "1"))
    print("  ارجاع‌ها: %d" % output.count("{fileID:"))


if __name__ == "__main__":
    main()
