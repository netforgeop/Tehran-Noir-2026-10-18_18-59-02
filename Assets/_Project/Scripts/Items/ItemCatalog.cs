using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// فهرست آیتم‌ها از فایل می‌آید، نه از کد: Resources/Items/items.txt
    ///
    ///   id | فارسی | انگلیسی | دسته | بیشترین تعداد | گزینه‌ها
    ///
    /// ستون پنجم اختیاری است و هر گزینه‌ی بعد از آن به شکل key=value:
    ///   weight=0.1  value=2  flags=use,drop  icon=Icons/Keys/archive
    ///   prefab=Prefabs/Items/key  sub=police  desc.fa=...  desc.en=...
    ///
    /// آیتم تازه = یک خط در همان فایل؛ هیچ کلاسی عوض نمی‌شود.
    /// خط خراب فقط هشدار می‌دهد و رد می‌شود، پس یک غلط تایپی بازی را نمی‌خواباند.
    ///
    /// این کلاس تنها راه دسترسی به تعریف‌هاست؛ اگر روزی منبع داده از فایل به
    /// ScriptableObject عوض شود، فقط همین فایل دست می‌خورد و بقیه‌ی کد نه.
    /// </summary>
    public static class ItemCatalog
    {
        public const string ResourcePath = "Items/items";

        /// <summary>بدون فیلد پنجم، تعداد ۱ در نظر گرفته می‌شود؛ پس ۴ فیلد هم قبول است.</summary>
        private const int MinFields = 4;

        private static readonly List<ItemDefinition> Ordered = new List<ItemDefinition>(32);

        private static readonly Dictionary<string, ItemDefinition> Map =
            new Dictionary<string, ItemDefinition>(32, StringComparer.Ordinal);

        /// <summary>شناسه‌های ناشناس هم تعریف می‌گیرند تا هیچ‌جا NullReference نگیریم.</summary>
        private static readonly Dictionary<string, ItemDefinition> Fallbacks =
            new Dictionary<string, ItemDefinition>(4, StringComparer.Ordinal);

        private static bool _loaded;

        public static IReadOnlyList<ItemDefinition> All
        {
            get
            {
                EnsureLoaded();
                return Ordered;
            }
        }

        public static int Count
        {
            get
            {
                EnsureLoaded();
                return Ordered.Count;
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;

            _loaded = true;
            Load();
        }

        /// <summary>خواندن دوباره‌ی فایل؛ برای پنجره‌ی ادیتور و تست.</summary>
        public static void Reload()
        {
            Ordered.Clear();
            Map.Clear();
            Fallbacks.Clear();
            _loaded = false;
            EnsureLoaded();
        }

        public static bool Exists(string id)
        {
            EnsureLoaded();

            return !string.IsNullOrEmpty(id) && Map.ContainsKey(Normalize(id));
        }

        /// <summary>تعریف آیتم؛ برای شناسه‌ی ناشناس هم یک تعریف جایگزین برمی‌گرداند.</summary>
        public static ItemDefinition Get(string id)
        {
            EnsureLoaded();

            string key = Normalize(id);

            ItemDefinition definition;

            if (Map.TryGetValue(key, out definition)) return definition;
            if (Fallbacks.TryGetValue(key, out definition)) return definition;

            definition = new ItemDefinition(key, key, key, ItemCategory.Miscellaneous, 1);
            Fallbacks[key] = definition;

            Debug.LogWarning("[Items] آیتم «" + key + "» در " + ResourcePath + " تعریف نشده؛ موقتاً یک‌تایی فرض شد.");

            return definition;
        }

        /// <summary>
        /// شناسه و کلیدِ دنیا همیشه با حروف کوچک: جلوی دو کلید شدنِ item.Key و item.key را می‌گیرد.
        /// </summary>
        public static string Normalize(string id)
        {
            return string.IsNullOrEmpty(id) ? string.Empty : id.Trim().ToLowerInvariant();
        }

        /* ============================================================
           خواندن فایل
           ============================================================ */

        private static void Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);

            if (asset == null)
            {
                Debug.LogWarning("[Items] فایل " + ResourcePath + ".txt پیدا نشد؛ جیب خالی می‌ماند.");
                return;
            }

            string body = asset.text;

            // بعضی ادیتورها فایل را با BOM ذخیره می‌کنند؛ اگر بماند، شناسه‌ی خط اول
            // یک کاراکتر نامرئی اضافه می‌گیرد و آیتم پیدا نمی‌شود.
            if (body.Length > 0 && body[0] == '\uFEFF') body = body.Substring(1);

            string[] lines = body.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                ItemDefinition definition = Parse(lines[i], i + 1);

                if (definition == null) continue;

                if (Map.ContainsKey(definition.Id))
                {
                    Debug.LogWarning("[Items] «" + definition.Id + "» در خط " + (i + 1) + " تکراری است؛ رد شد.");
                    continue;
                }

                Map[definition.Id] = definition;
                Ordered.Add(definition);
            }
        }

        private sealed class RuleDraft
        {
            public ItemVerb Verb;
            public string WhenSource = string.Empty;
            public Condition When;
            public readonly List<string> Effects = new List<string>(2);
            public bool Consume;
            public string TargetId = string.Empty;
            public int Order;
            public bool Broken;

            public ItemRule Build()
            {
                return new ItemRule(Verb, WhenSource, When, Effects.ToArray(), Consume, TargetId, Order);
            }
        }

        private static ItemDefinition Parse(string line, int number)
        {
            string text = (line ?? string.Empty).Trim();

            if (text.Length == 0 || text[0] == '#') return null;

            string[] parts = text.Split('|');

            if (parts.Length < MinFields)
            {
                Debug.LogWarning("[Items] خط " + number + " فرمت درست ندارد: " + text);
                return null;
            }

            string id = Normalize(parts[0]);

            if (id.Length == 0 || HasSpace(id))
            {
                Debug.LogWarning("[Items] شناسه‌ی خط " + number + " خالی یا دارای فاصله است: " + text);
                return null;
            }

            int maxStack = 1;

            if (parts.Length >= MinFields + 1 && parts[MinFields].Trim().Length > 0)
            {
                string raw = parts[MinFields].Trim();

                if (!int.TryParse(raw, out maxStack) || maxStack < 1) maxStack = 1;
            }

            string subcategory = string.Empty;
            string descriptionPersian = string.Empty;
            string descriptionEnglish = string.Empty;
            string iconPath = string.Empty;
            string prefabPath = string.Empty;
            string documentPath = string.Empty;
            ItemFlags flags = ItemFlags.All;
            float weight = 0f;
            int value = 0;

            List<ItemStatePair> initialState = new List<ItemStatePair>(2);
            List<string> takeEffects = new List<string>(2);
            List<string> dropEffects = new List<string>(2);

            Dictionary<string, RuleDraft> drafts =
                new Dictionary<string, RuleDraft>(4, StringComparer.Ordinal);

            for (int i = MinFields + 1; i < parts.Length; i++)
            {
                string token = parts[i].Trim();

                if (token.Length == 0) continue;

                int split = token.IndexOf('=');

                if (split <= 0)
                {
                    Debug.LogWarning("[Items] خط " + number + " گزینه‌ی بی‌شکل (باید key=value باشد): " + token);
                    continue;
                }

                string key = token.Substring(0, split).Trim().ToLowerInvariant();
                string val = token.Substring(split + 1).Trim();

                if (key == "doc")
                {
                    documentPath = val;
                    continue;
                }

                if (key == "ontake.do")
                {
                    AddEffects(takeEffects, val);
                    continue;
                }

                if (key == "ondrop.do")
                {
                    AddEffects(dropEffects, val);
                    continue;
                }

                if (key.StartsWith(ItemState.Prefix, StringComparison.Ordinal))
                {
                    string stateKey = key.Substring(ItemState.Prefix.Length);

                    if (stateKey.Length == 0)
                    {
                        Debug.LogWarning("[Items] خط " + number + " کلید وضعیت خالی است: " + token);
                        continue;
                    }

                    initialState.Add(new ItemStatePair(stateKey, val));
                    continue;
                }

                ItemVerb verb;
                string field;
                int order;

                if (TrySplitRuleKey(key, out verb, out field, out order))
                {
                    string draftKey = ItemVerbs.Name(verb) + "#" + order.ToString(CultureInfo.InvariantCulture);

                    RuleDraft draft;

                    if (!drafts.TryGetValue(draftKey, out draft))
                    {
                        draft = new RuleDraft();
                        draft.Verb = verb;
                        draft.Order = order;
                        drafts[draftKey] = draft;
                    }

                    switch (field)
                    {
                        case "if":
                            draft.WhenSource = val;

                            Condition condition;
                            string error;

                            if (ConditionParser.TryParse(val, out condition, out error))
                            {
                                draft.When = condition;
                            }
                            else
                            {
                                draft.Broken = true;
                                Debug.LogWarning("[Items] خط " + number + " شرط خراب در «" + key + "»: " + error);
                            }
                            break;

                        case "do":
                            AddEffects(draft.Effects, val);
                            break;

                        case "consume":
                            draft.Consume = ParseBool(val, true);
                            break;

                        case "target":
                            draft.TargetId = val;
                            break;
                    }

                    continue;
                }

                switch (key)
                {
                    case "weight":
                        if (!float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out weight) || weight < 0f)
                        {
                            weight = 0f;
                            Debug.LogWarning("[Items] خط " + number + " وزن نامعتبر: " + val);
                        }
                        break;

                    case "value":
                        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 0)
                        {
                            value = 0;
                            Debug.LogWarning("[Items] خط " + number + " ارزش نامعتبر: " + val);
                        }
                        break;

                    case "flags":
                        flags = ItemCategories.ParseFlags(val);
                        break;

                    case "icon":
                        iconPath = val;
                        break;

                    case "prefab":
                        prefabPath = val;
                        break;

                    case "sub":
                        subcategory = val;
                        break;

                    case "desc.fa":
                        descriptionPersian = val;
                        break;

                    case "desc.en":
                        descriptionEnglish = val;
                        break;

                    default:
                        Debug.LogWarning("[Items] خط " + number + " کلید ناشناس: " + key);
                        break;
                }
            }

            List<ItemRule> rules = new List<ItemRule>(drafts.Count);

            foreach (KeyValuePair<string, RuleDraft> pair in drafts)
                if (!pair.Value.Broken) rules.Add(pair.Value.Build());

            return new ItemDefinition(
                id,
                parts[1].Trim(),
                parts[2].Trim(),
                ItemCategories.Parse(parts[3]),
                maxStack,
                weight,
                value,
                flags,
                iconPath,
                prefabPath,
                subcategory,
                descriptionPersian,
                descriptionEnglish,
                documentPath,
                initialState,
                new ItemRuleSet(rules),
                takeEffects.ToArray(),
                dropEffects.ToArray());
        }

        /// <summary>«read.if» یا «read.do.2» را به فعل، فیلد و شماره می‌شکند.</summary>
        private static bool TrySplitRuleKey(string key, out ItemVerb verb, out string field, out int order)
        {
            verb = ItemVerb.None;
            field = string.Empty;
            order = 0;

            string[] pieces = (key ?? string.Empty).Split('.');

            if (pieces.Length < 2 || pieces.Length > 3) return false;
            if (!ItemVerbs.TryParse(pieces[0], out verb)) return false;

            field = pieces[1];

            if (field != "if" && field != "do" && field != "consume" && field != "target") return false;

            if (pieces.Length == 3 &&
                !int.TryParse(pieces[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out order))
                return false;

            return true;
        }

        /// <summary>اثرها با «؛» جدا می‌شوند: «flag.x = true; rel.reza - 1»</summary>
        private static void AddEffects(List<string> target, string raw)
        {
            if (target == null || string.IsNullOrEmpty(raw)) return;

            string[] pieces = raw.Split(';');

            for (int i = 0; i < pieces.Length; i++)
            {
                string piece = pieces[i].Trim();

                if (piece.Length > 0) target.Add(piece);
            }
        }

        private static bool ParseBool(string raw, bool fallback)
        {
            string text = (raw ?? string.Empty).Trim().ToLowerInvariant();

            if (text == "1" || text == "true" || text == "yes" || text == "on") return true;
            if (text == "0" || text == "false" || text == "no" || text == "off") return false;

            return fallback;
        }

        private static bool HasSpace(string id)
        {
            for (int i = 0; i < id.Length; i++)
                if (char.IsWhiteSpace(id[i])) return true;

            return false;
        }
    }
}
