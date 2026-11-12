using System;
using System.Collections.Generic;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// کارهایی که آیتم اجازه می‌دهد. به‌جای یک بولی برای هر کار، یک عدد بیتی:
    /// آیتم تازه = یک مقدار در فایل، نه یک فیلد تازه در کلاس.
    ///
    /// این پرچم‌ها «اجازه‌ی کلی» هستند؛ شرط دقیق‌تر (مثلاً «فقط وقتی مهر باز است»)
    /// در قاعده‌ی همان فعل نوشته می‌شود، نه اینجا.
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
        Equippable = 64,

        /// <summary>
        /// پیش‌فرض آیتمی که در فایل `flags` ندارد. «استفاده» و «تجهیز» عمداً
        /// نیستند: پوکه و فشنگ نباید دکمه‌ی «استفاده» بگیرند. اگر آیتمی
        /// واقعاً مصرف‌شدنی یا تجهیزشدنی است، در همان خط بنویس.
        /// </summary>
        Common = Droppable | Tradable | Storable | Examinable | Destroyable,

        All = Common | Usable | Equippable
    }

    /// <summary>
    /// تعریف یک آیتم: فقط داده، بدون وضعیت. همه‌ی فیلدهای ساده readonly‌اند تا
    /// هیچ‌کس اشتباهی حالت یک نمونه را روی تعریف مشترک ننویسد.
    ///
    /// رفتار (چه شرطی، چه اثری) در `Rules` است و از `items.txt` می‌آید؛
    /// برای آیتم تازه هیچ کلاس C# نوشته نمی‌شود.
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

        /// <summary>مسیر سند خواندنی داخل Resources. خالی = سند نیست.</summary>
        public readonly string DocumentPath;

        /// <summary>وضعیت‌های اولیه‌ی نمونه؛ `state.sealed = 1` و مانند آن.</summary>
        public readonly List<ItemStatePair> InitialState;

        /// <summary>قاعده‌های فعل‌ها: use / read / open / unlock / …</summary>
        public readonly ItemRuleSet Rules;

        /// <summary>اثرهایی که خودِ برداشتن اجرا می‌کند.</summary>
        public readonly string[] TakeEffects;

        /// <summary>اثرهایی که خودِ انداختن اجرا می‌کند.</summary>
        public readonly string[] DropEffects;

        /// <summary>نام‌های جایگزین بر اساس وضعیت؛ خالی = همیشه نام پایه.</summary>
        public readonly List<ItemTitleVariant> Titles;

        public ItemDefinition(
            string id,
            string persian,
            string english,
            ItemCategory category,
            int maxStack,
            float weight = 0f,
            int value = 0,
            ItemFlags flags = ItemFlags.Common,
            string iconPath = "",
            string prefabPath = "",
            string subcategory = "",
            string descriptionPersian = "",
            string descriptionEnglish = "",
            string documentPath = "",
            List<ItemStatePair> initialState = null,
            ItemRuleSet rules = null,
            string[] takeEffects = null,
            string[] dropEffects = null,
            List<ItemTitleVariant> titles = null)
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
            DocumentPath = documentPath ?? string.Empty;

            InitialState = initialState ?? new List<ItemStatePair>(0);
            Rules = rules ?? ItemRuleSet.Empty;
            TakeEffects = takeEffects ?? ItemRule.Empty;
            DropEffects = dropEffects ?? ItemRule.Empty;
            Titles = titles ?? new List<ItemTitleVariant>(0);
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

        public bool IsDocument
        {
            get { return !string.IsNullOrEmpty(DocumentPath); }
        }

        /// <summary>آیا اصلاً برای این فعل قاعده‌ای نوشته شده؟ (بدون بررسی شرط)</summary>
        public bool HasRule(ItemVerb verb)
        {
            return Rules != null && Rules.HasVerb(verb);
        }

        /// <summary>
        /// پیدا کردن قاعده‌ی جاری. `view` دنیای «شده» است (دنیا + state این نمونه).
        /// </summary>
        public ItemRule FindRule(ItemVerb verb, WorldState view, string targetId)
        {
            if (Rules == null) return null;

            return Rules.Find(verb, view, targetId);
        }

        /// <summary>
        /// آیا این فعل روی این آیتم «قابل انجام» است؟ برای نمایش دکمه‌ها.
        /// قاعده‌ی نوشته‌شده مقدم است؛ اگر قاعده‌ای نبود، پرچم‌ها و سند تعیین می‌کنند.
        /// </summary>
        public bool CanDo(ItemVerb verb, WorldState view, string targetId)
        {
            if (HasRule(verb)) return FindRule(verb, view, targetId) != null;

            return HasDefault(verb);
        }

        /// <summary>
        /// فعل‌هایی که همین حالا روی این آیتم ممکن‌اند، به ترتیب نمایش.
        /// رابط کاربری همین را می‌خواند، پس هیچ فعل غیرممکنی نشان داده نمی‌شود
        /// و فهرست با عوض شدن وضعیت (مهر باز شد) خودش عوض می‌شود.
        /// </summary>
        public int Verbs(WorldState view, string targetId, List<ItemVerb> results)
        {
            if (results == null) return 0;

            results.Clear();

            for (int i = 0; i < ItemVerbs.Ordered.Length; i++)
            {
                ItemVerb verb = ItemVerbs.Ordered[i];

                // «استفاده» روی سند معنا ندارد، مگر طراحی خودش قاعده نوشته باشد.
                if (IsDocument && !HasRule(ItemVerb.Use) &&
                    (verb == ItemVerb.Use || verb == ItemVerb.Consume))
                    continue;

                if (!CanDo(verb, view, targetId)) continue;

                results.Add(verb);
            }

            return results.Count;
        }

        /// <summary>رفتار پیش‌فرض وقتی طراحی برای فعل، قاعده‌ای ننوشته است.</summary>
        public bool HasDefault(ItemVerb verb)
        {
            switch (verb)
            {
                case ItemVerb.Examine: return Has(ItemFlags.Examinable);
                case ItemVerb.Drop: return Has(ItemFlags.Droppable);
                case ItemVerb.Equip: return Has(ItemFlags.Equippable);
                case ItemVerb.Read: return IsDocument;
                case ItemVerb.Use: return Has(ItemFlags.Usable);

                // «مصرف» و «جدا کردن» عمداً پیش‌فرض ندارند: بدون قاعده، هیچ آیتمی
                // دکمه‌ی بی‌معنی نمی‌گیرد. اگر لازم بود، در همان خط بنویس
                //   consume.do=...   /   unequip.do=...
                case ItemVerb.Consume:
                case ItemVerb.Unequip: return false;

                default: return false;
            }
        }

        /// <summary>کوتاه‌نویسی برای حالت‌های نمایشی (معادل HasDefault).</summary>
        public bool Allows(ItemFlags flag)
        {
            return Has(flag);
        }

        public string Name(bool persian)
        {
            return persian ? Persian : English;
        }

        /// <summary>
        /// نام همین نمونه در این لحظه: اولین variant که شرطش برقرار است،
        /// وگرنه نام پایه. سندِ مهرشده بعد از باز شدن، نام دیگری می‌گیرد.
        /// </summary>
        public string Title(WorldState view, bool persian)
        {
            if (Titles != null && Titles.Count > 0)
            {
                for (int i = 0; i < Titles.Count; i++)
                {
                    ItemTitleVariant variant = Titles[i];

                    if (!variant.HasText) continue;
                    if (!variant.Matches(view)) continue;

                    return variant.Text(persian);
                }
            }

            return Name(persian);
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
            return Id + " (" + Category + ", max " + MaxStack + ", rules " + (Rules != null ? Rules.Count : 0) + ")";
        }
    }
}
