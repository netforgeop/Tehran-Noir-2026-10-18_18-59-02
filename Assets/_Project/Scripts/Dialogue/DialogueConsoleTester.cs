using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TehranNoir.Dialogue
{
    /// تست موتور دیالوگ در Console — بدون UI.
    /// روی یک GameObject خالی بگذار و فایل .txt را به آن بده.
    public sealed class DialogueConsoleTester : MonoBehaviour
    {
        [Header("فایل")]
        [SerializeField] private TextAsset dialogueFile;

        [Header("کلیدها")]
        [SerializeField] private Key startKey = Key.Space;

        [Header("زمان‌بندی (مثل بازی)")]
        [SerializeField] private float secondsPerWord = 0.35f;
        [SerializeField] private float minimumLineTime = 1.2f;
        [SerializeField] private bool autoAdvance = true;

        [Header("نمایش")]
        [SerializeField] private bool showEnglish;

        [Header("دنیای تست")]
        [SerializeField] private bool resetWorldOnStart = true;
        [SerializeField] private string[] initialVariables = new string[0];

        private readonly DialogueRunner _runner = new DialogueRunner();
        private WorldState _world;
        private float _timer;
        private float _duration;

        private void Awake()
        {
            _runner.LineShown += HandleLine;
            _runner.OptionsShown += HandleOptions;
            _runner.Finished += HandleFinished;
        }

        private void OnDestroy()
        {
            _runner.LineShown -= HandleLine;
            _runner.OptionsShown -= HandleOptions;
            _runner.Finished -= HandleFinished;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (!_runner.IsRunning)
            {
                if (keyboard[startKey].wasPressedThisFrame) StartDialogue();
                return;
            }

            if (_runner.IsWaitingForChoice)
            {
                ReadChoiceKeys(keyboard);
                return;
            }

            if (_runner.CurrentLine == null) return;

            bool skip = keyboard[startKey].wasPressedThisFrame
                        || keyboard.enterKey.wasPressedThisFrame
                        || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

            if (skip)
            {
                _runner.Advance();
                return;
            }

            if (!autoAdvance) return;

            _timer += Time.deltaTime;
            if (_timer >= _duration) _runner.Advance();
        }

        [ContextMenu("شروع دیالوگ")]
        public void StartDialogue()
        {
            if (dialogueFile == null)
            {
                Debug.LogError("[Dialogue] فایل دیالوگ در Inspector انتخاب نشده است.");
                return;
            }

            DialogueDocument document = DialogueParser.Parse(dialogueFile.text, dialogueFile.name);

            for (int i = 0; i < document.Errors.Count; i++)
                Debug.LogError("[Dialogue] " + dialogueFile.name + " → " + document.Errors[i]);

            for (int i = 0; i < document.Warnings.Count; i++)
                Debug.LogWarning("[Dialogue] " + dialogueFile.name + " → " + document.Warnings[i]);

            if (document.HasErrors) return;

            if (_world == null || resetWorldOnStart) _world = WorldState.CreateDefault(document);

            for (int i = 0; i < initialVariables.Length; i++)
                DialogueRunner.ApplyEffect(initialVariables[i], _world);

            Debug.Log("[Dialogue] شروع «" + document.Person.Id + "» — " + document.Nodes.Count + " گره، " +
                      document.Variables.Count + " متغیر.");

            _runner.Begin(document, _world);
        }

        private void HandleLine(SayItem line)
        {
            string persian = line.Text.Get(true);
            string english = line.Text.Get(false);

            string message = "[" + line.Speaker + "] " + persian;
            if (showEnglish && english != persian) message += "\n            " + english;

            Debug.Log("[Dialogue] " + message);

            _timer = 0f;
            _duration = Mathf.Max(minimumLineTime, CountWords(persian) * secondsPerWord);
        }

        private void HandleOptions(IReadOnlyList<OptionItem> options)
        {
            Debug.Log("[Dialogue] — گزینه‌ها —");

            for (int i = 0; i < options.Count; i++)
                Debug.Log("[Dialogue] " + (i + 1) + ") " + options[i].Text.Get(true));
        }

        private void HandleFinished()
        {
            Debug.Log("[Dialogue] پایان دیالوگ. برای شروع دوباره " + startKey + " را بزن.");
        }

        private void ReadChoiceKeys(Keyboard keyboard)
        {
            for (int i = 0; i < 9; i++)
            {
                Key key = (Key)((int)Key.Digit1 + i);
                if (!keyboard[key].wasPressedThisFrame) continue;

                _runner.Choose(i);
                return;
            }
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            string[] parts = text.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length;
        }
    }
}
