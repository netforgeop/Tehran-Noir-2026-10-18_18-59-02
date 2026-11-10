using UnityEngine;
using TehranNoir.Core;
using TehranNoir.Dialogue;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// شیء قابل تعامل بدون رفتار خاص: قفسه، دفتر ثبت، یادداشت.
    /// اگر Dialogue Starter روی همان آبجکت فایل داشته باشد، دیالوگ را پخش می‌کند؛
    /// وگرنه فقط یک خط در Console می‌نویسد. این‌طوری خواندنی‌ها کد تازه لازم ندارند.
    /// </summary>
    public class SimpleInteractable : Interactable
    {
        [Header("Dialogue")]
        [Tooltip("خالی بماند تا خودکار روی همین آبجکت پیدا شود")]
        [SerializeField] private DialogueStarter dialogueStarter;

        [Tooltip("از این گره شروع کن؛ خالی = گره‌ی اول فایل")]
        [SerializeField] private string startNodeId = string.Empty;

        [Tooltip("اگر دیالوگی نبود، فقط یک خط در Console بنویس")]
        [SerializeField] private bool logInteraction = true;

        private void Awake()
        {
            if (dialogueStarter == null)
                dialogueStarter = GetComponent<DialogueStarter>();
        }

        public override void Interact(GameObject interactor)
        {
            if (dialogueStarter != null && dialogueStarter.HasDialogue)
            {
                dialogueStarter.StartDialogue(startNodeId);
                return;
            }

            if (logInteraction)
                Debug.Log(Title + ": " + Localization.Action(Action), this);
        }
    }
}
