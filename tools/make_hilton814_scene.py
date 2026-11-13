#!/usr/bin/env python3
"""ساخت صحنه‌ی «اتاق ۸۱۴» — راهروی طبقه‌ی هشتم هتل.

این صحنه قلب مأموریت است: راهرویی با هشت در، اتاق جرم، و دفتر مدیر طبقه.
همه از روی `Sandbox.unity` ساخته می‌شود، مثل لابی و چای‌خانه.

قاعده‌ی چیدمان: بازیکن از آسانسور (جنوب) وارد می‌شود و رو به شمال می‌رود.
درِ ۸۱۴ سمت راست، باز و با نوار صحنه بسته； اتاق ۸۱۴ یک جعبه‌ی شش‌در‌پنج‌ونیم
است که سمت شرقِ راهرو چسبیده. دفتر رحیمی سمت چپ، ته راهرو.

اجرا:  python3 tools/make_hilton814_scene.py
"""

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import scenekit as kit

SOURCE = "Assets/_Project/Scenes/Sandbox.unity"
TARGET = "Assets/_Project/Scenes/Hilton814.unity"
MATERIAL_FOLDER = "Assets/_Project/Art/Hilton814"
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

DOCUMENT_SCRIPT_GUID = "4f8b2d1c9a7e4b3d8c6f0a2e5b9d7c14"
PAPER_GUID = "4a8603e6b0d4eed4e8778f6120261937"
NPC_GUID = "5952c87a01e696a45a3c76677c2b3f30"
MARKER_GUID = "7d32222d12ba24edcd0410ce3e475a72"
CUTAWAY_TEST_GUID = "d50a984b5d36d67e530506f47ae62c84"
SIMPLE_SCRIPT_GUID = "3a720bb0e6966394ab363f5c0aed4660"
STARTER_SCRIPT_GUID = "1acd582d969747f44ba68ce463e05588"

SKY = "{r: 0.20, g: 0.18, b: 0.17, a: 1}"
FOG = "{r: 0.09, g: 0.085, b: 0.09, a: 1}"

MATERIALS = {
    "M_814Wall": ((0.50, 0.44, 0.36, 1.0), "Assets/_Project/Art/Hilton/M_HotelWall.mat"),
    "M_814Floor": ((0.15, 0.12, 0.11, 1.0), "Assets/_Project/Art/Hilton/M_HotelFloor.mat"),
    "M_814Carpet": ((0.31, 0.09, 0.09, 1.0), "Assets/_Project/Art/Hilton/M_HotelFloor.mat"),
    "M_814Wood": ((0.24, 0.15, 0.10, 1.0), "Assets/_Project/Art/Hilton/M_HotelWood.mat"),
    "M_814Door": ((0.33, 0.21, 0.12, 1.0), "Assets/_Project/Art/Hilton/M_HotelWood.mat"),
    "M_814Brass": ((0.60, 0.48, 0.22, 1.0), "Assets/_Project/Art/Hilton/M_HotelBrass.mat"),
    "M_814Sheet": ((0.80, 0.78, 0.72, 1.0), "Assets/_Project/Art/Hilton/M_HotelWall.mat"),
    "M_814Blood": ((0.15, 0.02, 0.02, 1.0), "Assets/_Project/Art/Hilton/M_HotelFloor.mat"),
    "M_814Night": ((0.02, 0.03, 0.06, 1.0), "Assets/_Project/Art/Hilton/M_HotelFloor.mat"),
    "M_814Tape": ((0.42, 0.50, 0.60, 1.0), "Assets/_Project/Art/Hilton/M_HotelWall.mat"),
    "M_814Suit": ((0.13, 0.14, 0.16, 1.0), "Assets/_Project/Art/Materials/M_Npc.mat"),
    "M_814Uniform": ((0.42, 0.45, 0.50, 1.0), "Assets/_Project/Art/Materials/M_Npc.mat"),
    "M_814Police": ((0.10, 0.13, 0.22, 1.0), "Assets/_Project/Art/Materials/M_Npc.mat"),
    "M_814Skin": ((0.62, 0.50, 0.40, 1.0), "Assets/_Project/Art/Materials/M_Npc.mat"),
    "M_814Victim": ((0.52, 0.50, 0.46, 1.0), "Assets/_Project/Art/Materials/M_Npc.mat"),
}


