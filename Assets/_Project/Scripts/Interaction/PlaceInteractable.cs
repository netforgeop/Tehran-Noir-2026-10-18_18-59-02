using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// جایی که چیزی روی آن گذاشته می‌شود: جای پرونده روی میز، قلاب کلید، طاقچه.
    /// مثل PickupInteractable داده‌محور است: شرط دیده‌شدن و چند اثر، همه در Inspector.
    /// با گذاشتن، اثرها در WorldState نوشته می‌شوند و این نقطه ناپدید می‌شود؛
    /// تا وقتی شرطش برقرار نشود هم دیده نمی‌شود، پس یک جسم خالی کافی است.
    /// </summary>
    public class PlaceInteractable : Interactable
    {
        [Header("World")]
        [Tooltip("تا وقتی این متغیر مقدار خواسته‌شده را نداشته باشد، این نقطه دیده نمی‌شود. خالی = همیشه")]
        [SerializeField] private string requireVariable = string.Empty;

        [Tooltip("مقدار لازم؛ مثلاً true یا 1")]
        [SerializeField] private string requireValue = "true";

        [Tooltip("اثرهای گذاشتن؛ هر خط مثل [do] دیالوگ:  flag.delivery = desk  یا  rel.reza +2")]
        [SerializeField] private string[] effects = new string[0];

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private bool _placed;
        private bool _visible = true;

        // همیشه Place است؛ فیلد Action در Inspector برای این کلاس نادیده گرفته می‌شود
        public override InteractionAction Action => InteractionAction.Place;

        protected virtual void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        private void Update()
        {
            if (_placed) return;

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
            if (_placed || !RequirementMet()) return;

            _placed = true;

            ApplyEffects();
            SetVisible(false);

            Debug.Log("[Place] placed: " + Title, this);
        }

        /// <summary>نوشتن در WorldState با همان نحو [do] دیالوگ‌ها؛ یک قاعده، سه جا.</summary>
        private void ApplyEffects()
        {
            if (WorldStateProvider.Instance == null) return;

            for (int i = 0; i < effects.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(effects[i])) continue;

                DialogueRunner.ApplyEffect(effects[i], WorldStateProvider.Instance.State);
            }
        }
    }
}
