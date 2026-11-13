#!/usr/bin/env python3
"""ساخت صحنه‌ی لابی هتل هیلتون از روی صحنه‌ی Sandbox.

این صحنه «برش عمودی» است، نه تمام هتل: لابی، پیشخوان پذیرش و صندوق، و
همان یک نفری که تحویل اول مأموریت لازم دارد. دو برج و پانصد اتاق ساخته
نمی‌شود؛ راهروهای دیگر با دیوار بسته شده‌اند.

ابزار مشترک با چای‌خانه در `tools/scenekit.py` است.

اجرا:  python3 tools/make_hilton_scene.py
"""

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import scenekit as kit

SOURCE = "Assets/_Project/Scenes/Sandbox.unity"
TARGET = "Assets/_Project/Scenes/Hilton.unity"
MATERIAL_FOLDER = "Assets/_Project/Art/Hilton"
DIALOGUE_FOLDER = "Assets/_Project/Dialogue"

# شناسه‌های صحنه‌ی مبدأ
OFFICE = "1184640865"
REZA = "978304783"
CUBE = "1537828487"
WALL = "1124334549"
PLAYER = "1182697546"
CLOCK_COMPONENT = "445466633"
LIGHT = "1107200289"

DOCUMENT_SCRIPT_GUID = "4f8b2d1c9a7e4b3d8c6f0a2e5b9d7c14"
PAPER_GUID = "4a8603e6b0d4eed4e8778f6120261937"
NPC_GUID = "5952c87a01e696a45a3c76677c2b3f30"

