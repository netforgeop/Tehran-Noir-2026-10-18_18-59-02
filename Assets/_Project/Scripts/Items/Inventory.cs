using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>یک ردیف جیب: تعریف آیتم و تعدادی که همراهش است.</summary>
    public struct InventorySlot
    {
        public ItemDefinition Definition;
        public int Count;
    }

    /// <summary>
    /// جیب نادر.
    ///
    /// هیچ عددی اینجا نگه داشته نمی‌شود: تعداد هر آیتم همان متغیر
    /// item.&lt;id&gt; در WorldState است.
    /// چرا؟ چون دیالوگ‌ها و کارها از قبل با همان زبان شرط کار می‌کنند:
    ///   [if] item.file.sealed >= 1
    ///   [do] item.cigarettes - 1
    /// و پنل تست (F10) هم همان متغیرها را نشان می‌دهد. اگر این کلاس نسخه‌ی
    /// جداگانه‌ای از تعدادها داشت، دیر یا زود دو نسخه از هم جدا می‌شدند.
    /// </summary>
    public static class Inventory
    {
        /// <summary>پیشوند کلید در WorldState؛ در WorldState.Prefixes هم ثبت شده.</summary>
        public const string Prefix = "item.";

        private static WorldState World
        {
            get
            {
                return WorldStateProvider.Instance != null
                    ? WorldStateProvider.Instance.State
                    : null;
            }
        }

        public static string Key(string id)
        {
            return Prefix + ItemCatalog.Normalize(id);
        }

        /// <summary>تعداد فعلی؛ همیشه بین ۰ و بیشترین تعداد تعریف‌شده.</summary>
        public static int Count(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;

            WorldState world = World;
            if (world == null) return 0;

            double number = world.Get(Key(id)).AsNumber();

            if (double.IsNaN(number) || number <= 0d) return 0;

            return Mathf.Clamp(Mathf.RoundToInt((float)number), 0, ItemCatalog.Get(id).MaxStack);
        }

        public static bool Has(string id, int amount = 1)
        {
            if (amount <= 0) return true;

            return Count(id) >= amount;
        }

        /// <summary>چقدر جا مانده؛ صفر یعنی پرشده.</summary>
        public static int Room(string id)
        {
            return Mathf.Max(0, ItemCatalog.Get(id).MaxStack - Count(id));
        }

        /// <summary>اضافه‌کردن. تعداد واقعیِ اضافه‌شده را برمی‌گرداند (سقف‌دار).</summary>
        public static int Add(string id, int amount = 1)
        {
            WorldState world = World;

            if (world == null || string.IsNullOrEmpty(id) || amount <= 0) return 0;

            int room = Room(id);
            if (room <= 0) return 0;

            int added = Mathf.Min(room, amount);

            world.Set(Key(id), (double)(Count(id) + added));

            return added;
        }

        /// <summary>کم‌کردن. تعداد واقعیِ کم‌شده را برمی‌گرداند.</summary>
        public static int Remove(string id, int amount = 1)
        {
            WorldState world = World;

            if (world == null || string.IsNullOrEmpty(id) || amount <= 0) return 0;

            int current = Count(id);
            if (current <= 0) return 0;

            int removed = Mathf.Min(current, amount);

            world.Set(Key(id), (double)(current - removed));

            return removed;
        }

        /// <summary>
        /// برداشتن همه‌یا‌هیچ: اگر به اندازه‌ی کافی نباشد، هیچ چیز کم نمی‌شود.
        /// برای مصرف آیتم (سیگار، کلید یک‌بارمصرف) از این استفاده کن، نه Remove.
        /// </summary>
        public static bool Take(string id, int amount = 1)
        {
            if (amount <= 0) return true;
            if (!Has(id, amount)) return false;

            Remove(id, amount);
            return true;
        }

        /// <summary>
        /// ردیف‌های غیرخالی، به ترتیب فایل. بافر از بیرون می‌آید تا هر فریم
        /// لیست تازه ساخته نشود.
        /// </summary>
        public static int Slots(List<InventorySlot> results)
        {
            if (results == null) return 0;

            results.Clear();

            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                int count = Count(all[i].Id);

                if (count <= 0) continue;

                InventorySlot slot = new InventorySlot();
                slot.Definition = all[i];
                slot.Count = count;

                results.Add(slot);
            }

            return results.Count;
        }

        public static int DistinctCount
        {
            get
            {
                IReadOnlyList<ItemDefinition> all = ItemCatalog.All;
                int count = 0;

                for (int i = 0; i < all.Count; i++)
                    if (Count(all[i].Id) > 0) count++;

                return count;
            }
        }

        public static bool IsEmpty
        {
            get { return DistinctCount == 0; }
        }

        /// <summary>
        /// انگشت‌نگاری ارزان برای تشخیص تغییر در رابط. چون عددها در WorldState‌اند
        /// و هر سیستمی (دیالوگ، پنل تست، برداشتن) می‌تواند عوضشان کند،
        /// به‌جای رویداد، همین امضا هر فریم (فقط وقتی منو باز است) خوانده می‌شود.
        /// </summary>
        public static int Signature()
        {
            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;

            int signature = 23;

            for (int i = 0; i < all.Count; i++)
                signature = signature * 31 + Count(all[i].Id);

            return signature;
        }

        /// <summary>
        /// هر آیتم فهرست را از روز اول در دنیا هست (با صفر) تا در پنل تست
        /// دیده شود و بتوان دستی مقدار داد. مقدار موجود دست نمی‌خورد.
        /// </summary>
        public static void EnsureDefaults(WorldState world)
        {
            if (world == null) return;

            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                string key = Prefix + all[i].Id;

                if (!world.Has(key)) world.Set(key, 0d);
            }
        }
    }
}
