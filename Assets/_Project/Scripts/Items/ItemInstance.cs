using System;
using System.Collections.Generic;

namespace TehranNoir.Items
{
    /// <summary>
    /// یک آیتمِ واقعی در دست بازیکن یا درون یک ظرف.
    ///
    /// تعریف (ItemDefinition) مشترک و تغییرناپذیر است؛ هر چیزی که در بازی
    /// عوض می‌شود اینجاست: تعداد، دوام، مهر سند، حالت خوانده‌شدن، مالک.
    ///
    /// دو پرونده‌ی پلیس با یک DefinitionId می‌توانند یکی مهرشده و نخوانده باشد
    /// و دیگری بازشده و خوانده‌شده؛ چون وضعیت روی نمونه است، نه روی تعریف.
    /// </summary>
    public sealed class ItemInstance
    {
        public const int MinQuantity = 1;

        /// <summary>وضعیت آزاد؛ بعداً DocumentState / EvidenceState روی همین می‌نشینند.</summary>
        private readonly Dictionary<string, string> _state =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>شناسه‌ی یکتا در کل بازی؛ برای ذخیره و بارگذاری.</summary>
        public string InstanceId { get; private set; }

        /// <summary>شناسه‌ی پایدار تعریف، نه نام آبجکت.</summary>
        public string DefinitionId { get; private set; }

        public int Quantity { get; private set; }

        /// <summary>۰ تا ۱؛ ۱ یعنی سالم. آیتم‌های بدون دوام همیشه ۱ می‌مانند.</summary>
        public float Durability { get; private set; }

        /// <summary>ظرفی که آیتم داخلش است؛ خالی یعنی هنوز جایی گذاشته نشده.</summary>
        public string ContainerId { get; set; }

        private ItemInstance(string instanceId, string definitionId, int quantity, float durability, string containerId)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            Quantity = quantity;
            Durability = durability;
            ContainerId = containerId ?? string.Empty;
        }

        /* ============================================================
           ساخت
           ============================================================ */

        /// <summary>نمونه‌ی تازه با شناسه‌ی یکتا.</summary>
        public static ItemInstance Create(string definitionId, int quantity = 1)
        {
            return Create(definitionId, quantity, Guid.NewGuid().ToString("N"));
        }

        /// <summary>برای بارگذاری ذخیره: شناسه از بیرون می‌آید.</summary>
        public static ItemInstance Create(string definitionId, int quantity, string instanceId)
        {
            string id = ItemCatalog.Normalize(definitionId);

            if (id.Length == 0) return null;

            string unique = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId.Trim();

            if (unique.Length == 0) return null;

            return new ItemInstance(unique, id, ClampQuantity(quantity), 1f, string.Empty);
        }

        /// <summary>
        /// بازسازی از داده‌ی ذخیره‌شده. داده‌ی خراب (شناسه‌ی خالی) → null تا
        /// بارگذار خودش تصمیم بگیرد؛ هیچ‌وقت Future/NullReference نمی‌دهیم.
        /// </summary>
        public static ItemInstance Restore(
            string instanceId,
            string definitionId,
            int quantity,
            float durability,
            string containerId)
        {
            string unique = (instanceId ?? string.Empty).Trim();
            string id = ItemCatalog.Normalize(definitionId);

            if (unique.Length == 0 || id.Length == 0) return null;

            ItemInstance instance = new ItemInstance(unique, id, ClampQuantity(quantity), ClampDurability(durability), containerId);

            return instance;
        }

        /* ============================================================
           خواندن
           ============================================================ */

        /// <summary>تعریف آیتم؛ برای شناسه‌ی ناشناس هم تعریف جایگزین برمی‌گردد.</summary>
        public ItemDefinition Definition
        {
            get { return ItemCatalog.Get(DefinitionId); }
        }

        /// <summary>آیا تعریف واقعاً در فهرست هست؟ (برای هشدار داده‌ی قدیمی)</summary>
        public bool IsKnown
        {
            get { return ItemCatalog.Exists(DefinitionId); }
        }

        public float TotalWeight
        {
            get { return Definition.Weight * Quantity; }
        }

        public bool IsStackFull
        {
            get { return Quantity >= Definition.MaxStack; }
        }

        public int FreeSpace
        {
            get { return Definition.MaxStack - Quantity; }
        }

        /* ============================================================
           وضعیت آزاد (state)
           ============================================================ */

        public int StateCount
        {
            get { return _state.Count; }
        }

        public string GetState(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;

            string value;

            if (_state.TryGetValue(key, out value)) return value;

            return fallback ?? string.Empty;
        }