MATERIALS = {
    "M_HotelWall": ((0.66, 0.61, 0.52, 1.0), "Assets/_Project/Art/Office/M_Wall.mat"),
    "M_HotelFloor": ((0.22, 0.21, 0.20, 1.0), "Assets/_Project/Art/Office/M_Floor.mat"),
    "M_HotelWood": ((0.26, 0.17, 0.11, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
    "M_HotelBrass": ((0.55, 0.44, 0.20, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
}


def paper(builder, root, name, position, size, material, display_name, subtype,
          document_path, read_variable):
    """یک کاغذ خواندنی: همان الگوی چای‌خانه و بایگانی، بدون کد تازه."""
    go, _ = builder.clone(CUBE, name, position, size, material, root)

    for doc in builder.docs:
        if doc["cls"] != "114":
            continue
        if not re.search(r"m_GameObject: \{fileID: " + go + r"\}", doc["body"]):
            continue

        doc["body"] = (
            "MonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n"
            "  m_GameObject: {fileID: " + go + "}\n"
            "  m_Enabled: 1\n"
            "  m_EditorHideFlags: 0\n"
            "  m_Script: {fileID: 11500000, guid: " + DOCUMENT_SCRIPT_GUID + ", type: 3}\n"
            "  m_Name: \n"
            "  m_EditorClassIdentifier: Assembly-CSharp::TehranNoir.Documents.DocumentInteractable\n"
            "  action: 5\n"
            "  interactionPoint: {fileID: 0}\n"
            "  maxDistance: 2.5\n"
            "  maxAngle: 360\n"
            '  displayName: "' + kit.yaml_text(display_name) + '"\n'
            '  subtype: "' + kit.yaml_text(subtype) + '"\n'
            "  outline: {fileID: 0}\n"
            "  documentPath: " + document_path + "\n"
            "  onReadVariable: " + read_variable + "\n"
            "  hideAfterRead: 0\n"
            "  logMissing: 1\n\n")

    return go


def builder_root(builder, name):
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
                "  m_Father: {fileID: 0}\n"
                "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n\n")

    return go, transform


def name_npc(builder, npc, display_name, subtype, dialogue_guid):
    for doc in builder.docs:
        if doc["cls"] != "114":
            continue
        if not re.search(r"m_GameObject: \{fileID: " + npc + r"\}", doc["body"]):
            continue

        if "TalkInteractable" in doc["body"]:
            doc["body"] = re.sub(
                r'displayName: ".*"',
                lambda _: 'displayName: "' + kit.yaml_text(display_name) + '"',
                doc["body"], count=1)
            doc["body"] = re.sub(
                r'subtype: ".*"',
                lambda _: 'subtype: "' + kit.yaml_text(subtype) + '"',
                doc["body"], count=1)
        elif "DialogueStarter" in doc["body"]:
            doc["body"] = re.sub(
                r"dialogueFile: \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\}",
                "dialogueFile: {fileID: 4900000, guid: " + dialogue_guid + ", type: 3}",
                doc["body"], count=1)


def main():
    kit.ensure_folder_meta(MATERIAL_FOLDER)
    kit.write_materials(MATERIAL_FOLDER, MATERIALS)

    dialogue_guid = kit.guid_of(DIALOGUE_FOLDER + "/cashier_hilton.txt")

    with io.open(SOURCE, encoding="utf-8") as handle:
        header, docs = kit.split_scene(handle.read())

    drop = set()
    for root in (OFFICE, REZA, CUBE):
        drop |= kit.collect_subtree(docs, root)

    templates = {doc["id"]: doc for doc in docs}
    docs = [doc for doc in docs if doc["id"] not in drop]

    builder = kit.Builder(docs, templates, WALL, LIGHT)

    # شب سیزدهم مرداد ۱۳۵۲، یک ربع به نیمه‌شب — بعد از پرداخت ساعت ۲۳:۴۰.
    kit.set_clock(docs, CLOCK_COMPONENT, 1352, 5, 13, 23, 45)
    kit.set_player(docs, PLAYER, (0, 1, -4.6))

    root_id, root_transform = builder_root(builder, "HiltonLobby")

    wall = kit.guid_of(MATERIAL_FOLDER + "/M_HotelWall.mat")
    floor = kit.guid_of(MATERIAL_FOLDER + "/M_HotelFloor.mat")
    wood = kit.guid_of(MATERIAL_FOLDER + "/M_HotelWood.mat")
    brass = kit.guid_of(MATERIAL_FOLDER + "/M_HotelBrass.mat")

    # ۱) کف و پوسته — ورودی جنوبی، بدون اتاق و بدون راهرو
    builder.box("Floor", (0, -0.05, 0), (16, 0.1, 12), floor, root_transform)
    builder.box("Wall_S_West", (-5.75, 0.6, -6), (8.5, 1.2, 0.2), wall, root_transform)
    builder.box("Wall_S_East", (5.75, 0.6, -6), (8.5, 1.2, 0.2), wall, root_transform)
    builder.box("Wall_North", (0, 0.6, 6), (16.4, 1.2, 0.2), wall, root_transform)
    builder.box("Wall_West", (-8, 0.6, 0), (0.2, 1.2, 12.4), wall, root_transform)
    builder.box("Wall_East", (8, 0.6, 0), (0.2, 1.2, 12.4), wall, root_transform)

    builder.box("Entrance_Mat", (0, 0.01, -5.1), (3.2, 0.02, 1.8), floor, root_transform)
    builder.box("Door_Glass_W", (-0.78, 0.6, -5.9), (1.4, 1.15, 0.06), brass, root_transform)
    builder.box("Door_Glass_E", (0.78, 0.6, -5.9), (1.4, 1.15, 0.06), brass, root_transform)

    # ۲) ستون‌ها و سقفِ خیالی
    for index, x in enumerate((-4.5, 4.5), start=1):
        builder.box("Pillar_%d" % index, (x, 0.6, -1.0), (0.5, 1.2, 0.5), wall, root_transform)

    builder.box("Chandelier_1", (0, 2.05, 0.5), (1.1, 0.06, 1.1), brass, root_transform)
    builder.box("Chandelier_2", (0, 2.05, -4.0), (0.7, 0.05, 0.7), brass, root_transform)

    # ۳) پیشخوان پذیرش و صندوق
    builder.box("Counter", (0, 0.5, 3.9), (7.0, 1.0, 0.6), wood, root_transform)
    builder.box("Counter_Top", (0, 1.04, 3.9), (7.3, 0.08, 0.85), brass, root_transform)
    builder.box("Counter_Back", (0, 0.6, 5.2), (7.0, 1.2, 0.15), wood, root_transform)
    builder.box("Key_Rack", (0, 0.95, 5.1), (3.6, 0.5, 0.08), wood, root_transform)

    for index, x in enumerate((-1.5, -1.1, -0.7, -0.3, 0.1, 0.5, 0.9, 1.3), start=1):
        builder.box("Key_Slot_%d" % index, (x, 0.95, 5.02), (0.05, 0.34, 0.04), brass, root_transform)

    builder.box("Register", (1.9, 1.13, 4.0), (0.45, 0.16, 0.35), brass, root_transform)
    builder.box("Bell", (-0.6, 1.11, 3.75), (0.09, 0.06, 0.09), brass, root_transform)
    builder.box("Key_Box", (-2.4, 1.09, 3.95), (0.4, 0.08, 0.3), wood, root_transform)

    # ۴) سالن انتظار
    builder.box("Sofa", (-5.6, 0.3, -2.2), (2.8, 0.55, 0.9), wood, root_transform)
    builder.box("Sofa_Back", (-5.6, 0.72, -2.6), (2.8, 0.5, 0.2), wood, root_transform)
    builder.box("Sofa_Table", (-5.6, 0.4, -0.9), (1.5, 0.06, 0.8), wood, root_transform)
    builder.box("Sofa_Table_Leg", (-5.6, 0.2, -0.9), (0.12, 0.4, 0.12), wood, root_transform)

    builder.box("Luggage_Cart", (5.7, 0.5, -3.2), (1.1, 0.06, 0.7), brass, root_transform)
    builder.box("Luggage_Cart_Leg", (5.7, 0.24, -3.2), (0.08, 0.5, 0.08), brass, root_transform)
    builder.box("Suitcase", (5.7, 0.62, -3.2), (0.6, 0.22, 0.4), wood, root_transform)

    builder.box("Planter", (6.6, 0.28, 2.2), (0.7, 0.56, 0.7), wood, root_transform)
    builder.box("Lobby_Desk", (-6.4, 0.5, 2.6), (1.6, 1.0, 0.7), wood, root_transform)

    # ۵) نور
    builder.light("Light_Counter", (0, 2.0, 3.6), 1.8, 8.0, root_transform)
    builder.light("Light_Lobby", (0, 2.0, -0.6), 1.5, 10.0, root_transform)
    builder.light("Light_Entrance", (0, 2.0, -4.8), 0.8, 5.0, root_transform)

    # ۶) صندوق‌دار
    cashier, _ = builder.clone(REZA, "Cashier", (0.9, 1.0, 4.9), (0.8, 0.95, 0.8),
                               NPC_GUID, root_transform)

    name_npc(builder, cashier, "صندوق-دار|Cashier", "پیشخوان پذیرش | Reception", dialogue_guid)

    # ۷) دو کاغذ روی پیشخوان
    paper(builder, root_transform, "Cash_Slip", (-1.6, 1.09, 3.62), (0.24, 0.012, 0.32),
          PAPER_GUID, "رسید ۱۸۶|Receipt 186", "صندوق | Cashier desk",
          "Documents/hilton_cash_slip", "flag.hilton.ev.bill")

    paper(builder, root_transform, "Cash_Ledger", (-0.75, 1.10, 3.7), (0.38, 0.05, 0.28),
          PAPER_GUID, "دفتر صندوق|Cash book", "امشب | Tonight",
          "Documents/hilton_cash_ledger", "flag.hilton.ev.ledger")

    output = kit.write_scene(TARGET, header, builder.docs)

    with io.open(TARGET + ".meta", "w", encoding="utf-8", newline="\n") as handle:
        handle.write("fileFormatVersion: 2\nguid: " + kit.guid_of(TARGET) +
                     "\nDefaultImporter:\n  externalObjects: {}\n  userData: \n" +
                     "  assetBundleName: \n  assetBundleVariant: \n")

    print("نوشته شد: " + TARGET)
    print("  آبجکت‌ها: %d" % sum(1 for doc in builder.docs if doc["cls"] == "1"))
    print("  ارجاع‌ها: %d" % output.count("{fileID:"))


if __name__ == "__main__":
    main()
