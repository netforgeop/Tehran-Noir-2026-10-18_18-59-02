using System.Collections.Generic;

namespace TehranNoir.Items
{
    /// <summary>
    /// یک ظرف: جیب بازیکن، صندوق ماشین، کشوی میز، انبار مأموریت.
    ///
    /// خودش رویداد پخش نمی‌کند و وضعیت جهان را دست نمی‌زند؛ فقط فهرست است.
    /// نوشتن در دنیا و خبررسانی کار `InventoryManager` است. این جدایی باعث
    /// می‌شود انتقال بین دو ظرف یک تراکنش باشد، نه دو عملیات نیمه‌کاره.
    /// </summary>
    public sealed class ItemContainer
    {
        public readonly string Id;

        /// <summary>کلید ترجمه‌ی نام ظرف؛ «جیب»، «صندوق ماشین».</summary>
        public string LabelKey;

        /// <summary>حداکثر تعداد پشته؛ صفر = بی‌نهایت.</summary>
        public int SlotLimit;

        /// <summary>حداکثر وزن؛ صفر = بی‌نهایت.</summary>
        public float WeightLimit;

        private readonly List<ItemInstance> _items;

        public ItemContainer(string id, string labelKey = "", int slotLimit = 0, float weightLimit = 0f)
        {
            Id = id ?? string.Empty;
            LabelKey = labelKey ?? string.Empty;
            SlotLimit = slotLimit < 0 ? 0 : slotLimit;
            WeightLimit = weightLimit < 0f ? 0f : weightLimit;
            _items = new List<ItemInstance>(8);
        }

        public IReadOnlyList<ItemInstance> Items
        {
            get { return _items; }
        }

        /// <summary>تعداد پشته‌ها؛ برای محدودیت جا.</summary>
        public int StackCount
        {
            get { return _items.Count; }
        }

        public int TotalQuantity
        {
            get
            {
                int total = 0;

                for (int i = 0; i < _items.Count; i++)
                    total += _items[i].Quantity;

                return total;
            }
        }

        public float TotalWeight
        {
            get
            {
                float total = 0f;

                for (int i = 0; i < _items.Count; i++)
                    total += _items[i].TotalWeight;

                return total;
            }
        }

        public bool IsEmpty
        {
            get { return _items.Count == 0; }
        }

        public int CountOf(string definitionId)
        {
            string id = ItemCatalog.Normalize(definitionId);

            if (id.Length == 0) return 0;

            int total = 0;

            for (int i = 0; i < _items.Count; i++)
                if (_items[i].DefinitionId == id) total += _items[i].Quantity;

            return total;
        }

        public ItemInstance Find(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;

            for (int i = 0; i < _items.Count; i++)
                if (_items[i].InstanceId == instanceId) return _items[i];

            return null;
        }

        public bool Contains(ItemInstance instance)
        {
            return instance != null && _items.Contains(instance);
        }

        /// <summary>پشته‌ی نیمه‌پُرِ سازگار برای ادغام؛ null اگر نبود.</summary>
        public ItemInstance FindStackFor(ItemInstance instance)
        {
            if (instance == null) return null;

            for (int i = 0; i < _items.Count; i++)
            {
                ItemInstance candidate = _items[i];

                if (candidate == instance) continue;
                if (candidate.IsStackFull) continue;
                if (!candidate.CanStackWith(instance)) continue;

                return candidate;
            }

            return null;
        }

        /// <summary>
        /// آیا این نمونه جا می‌شود؟ ادغام با پشته‌ی موجود جای تازه نمی‌خواهد.
        /// </summary>
        public bool CanAccept(ItemInstance instance)
        {
            if (instance == null) return false;

            bool merges = FindStackFor(instance) != null;

            if (!merges && SlotLimit > 0 && _items.Count >= SlotLimit) return false;

            if (WeightLimit > 0f && TotalWeight + instance.TotalWeight > WeightLimit + 0.0001f) return false;

            return true;
        }

        /// <summary>
        /// افزودن بدون ادغام. شناسه‌ی تکراری رد می‌شود تا بارگذاری ذخیره
        /// هرگز آیتم را دو بار نسازد.
        /// </summary>
        public bool Add(ItemInstance instance)
        {
            if (instance == null) return false;
            if (!string.IsNullOrEmpty(instance.InstanceId) && Find(instance.InstanceId) != null) return false;

            instance.ContainerId = Id;
            _items.Add(instance);

            return true;
        }

        public bool Remove(ItemInstance instance)
        {
            if (instance == null) return false;

            return _items.Remove(instance);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public override string ToString()
        {
            return Id + " (" + _items.Count + " stacks, " + TotalQuantity + " items)";
        }
    }
}
