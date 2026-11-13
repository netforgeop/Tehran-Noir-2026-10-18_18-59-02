#!/usr/bin/env python3
"""ساخت صحنه‌ی هتل هیلتون از خیابان تا لابی — با برش ساختمان.

این صحنه دیگر «یک اتاق بی‌سقف» نیست: یک ساختمان چهارطبقه است که از خیابان
کامل دیده می‌شود، و وقتی واردش می‌شوی طبقه‌های بالایی و سقف برداشته می‌شوند
تا دوربین بالاسری داخل لابی را ببیند. منطق برش در
`Assets/_Project/Scripts/World/BuildingCutaway.cs` است؛ این‌جا فقط تکه‌ها
ساخته و علامت‌گذاری می‌شوند.

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
SUN = "1107200289"
CUTAWAY_TEST_GUID = "d50a984b5d36d67e530506f47ae62c84"

DOCUMENT_SCRIPT_GUID = "4f8b2d1c9a7e4b3d8c6f0a2e5b9d7c14"
PAPER_GUID = "4a8603e6b0d4eed4e8778f6120261937"
NPC_GUID = "5952c87a01e696a45a3c76677c2b3f30"

SKY = "{r: 0.10, g: 0.12, b: 0.18, a: 1}"
FOG = "{r: 0.06, g: 0.07, b: 0.10, a: 1}"

HALF_W = 8.0        # نیم‌عرض ساختمان
HALF_D = 6.0        # نیم‌عمق ساختمان
LOBBY_H = 2.8       # ارتفاع دیوار لابی
FLOOR_H = 3.0       # ارتفاع هر طبقه‌ی بالا
LEVELS = 3          # طبقه‌های بالای لابی
ROOF_Y = FLOOR_H * (LEVELS + 1)   # ۱۲

MATERIALS = {
    "M_HotelWall": ((0.66, 0.61, 0.52, 1.0), "Assets/_Project/Art/Office/M_Wall.mat"),
    "M_HotelFloor": ((0.22, 0.21, 0.20, 1.0), "Assets/_Project/Art/Office/M_Floor.mat"),
    "M_HotelWood": ((0.26, 0.17, 0.11, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
    "M_HotelBrass": ((0.55, 0.44, 0.20, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
    "M_HotelGlass": ((0.08, 0.11, 0.15, 1.0), "Assets/_Project/Art/Office/M_Floor.mat"),
    "M_HotelLit": ((0.95, 0.78, 0.45, 1.0), "Assets/_Project/Art/Office/M_Wall.mat"),
    "M_HotelAsphalt": ((0.08, 0.08, 0.09, 1.0), "Assets/_Project/Art/Office/M_Floor.mat"),
    "M_HotelSidewalk": ((0.34, 0.33, 0.31, 1.0), "Assets/_Project/Art/Office/M_Wall.mat"),
    "M_HotelSign": ((0.72, 0.16, 0.12, 1.0), "Assets/_Project/Art/Office/M_Wood.mat"),
}


def paper(builder, root, name, position, size, display_name, subtype,
          document_path, read_variable):
    """یک کاغذ خواندنی: همان الگوی چای‌خانه و بایگانی، بدون کد تازه."""
    go, _ = builder.clone(CUBE, name, position, size, PAPER_GUID, root)

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


def facade(builder, floor_root, level, guids, mullions=True):
    """پوستِ یک طبقه‌ی بالا: دال + نوارهای دیوار + شیشه + تیغه‌ها.

    دالِ هر طبقه، سقفِ طبقه‌ی زیرِ خودش است. پس وقتی برش، این طبقه را
    خاموش می‌کند، سقفِ طبقه‌ی پایین هم خودبه‌خود می‌رود.
    """
    base = FLOOR_H * level
    wall, glass, lit = guids["wall"], guids["glass"], guids["lit"]

    builder.box("F%d_Slab" % level, (0, base, 0),
                (HALF_W * 2 + 0.4, 0.2, HALF_D * 2 + 0.4), wall, floor_root)

    # نوار پایین (زیر پنجره) و نوار بالا (بالای پنجره)
    for name, z in (("S", -HALF_D - 0.2), ("N", HALF_D + 0.2)):
        builder.box("F%d_Wall_%s_Low" % (level, name),
                    (0, base + 0.55, z), (HALF_W * 2 + 0.4, 0.9, 0.2), wall, floor_root)
        builder.box("F%d_Wall_%s_High" % (level, name),
                    (0, base + 2.55, z), (HALF_W * 2 + 0.4, 1.1, 0.2), wall, floor_root)
        builder.box("F%d_Glass_%s" % (level, name),
                    (0, base + 1.5, z + (0.04 if z < 0 else -0.04)),
                    (HALF_W * 2, 1.0, 0.06), glass, floor_root)

    for name, x in (("E", HALF_W + 0.2), ("W", -HALF_W - 0.2)):
        builder.box("F%d_Wall_%s_Low" % (level, name),
                    (x, base + 0.55, 0), (0.2, 0.9, HALF_D * 2), wall, floor_root)
        builder.box("F%d_Wall_%s_High" % (level, name),
                    (x, base + 2.55, 0), (0.2, 1.1, HALF_D * 2), wall, floor_root)
        builder.box("F%d_Glass_%s" % (level, name),
                    (x + (-0.04 if x > 0 else 0.04), base + 1.5, 0),
                    (0.06, 1.0, HALF_D * 2 - 0.4), glass, floor_root)

    if not mullions:
        return

    # تیغه‌های پنجره فقط روی نمای جنوبی؛ نمای شمالی از خیابان دیده نمی‌شود.
    for index, x in enumerate((-6.0, -3.0, 0.0, 3.0, 6.0), start=1):
        builder.box("F%d_Mullion_%d" % (level, index),
                    (x, base + 1.5, -HALF_D - 0.13),
                    (0.09, 1.0, 0.12), wall, floor_root)

    # یک پنجره‌ی روشن، تا ساختمان از بیرون زنده باشد.
    builder.box("F%d_Lit_%d" % (level, 1), (-3.0, base + 1.5, -HALF_D - 0.16),
                (2.6, 0.86, 0.04), lit, floor_root)


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

    # شب سیزدهم مرداد ۱۳۵۲، یک ربع به نیمه‌شب — بازیکن از خیابان می‌رسد.
    kit.set_clock(docs, CLOCK_COMPONENT, 1352, 5, 13, 23, 45)
    kit.set_player(docs, PLAYER, (0, 1, -13.5))
    kit.set_environment(docs, SKY, 0.55, fog=True)

    for doc in docs:
        if doc["cls"] == "104":
            doc["body"] = re.sub(r"m_FogColor: \{[^}]*\}", "m_FogColor: " + FOG,
                                 doc["body"], count=1)

    for doc in docs:
        if doc["cls"] != "108":
            continue
        if not re.search(r"m_GameObject: \{fileID: " + SUN + r"\}", doc["body"]):
            continue
        doc["body"] = re.sub(r"m_Intensity: [0-9.]+", "m_Intensity: 0.08",
                             doc["body"], count=1)
        doc["body"] = re.sub(r"m_Color: \{[^}]*\}",
                             "m_Color: {r: 0.40, g: 0.48, b: 0.66, a: 1}",
                             doc["body"], count=1)

    viewer = builder.transform_id(PLAYER)

    root_id, root_transform = builder_root(builder, "HiltonBuilding")

    wall = kit.guid_of(MATERIAL_FOLDER + "/M_HotelWall.mat")
    floor = kit.guid_of(MATERIAL_FOLDER + "/M_HotelFloor.mat")
    wood = kit.guid_of(MATERIAL_FOLDER + "/M_HotelWood.mat")
    brass = kit.guid_of(MATERIAL_FOLDER + "/M_HotelBrass.mat")
    glass = kit.guid_of(MATERIAL_FOLDER + "/M_HotelGlass.mat")
    lit = kit.guid_of(MATERIAL_FOLDER + "/M_HotelLit.mat")
    asphalt = kit.guid_of(MATERIAL_FOLDER + "/M_HotelAsphalt.mat")
    sidewalk = kit.guid_of(MATERIAL_FOLDER + "/M_HotelSidewalk.mat")
    sign = kit.guid_of(MATERIAL_FOLDER + "/M_HotelSign.mat")

    guids = {"wall": wall, "glass": glass, "lit": lit}

    # ---------------------------------------------------------------- خیابان
    builder.box("Sidewalk", (0, -0.15, -9.0), (26, 0.3, 6.0), sidewalk, root_transform)
    builder.box("Curb", (0, -0.08, -12.05), (26, 0.16, 0.25), sidewalk, root_transform)
    builder.box("Road", (0, -0.3, -18.0), (30, 0.3, 12.0), asphalt, root_transform)
    builder.box("Opposite_Sidewalk", (0, -0.15, -25.0), (30, 0.3, 2.0), sidewalk, root_transform)
    builder.box("Opposite_Block", (0, 6.4, -32.0), (46, 13, 8), asphalt, root_transform)
    builder.box("Opposite_Lit", (0, 3.2, -27.95), (44, 1.2, 0.1), glass, root_transform)

    builder.box("Car_Body", (9.2, 0.22, -14.6), (1.9, 0.5, 4.2), wood, root_transform)
    builder.box("Car_Cabin", (9.2, 0.62, -14.8), (1.7, 0.42, 2.0), glass, root_transform)
    for index, (x, z) in enumerate(((8.4, -13.0), (10.0, -13.0), (8.4, -16.2), (10.0, -16.2)), start=1):
        builder.box("Car_Wheel_%d" % index, (x, 0.05, z), (0.25, 0.35, 0.35), asphalt, root_transform)

    for index, x in enumerate((-9.6, 9.6), start=1):
        builder.box("Street_Lamp_Pole_%d" % index, (x, 2.3, -9.9), (0.12, 4.6, 0.12), wall, root_transform)
        builder.box("Street_Lamp_Arm_%d" % index, (x * 0.94, 4.55, -9.9), (0.9, 0.1, 0.1), wall, root_transform)
        builder.box("Street_Lamp_Head_%d" % index, (x * 0.88, 4.42, -9.9), (0.42, 0.16, 0.3), lit, root_transform)
        lamp, _ = builder.light("Street_Light_%d" % index, (x * 0.88, 4.25, -9.9), 1.6, 9.0, root_transform)
        for doc in builder.docs:
            if doc["cls"] == "108" and re.search(
                    r"m_GameObject: \{fileID: " + lamp + r"\}", doc["body"]):
                doc["body"] = re.sub(r"m_Color: \{[^}]*\}",
                                     "m_Color: {r: 0.62, g: 0.72, b: 0.9, a: 1}",
                                     doc["body"], count=1)

    # ---------------------------------------------------------------- لابی
    lobby_id, lobby = kit.empty(builder, "Lobby", root_transform)
    kit.cutaway_piece(builder, lobby_id, 0, 0)

    builder.box("Floor", (0, -0.05, 0), (HALF_W * 2, 0.1, HALF_D * 2), floor, lobby)
    builder.box("Wall_North", (0, LOBBY_H * 0.5, HALF_D), (HALF_W * 2 + 0.4, LOBBY_H, 0.2), wall, lobby)
    builder.box("Wall_West", (-HALF_W, LOBBY_H * 0.5, 0), (0.2, LOBBY_H, HALF_D * 2 + 0.4), wall, lobby)
    builder.box("Wall_East", (HALF_W, LOBBY_H * 0.5, 0), (0.2, LOBBY_H, HALF_D * 2 + 0.4), wall, lobby)

    # دیوار جنوبی دو تکه است؛ وسطش باز است. درِ شیشه‌ایِ بسته برداشته شد:
    # با آن، بازیکن هرگز نمی‌توانست از خیابان وارد شود.
    builder.box("Wall_S_West", (-5.75, LOBBY_H * 0.5, -HALF_D), (8.5, LOBBY_H, 0.2), wall, lobby)
    builder.box("Wall_S_East", (5.75, LOBBY_H * 0.5, -HALF_D), (8.5, LOBBY_H, 0.2), wall, lobby)
    builder.box("Entrance_Frame_W", (-1.62, LOBBY_H * 0.5, -HALF_D), (0.25, LOBBY_H, 0.35), wood, lobby)
    builder.box("Entrance_Frame_E", (1.62, LOBBY_H * 0.5, -HALF_D), (0.25, LOBBY_H, 0.35), wood, lobby)
    builder.box("Entrance_Lintel", (0, 2.55, -HALF_D), (3.5, 0.5, 0.35), wood, lobby)
    builder.box("Entrance_Mat", (0, 0.01, -4.9), (3.0, 0.02, 1.6), floor, lobby)

    # دو لنگه‌ی در که به بیرون باز شده‌اند
    builder.box("Door_Leaf_W", (-1.62, 1.05, -6.6), (1.15, 2.1, 0.07), wood, lobby,
                rotation=(0.70710678, 0.70710678))
    builder.box("Door_Leaf_E", (1.62, 1.05, -6.6), (1.15, 2.1, 0.07), wood, lobby,
                rotation=(0.70710678, 0.70710678))

    # سایبان و ستون‌ها — بیرون، بالای در
    builder.box("Canopy", (0, 3.05, -7.4), (4.6, 0.18, 2.6), wood, lobby)
    builder.box("Canopy_Column_W", (-2.0, 1.5, -8.4), (0.28, 3.0, 0.28), wood, lobby)
    builder.box("Canopy_Column_E", (2.0, 1.5, -8.4), (0.28, 3.0, 0.28), wood, lobby)

    # ستون‌های داخل و لوستر
    for index, x in enumerate((-4.5, 4.5), start=1):
        builder.box("Pillar_%d" % index, (x, 1.4, -1.0), (0.5, 2.8, 0.5), wall, lobby)

    builder.box("Chandelier_1", (0, 2.5, 0.5), (1.1, 0.06, 1.1), brass, lobby)
    builder.box("Chandelier_2", (0, 2.5, -4.0), (0.7, 0.05, 0.7), brass, lobby)

    # پیشخوان پذیرش و صندوق
    builder.box("Counter", (0, 0.5, 3.9), (7.0, 1.0, 0.6), wood, lobby)
    builder.box("Counter_Top", (0, 1.04, 3.9), (7.3, 0.08, 0.85), brass, lobby)
    builder.box("Counter_Back", (0, 0.6, 5.2), (7.0, 1.2, 0.15), wood, lobby)
    builder.box("Key_Rack", (0, 0.95, 5.1), (3.6, 0.5, 0.08), wood, lobby)

    for index, x in enumerate((-1.5, -1.1, -0.7, -0.3, 0.1, 0.5, 0.9, 1.3), start=1):
        builder.box("Key_Slot_%d" % index, (x, 0.95, 5.02), (0.05, 0.34, 0.04), brass, lobby)

    builder.box("Register", (1.9, 1.13, 4.0), (0.45, 0.16, 0.35), brass, lobby)
    builder.box("Bell", (-0.6, 1.11, 3.75), (0.09, 0.06, 0.09), brass, lobby)
    builder.box("Key_Box", (-2.4, 1.09, 3.95), (0.4, 0.08, 0.3), wood, lobby)

    # سالن انتظار
    builder.box("Sofa", (-5.6, 0.3, -2.2), (2.8, 0.55, 0.9), wood, lobby)
    builder.box("Sofa_Back", (-5.6, 0.72, -2.6), (2.8, 0.5, 0.2), wood, lobby)
    builder.box("Sofa_Table", (-5.6, 0.4, -0.9), (1.5, 0.06, 0.8), wood, lobby)
    builder.box("Sofa_Table_Leg", (-5.6, 0.2, -0.9), (0.12, 0.4, 0.12), wood, lobby)

    builder.box("Luggage_Cart", (5.7, 0.5, -3.2), (1.1, 0.06, 0.7), brass, lobby)
    builder.box("Luggage_Cart_Leg", (5.7, 0.24, -3.2), (0.08, 0.5, 0.08), brass, lobby)
    builder.box("Suitcase", (5.7, 0.62, -3.2), (0.6, 0.22, 0.4), wood, lobby)

    builder.box("Planter", (6.6, 0.28, 2.2), (0.7, 0.56, 0.7), wood, lobby)
    builder.box("Lobby_Desk", (-6.4, 0.5, 2.6), (1.6, 1.0, 0.7), wood, lobby)

    # نور لابی
    builder.light("Light_Counter", (0, 2.2, 3.6), 1.8, 8.0, lobby)
    builder.light("Light_Lobby", (0, 2.2, -0.6), 1.5, 10.0, lobby)
    builder.light("Light_Entrance", (0, 2.2, -4.8), 0.9, 5.0, lobby)
    entrance_light, _ = builder.light("Light_Canopy", (0, 2.8, -7.4), 1.4, 6.0, lobby)
    for doc in builder.docs:
        if doc["cls"] == "108" and re.search(
                r"m_GameObject: \{fileID: " + entrance_light + r"\}", doc["body"]):
            doc["body"] = re.sub(r"m_Color: \{[^}]*\}",
                                 "m_Color: {r: 1, g: 0.78, b: 0.5, a: 1}",
                                 doc["body"], count=1)

    # صندوق‌دار و دو کاغذ
    cashier, _ = builder.clone(REZA, "Cashier", (0.9, 1.0, 4.9), (0.8, 0.95, 0.8),
                               NPC_GUID, lobby)
    name_npc(builder, cashier, "صندوق-دار|Cashier", "پیشخوان پذیرش | Reception", dialogue_guid)

    paper(builder, lobby, "Cash_Slip", (-1.6, 1.09, 3.62), (0.24, 0.012, 0.32),
          "رسید ۱۸۶|Receipt 186", "صندوق | Cashier desk",
          "Documents/hilton_cash_slip", "flag.hilton.ev.bill")

    paper(builder, lobby, "Cash_Ledger", (-0.75, 1.10, 3.7), (0.38, 0.05, 0.28),
          "دفتر صندوق|Cash book", "امشب | Tonight",
          "Documents/hilton_cash_ledger", "flag.hilton.ev.ledger")

    # ---------------------------------------------------------------- طبقه‌های بالا
    for level in range(1, LEVELS + 1):
        floor_id, floor_root = kit.empty(builder, "Floor_%d" % level, root_transform)
        kit.cutaway_piece(builder, floor_id, 0, level)
        facade(builder, floor_root, level, guids)

    # تابلوی نئون، بالای سایبان — روی طبقه‌ی اول
    sign_root = None
    for doc in builder.docs:
        if doc["cls"] == "1" and "m_Name: Floor_1" in doc["body"]:
            sign_root = builder.transform_id(doc["id"])

    builder.box("Sign_Box", (0, 3.75, -HALF_D - 0.34), (5.2, 0.95, 0.22), sign, sign_root)
    builder.box("Sign_Glow", (0, 3.75, -HALF_D - 0.47), (4.8, 0.62, 0.05), lit, sign_root)
    sign_light, _ = builder.light("Light_Sign", (0, 3.75, -HALF_D - 1.1), 1.2, 6.0, sign_root)
    for doc in builder.docs:
        if doc["cls"] == "108" and re.search(
                r"m_GameObject: \{fileID: " + sign_light + r"\}", doc["body"]):
            doc["body"] = re.sub(r"m_Color: \{[^}]*\}",
                                 "m_Color: {r: 0.95, g: 0.35, b: 0.28, a: 1}",
                                 doc["body"], count=1)

    # ---------------------------------------------------------------- بام
    roof_id, roof = kit.empty(builder, "Roof", root_transform)
    kit.cutaway_piece(builder, roof_id, 0, LEVELS + 1)

    builder.box("Roof_Slab", (0, ROOF_Y + 0.1, 0),
                (HALF_W * 2 + 0.6, 0.2, HALF_D * 2 + 0.6), wall, roof)
    builder.box("Parapet_S", (0, ROOF_Y + 0.55, -HALF_D - 0.2),
                (HALF_W * 2 + 0.6, 0.7, 0.25), wall, roof)
    builder.box("Parapet_N", (0, ROOF_Y + 0.55, HALF_D + 0.2),
                (HALF_W * 2 + 0.6, 0.7, 0.25), wall, roof)
    builder.box("Parapet_W", (-HALF_W - 0.2, ROOF_Y + 0.55, 0),
                (0.25, 0.7, HALF_D * 2 + 0.6), wall, roof)
    builder.box("Parapet_E", (HALF_W + 0.2, ROOF_Y + 0.55, 0),
                (0.25, 0.7, HALF_D * 2 + 0.6), wall, roof)
    builder.box("Water_Tank", (5.0, ROOF_Y + 1.05, 3.0), (1.4, 1.8, 1.4), wood, roof)
    builder.box("Stair_Hut", (-4.6, ROOF_Y + 0.75, 3.2), (2.4, 1.5, 2.0), wall, roof)

    # ---------------------------------------------------------------- برش
    kit.building_cutaway(
        builder, root_id, viewer,
        (0, 1.4, 0), (HALF_W * 2 + 0.4, LOBBY_H, HALF_D * 2 + 0.4),
        floor_height=FLOOR_H, ground_level=0.0, exit_margin=0.7)

    # ---------------------------------------------------------------- نشانگر و نوشتن
    kit.component_object(
        builder, root_transform, "SceneMarker_Hilton", "7d32222d12ba24edcd0410ce3e475a72",
        "TehranNoir.World.SceneMarker",
        "  placeId: hotel\n  logMarker: 1")

    # تست برش، یک بار با شروع صحنه. در بیلد خاموشش کن.
    kit.component_object(
        builder, root_transform, "CutawaySelfTest", CUTAWAY_TEST_GUID,
        "TehranNoir.World.CutawaySelfTest",
        "  runOnStart: 1\n  levels: 3")

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
