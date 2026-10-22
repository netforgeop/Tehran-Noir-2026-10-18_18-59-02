using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TehranNoir.Dialogue
{
    /// پنل دیالوگ: جعبه‌ی متن، نام گوینده و گزینه‌ها.
    /// تمام UI در زمان اجرا ساخته می‌شود؛ هیچ Canvas دستی لازم نیست.
    [DisallowMultipleComponent]
    public sealed class DialogueUI : MonoBehaviour
    {
        [Header("ظاهر")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private bool persian = true;
        [SerializeField] private Vector2 panelSize = new Vector2(1100f, 380f);
        [SerializeField] private float panelBottom = 40f;

        [Header("زمان‌بندی")]
        [SerializeField] private float secondsPerWord = 0.35f;
        [SerializeField] private float minimumLineTime = 1.2f;

        [Header("ورودی و توقف بازی")]
        [SerializeField] private bool releaseCursorWhileOpen = true;
        [SerializeField] private MonoBehaviour[] pauseWhileOpen = new MonoBehaviour[0];

        private static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.05f, 0.92f);
        private static readonly Color SpeakerColor = new Color(0.85f, 0.74f, 0.45f);
        private static readonly Color BodyColor = new Color(0.94f, 0.90f, 0.82f);
        private static readonly Color HintColor = new Color(0.55f, 0.52f, 0.45f);
        private static readonly Color OptionColor = new Color(0.13f, 0.12f, 0.10f, 0.96f);
        private static readonly Color OptionSelectedColor = new Color(0.26f, 0.22f, 0.14f, 1f);
        private static readonly Color OptionTextColor = new Color(0.88f, 0.85f, 0.78f);
        private static readonly Color OptionTextSelectedColor = new Color(1f, 0.92f, 0.65f);

        private sealed class OptionRow
        {
            public RectTransform Rect;
            public Image Background;
            public TMP_Text Label;
            public LayoutElement Layout;
        }

        private readonly List<OptionRow> _rows = new List<OptionRow>();

        private RectTransform _panel;
        private RectTransform _optionsRoot;
        private TMP_Text _speakerText;
        private TMP_Text _bodyText;

        private float _timer;
        private float _duration;
        private int _selected;
        private bool _built;
        private bool _cursorReleased;
        private bool[] _pauseSnapshot;
        private CursorLockMode _savedLockMode;
        private bool _savedCursorVisible;

        private static Type _rtlType;
        private static bool _rtlSearched;

        public DialogueRunner Runner { get; private set; }

        /// <summary>پنل باز فعلی؛ null یعنی هیچ دیالوگی باز نیست.</summary>
        public static DialogueUI Current { get; private set; }

        public bool IsOpen { get { return _panel != null && _panel.gameObject.activeSelf; } }

        private void Awake()
        {
            Runner = new DialogueRunner();
            Runner.LineShown += HandleLine;
            Runner.OptionsShown += HandleOptions;
            Runner.Finished += HandleFinished;

            Build();
            _panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;

            if (Runner == null) return;

            Runner.LineShown -= HandleLine;
            Runner.OptionsShown -= HandleOptions;
            Runner.Finished -= HandleFinished;
        }

        /* ============================================================
           API
           ============================================================ */

        public void Play(DialogueDocument document, WorldState world, string startNodeId = null)
        {
            if (document == null)
            {
                Debug.LogError("[Dialogue] سند دیالوگ خالی است.");
                return;
            }

            if (document.HasErrors)
            {
                Debug.LogError("[Dialogue] سند خطا دارد و پخش نمی‌شود.");
                return;
            }

            if (IsOpen) Close();

            _timer = 0f;
            _duration = 0f;
            _selected = 0;
            HideOptions();

            _panel.gameObject.SetActive(true);
            ApplyCursorRelease();
            ApplyPause(true);

            Current = this;

            Runner.Begin(document, world, startNodeId);
        }

        public void Close()
        {
            if (Runner != null && Runner.IsRunning) Runner.Stop();

            HidePanel();
        }

        /* ============================================================
           INPUT
           ============================================================ */

        private void Update()
        {
            if (Runner == null || !Runner.IsRunning) return;

            if (Runner.IsWaitingForChoice)
            {
                UpdateChoiceInput();
                return;
            }

            if (Runner.CurrentLine == null) return;

            if (AdvancePressed())
            {
                Runner.Advance();
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= _duration) Runner.Advance();
        }

        private bool AdvancePressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame) return true;
                if (keyboard.enterKey.wasPressedThisFrame) return true;
                if (keyboard.numpadEnterKey.wasPressedThisFrame) return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonEast.wasPressedThisFrame) return true;
                if (gamepad.buttonSouth.wasPressedThisFrame) return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return PointerInside(_panel, mouse.position.ReadValue());

            return false;
        }

        private void UpdateChoiceInput()
        {
            int count = Runner.CurrentOptions.Count;
            if (count == 0) return;

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null)
            {
                for (int i = 0; i < count && i < 9; i++)
                {
                    Key key = (Key)((int)Key.Digit1 + i);
                    if (!keyboard[key].wasPressedThisFrame) continue;

                    Choose(i);
                    return;
                }

                if (keyboard.downArrowKey.wasPressedThisFrame) MoveSelection(1);
                else if (keyboard.upArrowKey.wasPressedThisFrame) MoveSelection(-1);
                else if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    Choose(_selected);
                    return;
                }
            }

            if (gamepad != null)
            {
                if (gamepad.dpad.down.wasPressedThisFrame) MoveSelection(1);
                else if (gamepad.dpad.up.wasPressedThisFrame) MoveSelection(-1);
                else if (gamepad.buttonSouth.wasPressedThisFrame)
                {
                    Choose(_selected);
                    return;
                }
            }

            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();
                bool moved = mouse.delta.ReadValue().sqrMagnitude > 0.01f;

                for (int i = 0; i < _rows.Count && i < count; i++)
                {
                    if (!PointerInside(_rows[i].Rect, pointer)) continue;

                    if (_selected != i && moved)
                    {
                        _selected = i;
                        RefreshHighlight();
                    }

                    if (mouse.leftButton.wasPressedThisFrame) Choose(i);
                    return;
                }
            }
        }

        private static bool PointerInside(RectTransform rect, Vector2 screenPoint)
        {
            if (rect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        /* ============================================================
           RUNNER EVENTS
           ============================================================ */

        private void HandleLine(SayItem line)
        {
            string text = line.Text.Get(persian);

            _bodyText.text = text;
            _bodyText.alignment = persian ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

            _speakerText.text = ResolveSpeakerName(line.Speaker);

            HideOptions();

            _timer = 0f;
            _duration = Mathf.Max(minimumLineTime, CountWords(text) * secondsPerWord);
        }

        private void HandleOptions(IReadOnlyList<OptionItem> options)
        {
            if (options == null || options.Count == 0) return;

            EnsureRows(options.Count);

            const float optionsHeight = 130f;
            float rowHeight = Mathf.Clamp((optionsHeight - (options.Count - 1) * 3f) / options.Count, 22f, 30f);

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < options.Count;
                _rows[i].Rect.gameObject.SetActive(used);
                if (!used) continue;

                _rows[i].Layout.preferredHeight = rowHeight;

                _rows[i].Label.text = (i + 1) + ". " + options[i].Text.Get(persian);
                _rows[i].Label.alignment = persian ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            }

            _selected = 0;
            RefreshHighlight();
        }

        private void HandleFinished()
        {
            HidePanel();
        }

        private void Choose(int index)
        {
            if (Runner == null || !Runner.IsWaitingForChoice) return;
            if (index < 0 || index >= Runner.CurrentOptions.Count) return;

            HideOptions();
            Runner.Choose(index);
        }

        private void MoveSelection(int delta)
        {
            int count = Runner.CurrentOptions.Count;
            if (count == 0) return;

            _selected = (_selected + delta + count) % count;
            RefreshHighlight();
        }

        private void RefreshHighlight()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                bool selected = i == _selected;
                _rows[i].Background.color = selected ? OptionSelectedColor : OptionColor;
                _rows[i].Label.color = selected ? OptionTextSelectedColor : OptionTextColor;
            }
        }

        private void HideOptions()
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].Rect.gameObject.SetActive(false);
        }

        private string ResolveSpeakerName(string speakerId)
        {
            DialogueDocument document = Runner != null ? Runner.Document : null;

            if (document != null && string.Equals(document.Person.Id, speakerId, StringComparison.OrdinalIgnoreCase))
                return document.Person.Name.Get(persian);

            if (string.IsNullOrEmpty(speakerId)) return string.Empty;
            return char.ToUpperInvariant(speakerId[0]) + speakerId.Substring(1);
        }

        /* ============================================================
           PANEL STATE
           ============================================================ */

        private void HidePanel()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
            if (Current == this) Current = null;

            RestoreCursor();
            ApplyPause(false);
        }

        private void ApplyCursorRelease()
        {
            if (!releaseCursorWhileOpen || _cursorReleased) return;

            _savedLockMode = Cursor.lockState;
            _savedCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorReleased = true;
        }

        private void RestoreCursor()
        {
            if (!_cursorReleased) return;

            Cursor.lockState = _savedLockMode;
            Cursor.visible = _savedCursorVisible;
            _cursorReleased = false;
        }

        private void ApplyPause(bool paused)
        {
            if (pauseWhileOpen == null || pauseWhileOpen.Length == 0) return;

            if (paused)
            {
                if (_pauseSnapshot == null || _pauseSnapshot.Length != pauseWhileOpen.Length)
                    _pauseSnapshot = new bool[pauseWhileOpen.Length];

                for (int i = 0; i < pauseWhileOpen.Length; i++)
                {
                    if (pauseWhileOpen[i] == null) continue;

                    _pauseSnapshot[i] = pauseWhileOpen[i].enabled;
                    pauseWhileOpen[i].enabled = false;
                }

                return;
            }

            if (_pauseSnapshot == null) return;

            for (int i = 0; i < pauseWhileOpen.Length && i < _pauseSnapshot.Length; i++)
            {
                if (pauseWhileOpen[i] == null) continue;
                pauseWhileOpen[i].enabled = _pauseSnapshot[i];
            }
        }

        /* ============================================================
           BUILD
           ============================================================ */

        private void Build()
        {
            if (_built) return;

            if (font == null)
                Debug.LogWarning("[Dialogue] فونت TMP انتخاب نشده؛ متن فارسی ممکن است درست نمایش داده نشود.");

            GameObject canvasObject = new GameObject("DialogueCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _panel = CreateNode("Panel", canvasObject.transform);
            _panel.anchorMin = new Vector2(0.5f, 0f);
            _panel.anchorMax = new Vector2(0.5f, 0f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.anchoredPosition = new Vector2(0f, panelBottom);
            _panel.sizeDelta = panelSize;

            Image background = _panel.gameObject.AddComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;

            RectTransform speakerRect = CreateNode("Speaker", _panel);
            speakerRect.anchorMin = new Vector2(1f, 1f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(1f, 1f);
            speakerRect.anchoredPosition = new Vector2(-36f, -20f);
            speakerRect.sizeDelta = new Vector2(560f, 40f);
            _speakerText = CreateText("SpeakerText", speakerRect, 28f, FontStyles.Bold, SpeakerColor);

            RectTransform hintRect = CreateNode("Hint", _panel);
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(0f, 1f);
            hintRect.pivot = new Vector2(0f, 1f);
            hintRect.anchoredPosition = new Vector2(36f, -24f);
            hintRect.sizeDelta = new Vector2(460f, 28f);
            TMP_Text hint = CreateText("HintText", hintRect, 18f, FontStyles.Normal, HintColor);
            hint.alignment = TextAlignmentOptions.TopLeft;
            hint.text = "ادامه: Space یا B";

            RectTransform bodyRect = CreateNode("Body", _panel);
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(36f, 150f);
            bodyRect.offsetMax = new Vector2(-36f, -72f);
            _bodyText = CreateText("BodyText", bodyRect, 32f, FontStyles.Normal, BodyColor);

            _optionsRoot = CreateNode("Options", _panel);
            _optionsRoot.anchorMin = new Vector2(0f, 0f);
            _optionsRoot.anchorMax = new Vector2(1f, 0f);
            _optionsRoot.pivot = new Vector2(0.5f, 0f);
            _optionsRoot.offsetMin = new Vector2(36f, 12f);
            _optionsRoot.offsetMax = new Vector2(-36f, 142f);

            VerticalLayoutGroup layout = _optionsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _built = true;
        }

        private void EnsureRows(int count)
        {
            while (_rows.Count < count)
            {
                RectTransform rect = CreateNode("Option" + (_rows.Count + 1), _optionsRoot);

                Image background = rect.gameObject.AddComponent<Image>();
                background.color = OptionColor;
                background.raycastTarget = false;

                LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 30f;
                element.preferredHeight = 30f;

                TMP_Text label = CreateText("Label", rect, 24f, FontStyles.Normal, OptionTextColor);

                OptionRow row = new OptionRow();
                row.Rect = rect;
                row.Background = background;
                row.Label = label;
                row.Layout = element;
                _rows.Add(row);
            }
        }

        private static RectTransform CreateNode(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private TMP_Text CreateText(string name, RectTransform parent, float size, FontStyles style, Color color)
        {
            RectTransform rect = CreateNode(name, parent);
            Stretch(rect);

            TMP_Text text = AddTextComponent(rect.gameObject);

            if (font != null) text.font = font;

            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = persian ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

            return text;
        }

        /// اگر RTLTMPro نصب باشد از آن استفاده می‌کند؛ وگرنه TextMeshPro معمولی.
        private static TMP_Text AddTextComponent(GameObject go)
        {
            Type rtl = FindRtlTextType();

            if (rtl != null)
            {
                Component component = go.AddComponent(rtl);
                TMP_Text text = component as TMP_Text;

                if (text != null)
                {
                    SetBoolMember(component, "Farsi", true);
                    SetBoolMember(component, "FixTags", true);
                    return text;
                }
            }

            return go.AddComponent<TextMeshProUGUI>();
        }

        private static Type FindRtlTextType()
        {
            if (_rtlSearched) return _rtlType;

            _rtlSearched = true;

            // ۱) اگر RTLTMPro داخل Assets پروژه باشد، اسمبلی همان Assembly-CSharp است.
            _rtlType = typeof(DialogueUI).Assembly.GetType("RTLTMPro.RTLTextMeshPro", false);
            if (_rtlType != null) return _rtlType;

            // ۲) اگر به شکل پکیج نصب شده باشد، اسمبلی خودش RTLTMPro نام دارد.
            _rtlType = Type.GetType("RTLTMPro.RTLTextMeshPro, RTLTMPro", false);
            return _rtlType;
        }

        private static void SetBoolMember(Component component, string name, bool value)
        {
            if (component == null) return;

            Type type = component.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
            {
                property.SetValue(component, value);
                return;
            }

            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(component, value);
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            string[] parts = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length;
        }
    }
}