def paper(builder, root, name, position, size, display_name, subtype,
          document_path, read_variable):
    """یک کاغذ خواندنی؛ همان الگوی لابی، بدون کد تازه."""
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


def strip_interactables(builder, go):
    """کامپوننت‌های تعامل را از یک کپی برمی‌دارد.

    جسد را از روی کپسول رضا کپی می‌کنیم؛ بدون این، جسد هم `TalkInteractable`
    دارد و می‌شود با مرده حرف زد — با دیالوگ رضا.
    """
    body = None
    for doc in builder.docs:
        if doc["cls"] == "1" and doc["id"] == go:
            body = doc

    if body is None:
        return

    doomed = []
    for doc in builder.docs:
        if doc["cls"] != "114":
            continue
        if re.search(r"m_GameObject: \{fileID: " + go + r"\}", doc["body"]):
            doomed.append(doc["id"])

    for component in doomed:
        body["body"] = body["body"].replace(
            "  - component: {fileID: " + component + "}\n", "")

    builder.docs[:] = [doc for doc in builder.docs if doc["id"] not in doomed]


def inspectable(builder, root, name, position, scale, material, display_name,
                subtype, dialogue_guid, node, ui_id):
    """یک شیء فیزیکی که با نگاه کردن یک دیالوگ کوتاه بازی می‌کند.

    همان دو کامپوننتی که روی NPCهاست، فقط با `SimpleInteractable` به‌جای
    `TalkInteractable`: فعلِ «استفاده» نه «صحبت».
    """
    go, _ = builder.clone(CUBE, name, position, scale, material, root)

    (starter,) = builder.take(1)

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
            "  m_Script: {fileID: 11500000, guid: " + SIMPLE_SCRIPT_GUID + ", type: 3}\n"
            "  m_Name: \n"
            "  m_EditorClassIdentifier: Assembly-CSharp::TehranNoir.Interaction.SimpleInteractable\n"
            "  action: 0\n"
            "  interactionPoint: {fileID: 0}\n"
            "  maxDistance: 2.5\n"
            "  maxAngle: 360\n"
            '  displayName: "' + kit.yaml_text(display_name) + '"\n'
            '  subtype: "' + kit.yaml_text(subtype) + '"\n'
            "  outline: {fileID: 0}\n"
            "  dialogueStarter: {fileID: " + starter + "}\n"
            "  logInteraction: 1\n\n")

    builder.add("114", starter,
                "MonoBehaviour:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: " + go + "}\n"
                "  m_Enabled: 1\n"
                "  m_EditorHideFlags: 0\n"
                "  m_Script: {fileID: 11500000, guid: " + STARTER_SCRIPT_GUID + ", type: 3}\n"
                "  m_Name: \n"
                "  m_EditorClassIdentifier: Assembly-CSharp::TehranNoir.Dialogue.DialogueStarter\n"
                "  dialogueFile: {fileID: 4900000, guid: " + dialogue_guid + ", type: 3}\n"
                "  startNodeId: " + node + "\n"
                "  dialogueUI: {fileID: " + ui_id + "}\n\n")

    for doc in builder.docs:
        if doc["cls"] == "1" and doc["id"] == go:
            doc["body"] = doc["body"].replace(
                "  m_Layer: 0",
                "  - component: {fileID: " + starter + "}\n  m_Layer: 0", 1)

    return go


