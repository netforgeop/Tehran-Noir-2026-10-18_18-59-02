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
        /// اول در ظرف زمین دنبال یک نمونه‌ی آزاد از همان تعریف می‌گردد تا
        /// اگر چیزی از قبل افتاده بود، آبجکت تازه دوتایش نکند.
        /// </summary>
        public static ItemInstance Ensure(Component owner, string definitionId)
        {
            if (owner == null) return null;

            string id = ItemCatalog.Normalize(definitionId);

            if (id.Length == 0) return null;

            ItemInstance existing;

            if (Bound.TryGetValue(owner, out existing) && existing != null)
                return existing;

            InventoryManager.EnsureDefaults();

            ItemContainer world = InventoryManager.WorldBox;

            if (world != null)
            {
                for (int i = 0; i < world.Items.Count; i++)
                {
                    ItemInstance candidate = world.Items[i];

                    if (candidate.DefinitionId != id) continue;
                    if (Claimed.Contains(candidate.InstanceId)) continue;

                    Bound[owner] = candidate;
                    Claimed.Add(candidate.InstanceId);

                    return candidate;
                }
            }

            ItemInstance made = ItemInstance.Create(id);

            if (made == null) return null;

            InventoryManager.AddRestored(made, InventoryManager.WorldId);

            Bound[owner] = made;
            Claimed.Add(made.InstanceId);

            return made;
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
