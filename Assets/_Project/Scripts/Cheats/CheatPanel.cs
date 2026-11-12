using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TehranNoir.Cameras;
using TehranNoir.Characters;
using TehranNoir.Core;
using TehranNoir.Dialogue;
using TehranNoir.Documents;
using TehranNoir.Inputs;
using TehranNoir.Interaction;
using TehranNoir.Player;
using TehranNoir.Settings;
using TehranNoir.UI;

namespace TehranNoir.Debugging
{
    /// <summary>
    /// پنل تست بازی: سه تب دارد و هیچ چیزی را از خودش نگه نمی‌دارد.
    ///
    ///   کاراکتر — پول، جان، شرافت، جذبه، شهرت، صداقت، وفاداری
    ///   دنیا    — همه‌ی متغیرهای WorldState، همان‌هایی که دیالوگ‌ها می‌خوانند
    ///   دیالوگ  — هر DialogueStarter داخل صحنه را از اول پخش می‌کند
    ///
    /// چرا این‌طور ساخته شده: هر تغییری که این پنل می‌دهد، در WorldState نوشته می‌شود؛
    /// پس بازی دقیقاً همان‌طور واکنش می‌دهد که انگار خودت آن مسیر را رفته‌ای.
    /// پنل در Build نهایی وجود ندارد (بوت‌استرپ فقط در Editor و Development Build اجرا می‌شود).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CheatPanel : MonoBehaviour
    {
        private enum Tab
        {
            Character = 0,
            World = 1,
            Dialogue = 2
        }

        /* ---------- چیدمان (واحد کانواس ۱۹۲۰×۱۰۸۰) ---------- */
        private const float PanelX = 120f;
        private const float PanelY = 88f;
        private const float PanelWidth = 1680f;
        private const float PanelHeight = 904f;
        private const float PadX = 44f;

        private const float DividerTop = 112f;
        private const float TabTop = 124f;
        private const float TabHeight = 40f;
        private const float TabWidth = 240f;
        private const float TabGap = 12f;

        private const float ListTop = 186f;
        private const float RowHeight = 46f;
        private const int VisibleRows = 13;

        private const float EditorTop = 792f;
        private const float EditorHeight = 46f;

        private const float FooterTop = 850f;
        private const float FooterHeight = 30f;

        private const int TabCount = 3;

        /* ---------- رنگ ---------- */
        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.66f);
        private static readonly Color PanelColor = new Color(0.075f, 0.084f, 0.098f, 0.99f);
        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0f);
        private static readonly Color RowSelectedColor = new Color(0.16f, 0.18f, 0.20f, 0.9f);
        private static readonly Color TitleColor = new Color(0.941f, 0.933f, 0.914f, 1f);
        private static readonly Color ValueColor = new Color(0.85f, 0.87f, 0.80f, 1f);
        private static readonly Color DimColor = new Color(0.55f, 0.57f, 0.53f, 1f);
        private static readonly Color RuleColor = new Color(0.227f, 0.227f, 0.227f, 1f);
        private static readonly Color AccentColor = new Color(0.541f, 0.604f, 0.357f, 1f);
        private static readonly Color EditorColor = new Color(0.17f, 0.19f, 0.21f, 0.95f);

        /* ---------- وضعیت ---------- */
        public static bool IsOpen { get; private set; }

        [Tooltip("کلید باز و بسته کردن پنل")]
        [SerializeField] private Key toggleKey = Key.F10;

        private sealed class Row
        {
            public string Label;
            public Func<string> Read;
            public Action<int, bool> Adjust;
            public Action Activate;
        }

        private sealed class RowView
        {
            public RectTransform Rect;
            public Image Background;
            public Image Bar;
            public TMP_Text Label;
            public TMP_Text Value;
            public Button Button;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly List<RowView> _rowViews = new List<RowView>(VisibleRows);
        private readonly List<TMP_Text> _tabLabels = new List<TMP_Text>(TabCount);
        private readonly List<GameObject> _tabBars = new List<GameObject>(TabCount);
        private readonly List<Behaviour> _frozen = new List<Behaviour>();

        private GameObject _canvas;
        private RectTransform _panel;
        private TMP_Text _title;
        private TMP_Text _hint;
        private TMP_Text _footer;
        private RectTransform _editorBox;
        private TMP_Text _editorText;

        private TMP_FontAsset _font;
        private PauseMenuUI _pause;

        private Tab _tab = Tab.Character;
        private int _selected;
        private int _scroll;
        private bool _persian;
        private bool _languageDirty;

        private bool _editing;
        private string _editName = string.Empty;
        private string _editBuffer = string.Empty;

        private float _refreshTimer;
        private int _resumeFrame;
        private CursorLockMode _previousLock;
        private bool _previousVisible;

        private static WorldState World
        {
            get
            {
                return WorldStateProvider.Instance != null
                    ? WorldStateProvider.Instance.State
                    : null;
            }
        }

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            _persian = Localization.Persian;

            DialogueUI ui = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
            if (ui != null) _font = ui.Font;
            if (_font == null) _font = TMP_Settings.defaultFontAsset;

            Build();
            _canvas.SetActive(false);

            IsOpen = false;
        }

        private void OnEnable()
        {
            Loc.Changed += HandleLanguage;
        }

        private void OnDisable()
        {
            Loc.Changed -= HandleLanguage;
        }

        private void OnDestroy()
        {
            if (IsOpen)
            {
                IsOpen = false;
                Unfreeze();
                RestoreCursor();
            }

            StopListening();
        }

        private void HandleLanguage()
        {
            _languageDirty = true;
        }

        /* ============================================================
           ورودی
           ============================================================ */

        private void Update()
        {
            if (!IsOpen)
            {
                ResumePauseIfDue();
                TryOpen();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (_editing)
            {
                HandleEditing(keyboard);
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame) SetTab((int)_tab - 1);
            if (keyboard.eKey.wasPressedThisFrame) SetTab((int)_tab + 1);

            bool up = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
            bool down = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;

            if (up) Move(-1);
            if (down) Move(1);

            bool big = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

            if (keyboard.leftArrowKey.wasPressedThisFrame) Adjust(-1, big);
            if (keyboard.rightArrowKey.wasPressedThisFrame) Adjust(1, big);

            if (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame) Activate();

            _refreshTimer += Time.unscaledDeltaTime;

            if (_refreshTimer >= 0.25f)
            {
                _refreshTimer = 0f;
                RefreshValues();
            }
        }

        private void HandleEditing(Keyboard keyboard)
        {
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                CommitEdit();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                EndEdit();
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame && _editBuffer.Length > 0)
            {
                _editBuffer = _editBuffer.Substring(0, _editBuffer.Length - 1);
                UpdateEditorLine();
            }
        }

        private void HandleTextInput(char character)
        {
            if (!_editing) return;
            if (character < 32 || character == 127) return;
            if (_editBuffer.Length >= 48) return;

            _editBuffer += character;
            UpdateEditorLine();
        }

        /* ============================================================
           باز و بسته
           ============================================================ */

        public void Open()
        {
            if (IsOpen) return;

            if (DialogueUI.Current != null) return;
            if (DocumentView.IsOpen) return;
            if (PauseMenuUI.IsOpen) return;

            if (_languageDirty)
            {
                _languageDirty = false;
                _persian = Localization.Persian;
                RebuildCanvas();
            }

            IsOpen = true;

            _previousLock = Cursor.lockState;
            _previousVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _pause = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            if (_pause != null) _pause.enabled = false;

            Freeze();

            CharacterStats.EnsureDefaults();

            _canvas.SetActive(true);

            _tab = Tab.Character;
            _selected = 0;
            _scroll = 0;

            Rebuild();
        }

        public void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;

            EndEdit();

            if (_canvas != null) _canvas.SetActive(false);

            Unfreeze();

            // یک فریم صبر می‌کنیم؛ وگرنه PauseMenuUI همان Esc را می‌بیند و فوراً باز می‌شود
            _resumeFrame = Time.frameCount + 1;

            RestoreCursor();
        }

        private void TryOpen()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!keyboard[toggleKey].wasPressedThisFrame) return;

            Open();
        }

        private void ResumePauseIfDue()
        {
            if (_pause == null || _pause.enabled) return;
            if (Time.frameCount < _resumeFrame) return;

            _pause.enabled = true;
        }

        private void RestoreCursor()
        {
            Cursor.lockState = _previousLock;
            Cursor.visible = _previousVisible;
        }

        private void Freeze()
        {
            _frozen.Clear();

            AddFreeze(FindAnyObjectByType<PlayerController>());
            AddFreeze(FindAnyObjectByType<CameraOrbitController>());
            AddFreeze(FindAnyObjectByType<PlayerInputReader>());
            AddFreeze(FindAnyObjectByType<InteractionSensor>());
        }

        private void AddFreeze(Behaviour behaviour)
        {
            if (behaviour == null || !behaviour.enabled) return;

            behaviour.enabled = false;
            _frozen.Add(behaviour);
        }

        private void Unfreeze()
        {
            for (int i = 0; i < _frozen.Count; i++)
                if (_frozen[i] != null) _frozen[i].enabled = true;

            _frozen.Clear();
        }

        /* ============================================================
           ناوبری
           ============================================================ */

        private void SetTab(int tab)
        {
            int wrapped = ((tab % TabCount) + TabCount) % TabCount;

            if (wrapped == (int)_tab) return;

            EndEdit();

            _tab = (Tab)wrapped;
            _selected = 0;
            _scroll = 0;

            Rebuild();
        }

        private void Move(int delta)
        {
            if (_rows.Count == 0) return;

            _selected = Mathf.Clamp(_selected + delta, 0, _rows.Count - 1);

            EnsureVisible();
            Render();
        }

        private void SelectSlot(int slot)
        {
            int index = _scroll + slot;

            if (index < 0 || index >= _rows.Count) return;

            _selected = index;
            Render();
        }

        private void EnsureVisible()
        {
            if (_selected < _scroll) _scroll = _selected;

            if (_selected >= _scroll + VisibleRows)
                _scroll = _selected - VisibleRows + 1;

            if (_scroll < 0) _scroll = 0;
        }

        private Row Current
        {
            get
            {
                if (_selected < 0 || _selected >= _rows.Count) return null;
                return _rows[_selected];
            }
        }

        private void Adjust(int direction, bool big)
        {
            Row row = Current;
            if (row == null || row.Adjust == null) return;

            EndEdit();
            row.Adjust(direction, big);
            Render();
        }

        private void Activate()
        {
            Row row = Current;
            if (row == null || row.Activate == null) return;

            row.Activate();
        }

        /* ============================================================
           محتوا
           ============================================================ */

        private void Rebuild()
        {
            _rows.Clear();

            if (_tab == Tab.Character) BuildStatRows();
            else if (_tab == Tab.World) BuildWorldRows();
            else BuildDialogueRows();

            if (_selected >= _rows.Count) _selected = Mathf.Max(0, _rows.Count - 1);

            EnsureVisible();
            UpdateTabs();
            UpdateHeader();
            Render();
        }

        private void BuildStatRows()
        {
            IReadOnlyList<StatDefinition> definitions = CharacterStats.All;

            for (int i = 0; i < definitions.Count; i++)
            {
                StatDefinition definition = definitions[i];
                StatId id = definition.Id;

                Row row = new Row();
                row.Label = definition.Label(_persian);
                row.Read = () => CharacterStats.Format(id, _persian);
                row.Adjust = (direction, big) =>
                    CharacterStats.Add(id, direction * (big ? definition.BigStep : definition.Step));
                row.Activate = () =>
                {
                    CharacterStats.Set(id, definition.Default);
                    Render();
                };

                _rows.Add(row);
            }
        }

        private void BuildWorldRows()
        {
            WorldState world = World;
            if (world == null) return;

            List<string> names = new List<string>(world.Names);
            names.Sort(StringComparer.Ordinal);

            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];

                Row row = new Row();
                row.Label = name;
                row.Read = () => ValueText(name);
                row.Adjust = (direction, big) => Nudge(name, direction, big);
                row.Activate = () => BeginEdit(name);

                _rows.Add(row);
            }
        }

        private void BuildDialogueRows()
        {
            DialogueStarter[] starters = FindObjectsByType<DialogueStarter>(FindObjectsInactive.Exclude);
            Array.Sort(starters, (a, b) => string.CompareOrdinal(a.name, b.name));

            for (int i = 0; i < starters.Length; i++)
            {
                DialogueStarter starter = starters[i];

                Row row = new Row();
                row.Label = starter.name;
                row.Read = () => starter.HasDialogue ? "▶" : "—";
                row.Activate = () => Play(starter);

                _rows.Add(row);
            }
        }

        private void Play(DialogueStarter starter)
        {
            if (starter == null) return;

            Close();
            starter.StartDialogue();
        }

        private string ValueText(string name)
        {
            WorldState world = World;
            if (world == null) return "—";

            DialogueValue value = world.Get(name);
            if (value.IsUndefined) return "—";

            return value.AsText();
        }

        private void Nudge(string name, int direction, bool big)
        {
            WorldState world = World;
            if (world == null) return;

            DialogueValue value = world.Get(name);

            if (value.Kind == DialogueValueKind.Bool)
            {
                world.Set(name, !value.Truthy());
                return;
            }

            if (value.Kind == DialogueValueKind.Number)
                world.Set(name, value.AsNumber() + direction * (big ? 10 : 1));
        }

        /* ============================================================
           ویرایش متن
           ============================================================ */

        private void BeginEdit(string name)
        {
            _editing = true;
            _editName = name;
            _editBuffer = string.Empty;

            if (Keyboard.current != null)
                Keyboard.current.onTextInput += HandleTextInput;

            UpdateEditorLine();
        }

        private void CommitEdit()
        {
            string text = _editBuffer.Trim();
            string name = _editName;
            WorldState world = World;

            EndEdit();

            if (string.IsNullOrEmpty(text) || world == null) return;

            // همان قاعده‌ی [do] دیالوگ؛ یک مفسر، دو جا
            DialogueRunner.ApplyEffect(name + " = " + text, world);

            Rebuild();
        }

        private void EndEdit()
        {
            if (!_editing) return;

            _editing = false;
            _editName = string.Empty;
            _editBuffer = string.Empty;

            StopListening();

            UpdateEditorLine();
        }

        private void StopListening()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput -= HandleTextInput;
        }

        private void UpdateEditorLine()
        {
            if (_editorBox == null) return;

            _editorBox.gameObject.SetActive(_editing);

            if (!_editing) return;

            string hint = _persian
                ? "مقدار تازه برای " + _editName + " (Enter تأیید، Esc لغو): "
                : "New value for " + _editName + " (Enter to apply, Esc to cancel): ";

            _editorText.text = hint + _editBuffer + "_";
        }

        /* ============================================================
           رسم
           ============================================================ */

        private void RefreshValues()
        {
            if (_tab == Tab.World)
            {
                WorldState world = World;

                // متغیر تازه اضافه شده (مثلاً یک دیالوگ تازه اجرا شده)؟ فهرست از نو
                if (world != null && world.Count != _rows.Count)
                {
                    Rebuild();
                    return;
                }
            }

            Render();
        }

        private void Render()
        {
            for (int i = 0; i < _rowViews.Count; i++)
            {
                RowView view = _rowViews[i];
                int index = _scroll + i;
                bool used = index >= 0 && index < _rows.Count;

                view.Rect.gameObject.SetActive(used);

                if (!used) continue;

                Row row = _rows[index];
                bool selected = index == _selected;

                view.Label.text = row.Label;
                view.Value.text = row.Read != null ? row.Read() : string.Empty;

                view.Background.color = selected ? RowSelectedColor : RowColor;
                view.Bar.gameObject.SetActive(selected);
                view.Label.color = selected ? TitleColor : DimColor;
                view.Value.color = selected ? ValueColor : DimColor;
            }
        }

        private void UpdateTabs()
        {
            for (int i = 0; i < _tabLabels.Count; i++)
            {
                bool active = i == (int)_tab;

                _tabLabels[i].color = active ? TitleColor : DimColor;
                _tabBars[i].SetActive(active);
            }
        }

        private void UpdateHeader()
        {
            _title.text = _persian ? "پنل تست بازی" : "Game test panel";

            string rows = _persian
                ? "ردیف‌ها: " + _rows.Count
                : "Rows: " + _rows.Count;

            _hint.text = _persian
                ? "هر تغییری همین‌جا در WorldState نوشته می‌شود؛ بازی همان لحظه واکنش می‌دهد.  " + rows
                : "Every change is written to WorldState; the game reacts immediately.  " + rows;
        }

        /* ============================================================
           ساخت
           ============================================================ */

        private void RebuildCanvas()
        {
            if (_canvas != null)
            {
                _canvas.SetActive(false);
                Destroy(_canvas);
                _canvas = null;
            }

            _rowViews.Clear();
            _tabLabels.Clear();
            _tabBars.Clear();

            Build();

            if (IsOpen) _canvas.SetActive(true);
        }

        private void Build()
        {
            _canvas = new GameObject("Cheat Canvas (runtime)",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvas.transform.SetParent(null, false);

            Canvas canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildBackdrop();
            BuildPanel();
            BuildHeader();
            BuildTabs();
            BuildList();
            BuildEditor();
            BuildFooter();
        }

        private void BuildBackdrop()
        {
            RectTransform backdrop = UiKit.Node("Backdrop", _canvas.transform);
            UiKit.Stretch(backdrop);

            Image image = backdrop.gameObject.AddComponent<Image>();
            image.color = BackdropColor;
        }

        private void BuildPanel()
        {
            _panel = UiKit.Node("Panel", _canvas.transform);
            _panel.anchorMin = new Vector2(0f, 1f);
            _panel.anchorMax = new Vector2(0f, 1f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.anchoredPosition = new Vector2(PanelX, -PanelY);
            _panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image background = _panel.gameObject.AddComponent<Image>();
            background.sprite = UiKit.RoundedRect;
            background.type = Image.Type.Sliced;
            background.color = PanelColor;
        }

        private void BuildHeader()
        {
            RectTransform titleBox = UiKit.Box("Title", _panel, PadX, 22f, PanelWidth - PadX * 2f, 42f);
            _title = UiKit.Text("Text", titleBox, 30f, TitleColor, _font, _persian);
            _title.alignment = _persian ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

            RectTransform hintBox = UiKit.Box("Hint", _panel, PadX, 68f, PanelWidth - PadX * 2f, 28f);
            _hint = UiKit.Text("Text", hintBox, 18f, DimColor, _font, _persian);
            _hint.alignment = _persian ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

            BuildRule("DividerTop", DividerTop);
        }

        private void BuildRule(string name, float top)
        {
            RectTransform rect = UiKit.Box(name, _panel, PadX, top, PanelWidth - PadX * 2f, 1f);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = RuleColor;
            image.raycastTarget = false;
        }

        private void BuildTabs()
        {
            string[] persianLabels = { "کاراکتر", "دنیا", "دیالوگ" };
            string[] englishLabels = { "Character", "World", "Dialogue" };

            for (int i = 0; i < TabCount; i++)
            {
                float x = _persian
                    ? PanelWidth - PadX - (i + 1) * TabWidth - i * TabGap
                    : PadX + i * (TabWidth + TabGap);

                RectTransform rect = UiKit.Box("Tab_" + i, _panel, x, TabTop, TabWidth, TabHeight);

                Image hit = rect.gameObject.AddComponent<Image>();
                hit.color = RowColor;

                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;

                int slot = i;
                button.onClick.AddListener(() => SetTab(slot));

                TMP_Text label = UiKit.Text("Label", rect, 24f, DimColor, _font, _persian);
                label.alignment = TextAlignmentOptions.MidlineCenter;

                RectTransform barRect = UiKit.Box("Bar", rect, 0f, TabHeight - 2f, TabWidth, 2f);
                Image bar = barRect.gameObject.AddComponent<Image>();
                bar.color = AccentColor;
                bar.raycastTarget = false;

                _tabLabels.Add(label);
                _tabBars.Add(barRect.gameObject);

                _tabLabels[i].text = _persian ? persianLabels[i] : englishLabels[i];
            }
        }

        private void BuildList()
        {
            float width = PanelWidth - PadX * 2f;

            for (int i = 0; i < VisibleRows; i++)
            {
                RectTransform rect = UiKit.Box("Row_" + i, _panel, PadX, ListTop + i * RowHeight,
                    width, RowHeight - 4f);

                Image background = rect.gameObject.AddComponent<Image>();
                background.color = RowColor;

                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;

                int slot = i;
                button.onClick.AddListener(() => SelectSlot(slot));

                float labelWidth = width - 460f;
                float valueWidth = 400f;

                float labelX = _persian ? width - labelWidth - 26f : 26f;
                float valueX = _persian ? 26f : width - valueWidth - 26f;

                RectTransform labelBox = UiKit.Box("Label", rect, labelX, 0f, labelWidth, RowHeight - 4f);
                TMP_Text label = UiKit.Text("Text", labelBox, 22f, DimColor, _font, _persian);
                label.alignment = _persian
                    ? TextAlignmentOptions.MidlineRight
                    : TextAlignmentOptions.MidlineLeft;

                RectTransform valueBox = UiKit.Box("Value", rect, valueX, 0f, valueWidth, RowHeight - 4f);
                TMP_Text value = UiKit.Text("Text", valueBox, 22f, ValueColor, _font, _persian);
                value.alignment = _persian
                    ? TextAlignmentOptions.MidlineLeft
                    : TextAlignmentOptions.MidlineRight;

                RectTransform barRect = UiKit.Box("Bar", rect, _persian ? width - 3f : 0f, 6f,
                    3f, RowHeight - 16f);
                Image bar = barRect.gameObject.AddComponent<Image>();
                bar.color = AccentColor;
                bar.raycastTarget = false;

                RowView view = new RowView();
                view.Rect = rect;
                view.Background = background;
                view.Bar = bar;
                view.Label = label;
                view.Value = value;
                view.Button = button;

                _rowViews.Add(view);
            }
        }

        private void BuildEditor()
        {
            _editorBox = UiKit.Box("Editor", _panel, PadX, EditorTop, PanelWidth - PadX * 2f, EditorHeight);

            Image background = _editorBox.gameObject.AddComponent<Image>();
            background.sprite = UiKit.RoundedRect;
            background.type = Image.Type.Sliced;
            background.color = EditorColor;

            _editorText = UiKit.Text("Text", _editorBox, 22f, ValueColor, _font, _persian);
            _editorText.alignment = _persian
                ? TextAlignmentOptions.MidlineRight
                : TextAlignmentOptions.MidlineLeft;

            _editorText.rectTransform.offsetMin = new Vector2(20f, 4f);
            _editorText.rectTransform.offsetMax = new Vector2(-20f, -4f);

            _editorBox.gameObject.SetActive(false);
        }

        private void BuildFooter()
        {
            RectTransform box = UiKit.Box("Footer", _panel, PadX, FooterTop,
                PanelWidth - PadX * 2f, FooterHeight);

            _footer = UiKit.Text("Text", box, 17f, DimColor, _font, _persian);
            _footer.alignment = _persian
                ? TextAlignmentOptions.MidlineRight
                : TextAlignmentOptions.MidlineLeft;

            _footer.text = _persian
                ? "F10 بستن   |   Q/E تب   |   W/S ردیف   |   ← → کم و زیاد (Shift = پرش بزرگ‌تر)   |   Enter انتخاب، ویرایش، پخش"
                : "F10 close   |   Q/E tab   |   W/S row   |   ← → change (Shift = bigger step)   |   Enter select, edit, play";
        }
    }
}
