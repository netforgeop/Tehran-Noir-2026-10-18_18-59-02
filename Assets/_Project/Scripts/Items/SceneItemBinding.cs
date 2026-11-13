using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.Items
{
    /// <summary>
    /// پیوند بین یک آبجکت صحنه و یک نمونه‌ی آیتم.
    ///
    /// چرا لازم است؟ تا وقتی پرونده روی میز است، «مهر بسته» یک واقعیت است؛
    /// وقتی برداشته می‌شود، همان واقعیت باید داخل جیب برود، نه یک نسخه‌ی تازه.
    /// پس هر آبجکت صحنه در `Awake` یک نمونه می‌گیرد و تا آخر عمر همان را
    /// نگه می‌دارد. برداشتن = `Move` همان نمونه به جیب.
    ///
    /// پیوند در حافظه است و در فایل ذخیره نوشته نمی‌شود؛ دفعه‌ی بعد که صحنه
    /// بار شود، آبجکت‌ها نمونه‌ی خودشان را دوباره می‌گیرند.
    /// </summary>
    public static class SceneItemBinding
    {
        private static readonly Dictionary<Component, ItemInstance> Bound =
            new Dictionary<Component, ItemInstance>(16);

        /// <summary>نمونه‌هایی که امسال به یک آبجکت داده شده‌اند، تا دوباره داده نشوند.</summary>
        private static readonly HashSet<string> Claimed = new HashSet<string>();

        /// <summary>
        /// نمونه‌ی همین آبجکت را برمی‌گرداند و اگر نبود می‌سازد.
        /// اول دنبال یک نمونه‌ی آزاد از همان تعریف در هر ظرفی می‌گردد (زمین،
        /// جیب، بایگانی) تا اگر آیتم از قبل وجود دارد، آبجکت دوتایش نکند.
        /// </summary>
        public static ItemInstance Ensure(Component owner, string definitionId)
        {
            if (owner == null) return null;

            string id = ItemCatalog.Normalize(definitionId);

            if (id.Length == 0) return null;

            ItemInstance existing;

            if (Bound.TryGetValue(owner, out existing) && existing != null)
                return existing;

            ItemInstance found = FindExisting(id);

            if (found != null)
            {
                Bound[owner] = found;
                Claimed.Add(found.InstanceId);

                return found;
            }

            ItemInstance made = ItemInstance.Create(id);

            if (made == null) return null;

            InventoryManager.AddRestored(made, InventoryManager.WorldId);

            Bound[owner] = made;
            Claimed.Add(made.InstanceId);

            return made;
        }

        /// <summary>
        /// نمونه‌ی آزادِ این تعریف در **هر** ظرفی — اول زمین، بعد جیب و بقیه.
        ///
        /// چرا کل ظرف‌ها؟ چون آبجکت صحنه دوباره ساخته می‌شود ولی آیتم یکی است.
        /// اگر پرونده در بایگانی باشد و صحنه دوباره لود شود، جست‌وجوی فقط-زمین
        /// یک نسخه‌ی مهرشده‌ی تازه روی میز می‌گذاشت. حالا همان نمونه‌ی موجود
        /// پیدا می‌شود و آبجکت از همان لحظه «برداشته‌شده» حساب می‌شود.
        /// </summary>
        public static ItemInstance FindExisting(string definitionId)
        {
            string id = ItemCatalog.Normalize(definitionId);

            if (id.Length == 0) return null;

            InventoryManager.EnsureDefaults();

            ItemContainer world = InventoryManager.WorldBox;

            if (world != null)
            {
                ItemInstance inWorld = FindIn(world, id);

                if (inWorld != null) return inWorld;
            }

            foreach (ItemContainer container in InventoryManager.All)
            {
                if (container == null || container.Id == InventoryManager.WorldId) continue;

                ItemInstance elsewhere = FindIn(container, id);

                if (elsewhere != null) return elsewhere;
            }

            return null;
        }

        private static ItemInstance FindIn(ItemContainer container, string definitionId)
        {
            for (int i = 0; i < container.Items.Count; i++)
            {
                ItemInstance candidate = container.Items[i];

                if (candidate == null || candidate.DefinitionId != definitionId) continue;
                if (Claimed.Contains(candidate.InstanceId)) continue;

                return candidate;
            }

            return null;
        }

        public static void Release(Component owner)
        {
            if (owner == null) return;

            ItemInstance instance;

            if (!Bound.TryGetValue(owner, out instance)) return;

            if (instance != null) Claimed.Remove(instance.InstanceId);

            Bound.Remove(owner);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Bound.Clear();
            Claimed.Clear();
        }
    }
}
