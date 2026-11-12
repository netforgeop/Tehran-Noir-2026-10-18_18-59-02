#!/usr/bin/env python3
"""بررسی فایل آیتم‌ها — همان قواعدی که ItemCatalog.Parse در بازی اجرا می‌کند.

    python3 tools/itemcheck.py
    python3 tools/itemcheck.py Assets/_Project/Resources/Items/items.txt

چه چیزی را می‌گیرد:
  * شناسه‌ی خالی، دارای فاصله یا تکراری
  * دسته یا فعل ناشناس
  * بیشترین تعداد نامعتبر
  * گزینه‌ی بی‌شکل (بدون =) یا کلید ناشناس
  * وزن/ارزش/پرچم/مصرف نامعتبر
  * اثر بی‌عملگر (مثلاً `flag.x true`)
  * آیکون، Prefab یا سندی که مسیرش وجود ندارد

خروجی: یک خط برای هر مشکل، و در آخر شمارش. کد خروج ۱ یعنی مشکل هست.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT = os.path.join(ROOT, "Assets/_Project/Resources/Items/items.txt")
RESOURCES = os.path.join(ROOT, "Assets/_Project/Resources")

CATEGORIES = {
    "document", "key", "weapon", "ammo", "evidence",
    "consumable", "tool", "quest", "item", "misc",
}

VERBS = {
    "use", "equip", "unequip", "drop", "read", "open", "close",
    "consume", "unlock", "give", "store", "reload", "examine",
}

FLAGS = {"use", "drop", "trade", "store", "examine", "destroy", "equip"}
FLAG_ALIASES = {"all", "none", "-", "*"}

SIMPLE_KEYS = {"weight", "value", "flags", "icon", "prefab", "sub", "desc.fa", "desc.en", "doc"}

BOOLEANS = {"1", "0", "true", "false", "yes", "no", "on", "off"}

RULE_RE = re.compile(r"^(?P<verb>[a-z]+)\.(?P<field>if|do|consume|target)(?:\.(?P<order>\d+))?$")

EXTENSIONS = ("", ".png", ".jpg", ".jpeg")


INVISIBLE = "\u200e\u200f\ufeff\u202a\u202b\u202c\u202d\u202e\u2066\u2067\u2068\u2069"


def clean(text):
    """کاراکترهای نامرئی را برمی‌دارد و «=» تمام‌عرض را عادی می‌کند."""
    if not text:
        return ""

    out = []

    for char in text:
        if char in INVISIBLE:
            continue

        if char == "\uff1d":
            char = "="

        out.append(char)

    return "".join(out).strip()


def normalize(text):
    return clean(text).lower()


def resource_exists(path, extension=".prefab"):
    clean = path.strip()
    base = os.path.join(RESOURCES, clean)

    if extension == "":
        if os.path.isdir(base):
            return True

        for ext in EXTENSIONS:
            if os.path.isfile(base + ext):
                return True

        return False

    return os.path.isfile(base + extension)


def has_operator(effect):
    for index, char in enumerate(effect):
        if char in "=+-" and index > 0:
            return True

    return False


# ---------------------------------------------------------------------------
#  گرامر شرط — همان چیزی که DialogueCondition می‌خواند
#  اگر شرطی خوانده نشود، قاعده بی‌صدا حذف می‌شود؛ پس همان‌جا باید گرفته شود.
# ---------------------------------------------------------------------------

OPERATORS = ("==", "!=", ">=", "<=", ">", "<", "=")
KEYWORDS = ("and", "or", "not")


class ConditionError(Exception):
    pass


def tokenize_condition(source):
    tokens = []
    index = 0
    text = source or ""

    while index < len(text):
        char = text[index]

        if char.isspace():
            index += 1
            continue

        pair = text[index:index + 2]

        if pair in ("==", "!=", ">=", "<="):
            tokens.append(("op", pair))
            index += 2
            continue

        if char in "><=":
            tokens.append(("op", char))
            index += 1
            continue

        if char == "(":
            tokens.append(("open", char))
            index += 1
            continue

        if char == ")":
            tokens.append(("close", char))
            index += 1
            continue

        if char in "\"'":
            quote = char
            cursor = index + 1
            buffer = []

            while cursor < len(text) and text[cursor] != quote:
                buffer.append(text[cursor])
                cursor += 1

            tokens.append(("text", "".join(buffer)))
            index = cursor + 1
            continue

        if char.isdigit():
            cursor = index
            while cursor < len(text) and (text[cursor].isdigit() or text[cursor] == "."):
                cursor += 1
            tokens.append(("number", text[index:cursor]))
            index = cursor
            continue

        if char.isalpha() or char == "_" or "\u0600" <= char <= "\u06ff":
            cursor = index
            while cursor < len(text) and (
                text[cursor].isalnum() or text[cursor] in "_." or "\u0600" <= text[cursor] <= "\u06ff"
            ):
                cursor += 1
            tokens.append(("word", text[index:cursor]))
            index = cursor
            continue

        raise ConditionError("کاراکتر ناشناخته: «%s»" % char)

    return tokens


class ConditionParserMirror:
    def __init__(self, tokens):
        self.tokens = tokens
        self.position = 0

    def peek(self):
        return self.tokens[self.position] if self.position < len(self.tokens) else None

    def take(self):
        token = self.peek()
        if token is not None:
            self.position += 1
        return token

    def keyword(self, name):
        token = self.peek()
        return token is not None and token[0] == "word" and token[1].lower() == name

    def parse_all(self):
        self.parse_or()
        token = self.peek()
        if token is not None:
            raise ConditionError("بخش اضافه در شرط: «%s»" % token[1])

    def parse_or(self):
        self.parse_and()
        while self.keyword("or"):
            self.take()
            self.parse_and()

    def parse_and(self):
        self.parse_not()
        while self.keyword("and"):
            self.take()
            self.parse_not()

    def parse_not(self):
        if self.keyword("not"):
            self.take()
            self.parse_not()
            return
        self.parse_compare()

    def parse_compare(self):
        self.parse_primary()
        token = self.peek()
        if token is not None and token[0] == "op":
            op = token[1]
            if op == "=":
                op = "=="
            if op in ("==", "!=", ">", "<", ">=", "<="):
                self.take()
                self.parse_primary()

    def parse_primary(self):
        token = self.take()
        if token is None:
            raise ConditionError("شرط ناقص است")

        if token[0] in ("number", "text", "word"):
            return

        if token[0] == "open":
            self.parse_or()
            closing = self.take()
            if closing is None or closing[0] != "close":
                raise ConditionError("پرانتز بسته نشده")
            return

        raise ConditionError("عبارت شرط نامعتبر: «%s»" % token[1])


def check_condition(raw, problems, line_no, label):
    try:
        ConditionParserMirror(tokenize_condition(raw)).parse_all()
    except ConditionError as error:
        problems.append((line_no, "%s شرط خوانده نمی‌شود: %s  →  «%s»" % (label, error, raw)))


def check_effects(raw, problems, line_no, label):
    for piece in raw.split(";"):
        piece = piece.strip()

        if not piece:
            continue

        key = piece.split("=")[0].split("+")[0].split("-")[0].strip()

        if not key:
            problems.append((line_no, "%s اثر بی‌نام دارد: %s" % (label, piece)))
            continue

        if "." not in key and key not in {
            "time", "weather", "energy", "fear", "sanity", "trust",
            "playerAge", "season", "year", "month", "day", "hour",
        }:
            problems.append((line_no, "%s کلیدِ اثر بدون پیشوند: %s" % (label, key)))

        if not has_operator(piece):
            problems.append((line_no, "%s اثر بدون عملگر: %s" % (label, piece)))


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT

    if not os.path.isfile(path):
        print("فایل پیدا نشد: " + path)
        return 1

    body = io.open(path, encoding="utf-8-sig").read()
    lines = body.splitlines()

    problems = []
    seen = {}
    items = 0

    for index, raw in enumerate(lines, start=1):
        text = raw.strip()

        if not text or text.startswith("#"):
            continue

        parts = text.split("|")

        if len(parts) < 4:
            problems.append((index, "کمتر از ۴ فیلد: " + text))
            continue

        item_id = normalize(parts[0])

        if not item_id:
            problems.append((index, "شناسه خالی است"))
            continue

        if " " in item_id or "\t" in item_id:
            problems.append((index, "شناسه فاصله دارد: " + item_id))
            continue

        if item_id in seen:
            problems.append((index, "شناسه تکراری «%s»؛ قبلاً خط %d" % (item_id, seen[item_id])))
            continue

        seen[item_id] = index

        category = normalize(parts[3])

        if category not in CATEGORIES:
            problems.append((index, "دسته‌ی ناشناس «%s» (یکی از: %s)" % (category, ", ".join(sorted(CATEGORIES)))))

        if len(parts) >= 5 and parts[4].strip():
            try:
                stack = int(parts[4].strip())
            except ValueError:
                stack = 0

            if stack < 1:
                problems.append((index, "بیشترین تعداد نامعتبر: " + parts[4].strip()))

        for token in parts[5:]:
            token = clean(token)

            if not token:
                continue

            if "=" not in token:
                problems.append((index, "گزینه بی‌شکل (باید key=value باشد): " + token))
                continue

            key, value = token.split("=", 1)
            key = normalize(key)
            value = value.strip()

            if key.startswith("state."):
                if len(key) <= len("state."):
                    problems.append((index, "کلید وضعیت خالی: " + token))
                continue

            if key in SIMPLE_KEYS:
                if key == "weight":
                    try:
                        if float(value) < 0:
                            raise ValueError
                    except ValueError:
                        problems.append((index, "وزن نامعتبر: " + value))

                elif key == "value":
                    try:
                        if int(value) < 0:
                            raise ValueError
                    except ValueError:
                        problems.append((index, "ارزش نامعتبر: " + value))

                elif key == "flags":
                    cleaned = normalize(value)
                    if cleaned not in FLAG_ALIASES:
                        for name in cleaned.split(","):
                            name = name.strip()
                            if name and name not in FLAGS:
                                problems.append((index, "پرچم ناشناس: " + name))

                elif key == "icon":
                    if value and not resource_exists(value, ""):
                        problems.append((index, "آیکون پیدا نشد: " + value))

                elif key == "prefab":
                    if value and not resource_exists(value):
                        problems.append((index, "Prefab پیدا نشد: " + value))

                elif key == "doc":
                    if value and not resource_exists(value, ".txt"):
                        problems.append((index, "سند پیدا نشد: " + value))

                continue

            match = RULE_RE.match(key)

            if not match:
                problems.append((index, "کلید ناشناس: " + key))
                continue

            verb = match.group("verb")
            field = match.group("field")

            if verb not in VERBS:
                problems.append((index, "فعل ناشناس: " + verb))
                continue

            if field == "if":
                if not value.strip():
                    problems.append((index, "%s.if خالی است (برای «همیشه» فیلد را ننویس)" % verb))
                else:
                    check_condition(value, problems, index, "%s.if" % verb)

            elif field == "consume":
                if normalize(value) not in BOOLEANS:
                    problems.append((index, "مقدار مصرف باید ۰ یا ۱ باشد: " + value))

            elif field == "do":
                check_effects(value, problems, index, "%s.do" % verb)

        items += 1

    for line_no, message in problems:
        print("  %s | خط %d | %s" % (os.path.basename(path), line_no, message))

    print("lines: %d items: %d problems: %d" % (len(lines), items, len(problems)))

    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
