using UnityEngine;
using TehranNoir.Dialogue;

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

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private bool _taken;
        private bool _visible = true;

        // همیشه Take است؛ فیلد Action در Inspector برای این کلاس نادیده گرفته می‌شود
        public override InteractionAction Action => InteractionAction.Take;

        protected virtual void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
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

            _taken = true;

            Write(takenVariable, "true");
            Write(setOnTakeVariable, setOnTakeValue);

            SetVisible(false);

            Debug.Log("[Item] taken: " + Title, this);
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
