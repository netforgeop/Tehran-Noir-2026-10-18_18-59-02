using System;

namespace TehranNoir.Items
{
    /// <summary>
    /// دسته‌ی آیتم: دقیق‌تر از ItemKind و مستقل از رابط کاربری.
    /// دسته‌ی تازه فقط یک مقدار تازه در همین enum است؛ هیچ کلاسی عوض نمی‌شود.
    /// </summary>
    public enum ItemCategory
    {
        Miscellaneous = 0,
        Weapons = 1,
        Ammunition = 2,
        Keys = 3,
        Documents = 4,
        Evidence = 5,
        Consumables = 6,
        Tools = 7,
        QuestItems = 8
    }

    /// <summary>
    /// دسته‌ی نمایشی قدیمی. تب «وسایل» فقط همین سه را می‌شناسد
    /// (فیلترهای همه / اسناد / کلیدها / بقیه)، پس دسته‌های تازه به «بقیه» می‌روند
    /// تا وقتی که رابط کاربری آگاهانه عوض شود.
    /// </summary>
    public enum ItemKind
    {
        Document = 0,
        Key = 1,
        Item = 2
    }

    /// <summary>
    /// گرامر متن آیتم‌ها: یک جا، هم برای خواندن دسته و هم برای خواندن پرچم‌ها.
    /// اگر روزی منابع از فایل به ScriptableObject عوض شود، فقط همین کلاس و
    /// ItemCatalog دست می‌خورند.
    /// </summary>
    public static class ItemCategories
    {
        /// <summary>نام فیلد چهارم در items.txt. ناشناس = Miscellaneous + هشدار.</summary>
        public static ItemCategory Parse(string raw)
        {
            string text = (raw ?? string.Empty).Trim().ToLowerInvariant();

            switch (text)
            {
                case "weapon": return ItemCategory.Weapons;
                case "ammo": return ItemCategory.Ammunition;
                case "key": return ItemCategory.Keys;
                case "document": return ItemCategory.Documents;
                case "evidence": return ItemCategory.Evidence;
                case "consumable": return ItemCategory.Consumables;
                case "tool": return ItemCategory.Tools;
                case "quest": return ItemCategory.QuestItems;
                case "item": return ItemCategory.Miscellaneous;
                case "misc": return ItemCategory.Miscellaneous;
                default: return ItemCategory.Miscellaneous;
            }
        }

        /// <summary>آیا این متن یک دسته‌ی شناخته‌شده است؟ (برای ابزار بررسی داده)</summary>
        public static bool IsKnown(string raw)
        {
            string text = (raw ?? string.Empty).Trim().ToLowerInvariant();

            switch (text)
            {
                case "weapon":
                case "ammo":
                case "key":
                case "document":
                case "evidence":
                case "consumable":
                case "tool":
                case "quest":
                case "item":
                case "misc":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>دسته‌ی دقیق به سبد نمایشی قدیمی نگاشت می‌شود.</summary>
        public static ItemKind KindOf(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Documents: return ItemKind.Document;
                case ItemCategory.Keys: return ItemKind.Key;
                default: return ItemKind.Item;
            }
        }

        /// <summary>کلید ترجمه: «char.items.kind.document» و مانند آن.</summary>
        public static string LocKey(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapons: return "weapon";
                case ItemCategory.Ammunition: return "ammo";
                case ItemCategory.Keys: return "key";
                case ItemCategory.Documents: return "document";
                case ItemCategory.Evidence: return "evidence";
                case ItemCategory.Consumables: return "consumable";
                case ItemCategory.Tools: return "tool";
                case ItemCategory.QuestItems: return "quest";

                default: return "item";
            }
        }

        /// <summary>
        /// «flags=use,drop,trade,store,examine,destroy» یا «flags=none» یا «flags=all».
        /// خالی یا خراب = همه‌ی پرچم‌ها، چون آیتم معمولی باید همه‌کاره باشد.
        /// </summary>
        public static ItemFlags ParseFlags(string raw)
        {
            string text = (raw ?? string.Empty).Trim().ToLowerInvariant();

            if (text.Length == 0 || text == "all" || text == "*") return ItemFlags.All;
            if (text == "none" || text == "-") return ItemFlags.None;

            ItemFlags flags = ItemFlags.None;
            string[] parts = text.Split(',');

            for (int i = 0; i < parts.Length; i++)
            {
                ItemFlags flag = ParseFlag(parts[i].Trim());

                if (flag != ItemFlags.None) flags |= flag;
            }

            return flags;
        }

        private static ItemFlags ParseFlag(string name)
        {
            switch (name)
            {
                case "use": return ItemFlags.Usable;
                case "drop": return ItemFlags.Droppable;
                case "trade": return ItemFlags.Tradable;
                case "store": return ItemFlags.Storable;
                case "examine": return ItemFlags.Examinable;
                case "destroy": return ItemFlags.Destroyable;

                default: return ItemFlags.None;
            }
        }

        /// <summary>برعکس ParseFlags؛ برای ذخیره و برای نمایش در ابزار بررسی.</summary>
        public static string ToText(ItemFlags flags)
        {
            if (flags == ItemFlags.None) return "none";
            if (flags == ItemFlags.All) return "all";

            string text = string.Empty;

            if ((flags & ItemFlags.Usable) != 0) text += "use,";
            if ((flags & ItemFlags.Droppable) != 0) text += "drop,";
            if ((flags & ItemFlags.Tradable) != 0) text += "trade,";
            if ((flags & ItemFlags.Storable) != 0) text += "store,";
            if ((flags & ItemFlags.Examinable) != 0) text += "examine,";
            if ((flags & ItemFlags.Destroyable) != 0) text += "destroy,";

            return text.TrimEnd(',');
        }
    }
}
