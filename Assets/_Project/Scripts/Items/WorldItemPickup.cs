using UnityEngine;
using TehranNoir.Core;
using TehranNoir.Interaction;
using TehranNoir.Settings;

namespace TehranNoir.Items
{
    /// <summary>
    /// آیتمی که روی زمین افتاده و دوباره برداشته می‌شود.
    ///
    /// مهم‌ترین نکته: همین نمونه را برمی‌گرداند، نه یک نسخه‌ی تازه.
    /// پس سندی که مهرش باز شده، بعد از افتادن و برداشتن هم باز می‌ماند و
    /// شناسه‌ی یکتایش عوض نمی‌شود. اگر نسخه‌ی تازه ساخته می‌شد، دو سند با
    /// شناسه‌های مختلف و حالت‌های مختلف در بازی می‌داشتیم.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldItemPickup : Interactable
    {
        private ItemInstance _instance;

        /// <summary>همیشه «برداشتن»؛ آبجکتی که روی زمین افتاده کار دیگری ندارد.</summary>
        public override InteractionAction Action => InteractionAction.Take;

        public ItemInstance Instance
        {
            get { return _instance; }
        }

        /// <summary>فقط ساخته‌کننده صدا می‌زند (ItemDropSpawner یا صحنه).</summary>
        public void Bind(ItemInstance instance)
        {
            _instance = instance;

            if (instance != null && instance.Definition != null)
                name = "Item_" + instance.Definition.Id;
        }

        public override string Title
        {
            get
            {
                if (_instance == null) return base.Title;

                return ItemRuntime.Title(_instance, Localization.Persian);
            }
        }

        public override string Subtitle
        {
            get
            {
                if (_instance == null) return base.Subtitle;

                string kind = ItemCategories.LocKey(_instance.Definition.Category);

                return Loc.Get(Loc.Channel.Menu, "char.items.kind." + kind);
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (_instance == null)
            {
                Debug.LogWarning("[Items] آبجکت زمین بدون آیتم بود؛ حذف شد.", this);
                Destroy(gameObject);
                return;
            }

            ItemResult result = InventoryManager.Move(_instance, InventoryManager.PlayerId);

            if (!result.Ok)
            {
                // جیب پر است یا آیتم در ظرف دیگری است: شیء همان‌جا می‌ماند.
                Debug.LogWarning("[Items] برداشتن ممکن نشد: " + result.ReasonKey, this);
                return;
            }

            Destroy(gameObject);
        }
    }
}
