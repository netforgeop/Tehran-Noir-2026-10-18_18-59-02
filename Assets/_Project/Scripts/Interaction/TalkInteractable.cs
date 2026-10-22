using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// آدم قابل گفتگو. اسم و نقش را می‌گیرد؛ فایل دیالوگ از DialogueStarter
    /// روی همین آبجکت خوانده می‌شود. دکمه و متن خودکارند.
    /// </summary>
    public class TalkInteractable : Interactable
    {
        [Header("Person")]
        [SerializeField] private string displayName = "Unknown";
        [SerializeField] private string role = "";

        [Header("Dialogue")]
        [Tooltip("خالی بماند تا خودکار روی همین آبجکت پیدا شود")]
        [SerializeField] private DialogueStarter dialogueStarter;

        [Tooltip("اگر DialogueStarter نبود یا فایل نداشت، فقط یک خط در Console می‌نویسد")]
        [SerializeField] private bool logInteraction = true;

        // همیشه Talk است؛ فیلد Action در Inspector برای این کلاس نادیده گرفته می‌شود
        public override InteractionAction Action => InteractionAction.Talk;

        public override string Detail =>
            string.IsNullOrWhiteSpace(role) ? displayName : $"{displayName}\n{role}";

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
                Debug.Log($"{displayName} ({role}): ...", this);
        }
    }
}
