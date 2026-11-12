using System;

namespace TehranNoir.Items
{
    /// <summary>
    /// کاری که روی یک آیتم انجام می‌شود.
    /// این enum فقط نام فعل‌ها را نگه می‌دارد؛ رفتارشان در داده‌ی آیتم است، نه در کد.
    /// </summary>
    public enum ItemVerb
    {
        None = 0,
        Use = 1,
        Equip = 2,
        Unequip = 3,
        Drop = 4,
        Read = 5,
        Open = 6,
        Close = 7,
        Consume = 8,
        Unlock = 9,
        Give = 10,
        Store = 11,
        Reload = 12,
        Examine = 13
    }

    /// <summary>
    /// خواندن و نوشتن نام فعل‌ها.
    /// در `items.txt` فعل با همین نام کوچک نوشته می‌شود: `read.if=…`، `unlock.do=…`
    /// </summary>
    public static class ItemVerbs
    {
        /// <summary>همه‌ی فعل‌ها به ترتیبی که در رابط کاربری نشان داده می‌شوند.</summary>
        public static readonly ItemVerb[] Ordered =
        {
            ItemVerb.Examine,
            ItemVerb.Use,
            ItemVerb.Read,
            ItemVerb.Open,
            ItemVerb.Equip,
            ItemVerb.Unequip,
            ItemVerb.Consume,
            ItemVerb.Reload,
            ItemVerb.Unlock,
            ItemVerb.Give,
            ItemVerb.Store,
            ItemVerb.Drop,
            ItemVerb.Close
        };

        public static bool TryParse(string raw, out ItemVerb verb)
        {
            verb = ItemVerb.None;

            string text = (raw ?? string.Empty).Trim().ToLowerInvariant();

            if (text.Length == 0) return false;

            switch (text)
            {
                case "use": verb = ItemVerb.Use; return true;
                case "equip": verb = ItemVerb.Equip; return true;
                case "unequip": verb = ItemVerb.Unequip; return true;
                case "drop": verb = ItemVerb.Drop; return true;
                case "read": verb = ItemVerb.Read; return true;
                case "open": verb = ItemVerb.Open; return true;
                case "close": verb = ItemVerb.Close; return true;
                case "consume": verb = ItemVerb.Consume; return true;
                case "unlock": verb = ItemVerb.Unlock; return true;
                case "give": verb = ItemVerb.Give; return true;
                case "store": verb = ItemVerb.Store; return true;
                case "reload": verb = ItemVerb.Reload; return true;
                case "examine": verb = ItemVerb.Examine; return true;

                default: return false;
            }
        }

        /// <summary>آیا این متن یک فعل شناخته‌شده است؟ (برای ابزار بررسی داده)</summary>
        public static bool IsKnown(string raw)
        {
            ItemVerb verb;
            return TryParse(raw, out verb);
        }

        /// <summary>نام کوچک انگلیسی؛ همان چیزی که در فایل نوشته می‌شود.</summary>
        public static string Name(ItemVerb verb)
        {
            switch (verb)
            {
                case ItemVerb.Use: return "use";
                case ItemVerb.Equip: return "equip";
                case ItemVerb.Unequip: return "unequip";
                case ItemVerb.Drop: return "drop";
                case ItemVerb.Read: return "read";
                case ItemVerb.Open: return "open";
                case ItemVerb.Close: return "close";
                case ItemVerb.Consume: return "consume";
                case ItemVerb.Unlock: return "unlock";
                case ItemVerb.Give: return "give";
                case ItemVerb.Store: return "store";
                case ItemVerb.Reload: return "reload";
                case ItemVerb.Examine: return "examine";

                default: return "none";
            }
        }

        /// <summary>کلید ترجمه؛ متن دکمه از Localization می‌آید، نه از Inspector.</summary>
        public static string LocKey(ItemVerb verb)
        {
            return "item.verb." + Name(verb);
        }

        /// <summary>نوشتن نام فعل با حرف بزرگ برای لاگ و ابزار.</summary>
        public static string Display(ItemVerb verb)
        {
            string name = Name(verb);

            if (name.Length == 0) return name;

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
    }
}
