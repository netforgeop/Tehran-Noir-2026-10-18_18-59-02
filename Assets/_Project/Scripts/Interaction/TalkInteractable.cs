using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Core;
using TehranNoir.Dialogue;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// آدم قابل گفتگو. نام و نقش را از بخش Identity خودِ Interactable می‌گیرد
    /// (دوزبانه: «رضا|Reza»)؛ فایل دیالوگ از DialogueStarter روی همین آبجکت خوانده می‌شود.
    /// دکمه و متن خودکارند.
    ///
    /// اگر متغیر «دادن» در دنیا درست باشد، یک کار دوم هم می‌آید («دادن») که
    /// همان فایل دیالوگ را از گره‌ی مشخصی شروع می‌کند. این‌طوری تحویل پرونده
    /// یک کار دیدنی است، نه یک خط پنهان داخل گره‌ی صحبت.
    /// </summary>
    public class TalkInteractable : Interactable
    {
        [Header("Dialogue")]
        [Tooltip("خالی بماند تا خودکار روی همین آبجکت پیدا شود")]
        [SerializeField] private DialogueStarter dialogueStarter;

        [Tooltip("اگر DialogueStarter نبود یا فایل نداشت، فقط یک خط در Console می‌نویسد")]
        [SerializeField] private bool logInteraction = true;

        [Header("Handover")]
        [Tooltip("تا وقتی این متغیر درست است، کار «دادن» هم نمایش داده می‌شود (مثلاً flag.file.taken). خالی = فقط صحبت")]
        [SerializeField] private string handoverVariable = string.Empty;

        [Tooltip("کار «دادن» دیالوگ را از این گره شروع می‌کند")]
        [SerializeField] private string handoverNode = "handover";

        // همیشه Talk است؛ فیلد Action در Inspector برای این کلاس نادیده گرفته می‌شود
        public override InteractionAction Action => InteractionAction.Talk;

        private readonly List<InteractionAction> _verbs = new List<InteractionAction>(2);

        private void Awake()
        {
            if (dialogueStarter == null)
                dialogueStarter = GetComponent<DialogueStarter>();
        }

        /// <summary>کار «دادن» فقط وقتی معنی دارد که چیزی در دست بازیکن باشد.</summary>
        private bool CanHandOver
        {
            get
            {
                if (string.IsNullOrWhiteSpace(handoverVariable)) return false;
                if (WorldStateProvider.Instance == null) return false;

                return WorldStateProvider.Instance.Get(handoverVariable.Trim()).Truthy();
            }
        }

        /// <summary>۱) صحبت  ۲) دادن — دومی فقط با داشتن آن چیز.</summary>
        public override IReadOnlyList<InteractionAction> Verbs
        {
            get
            {
                _verbs.Clear();
                _verbs.Add(InteractionAction.Talk);

                if (CanHandOver) _verbs.Add(InteractionAction.Give);

                return _verbs;
            }
        }

        public override void Interact(GameObject interactor, InteractionAction verb)
        {
            if (verb == InteractionAction.Give)
            {
                HandOver();
                return;
            }

            base.Interact(interactor, verb);
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

        /// <summary>تحویل: همان فایل، ولی از گره‌ی تحویل.</summary>
        private void HandOver()
        {
            if (dialogueStarter == null)
                dialogueStarter = GetComponent<DialogueStarter>();

            if (dialogueStarter == null || !dialogueStarter.HasDialogue)
            {
                if (logInteraction) Debug.Log(Title + ": " + Localization.Action(InteractionAction.Give), this);
                return;
            }

            string node = string.IsNullOrWhiteSpace(handoverNode) ? null : handoverNode.Trim();

            dialogueStarter.StartDialogue(node);
        }
    }
}
