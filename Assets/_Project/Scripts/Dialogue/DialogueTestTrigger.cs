using UnityEngine;
using UnityEngine.InputSystem;

namespace TehranNoir.Dialogue
{
    /// شروع سریع یک فایل دیالوگ روی DialogueUI — برای تست، تا وقتی سیستم تعامل وصل شود.
    public sealed class DialogueTestTrigger : MonoBehaviour
    {
        [Header("فایل و پنل")]
        [SerializeField] private TextAsset dialogueFile;
        [SerializeField] private DialogueUI dialogueUI;

        [Header("کلید")]
        [SerializeField] private Key startKey = Key.Space;

        [Header("دنیای تست")]
        [Tooltip("جهان بین دیالوگ‌ها حفظ می‌شود؛ یعنی met.reza بعد از اولین صحبت ثبت می‌ماند.")]
        [SerializeField] private bool keepWorldBetweenDialogues = true;
        [SerializeField] private string[] initialVariables = new string[0];

        private WorldState _world;

        private void Awake()
        {
            if (dialogueUI == null) dialogueUI = GetComponent<DialogueUI>();
        }

        private void Update()
        {
            if (dialogueUI == null || dialogueUI.IsOpen) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[startKey].wasPressedThisFrame) StartDialogue();
        }

        [ContextMenu("ریست جهان")]
        public void ResetWorld()
        {
            _world = null;
            Debug.Log("[Dialogue] وضعیت جهان پاک شد. دوباره Space بزن.");
        }

        [ContextMenu("شروع دیالوگ")]
        public void StartDialogue()
        {
            if (dialogueFile == null)
            {
                Debug.LogError("[Dialogue] فایل دیالوگ انتخاب نشده است.");
                return;
            }

            if (dialogueUI == null)
            {
                Debug.LogError("[Dialogue] DialogueUI در صحنه پیدا نشد.");
                return;
            }

            DialogueDocument document = DialogueParser.Parse(dialogueFile.text, dialogueFile.name);

            for (int i = 0; i < document.Errors.Count; i++)
                Debug.LogError("[Dialogue] " + dialogueFile.name + " → " + document.Errors[i]);

            for (int i = 0; i < document.Warnings.Count; i++)
                Debug.LogWarning("[Dialogue] " + dialogueFile.name + " → " + document.Warnings[i]);

            if (document.HasErrors) return;

            WorldState world = WorldStateProvider.Instance != null ? WorldStateProvider.Instance.State : null;

            if (world == null)
            {
                bool createWorld = _world == null || !keepWorldBetweenDialogues;

                if (createWorld)
                {
                    _world = WorldState.CreateDefault(document);

                    for (int i = 0; i < initialVariables.Length; i++)
                        DialogueRunner.ApplyEffect(initialVariables[i], _world);
                }

                world = _world;
            }

            dialogueUI.Play(document, world);
        }
    }
}
