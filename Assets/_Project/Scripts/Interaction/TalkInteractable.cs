using UnityEngine;
using TehranNoir.Core;
using TehranNoir.Dialogue;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// آدم قابل گفتگو. نام و نقش را از بخش Identity خودِ Interactable می‌گیرد
    /// (دوزبانه: «رضا|Reza»)؛ فایل دیالوگ از DialogueStarter روی همین آبجکت خوانده می‌شود.
    /// دکمه و متن خودکارند.
    /// </summary>
    public class TalkInteractable : Interactable
    {
        [Header("Dialogue")]
        [Tooltip("خالی بماند تا خودکار روی همین آبجکت پیدا شود")]
        [SerializeField] private DialogueStarter dialogueStarter;

        [Tooltip("اگر DialogueStarter نبود یا فایل نداشت، فقط یک خط در Console می‌نویسد")]
        [SerializeField] private bool logInteraction = true;

        // همیشه Talk است؛ فیلد Action در Inspector برای این کلاس نادیده گرفته می‌شود
        public override InteractionAction Action => InteractionAction.Talk;

        private void Awake()
        {
            if (dialogueStarter == null)
                dialogueStarter = GetComponent<DialogueStarter>();
        }

        public override void Interact(GameObject interactor)
        {
            if (dialogueStarter != null && dialogueStarter.HasDialogue)
            {
                dialogueStarter.StartDialogue();
                return;
            }

            if (logInteraction)
                Debug.Log(Title + ": ...", this);
        }
    }
}