        public bool HasState(string key)
        {
            return !string.IsNullOrEmpty(key) && _state.ContainsKey(key);
        }

        public bool TryGetState(string key, out string value)
        {
            value = string.Empty;

            if (string.IsNullOrEmpty(key)) return false;

            return _state.TryGetValue(key, out value);
        }

        /// <summary>نوشتن وضعیت؛ مقدار خالی یعنی حذف کلید تا ذخیره شلوغ نشود.</summary>
        public void SetState(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (value == null)
            {
                _state.Remove(key);
                return;
            }

            _state[key] = value;
        }

        public bool RemoveState(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            return _state.Remove(key);
        }

        public void ClearState()
        {
            _state.Clear();
        }

        /// <summary>کلیدها با یک بافر بیرونی؛ هر فریم لیست تازه ساخته نمی‌شود.</summary>
        public void CopyStateKeys(List<string> results)
        {
            if (results == null) return;

            results.Clear();

            foreach (KeyValuePair<string, string> pair in _state)
                results.Add(pair.Key);
        }

        /// <summary>انگشت‌نگار وضعیت؛ برای مقایسه‌ی دو نمونه (ادغام پشته) و برای ذخیره.</summary>
        public string StateSignature()
        {
            if (_state.Count == 0) return string.Empty;

            List<string> keys = new List<string>(_state.Keys);
            keys.Sort(StringComparer.Ordinal);

            string text = string.Empty;

            for (int i = 0; i < keys.Count; i++)
                text += keys[i] + "=" + _state[keys[i]] + ";";

            return text;
        }

        public void CopyStateTo(ItemInstance other)
        {
            if (other == null || other == this) return;

            other._state.Clear();

            foreach (KeyValuePair<string, string> pair in _state)
                other._state[pair.Key] = pair.Value;
        }

        /* ============================================================
           تغییر
           ============================================================ */

        public bool SetQuantity(int quantity)
        {
            if (quantity < MinQuantity) return false;

            int max = Definition.MaxStack;

            Quantity = quantity > max ? max : quantity;
            return true;
        }

        /// <summary>اضافه‌کردن تا سقف پشته؛ تعداد واقعیِ اضافه‌شده را برمی‌گرداند.</summary>
        public int AddQuantity(int amount)
        {
            if (amount <= 0) return 0;

            int room = FreeSpace;

            if (room <= 0) return 0;

            int added = amount > room ? room : amount;

            Quantity += added;
            return added;
        }

        public bool SetDurability(float durability)
        {
            Durability = ClampDurability(durability);
            return true;
        }

        /// <summary>
        /// جدا کردن بخشی از پشته. این نمونه کوچک می‌شود و نمونه‌ی تازه برمی‌گردد.
        /// وضعیت (مهر، خوانده‌شدن) هم کپی می‌شود تا دو نیمه یک واقعیت باشند.
        /// </summary>
        public ItemInstance Split(int amount)
        {
            if (amount <= 0 || amount >= Quantity) return null;

            ItemInstance part = Create(DefinitionId, amount);

            if (part == null) return null;

            CopyStateTo(part);
            part.SetDurability(Durability);

            Quantity -= amount;
            return part;
        }

        /// <summary>کپی مستقل: شناسه‌ی تازه، تعداد و وضعیت یکسان.</summary>
        public ItemInstance Clone()
        {
            ItemInstance copy = Create(DefinitionId, Quantity);

            if (copy == null) return null;

            copy.SetDurability(Durability);
            CopyStateTo(copy);

            return copy;
        }

        /// <summary>
        /// آیا می‌شود این دو را یک پشته کرد؟ تعریف یکسان، پشته‌پذیر، وضعیت یکسان.
        /// سند مهرشده هرگز با سند بازشده ادغام نمی‌شود.
        /// </summary>
        public bool CanStackWith(ItemInstance other)
        {
            if (other == null || other == this) return false;
            if (!string.Equals(DefinitionId, other.DefinitionId, StringComparison.Ordinal)) return false;
            if (!Definition.CanStack) return false;
            if (Durability != other.Durability) return false;

            return string.Equals(StateSignature(), other.StateSignature(), StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return DefinitionId + (Quantity > 1 ? " x" + Quantity : string.Empty);
        }

        private static int ClampQuantity(int quantity)
        {
            return quantity < MinQuantity ? MinQuantity : quantity;
        }

        private static float ClampDurability(float durability)
        {
            if (durability < 0f) return 0f;
            if (durability > 1f) return 1f;

            return durability;
        }
    }
}
