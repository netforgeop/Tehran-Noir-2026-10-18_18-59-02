using UnityEngine;
using TehranNoir.Dialogue;
using TehranNoir.Items;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// چیزی که از روی زمین یا میز برداشته می‌شود: پرونده، پاکت، کلید.
    /// شرط دیده‌شدن و چیزی که با برداشتن عوض می‌شود، هر دو داده‌اند، نه کد؛
    /// برای آبجکت تازه فقط فیلدها را پر می‌کنی.
    /// </summary>
    public class PickupInteractable : Interactable
    {
        [Header("World")]
        [Tooltip("تا وقتی این متغیر مقدار خواسته‌شده را نداشته باشد، آیتم دیده نمی‌شود. خالی = همیشه")]
        [SerializeField] private string requireVariable = string.Empty;

        [Tooltip("مقدار لازم؛ مثلاً 1 یا true")]
        [SerializeField] private string requireValue = "1";

        [Tooltip("با برداشتن، این متغیر true می‌شود. خالی = چیزی نوشته نشود")]
        [SerializeField] private string takenVariable = string.Empty;

        [Tooltip("با برداشتن، این متغیر برابر مقدار زیر می‌شود. خالی = لازم نیست")]
        [SerializeField] private string setOnTakeVariable = string.Empty;

        [SerializeField] private string setOnTakeValue = "true";

        [Header("Inventory")]
        [Tooltip("شناسه‌ی آیتم در Resources/Items/items.txt؛ خالی = چیزی به جیب نمی‌رود")]
        [SerializeField] private string itemKey = string.Empty;

        [Tooltip("چند عدد؛ سند و کلید همان ۱")]
        [SerializeField, Min(1)] private int itemCount = 1;

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private bool _taken;
        private bool _visible = true;

        /// <summary>
        /// نمونه‌ی واقعیِ همین آبجکت. اگر `Item Key` خالی باشد null است.
        /// برداشتن، همین نمونه را به جیب می‌برد؛ پس مهر/خوانده‌شدنش گم نمی‌شود.
        /// </summary>
        protected ItemInstance BoundInstance { get; private set; }

        // همیشه Take است؛ فیلد Action در Inspector برای این کلاس نادیده گرفته می‌شود
        public override InteractionAction Action => InteractionAction.Take;

        /// <summary>
        /// نام کارت تعامل از خودِ آیتم می‌آید، پس با باز شدن مهر عوض می‌شود.
        /// اگر آیتمی وصل نباشد، همان Display Name دستی Inspector استفاده می‌شود.
        /// </summary>
        public override string Title
        {
            get
            {
                if (BoundInstance == null) return base.Title;

                return ItemRuntime.Title(BoundInstance, Localization.Persian);
            }
        }

        protected virtual void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);

            // آبجکت روی میز و آیتم در جیب باید یک واقعیت باشند، نه دو نسخه.
            BoundInstance = SceneItemBinding.Ensure(this, itemKey);
        }

        private void Update()
        {
            if (_taken) return;

            SetVisible(RequirementMet());
        }

        private WorldState World =>
            WorldStateProvider.Instance != null ? WorldStateProvider.Instance.State : null;

        /// <summary>آیا شرط‌های دنیا اجازه‌ی دیده‌شدن می‌دهند؟</summary>
        private bool RequirementMet()
        {
            if (string.IsNullOrWhiteSpace(requireVariable)) return true;

            WorldState world = World;
            if (world == null) return false;

            return world.Get(requireVariable).AsText() == requireValue.Trim();
        }

        /// <summary>پنهان‌کردن یعنی خاموش‌کردن رندرر و کالایدر؛ نه غیرفعال‌کردن خود آبجکت.</summary>
        private void SetVisible(bool visible)
        {
            if (visible == _visible) return;
            _visible = visible;

            if (_renderers != null)
                for (int i = 0; i < _renderers.Length; i++)
                    if (_renderers[i] != null) _renderers[i].enabled = visible;

            if (_colliders != null)
                for (int i = 0; i < _colliders.Length; i++)
                    if (_colliders[i] != null) _colliders[i].enabled = visible;
        }

        public override void Interact(GameObject interactor)
        {
            if (_taken || !RequirementMet()) return;

            // جیب پر است؟ پس شیء هم برداشته نمی‌شود؛ وگرنه آیتم برای همیشه گم می‌شد.
            if (!CanStoreItem()) return;

            // از مدیر جیب رد می‌شویم تا نمونه، رویداد و پروجکشن با هم ساخته شوند.
            if (!string.IsNullOrWhiteSpace(itemKey))
            {
                ItemResult added = BoundInstance != null
                    ? InventoryManager.Move(BoundInstance, InventoryManager.PlayerId)
                    : InventoryManager.Take(itemKey, itemCount);

                if (!added.Ok)
                {
                    Debug.LogWarning("[Item] برداشتن انجام نشد: " + added.Outcome, this);
                    return;
                }

                // اثرهای «با برداشتن» از تعریف آیتم (ontake.do) در همین مسیر اجرا می‌شوند.
                ItemDefinition definition = ItemCatalog.Get(itemKey);

                if (BoundInstance != null)
                    ItemRuntime.ApplyEffects(BoundInstance, definition.TakeEffects, ItemWorld.Current);
                else
                    ItemRuntime.ApplyEffects(null, definition.TakeEffects, ItemWorld.Current);
            }

            _taken = true;

            Write(takenVariable, "true");
            Write(setOnTakeVariable, setOnTakeValue);

            SetVisible(false);

            Debug.Log("[Item] taken: " + Title, this);
        }

        /// <summary>اگر این شیء آیتم دارد و جیب جا ندارد، برداشتن انجام نمی‌شود.</summary>
        private bool CanStoreItem()
        {
            if (string.IsNullOrWhiteSpace(itemKey)) return true;

            bool room = InventoryManager.CanAccept(
                ItemCatalog.Normalize(itemKey), Mathf.Max(1, itemCount));

            if (!room)
                Debug.LogWarning("[Item] جیب جا ندارد برای «" + itemKey + "»؛ شیء برداشته نشد.", this);

            return room;
        }

        /// <summary>نوشتن در WorldState با همان نحو [do] دیالوگ‌ها؛ یک قاعده، دو جا.</summary>
        private void Write(string variable, string value)
        {
            if (string.IsNullOrWhiteSpace(variable)) return;
            if (WorldStateProvider.Instance == null) return;

            DialogueRunner.ApplyEffect(variable.Trim() + " = " + value.Trim(), WorldStateProvider.Instance.State);
        }
    }
}
