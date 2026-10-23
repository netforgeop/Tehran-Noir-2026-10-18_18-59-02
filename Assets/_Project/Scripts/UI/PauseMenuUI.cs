using System.Collections.Generic;
using TehranNoir.Dialogue;
using TehranNoir.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// منوی توقف به سبک Mafia: نوار تب در بالا، صفحه‌ی مشکی، نوار راهنما در پایین.
    /// هر تب یک کلاس جدا دارد (PauseTab)؛ این کلاس فقط پوسته و ناوبری را می‌داند.
    /// نوارها به لبه‌های صفحه چسبیده‌اند، پس در هر نسبت تصویر درست می‌مانند.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        /* اندازه‌های مشترک با تب‌ها */
        public const float RowX = 130f;
        public const float RowWidth = 800f;
        public const float RowHeight = 52f;
        public const float RowFont = 28f;
        public const float FirstRowY = 396f;

        private const float BarHeight = 100f;
        private const float BottomBarHeight = 91f;
        private const float RuleMargin = 93f;
        private const float TabGap = 56f;
        private const float TabFont = 30f;
        private const float UnderlineHeight = 2f;
        private const float BadgeHeight = 30f;
        private const float BadgeTop = 51f;
        private const float BottomBadgeTop = 30f;
        private const float HintFont = 22f;

        [Header("فونت")]
        [SerializeField] private TMP_FontAsset font;

        [Header("بازیکن")]
        [Tooltip("موقع توقف خاموش می‌شود تا ورودی بازی قطع شود")]
        [SerializeField] private PlayerInputReader playerInput;

        [Header("صحنه‌ها")]
        [Tooltip("نام فایل صحنه، بدون .unity")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        /// <summary>بقیه‌ی سیستم‌ها با این پرچم می‌فهمند بازی متوقف است.</summary>
        public static bool IsOpen { get; private set; }

        public TMP_FontAsset Font => font;

        private readonly List<PauseTab> _tabs = new List<PauseTab>();
        private readonly List<GameObject> _panels = new List<GameObject>();
        private readonly List<TMP_Text> _tabLabels = new List<TMP_Text>();
        private readonly List<GameObject> _tabUnderlines = new List<GameObject>();

        private GameObject _canvas;
        private TMP_Text _hintText;
        private int _activeTab;

        private CursorLockMode _previousLock;
        private bool _previousVisible;

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            CreateTabs();

            Build();

            _canvas.SetActive(false);
            IsOpen = false;
        }

        private void OnDestroy()
        {
            if (!IsOpen) return;

            IsOpen = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                TryOpen();
                return;
            }

            HandleOpenInput();
        }

        /* ============================================================
           باز و بسته
           ============================================================ */

        private void TryOpen()
        {
            // روی دیالوگ باز نمی‌شود؛ دیالوگ خودش ورودی را در دست دارد
            if (DialogueUI.Current != null) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Open();
                return;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame) Open();
        }

        private void Open()
        {
            if (IsOpen) return;

            IsOpen = true;

            _previousLock = Cursor.lockState;
            _previousVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerInput != null) playerInput.enabled = false;

            Time.timeScale = 0f;
            _canvas.SetActive(true);

            ShowTab(_activeTab);
        }

        public void Resume()
        {
            if (!IsOpen) return;

            _tabs[_activeTab].OnHide();

            IsOpen = false;
            _canvas.SetActive(false);

            Time.timeScale = 1f;

            if (playerInput != null) playerInput.enabled = true;

            Cursor.lockState = _previousLock;
            Cursor.visible = _previousVisible;
        }

        /* ============================================================
           ناوبری
           ============================================================ */

        private void HandleOpenInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    Resume();
                    return;
                }

                if (keyboard.qKey.wasPressedThisFrame) SwitchTab(-1);
                if (keyboard.eKey.wasPressedThisFrame) SwitchTab(1);

                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) _tabs[_activeTab].Move(1);
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) _tabs[_activeTab].Move(-1);

                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame) _tabs[_activeTab].Activate();
            }

            if (gamepad != null)
            {
                if (gamepad.startButton.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame)
                {
                    Resume();
                    return;
                }

                if (gamepad.leftShoulder.wasPressedThisFrame) SwitchTab(-1);
                if (gamepad.rightShoulder.wasPressedThisFrame) SwitchTab(1);

                if (gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame) _tabs[_activeTab].Move(1);
                if (gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame) _tabs[_activeTab].Move(-1);

                if (gamepad.buttonSouth.wasPressedThisFrame) _tabs[_activeTab].Activate();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            _tabs[_activeTab].Tick(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame);
        }

        private void SwitchTab(int delta)
        {
            if (_tabs.Count == 0) return;

            _tabs[_activeTab].OnHide();
            _panels[_activeTab].SetActive(false);

            _activeTab = (_activeTab + delta + _tabs.Count) % _tabs.Count;

            ShowTab(_activeTab);
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool active = i == index;

                _panels[i].SetActive(active);
                _tabUnderlines[i].SetActive(active);
                _tabLabels[i].color = active ? UiKit.Gold : UiKit.Idle;
            }

            ClearHint();
            _tabs[index].OnShow();
        }

        /* ============================================================
           کارهای تب‌ها
           ============================================================ */

        public void ShowHint(string message)
        {
            if (_hintText == null) return;

            _hintText.text = message;
        }

        private void ClearHint()
        {
            if (_hintText != null) _hintText.text = string.Empty;
        }

        public void QuitToMainMenu()
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogError("[Pause] نام صحنه‌ی منوی اصلی خالی است.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError("[Pause] صحنه‌ی «" + mainMenuSceneName + "» در Build Profiles نیست.");
                return;
            }

            Resume();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /* ============================================================
           ساخت
           ============================================================ */

        private void CreateTabs()
        {
            _tabs.Add(new SystemPauseTab());
            _tabs.Add(new PlaceholderPauseTab("map", "نقشه"));
            _tabs.Add(new PlaceholderPauseTab("codex", "دفترچه"));
            _tabs.Add(new PlaceholderPauseTab("cases", "پرونده‌ها"));
        }

        private void Build()
        {
            GameObject canvasObject = new GameObject("PauseCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvas = canvasObject;

            RectTransform root = canvasObject.GetComponent<RectTransform>();

            RectTransform backdrop = UiKit.Node("Backdrop", root);
            UiKit.Stretch(backdrop);

            Image black = backdrop.gameObject.AddComponent<Image>();
            black.color = Color.black;

            BuildTopBar(root);
            BuildContent(root);
            BuildBottomBar(root);
            BuildHint(root);

            _activeTab = 0;
            ShowTab(0);
        }

        private void BuildTopBar(RectTransform root)
        {
            RectTransform bar = TopBand(root, "TopBar", BarHeight);

            float[] widths = new float[_tabs.Count];
            RectTransform[] roots = new RectTransform[_tabs.Count];

            for (int i = 0; i < _tabs.Count; i++)
            {
                RectTransform tab = UiKit.Box("Tab_" + _tabs[i].Id, bar, 0f, 0f, 200f, BarHeight);

                TMP_Text label = UiKit.Text("Label", tab, TabFont, UiKit.Idle, font, true);
                label.alignment = TextAlignmentOptions.Center;
                label.text = _tabs[i].Label;
                label.ForceMeshUpdate();

                widths[i] = Mathf.Max(40f, label.preferredWidth);
                roots[i] = tab;
                _tabLabels.Add(label);
            }

            float total = TabGap * (_tabs.Count - 1);
            for (int i = 0; i < _tabs.Count; i++) total += widths[i];

            // نوار تب دقیقاً وسط صفحه، مستقل از رزولوشن
            RectTransform strip = UiKit.Node("Tabs", bar);
            strip.anchorMin = new Vector2(0.5f, 1f);
            strip.anchorMax = new Vector2(0.5f, 1f);
            strip.pivot = new Vector2(0f, 1f);
            strip.anchoredPosition = new Vector2(-total * 0.5f, 0f);
            strip.sizeDelta = new Vector2(total, BarHeight);

            // تب اول راست‌ترین است؛ ترتیب خواندن فارسی
            float x = total;

            for (int i = 0; i < _tabs.Count; i++)
            {
                x -= widths[i];

                RectTransform tab = roots[i];
                tab.SetParent(strip, false);
                tab.anchoredPosition = new Vector2(x, 0f);
                tab.sizeDelta = new Vector2(widths[i], BarHeight);

                _tabUnderlines.Add(CreateUnderline(tab));

                x -= TabGap;
            }

            CreateKeyBadge(strip, -94f, "Q", BadgeTop);
            CreateKeyBadge(strip, total + 62f, "E", BadgeTop);

            CreateTopRule(root, BarHeight);
        }

        private static GameObject CreateUnderline(RectTransform tab)
        {
            RectTransform line = UiKit.Node("Underline", tab);
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.offsetMin = new Vector2(0f, 0f);
            line.offsetMax = new Vector2(0f, UnderlineHeight);

            Image image = line.gameObject.AddComponent<Image>();
            image.color = UiKit.Gold;
            image.raycastTarget = false;

            return line.gameObject;
        }

        private void BuildContent(RectTransform root)
        {
            RectTransform content = UiKit.Node("Content", root);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(0f, BottomBarHeight);
            content.offsetMax = new Vector2(0f, -BarHeight - 1f);

            for (int i = 0; i < _tabs.Count; i++)
            {
                RectTransform panel = UiKit.Node("Panel_" + _tabs[i].Id, content);
                UiKit.Stretch(panel);

                _tabs[i].Build(panel, this);
                _panels.Add(panel.gameObject);

                panel.gameObject.SetActive(false);
            }
        }

        private void BuildBottomBar(RectTransform root)
        {
            CreateBottomRule(root, BottomBarHeight);

            RectTransform bar = BottomBand(root, "BottomBar", BottomBarHeight);

            // چپ: تعویض تب
            CreateKeyBadge(bar, RuleMargin, "Q/E", BottomBadgeTop);
            CreateLabel(bar, RuleMargin + 64f, 220f, "تعویض تب", TextAlignmentOptions.MidlineLeft, BottomBadgeTop);

            // راست: بستن — کلمه راست‌ترین است، بج سمت چپش
            CreateLabel(bar, -RuleMargin, 120f, "بستن", TextAlignmentOptions.MidlineRight, BottomBadgeTop);
            CreateKeyBadge(bar, -RuleMargin - 130f, "Esc", BottomBadgeTop);
        }

        private void BuildHint(RectTransform root)
        {
            RectTransform box = UiKit.Box("Hint", root, RowX, 900f, 1200f, 34f);

            _hintText = UiKit.Text("HintText", box, HintFont, UiKit.Dim, font, true);
        }

        /* ============================================================
           ابزار ساخت
           ============================================================ */

        private static RectTransform TopBand(RectTransform parent, string name, float height)
        {
            RectTransform rect = UiKit.Node(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -height);
            rect.offsetMax = new Vector2(0f, 0f);
            return rect;
        }

        private static RectTransform BottomBand(RectTransform parent, string name, float height)
        {
            RectTransform rect = UiKit.Node(name, parent);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, height);
            return rect;
        }

        private static void CreateTopRule(RectTransform parent, float y)
        {
            RectTransform rule = UiKit.Node("RuleTop", parent);
            rule.anchorMin = new Vector2(0f, 1f);
            rule.anchorMax = new Vector2(1f, 1f);
            rule.pivot = new Vector2(0.5f, 1f);
            rule.offsetMin = new Vector2(RuleMargin, -y - 1f);
            rule.offsetMax = new Vector2(-RuleMargin, -y);

            Image image = rule.gameObject.AddComponent<Image>();
            image.color = UiKit.Rule;
            image.raycastTarget = false;
        }

        private static void CreateBottomRule(RectTransform parent, float y)
        {
            RectTransform rule = UiKit.Node("RuleBottom", parent);
            rule.anchorMin = new Vector2(0f, 0f);
            rule.anchorMax = new Vector2(1f, 0f);
            rule.pivot = new Vector2(0.5f, 0f);
            rule.offsetMin = new Vector2(RuleMargin, y);
            rule.offsetMax = new Vector2(-RuleMargin, y + 1f);

            Image image = rule.gameObject.AddComponent<Image>();
            image.color = UiKit.Rule;
            image.raycastTarget = false;
        }

        /// <summary>
        /// بج کلید سفید. x نسبت به لبه‌ی چپِ والد؛ اگر منفی باشد از لبه‌ی راست حساب می‌شود.
        /// </summary>
        private void CreateKeyBadge(RectTransform parent, float x, string key, float top)
        {
            float width = BadgeWidth(key);
            bool fromRight = x < 0f;

            RectTransform badge = UiKit.Node("Key_" + key, parent);
            badge.anchorMin = new Vector2(fromRight ? 1f : 0f, 1f);
            badge.anchorMax = badge.anchorMin;
            badge.pivot = new Vector2(fromRight ? 1f : 0f, 1f);
            badge.anchoredPosition = new Vector2(x, -top);
            badge.sizeDelta = new Vector2(width, BadgeHeight);

            Image background = badge.gameObject.AddComponent<Image>();
            background.sprite = UiKit.RoundedRect;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            background.raycastTarget = false;

            TMP_Text label = UiKit.Text("Key", badge, 18f, new Color(0.05f, 0.05f, 0.05f), font, false);
            label.alignment = TextAlignmentOptions.Center;
            label.text = key;
        }

        private void CreateLabel(RectTransform parent, float x, float width, string text,
            TextAlignmentOptions alignment, float top)
        {
            bool fromRight = x < 0f;

            RectTransform box = UiKit.Node("Label_" + text, parent);
            box.anchorMin = new Vector2(fromRight ? 1f : 0f, 1f);
            box.anchorMax = box.anchorMin;
            box.pivot = new Vector2(fromRight ? 1f : 0f, 1f);
            box.anchoredPosition = new Vector2(x, -top);
            box.sizeDelta = new Vector2(width, BadgeHeight);

            TMP_Text label = UiKit.Text("Text", box, HintFont, Color.white, font, true);
            label.alignment = alignment;
            label.text = text;
        }

        private static float BadgeWidth(string key)
        {
            return Mathf.Max(30f, 12f * key.Length + 16f);
        }
    }
}