def tint(builder, light_go, color):
    """رنگ یک نور را عوض می‌کند؛ `Builder.light` همیشه گرم است."""
    for doc in builder.docs:
        if doc["cls"] != "108":
            continue
        if re.search(r"m_GameObject: \{fileID: " + light_go + r"\}", doc["body"]):
            doc["body"] = re.sub(r"m_Color: \{[^}]*\}", "m_Color: " + color,
                                 doc["body"], count=1)


def dim_sun(docs, sun_go, intensity, color):
    for doc in docs:
        if doc["cls"] != "108":
            continue
        if not re.search(r"m_GameObject: \{fileID: " + sun_go + r"\}", doc["body"]):
            continue
        doc["body"] = re.sub(r"m_Intensity: [0-9.]+", "m_Intensity: " + str(intensity),
                             doc["body"], count=1)
        doc["body"] = re.sub(r"m_Color: \{[^}]*\}", "m_Color: " + color,
                             doc["body"], count=1)


def main():
    kit.ensure_folder_meta(MATERIAL_FOLDER)
    kit.write_materials(MATERIAL_FOLDER, MATERIALS)

    guides = dict((name, kit.guid_of(DIALOGUE_FOLDER + "/" + name + ".txt"))
                  for name in ("officer_hilton", "maid_hilton", "neighbor_hilton",
                               "manager_hilton", "ashtray_814"))

    with io.open(SOURCE, encoding="utf-8") as handle:
        header, docs = kit.split_scene(handle.read())

    drop = set()
    for root in (OFFICE, REZA, CUBE):
        drop |= kit.collect_subtree(docs, root)

    templates = {doc["id"]: doc for doc in docs}
    docs = [doc for doc in docs if doc["id"] not in drop]

    builder = kit.Builder(docs, templates, WALL, LIGHT)

    # صبح روز بعد؛ خدمتکار ساعت ۶:۲۰ جسد را پیدا کرده و پزشک ۷:۰۵ آمده است.
    kit.set_clock(docs, CLOCK_COMPONENT, 1352, 5, 14, 6, 40)
    kit.set_player(docs, PLAYER, (0, 1, -9.2))
    kit.set_environment(docs, SKY, 1.0, fog=True)

    for doc in docs:
        if doc["cls"] == "104":
            doc["body"] = re.sub(r"m_FogColor: \{[^}]*\}", "m_FogColor: " + FOG,
                                 doc["body"], count=1)

    dim_sun(docs, SUN, 0.06, "{r: 0.35, g: 0.42, b: 0.6, a: 1}")

    root_id, root_transform = builder_root(builder, "HiltonFloor8")

    wall = kit.guid_of(MATERIAL_FOLDER + "/M_814Wall.mat")
    floor = kit.guid_of(MATERIAL_FOLDER + "/M_814Floor.mat")
    carpet = kit.guid_of(MATERIAL_FOLDER + "/M_814Carpet.mat")
    wood = kit.guid_of(MATERIAL_FOLDER + "/M_814Wood.mat")
    door = kit.guid_of(MATERIAL_FOLDER + "/M_814Door.mat")
    brass = kit.guid_of(MATERIAL_FOLDER + "/M_814Brass.mat")
    sheet = kit.guid_of(MATERIAL_FOLDER + "/M_814Sheet.mat")
    blood = kit.guid_of(MATERIAL_FOLDER + "/M_814Blood.mat")
    night = kit.guid_of(MATERIAL_FOLDER + "/M_814Night.mat")
    tape = kit.guid_of(MATERIAL_FOLDER + "/M_814Tape.mat")
    suit = kit.guid_of(MATERIAL_FOLDER + "/M_814Suit.mat")
    uniform = kit.guid_of(MATERIAL_FOLDER + "/M_814Uniform.mat")
    police = kit.guid_of(MATERIAL_FOLDER + "/M_814Police.mat")
    skin = kit.guid_of(MATERIAL_FOLDER + "/M_814Skin.mat")
    victim = kit.guid_of(MATERIAL_FOLDER + "/M_814Victim.mat")

    # ۱) پوسته‌ی راهرو — از آسانسور جنوبی تا پنجره‌ی شمالی
    builder.box("Floor_Corridor", (0, -0.05, 0), (3.2, 0.1, 22), floor, root_transform)
    ceiling_corridor, _ = builder.box("Ceiling_Corridor", (0, 2.65, 0), (3.4, 0.1, 22), wall, root_transform)
    kit.cutaway_piece(builder, ceiling_corridor, 1, 0)
    builder.box("Wall_S", (0, 1.3, -11), (3.4, 2.6, 0.2), wall, root_transform)
    builder.box("Wall_N", (0, 1.3, 11), (3.4, 2.6, 0.2), wall, root_transform)
    builder.box("Wall_E_S", (1.6, 1.3, -5.85), (0.2, 2.6, 10.3), wall, root_transform)
    builder.box("Wall_E_N", (1.6, 1.3, 5.85), (0.2, 2.6, 10.3), wall, root_transform)
    builder.box("Wall_W_S", (-1.6, 1.3, -1.3), (0.2, 2.6, 19.4), wall, root_transform)
    builder.box("Wall_W_N", (-1.6, 1.3, 9.4), (0.2, 2.6, 3.2), wall, root_transform)
    builder.box("Lintel_814", (1.6, 2.35, 0), (0.24, 0.5, 1.4), wall, root_transform)
    builder.box("Lintel_Office", (-1.6, 2.35, 7.3), (0.24, 0.5, 1.0), wall, root_transform)

    builder.box("Carpet_Runner", (0, 0.006, 0), (1.9, 0.02, 21), carpet, root_transform)

    # ۲) اتاق ۸۱۴ — شرق راهرو
    builder.box("Floor_814", (4.6, -0.05, 0), (6.0, 0.1, 5.6), floor, root_transform)
    ceiling_814, _ = builder.box("Ceiling_814", (4.6, 2.65, 0), (6.0, 0.1, 5.6), wall, root_transform)
    kit.cutaway_piece(builder, ceiling_814, 1, 0)
    builder.box("Wall_814_N", (4.6, 1.3, 2.8), (6.2, 2.6, 0.2), wall, root_transform)
    builder.box("Wall_814_S", (4.6, 1.3, -2.8), (6.2, 2.6, 0.2), wall, root_transform)
    builder.box("Wall_814_E_S", (7.6, 1.3, -1.95), (0.2, 2.6, 1.7), wall, root_transform)
    builder.box("Wall_814_E_N", (7.6, 1.3, 2.15), (0.2, 2.6, 1.3), wall, root_transform)
    builder.box("Sill_814", (7.55, 0.9, 0.2), (0.3, 0.1, 2.6), wood, root_transform)
    builder.box("Header_814", (7.6, 2.35, 0.2), (0.2, 0.5, 2.6), wall, root_transform)
    builder.box("Sash_814_A", (7.58, 1.5, -0.65), (0.06, 1.2, 0.85), wood, root_transform)
    builder.box("Sash_814_B", (7.58, 1.5, 1.22), (0.06, 1.2, 0.5), wood, root_transform)
    builder.box("Night_Outside", (8.7, 1.6, 0.2), (0.1, 4, 10), night, root_transform)

    # ۳) دفتر مدیر طبقه — غرب راهرو، ته شمالی
    builder.box("Floor_Office", (-3.5, -0.05, 7.5), (3.8, 0.1, 4.2), floor, root_transform)
    ceiling_office, _ = builder.box("Ceiling_Office", (-3.5, 2.65, 7.5), (3.8, 0.1, 4.2), wall, root_transform)
    kit.cutaway_piece(builder, ceiling_office, 1, 0)
    builder.box("Wall_Office_W", (-5.4, 1.3, 7.5), (0.2, 2.6, 4.2), wall, root_transform)
    builder.box("Wall_Office_N", (-3.5, 1.3, 9.6), (3.8, 2.6, 0.2), wall, root_transform)
    builder.box("Wall_Office_S", (-3.5, 1.3, 5.4), (3.8, 2.6, 0.2), wall, root_transform)

    # ۴) درها
    for number, z in ((812, -4.5), (816, 4.5)):
        builder.box("Door_%d" % number, (1.47, 1.05, z), (0.06, 2.1, 0.9), door, root_transform)
        builder.box("Plate_%d" % number, (1.43, 1.62, z), (0.02, 0.1, 0.2), brass, root_transform)

    # درِ ۸۱۴ باز است؛ لولا سمت جنوب، برگ در دیوارِ اتاق خوابیده.
    builder.box("Door_814_Open", (2.05, 1.05, -0.7), (0.9, 2.1, 0.06), door, root_transform)
    builder.box("Door_Office_Open", (-2.05, 1.05, 7.85), (0.9, 2.1, 0.06), door, root_transform)
    builder.box("Tape_814_A", (1.62, 1.48, 0), (0.02, 0.07, 1.4), tape, root_transform)
    builder.box("Tape_814_B", (1.62, 1.14, 0), (0.02, 0.07, 1.4), tape, root_transform)

    # ۵) اثاثه‌ی اتاق جرم
    builder.box("Bed_Frame", (5.9, 0.3, -1.75), (2.8, 0.22, 2.0), wood, root_transform)
    builder.box("Mattress", (5.9, 0.48, -1.75), (2.6, 0.16, 1.88), sheet, root_transform)
    builder.box("Pillow", (6.85, 0.6, -1.75), (0.55, 0.12, 1.5), sheet, root_transform)
    builder.box("Blanket", (5.2, 0.59, -1.75), (1.6, 0.06, 1.9), carpet, root_transform)
    builder.box("Headboard", (7.3, 0.85, -1.75), (0.12, 0.9, 2.0), wood, root_transform)
    builder.box("Nightstand", (4.3, 0.5, -1.75), (0.6, 0.5, 0.6), wood, root_transform)
    builder.box("Lamp_Base", (4.3, 0.8, -1.75), (0.1, 0.12, 0.1), brass, root_transform)
    builder.box("Lamp_Shade", (4.3, 0.95, -1.75), (0.26, 0.18, 0.26), brass, root_transform)
    builder.box("Desk_814", (2.6, 0.72, 2.4), (1.4, 0.06, 0.6), wood, root_transform)
    builder.box("Desk_814_Leg_A", (1.98, 0.36, 2.4), (0.08, 0.72, 0.5), wood, root_transform)
    builder.box("Desk_814_Leg_B", (3.22, 0.36, 2.4), (0.08, 0.72, 0.5), wood, root_transform)
    builder.box("Chair_814", (2.6, 0.45, 1.7), (0.45, 0.9, 0.45), wood, root_transform)
    builder.box("Chair_814_Back", (2.6, 0.85, 1.5), (0.45, 0.5, 0.06), wood, root_transform)
    builder.box("Wardrobe", (2.2, 0.9, -2.55), (1.2, 1.8, 0.45), wood, root_transform)
    builder.box("Suitcase", (3.55, 0.16, -0.35), (0.62, 0.32, 0.42), wood, root_transform)
    builder.box("Ashtray_Bent_Corner", (4.26, 0.05, -0.6), (0.09, 0.09, 0.09), brass, root_transform)

    # جسد؛ یک کپسول خوابانده‌شده و یک لکه.
    builder.box("Blood_Stain", (5.3, 0.004, 0.2), (1.2, 0.01, 1.4), blood, root_transform)
    body, _ = builder.clone(REZA, "Body_814", (5.3, 0.33, 0.15), (1.0, 0.42, 1.0),
                            victim, root_transform)
    strip_interactables(builder, body)
    builder.box("Body_Head", (6.1, 0.13, 0.05), (0.22, 0.22, 0.22), skin, root_transform)

    # ۶) اثاثه‌ی راهرو و دفتر
    builder.box("Lift_Door_L", (-0.5, 1.1, -10.86), (0.92, 2.2, 0.08), brass, root_transform)
    builder.box("Lift_Door_R", (0.5, 1.1, -10.86), (0.92, 2.2, 0.08), brass, root_transform)
    builder.box("Lift_Header", (0, 2.24, -10.84), (2.1, 0.12, 0.14), brass, root_transform)
    builder.box("Lift_Call", (-1.3, 1.2, -10.84), (0.1, 0.2, 0.06), brass, root_transform)
    builder.box("Stair_Door", (-1.48, 1.05, -8.5), (0.06, 2.1, 0.9), door, root_transform)
    builder.box("Maid_Cart", (1.05, 0.55, -8.1), (0.9, 0.08, 0.5), wood, root_transform)
    builder.box("Maid_Cart_Leg_A", (0.68, 0.27, -8.28), (0.06, 0.55, 0.06), wood, root_transform)
    builder.box("Maid_Cart_Leg_B", (1.42, 0.27, -8.28), (0.06, 0.55, 0.06), wood, root_transform)
    builder.box("Maid_Cart_Linen", (1.05, 0.74, -8.1), (0.7, 0.28, 0.4), sheet, root_transform)
    builder.box("Corridor_Plant", (-1.15, 0.36, 3.2), (0.5, 0.72, 0.5), wood, root_transform)
    builder.box("Corridor_Bench", (-1.15, 0.42, -1.9), (0.5, 0.45, 1.4), wood, root_transform)
    builder.box("Wall_Table", (-1.15, 0.72, -2.9), (0.8, 0.06, 0.55), wood, root_transform)
    builder.box("Wall_Table_Leg", (-1.15, 0.36, -2.9), (0.6, 0.72, 0.08), wood, root_transform)

    builder.box("Desk_Office", (-4.6, 0.72, 7.5), (0.9, 0.06, 1.9), wood, root_transform)
    builder.box("Desk_Office_Leg_A", (-4.6, 0.36, 6.75), (0.7, 0.72, 0.08), wood, root_transform)
    builder.box("Desk_Office_Leg_B", (-4.6, 0.36, 8.25), (0.7, 0.72, 0.08), wood, root_transform)
    builder.box("Chair_Office", (-5.15, 0.45, 7.5), (0.45, 0.9, 0.45), wood, root_transform)
    builder.box("Cabinet_Office", (-5.05, 0.9, 6.0), (0.55, 1.8, 0.7), wood, root_transform)

    # ۷) نور
    for index, z in enumerate((-8.0, -4.0, 0.0, 4.0, 8.0), start=1):
        builder.light("Light_Corridor_%d" % index, (0, 2.4, z), 1.3, 7.0, root_transform)

    builder.light("Light_Room", (4.6, 2.4, 0.4), 1.5, 8.0, root_transform)
    builder.light("Light_Lamp", (4.3, 1.05, -1.75), 2.2, 4.5, root_transform)
    window_light, _ = builder.light("Light_Window", (7.0, 1.7, 0.2), 0.9, 5.0, root_transform)
    tint(builder, window_light, "{r: 0.45, g: 0.55, b: 0.75, a: 1}")
    builder.light("Light_Office", (-3.5, 2.3, 7.5), 1.5, 6.0, root_transform)

    # ۸) آدم‌ها
    moradi, _ = builder.clone(REZA, "Sgt_Moradi", (0.55, 1.0, -1.3), (0.95, 1.0, 0.95),
                              police, root_transform)
    name_npc(builder, moradi, "گروهبان مرادی|Sgt. Moradi", "مأمور صحنه | Scene officer",
             guides["officer_hilton"])

    narges, _ = builder.clone(REZA, "Maid_Narges", (0.7, 1.0, -7.6), (0.9, 0.95, 0.9),
                              uniform, root_transform)
    name_npc(builder, narges, "نرگس|Narges", "خدمتکار طبقه | Floor maid",
             guides["maid_hilton"])

    tehrani, _ = builder.clone(REZA, "Guest_Tehrani", (0.75, 1.0, 4.2), (0.9, 1.0, 0.9),
                               suit, root_transform)
    name_npc(builder, tehrani, "آقای تهرانی|Mr. Tehrani", "مهمان ۸۱۶ | Guest, 816",
             guides["neighbor_hilton"])

    rahimi, _ = builder.clone(REZA, "Manager_Rahimi", (-4.5, 1.0, 7.5), (0.95, 1.0, 0.95),
                              suit, root_transform)
    name_npc(builder, rahimi, "رحیمی|Rahimi", "مدیر طبقه | Floor manager",
             guides["manager_hilton"])

    # ۹) چهار کاغذ
    paper(builder, root_transform, "Doc_KeyRegister", (-1.2, 0.775, -2.8), (0.26, 0.02, 0.32),
          "دفتر کلید (رونوشت)|Key register (copy)", "پذیرش | Reception",
          "Documents/hilton_key_register", "flag.hilton.ev.key_register")

    paper(builder, root_transform, "Doc_Report", (-1.02, 0.775, -3.05), (0.24, 0.02, 0.3),
          "گزارش پرونده|Case report", "گروهبان مرادی | Sgt. Moradi",
          "Documents/hilton_report", "flag.hilton.case.reported")

    paper(builder, root_transform, "Doc_Coroner", (4.52, 0.815, -1.62), (0.2, 0.012, 0.26),
          "برگه‌ی پزشک قانونی|Coroner's slip", "اتاق ۸۱۴ | Room 814",
          "Documents/hilton_coroner", "flag.hilton.ev.coroner")

    paper(builder, root_transform, "Doc_Tally", (2.45, 0.78, 2.4), (0.26, 0.02, 0.34),
          "حساب دست‌نویس|Handwritten tally", "اتاق ۸۱۴ | Room 814",
          "Documents/hilton_tally", "flag.hilton.ev.tally")

    ui_id = "0"
    for doc in builder.docs:
        if doc["cls"] == "114" and "TehranNoir.Dialogue.DialogueStarter" in doc["body"]:
            found = re.search(r"dialogueUI: \{fileID: (\d+)\}", doc["body"])
            if found:
                ui_id = found.group(1)
                break

    inspectable(builder, root_transform, "Ashtray_814_Case",
                (4.15, 0.06, -0.5), (0.3, 0.12, 0.3), brass,
                "سینی سیگار برنجی|Brass ashtray", "اتاق ۸۱۴ | Room 814",
                guides["ashtray_814"], "", ui_id)

    # ۱۰) برش — هر سه سقف در همان لحظه که دوربین از بالای سر نگاه می‌کند
    # برداشته می‌شوند. بدون این، تصویر فقط یک تخته‌ی قهوه‌ای است.
    kit.building_cutaway(
        builder, root_id, builder.transform_id(PLAYER),
        (1.6, 1.3, 0.0), (15.0, 2.6, 23.0),
        floor_height=2.9, ground_level=0.0, exit_margin=0.5)

    # ۱۱) نشانگر صحنه — بدون این، کار «اتاق ۸۱۴» هرگز در فهرست کارها نمی‌آید.
    kit.component_object(
        builder, "0", "SceneMarker_Hilton814", MARKER_GUID,
        "TehranNoir.World.SceneMarker",
        "  placeId: hilton814\n  logMarker: 1")

    # تست برش، یک بار با شروع صحنه. در بیلد خاموشش کن.
    kit.component_object(
        builder, "0", "CutawaySelfTest", CUTAWAY_TEST_GUID,
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
