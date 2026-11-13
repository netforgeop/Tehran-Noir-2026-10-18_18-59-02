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
