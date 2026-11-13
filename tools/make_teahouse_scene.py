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

ابزار مشترک با صحنه‌ی هتل در `tools/scenekit.py` است.

اجرا:  python3 tools/make_teahouse_scene.py
"""

import io
import os
import re
import sys

# اجرا از ریشه‌ی ریپو:  python3 tools/make_teahouse_scene.py
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import scenekit as kit

SOURCE = "Assets/_Project/Scenes/Sandbox.unity"
TARGET = "Assets/_Project/Scenes/Teahouse.unity"
MATERIAL_FOLDER = "Assets/_Project/Art/TeaHouse"

# شناسه‌های صحنه‌ی مبدأ
OFFICE = "1184640865"          # دفتر و همه‌ی بچه‌هایش
REZA = "978304783"             # NPC دفتر (فقط به‌عنوان الگوی کپسول)
CUBE = "1537828487"            # پرونده‌ی روی میز (الگوی کاغذ)
WALL = "1124334549"            # یک دیوار، الگوی جعبه
PLAYER = "1182697546"
CLOCK_COMPONENT = "445466633"  # WorldClock
LIGHT = "1107200289"           # Directional Light (الگوی نور)

DIALOGUE_GUID = "c1a53b8b2b5f4ec9a8a7a3f8c0d61e42"
DOCUMENT_SCRIPT_GUID = "4f8b2d1c9a7e4b3d8c6f0a2e5b9d7c14"

MATERIALS = {
    "M_TeaWall": ((0.62, 0.55, 0.45, 1.0), "Assets/_Project/Art/Office/M_Wall.mat"),
    "M_TeaFloor": ((0.28, 0.20, 0.15, 1.0), "Assets/_Project/Art/Office/M_Floor.mat"),
    "M_TeaWood": ((0.24, 0.16, 0.10, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
    "M_TeaCloth": ((0.22, 0.27, 0.22, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
}

PAPER_GUID = "4a8603e6b0d4eed4e8778f6120261937"
NPC_GUID = "5952c87a01e696a45a3c76677c2b3f30"


def main():
    kit.ensure_folder_meta(MATERIAL_FOLDER)
    kit.write_materials(MATERIAL_FOLDER, MATERIALS)

    with io.open(SOURCE, encoding="utf-8") as handle:
        header, docs = kit.split_scene(handle.read())

    # ۱) برداشتن دفتر، رضا و پرونده
    drop = set()
    for root in (OFFICE, REZA, CUBE):
        drop |= kit.collect_subtree(docs, root)

    templates = {doc["id"]: doc for doc in docs}
    docs = [doc for doc in docs if doc["id"] not in drop]

    builder = kit.Builder(docs, templates, WALL, LIGHT)

    kit.set_clock(docs, CLOCK_COMPONENT, 1352, 5, 13, 21, 10)
    kit.set_player(docs, PLAYER, (0, 1, -3.2))

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

    wall = kit.guid_of(MATERIAL_FOLDER + "/M_TeaWall.mat")
    floor = kit.guid_of(MATERIAL_FOLDER + "/M_TeaFloor.mat")
    wood = kit.guid_of(MATERIAL_FOLDER + "/M_TeaWood.mat")
    cloth = kit.guid_of(MATERIAL_FOLDER + "/M_TeaCloth.mat")

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
        if not re.search(r"m_GameObject: \{fileID: " + masoud + r"\}", doc["body"]):
            continue
        if doc["cls"] != "114":
            continue

        if "TalkInteractable" in doc["body"]:
            doc["body"] = re.sub(
                r'displayName: ".*"',
                lambda _: 'displayName: "' + kit.yaml_text("مسعود|Masoud") + '"',
                doc["body"], count=1)
            doc["body"] = re.sub(
                r'subtype: ".*"',
                lambda _: 'subtype: "' + kit.yaml_text("پادوی چای‌خانه | Tea-house boy") + '"',
                doc["body"], count=1)
        elif "DialogueStarter" in doc["body"]:
            doc["body"] = re.sub(
                r"dialogueFile: \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\}",
                "dialogueFile: {fileID: 4900000, guid: " + DIALOGUE_GUID + ", type: 3}",
                doc["body"], count=1)

    # ۸) قبض شب سیزدهم — کاغذ روی پیشخوان
    chit, _ = builder.clone(CUBE, "Chit", (3.5, 1.09, 0.1), (0.24, 0.012, 0.32),
                            PAPER_GUID, root_transform)

    for doc in builder.docs:
        if doc["cls"] != "114":
            continue
        if not re.search(r"m_GameObject: \{fileID: " + chit + r"\}", doc["body"]):
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
            '  displayName: "' + kit.yaml_text("قبض شب سیزدهم|Slip, thirteenth night") + '"\n'
            '  subtype: "' + kit.yaml_text("دست‌نویس|Handwritten") + '"\n'
            "  outline: {fileID: 0}\n"
            "  documentPath: Documents/teahouse_chit\n"
            "  onReadVariable: flag.clue.chit\n"
            "  hideAfterRead: 0\n"
            "  logMissing: 1\n\n")

    output = kit.write_scene(TARGET, header, builder.docs)

    print("نوشته شد: " + TARGET)
    print("  آبجکت‌ها: %d" % sum(1 for doc in builder.docs if doc["cls"] == "1"))
    print("  ارجاع‌ها: %d" % output.count("{fileID:"))


if __name__ == "__main__":
    main()
