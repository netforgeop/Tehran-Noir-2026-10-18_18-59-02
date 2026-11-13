using UnityEngine;
using TehranNoir.Dialogue;
using TehranNoir.Interaction;

namespace TehranNoir.Documents
{
    /// <summary>
    /// کاغذِ خواندنی روی میز: پرونده، نامه، یادداشت، دفتر ثبت.
    /// با «خواندن» برگه باز می‌شود — نه نوار دیالوگ.
    ///
    /// داده‌محور است: فایل سند در Inspector داده می‌شود و می‌توان با خواندن، یک متغیر دنیا را روشن کرد.
    /// برای سند بعدی هیچ کد تازه‌ای لازم نیست.
    /// </summary>
    public class DocumentInteractable : Interactable
    {
        [Header("Document")]
        [Tooltip("فایل سند داخل Assets/_Project/Resources/Documents/ — بدون پسوند، مثل Documents/file_sealed")]
        [SerializeField] private string documentPath = "Documents/file_sealed";

        [Tooltip("با خواندن، این متغیر true می‌شود. خالی = چیزی نوشته نشود")]
        [SerializeField] private string onReadVariable = string.Empty;

        [Tooltip("بعد از خواندن، شیء ناپدید شود؟ برای نامه‌ای که برداشته می‌شود")]
        [SerializeField] private bool hideAfterRead;

        [Tooltip("اگر سند پیدا نشد، یک خط در Console بنویس")]
        [SerializeField] private bool logMissing = true;

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private bool _hidden;

        // همیشه Read است؛ فیلد Action در Inspector برای این کلاس نادیده گرفته می‌شود
        public override InteractionAction Action => InteractionAction.Read;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        public override void Interact(GameObject interactor)
        {
            if (_hidden) return;

            DocumentLayer.ShowResource(documentPath, null);

            WriteWorld();

            if (hideAfterRead) SetVisible(false);
        }

        private void WriteWorld()
        {
            if (string.IsNullOrWhiteSpace(onReadVariable)) return;
            if (WorldStateProvider.Instance == null)
            {
                if (logMissing) Debug.LogWarning("[Document] WorldStateProvider در صحنه نیست؛ " + onReadVariable + " نوشته نشد.", this);
                return;
            }

            DialogueRunner.ApplyEffect(onReadVariable.Trim() + " = true", WorldStateProvider.Instance.State);
        }

        private void SetVisible(bool visible)
        {
            _hidden = !visible;

            if (_renderers != null)
                for (int i = 0; i < _renderers.Length; i++)
                    if (_renderers[i] != null) _renderers[i].enabled = visible;

            if (_colliders != null)
                for (int i = 0; i < _colliders.Length; i++)
                    if (_colliders[i] != null) _colliders[i].enabled = visible;
        }
    }
}
