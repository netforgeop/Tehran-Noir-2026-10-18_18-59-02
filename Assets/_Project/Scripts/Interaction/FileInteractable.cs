using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Dialogue;
using TehranNoir.Documents;
using TehranNoir.Items;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// پرونده‌ی مهر و موم‌شده: دو کار دارد — باز کردن (یا بعداً خواندن) و برداشتن.
    /// باز کردن، مهر را می‌شکند و همین یک مدرک، کل مسیر داستان را می‌سازد.
    /// بعد از باز شدن، «باز کردن» جای خودش را به «خواندن» می‌دهد؛
    /// پرونده هر وقت بخواهی دوباره باز می‌شود، ولی مهر فقط یک بار می‌شکند.
    /// </summary>
    public class FileInteractable : PickupInteractable
    {
        [Header("Seal")]
        [Tooltip("با باز کردن، این متغیر true می‌شود. مهر شکسته = مدرک")]
        [SerializeField] private string openedVariable = "flag.file.opened";

        [Tooltip("سند کاغذی که با باز کردن نشان داده می‌شود — مسیر داخل Resources، مثل Documents/file_sealed")]
        [SerializeField] private string documentPath = string.Empty;

        [Tooltip("اگر سند کاغذی نبود: فایل دیالوگی که محتوای پرونده را نشان می‌دهد؛ خالی = فقط Console")]
        [SerializeField] private DialogueStarter contentReader;

        private readonly List<InteractionAction> _verbs = new List<InteractionAction>(2);

        private bool _opened;

        /// <summary>پرونده بعد از برداشتن دیگر روی میز نیست؛ این پرچم برای پرسش‌های بعدی است.</summary>
        public bool IsOpened => _opened;

        protected override void Awake()
        {
            // بدون این، رندررها و کالایدرها کش می‌شوند و برداشتن کار می‌کند
            base.Awake();

            ReadWorld();

            if (contentReader == null) contentReader = GetComponent<DialogueStarter>();
        }

        private WorldState World =>
            WorldStateProvider.Instance != null ? WorldStateProvider.Instance.State : null;

        /// <summary>اگر بازی از قبل می‌داند پرونده باز شده، دوباره «باز کن» نشان نده.</summary>
        private void ReadWorld()
        {
            if (World == null) return;

            if (string.IsNullOrWhiteSpace(openedVariable)) return;

            _opened = World.Get(openedVariable).Truthy();
        }

        /// <summary>مهروموم‌شده: باز کردن + برداشتن. بازشده: خواندن + برداشتن.</summary>
        public override IReadOnlyList<InteractionAction> Verbs
        {
            get
            {
                _verbs.Clear();

                // اگر آیتم این آبجکت شناخته شده باشد، وضعیتش (مهر باز/بسته) از
                // خودِ نمونه می‌آید، نه از یک بولی جدا در Inspector.
                bool opened = BoundInstance != null
                    ? ItemState.GetBool(BoundInstance, "sealed") == false
                    : _opened;

                _verbs.Add(opened ? InteractionAction.Read : InteractionAction.Open);
                _verbs.Add(InteractionAction.Take);

                return _verbs;
            }
        }

        public override void Interact(GameObject interactor, InteractionAction verb)
        {
            if (verb == InteractionAction.Open)
            {
                Open(interactor);
                return;
            }

            if (verb == InteractionAction.Read)
            {
                MarkRead();
                ShowContent();
                return;
            }

            base.Interact(interactor, verb);
        }

        /// <summary>باز کردن: نوشتن پرچم + نشان دادن محتوا. مهر فقط همین یک بار می‌شکند.</summary>
        private void Open(GameObject interactor)
        {
            if (BoundInstance != null)
            {
                ItemResult result = ItemRuntime.Perform(BoundInstance, ItemVerb.Open);

                if (!result.Ok)
                {
                    Debug.LogWarning("[File] باز کردن ممکن نشد: " + result.ReasonKey, this);
                    return;
                }

                _opened = true;

                Debug.Log("[File] seal broken: " + Title, this);

                ShowContent();
                return;
            }

            if (_opened)
            {
                ShowContent();
                return;
            }

            _opened = true;

            if (!string.IsNullOrWhiteSpace(openedVariable) && WorldStateProvider.Instance != null)
                DialogueRunner.ApplyEffect(openedVariable.Trim() + " = true", WorldStateProvider.Instance.State);

            Debug.Log("[File] seal broken: " + Title, this);

            ShowContent();
        }

        /// <summary>
        /// «خوانده شد» را روی نمونه می‌نویسد. اگر آیتم شناخته نشود، فقط نمایش
        /// انجام می‌شود تا رفتار قبلی صحنه عوض نشود.
        /// </summary>
        private void MarkRead()
        {
            if (BoundInstance == null) return;

            if (ItemRuntime.CanPerform(BoundInstance, ItemVerb.Read))
                ItemRuntime.Perform(BoundInstance, ItemVerb.Read);
        }

        /// <summary>
        /// یک جا برای هر دو کار: سند کاغذی اول، وگرنه دیالوگ.
        /// «خواندن» هیچ چیزی در دنیا عوض نمی‌کند؛ فقط دوباره نشان می‌دهد.
        /// </summary>
        private void ShowContent()
        {
            // اگر مسیر سند اینجا خالی بود، از تعریف آیتم (doc=) بخوان.
            string path = documentPath;

            if (string.IsNullOrWhiteSpace(path) && BoundInstance != null)
                path = BoundInstance.Definition.DocumentPath;

            if (!string.IsNullOrWhiteSpace(path))
            {
                if (DocumentLayer.ShowResource(path, null)) return;

                Debug.LogWarning("[File] سند «" + path + "» پیدا نشد؛ برگشت به دیالوگ.", this);
            }

            if (contentReader != null && contentReader.HasDialogue)
            {
                contentReader.StartDialogue();
                return;
            }

            Debug.LogWarning("[File] فایل محتوا انتخاب نشده؛ متن پرونده نمایش داده نمی‌شود.", this);
        }
    }
}
