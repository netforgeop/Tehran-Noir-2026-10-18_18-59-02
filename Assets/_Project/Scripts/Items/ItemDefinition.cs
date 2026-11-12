using System;

namespace TehranNoir.Items
{
    /// <summary>
    /// کارهایی که آیتم اجازه می‌دهد. به‌جای یک بولی برای هر کار، یک عدد بیتی:
    /// آیتم تازه = یک مقدار در فایل، نه یک فیلد تازه در کلاس.
    /// </summary>
    [Flags]
    public enum ItemFlags
    {
        None = 0,
        Usable = 1,
        Droppable = 2,
        Tradable = 4,
        Storable = 8,
        Examinable = 16,
        Destroyable = 32,
        All = Usable | Droppable | Tradable | Storable | Examinable | Destroyable
    }

    /// <summary>
    /// تعریف یک آیتم: فقط داده، بدون وضعیت. همه‌ی فیلدها readonly‌اند تا هیچ‌کس
    /// اشتباهی حالت یک نمونه را روی تعریف مشترک ننویسد.
    ///
    /// وضعیت در ItemInstance است؛ دو سند با یک تعریف می‌توانند یکی بازشده و
    /// یکی مهرشده باشند.
    /// </summary>
    public sealed class ItemDefinition
    {
        /// <summary>شناسه‌ی پایدار؛ هرگز از ایندکس آرایه یا نام فایل ساخته نمی‌شود.</summary>
        public readonly string Id;

        public readonly string Persian;
        public readonly string English;

        public readonly string DescriptionPersian;
        public readonly string DescriptionEnglish;

        public readonly ItemCategory Category;

        /// <summary>زیردسته‌ی آزاد: «police»، «office»، «smoking». خالی = ندارد.</summary>
        public readonly string Subcategory;

        public readonly int MaxStack;
        public readonly float Weight;
        public readonly int Value;
        public readonly ItemFlags Flags;

        /// <summary>مسیر داخل Resources، بدون پسوند. خالی = ندارد.</summary>
        public readonly string IconPath;

        /// <summary>مسیر Prefab داخل Resources، بدون پسوند. خالی = دارد نیست.</summary>
        public readonly string PrefabPath;

        public ItemDefinition(
            string id,
            string persian,
            string english,
            ItemCategory category,
            int maxStack,
            float weight = 0f,
            int value = 0,
            ItemFlags flags = ItemFlags.All,
            string iconPath = "",
            string prefabPath = "",
            string subcategory = "",
            string descriptionPersian = "",
            string descriptionEnglish = "")
        {
            Id = string.IsNullOrEmpty(id) ? string.Empty : id;

            Persian = string.IsNullOrEmpty(persian) ? Id : persian;
            English = string.IsNullOrEmpty(english) ? Id : english;

            DescriptionPersian = descriptionPersian ?? string.Empty;
            DescriptionEnglish = descriptionEnglish ?? string.Empty;

            Category = category;
            Subcategory = subcategory ?? string.Empty;

            MaxStack = maxStack < 1 ? 1 : maxStack;
            Weight = weight < 0f ? 0f : weight;
            Value = value < 0 ? 0 : value;
            Flags = flags;

            IconPath = iconPath ?? string.Empty;
            PrefabPath = prefabPath ?? string.Empty;
        }

        /// <summary>سبد نمایشی قدیمی؛ رابط کاربری فعلی همین را می‌خواند.</summary>
        public ItemKind Kind
        {
            get { return ItemCategories.KindOf(Category); }
        }

        public bool Has(ItemFlags flag)
        {
            if (flag == ItemFlags.None) return true;

            return (Flags & flag) == flag;
        }

        public bool CanStack
        {
            get { return MaxStack > 1; }
        }

        public string Name(bool persian)
        {
            return persian ? Persian : English;
        }

        /// <summary>شرح؛ اگر ترجمه خالی باشد همان زبان دیگر، و اگر آن هم نبود خالی.</summary>
        public string Description(bool persian)
        {
            string chosen = persian ? DescriptionPersian : DescriptionEnglish;
            if (!string.IsNullOrEmpty(chosen)) return chosen;

            return persian ? DescriptionEnglish : DescriptionPersian;
        }

        public override string ToString()
        {
            return Id + " (" + Category + ", max " + MaxStack + ")";
        }
    }
}
