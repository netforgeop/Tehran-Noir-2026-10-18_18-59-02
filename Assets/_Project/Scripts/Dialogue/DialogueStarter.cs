using UnityEngine;

namespace TehranNoir.Dialogue
{
    /// پخش یک فایل دیالوگ از هر جای بازی: NPC، صحنه‌ی بازجویی، تست.
    /// هر کسی که این کامپوننت را دارد، فقط StartDialogue را صدا می‌زند.
    public sealed class DialogueStarter : MonoBehaviour
    {
        [Header("فایل")]
        [SerializeField] private TextAsset dialogueFile;
        [SerializeField] private string startNodeId = string.Empty;

        [Header("پنل")]
        [SerializeField] private DialogueUI dialogueUI;

        public bool HasDialogue { get { return dialogueFile != null; } }

        [ContextMenu("شروع دیالوگ")]
        public void StartDialogue()
        {
            StartDialogue(startNodeId);
        }

        /// <summary>
        /// پخش از یک گره مشخص. کار «دادن پرونده» از همین استفاده می‌کند تا
        /// دیالوگ از گره‌ی handover شروع شود، نه از گره‌ی اول.
        /// </summary>
        public void StartDialogue(string nodeId)
        {
            if (dialogueFile == null)
            {
                Debug.LogError("[Dialogue] فایل دیالوگ انتخاب نشده: " + name);
                return;
            }

            if (dialogueUI == null)
            {
                Debug.LogError("[Dialogue] فیلد Dialogue UI خالی است: " + name);
                return;
            }

            // اگر پنل باز است، با زدن دوباره‌ی دکمه، دیالوگ از اول شروع نشود
            if (dialogueUI.IsOpen) return;

            DialogueDocument document = DialogueParser.Parse(dialogueFile.text, dialogueFile.name);

            for (int i = 0; i < document.Errors.Count; i++)
                Debug.LogError("[Dialogue] " + dialogueFile.name + " → " + document.Errors[i]);

            for (int i = 0; i < document.Warnings.Count; i++)
                Debug.LogWarning("[Dialogue] " + dialogueFile.name + " → " + document.Warnings[i]);

            if (document.HasErrors) return;

            WorldState world = WorldStateProvider.Instance != null
                ? WorldStateProvider.Instance.State
                : null;

            dialogueUI.Play(document, world, nodeId);
        }
    }
}
