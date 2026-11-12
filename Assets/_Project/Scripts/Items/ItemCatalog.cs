using System;
using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.Items
{
    /// <summary>
    /// نوع آیتم فقط برای آیکون و دسته‌بندی است؛ هیچ منطقی به آن وابسته نیست.
    /// </summary>
    public enum ItemKind
    {
        Document = 0,
        Key = 1,
        Item = 2
    }

    /// <summary>
    /// تعریف یک آیتم. نامش دوزبانه است تا تب «کاراکتر» با زبان بازی عوض شود.
    /// </summary>
    public sealed class ItemDefinition
    {
        public readonly string Id;
        public readonly string Persian;
        public readonly string English;
        public readonly ItemKind Kind;
        public readonly int MaxStack;

        public ItemDefinition(string id, string persian, string english, ItemKind kind, int maxStack)
        {
            Id = id;
            Persian = string.IsNullOrEmpty(persian) ? id : persian;
            English = string.IsNullOrEmpty(english) ? id : english;
            Kind = kind;
            MaxStack = Mathf.Max(1, maxStack);
        }

        public string Name(bool persian)
        {
            return persian ? Persian : English;
        }
    }

    /// <summary>
    /// فهرست آیتم‌ها از فایل می‌آید، نه از کد: Resources/Items/items.txt
    ///
    ///   id | فارسی | انگلیسی | نوع | بیشترین تعداد
    ///
    /// آیتم تازه = یک خط در همان فایل؛ هیچ کلاسی عوض نمی‌شود.
    /// خط خراب فقط هشدار می‌دهد و رد می‌شود، پس یک غلط تایپی بازی را نمی‌خواباند.
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

            definition = new ItemDefinition(key, key, key, ItemKind.Item, 1);
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

            if (parts.Length >= MinFields + 1)
            {
                string raw = parts[MinFields].Trim();

                if (!int.TryParse(raw, out maxStack) || maxStack < 1) maxStack = 1;
            }

            return new ItemDefinition(id, parts[1].Trim(), parts[2].Trim(), ParseKind(parts[3]), maxStack);
        }

        private static bool HasSpace(string id)
        {
            for (int i = 0; i < id.Length; i++)
                if (char.IsWhiteSpace(id[i])) return true;

            return false;
        }

        private static ItemKind ParseKind(string raw)
        {
            string kind = (raw ?? string.Empty).Trim().ToLowerInvariant();

            if (kind == "document") return ItemKind.Document;
            if (kind == "key") return ItemKind.Key;

            return ItemKind.Item;
        }
    }
}
