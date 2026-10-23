using System.Collections.Generic;
using TehranNoir.Dialogue;
using TehranNoir.Inputs;
using TehranNoir.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// منوی توقف به سبک Mafia: نوار تب در بالا، صفحه‌ی مشکی، نوار راهنما در پایین.
    /// هر تب یک کلاس جدا دارد (PauseTab)؛ این کلاس فقط پوسته، ناوبری و زبان را می‌داند.
    /// تب‌ها و ردیف‌ها با ماوس هم کار می‌کنند؛ باز و بسته‌شدن و موزیکش با محو نرم است.
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
        private const float BadgeSize = 34f;
        private const float BadgeTop = 33f;
        private const float BottomBadgeTop = 28f;
        private const float HintFont = 22f;
        private const float BadgeGap = 6f;
        private const float TextGap = 12f;
        private const float TabBadgeGap = 16f;

        private static readonly Color HoverColor = new Color(0.86f, 0.86f, 0.84f);

        [Header("فونت")]
        [Tooltip("فونت فارسی")]
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("فونت انگلیسی؛ اگر خالی بماند همان فونت فارسی استفاده می‌شود")]
        [SerializeField] private TMP_FontAsset latinFont;

        [Header("بازیکن")]
        [Tooltip("موقع توقف خاموش می‌شود تا ورودی بازی قطع شود")]
        [SerializeField] private PlayerInputReader playerInput;

        [Header("صحنه‌ها")]
        [Tooltip("نام فایل صحنه، بدون .unity")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("صدا")]
        [Tooltip("موزیک منوی توقف؛ خالی بماند یعنی این منو بی‌صدا است")]
        [SerializeField] private AudioClip musicClip;

        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        [Tooltip("مدت محو شدن موزیک هنگام باز و بسته شدن منو")]
        [SerializeField, Min(0.05f)] private float musicFadeSeconds = 0.8f;

        [Header("محو رابط")]
        [SerializeField, Min(0.01f)] private float uiFadeSeconds = 0.16f;

        /// <summary>بقیه‌ی سیستم‌ها با این پرچم می‌فهمند بازی متوقف است.</summary>
        public static bool IsOpen { get; private set; }

        public TMP_FontAsset Font => !Persian && latinFont != null ? latinFont : font;

        /// <summary>زبان رابط از کانال «منو» می‌آید.</summary>
        public bool Persian => Loc.IsRtl(Loc.Channel.Menu);

        private readonly List<PauseTab> _tabs = new List<PauseTab>();
        private readonly List<GameObject> _panels = new List<GameObject>();
        private readonly List<TMP_Text> _tabLabels = new List<TMP_Text>();
        private readonly List<GameObject> _tabUnderlines = new List<GameObject>();
        private readonly List<RectTransform> _tabRects = new List<RectTransform>();

        private GameObject _canvas;
        private CanvasGroup _group;
        private TMP_Text _hintText;
        private AudioSource _music;

        private int _activeTab;
        private int _hoverTab = -1;

        private float _uiTarget;
        private bool _closing;
        private float _musicLevel;
        private float _musicTarget;

        private CursorLockMode _previousLock;
        private bool _previousVisible;

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            CreateTabs();
            Build();
            EnsureMusic();

            _canvas.SetActive(false);
            IsOpen = false;
        }

        private void OnEnable()
        {
            Loc.Changed += Rebuild;
        }

        private void OnDisable()
        {
            Loc.Changed -= Rebuild;
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
            // محوها با زمان واقعی کار می‌کنند، پس بعد از بسته‌شدن هم ادامه پیدا می‌کنند
            UpdateFades();

            if (!IsOpen)
            {
                TryOpen();
                return;
            }

            HandleOpenInput();
        }

        /// <summary>تغییر زبان: کل رابط از نو ساخته می‌شود؛ ساده‌تر و بی‌خطاتر از عوض‌کردن تک‌تک متن‌ها.</summary>
        private void Rebuild()
        {
            int tab = Mathf.Clamp(_activeTab, 0, Mathf.Max(0, _tabs.Count - 1));
            int row = _tabs.Count > 0 ? _tabs[tab].Selected : 0;

            // تب تنظیمات ممکن است صفحه‌ی تو در تو باز داشته باشد؛ همان‌جا باید بماند
            object state = _tabs.Count > 0 ? _tabs[tab].SaveState() : null;

            if (_canvas != null)
            {
                _canvas.SetActive(false);
                Destroy(_canvas);
            }

            _tabs.Clear();
            _panels.Clear();
            _tabLabels.Clear();
            _tabUnderlines.Clear();
            _tabRects.Clear();

            _hoverTab = -1;
            _closing = false;

            CreateTabs();
            Build();

            _group.alpha = IsOpen ? 1f : 0f;
            _group.blocksRaycasts = IsOpen;

            if (!IsOpen) return;

            _activeTab = Mathf.Clamp(tab, 0, _tabs.Count - 1);
            ShowTab(_activeTab);
            _tabs[_activeTab].Select(row);
            _tabs[_activeTab].LoadState(state);
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

            _closing = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = true;
            _uiTarget = 1f;

            _musicTarget = _music != null ? 1f : 0f;

            ShowTab(_activeTab);
        }

        public void Resume()
        {
            if (!IsOpen) return;

            _tabs[_activeTab].OnHide();

            IsOpen = false;

            Time.timeScale = 1f;

            if (playerInput != null) playerInput.enabled = true;

            Cursor.lockState = _previousLock;
            Cursor.visible = _previousVisible;

            // پنل تا وقتی محو شود روشن می‌ماند، بعد خاموش می‌شود
            _uiTarget = 0f;
            _closing = true;
            _group.blocksRaycasts = false;

            _musicTarget = 0f;
        }

        /* ============================================================
           محو
           ============================================================ */

        private void UpdateFades()
        {
            float dt = Time.unscaledDeltaTime;

            if (_group != null)
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, _uiTarget, dt / Mathf.Max(0.01f, uiFadeSeconds));

                if (_closing && _group.alpha <= 0f)
                {
                    _closing = false;
                    if (_canvas != null) _canvas.SetActive(false);
                }
            }

            if (_music == null) return;

            _musicLevel = Mathf.MoveTowards(_musicLevel, _musicTarget, dt / Mathf.Max(0.05f, musicFadeSeconds));
            _music.volume = _musicLevel * musicVolume * GameSettings.SourceVolume(GameSettings.Band.Music);

            if (_musicTarget > 0f && _music.clip != null && !_music.isPlaying) _music.Play();
            if (_musicTarget <= 0f && _musicLevel <= 0.0001f && _music.isPlaying) _music.Stop();
        }

        private void EnsureMusic()
        {
            if (musicClip == null) return;

            _music = GetComponent<AudioSource>();
            if (_music == null) _music = gameObject.AddComponent<AudioSource>();

            _music.clip = musicClip;
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.volume = 0f;
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
                // Esc اول به تب می‌رسد: داخل تنظیمات یعنی «برگشت»، نه بستن کل منو
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    Back();
                    return;
                }

                // Q سمت چپ، E سمت راست — همان‌جایی که بج‌ها نشسته‌اند.
                // تب تنظیمات بخش دارد، پس داخلش Q/E بخش را عوض می‌کند، نه تب را.
                if (keyboard.qKey.wasPressedThisFrame) Horizontal(-1);
                if (keyboard.eKey.wasPressedThisFrame) Horizontal(1);
                if (keyboard.tabKey.wasPressedThisFrame) _tabs[_activeTab].NextSection();

                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) _tabs[_activeTab].Move(1);
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) _tabs[_activeTab].Move(-1);

                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) _tabs[_activeTab].Step(-1);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) _tabs[_activeTab].Step(1);

                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame) _tabs[_activeTab].Activate();
            }

            if (gamepad != null)
            {
                if (gamepad.startButton.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame)
                {
                    Back();
                    return;
                }

                if (gamepad.leftShoulder.wasPressedThisFrame) Horizontal(-1);
                if (gamepad.rightShoulder.wasPressedThisFrame) Horizontal(1);
                if (gamepad.buttonNorth.wasPressedThisFrame) _tabs[_activeTab].NextSection();

                if (gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame) _tabs[_activeTab].Move(1);
                if (gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame) _tabs[_activeTab].Move(-1);

                if (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame) _tabs[_activeTab].Step(-1);
                if (gamepad.dpad.right.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame) _tabs[_activeTab].Step(1);

                if (gamepad.buttonSouth.wasPressedThisFrame) _tabs[_activeTab].Activate();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pointer = mouse.position.ReadValue();
            bool clicked = mouse.leftButton.wasPressedThisFrame;

            int hovered = HoveredTab(pointer);

            if (_hoverTab != hovered)
            {
                _hoverTab = hovered;
                UpdateTabColors();
            }

            if (hovered >= 0)
            {
                if (clicked && hovered != _activeTab) SwitchTabTo(hovered);
                return;
            }

            _tabs[_activeTab].Pointer(pointer, clicked);
        }

        private int HoveredTab(Vector2 pointer)
        {
            for (int i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i] == null) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(_tabRects[i], pointer, null)) continue;

                return i;
            }

            return -1;
        }

        /// <summary>
        /// Esc یا دکمه‌ی برگشت: اول فعال‌ترین تب شانس دارد (تنظیمات → فهرست خودش)،
        /// وگرنه منو بسته می‌شود. یک کلید، یک معنی، بدون شرط‌های پراکنده در چند جا.
        /// </summary>
        private void Back()
        {
            if (_tabs.Count > 0 && _tabs[_activeTab].Back()) return;

            Resume();
        }

        /// <summary>
        /// چپ/راست در سطح منو. Q منفی، E مثبت.
        /// اگر تب فعال بخش‌های داخلی داشته باشد (تنظیمات)، بخش عوض می‌شود؛
        /// وگرنه تب قبلی/بعدی. چیدمان راست‌به‌چپ است، پس «چپ» یعنی تب قبلی.
        /// </summary>
        private void Horizontal(int delta)
        {
            if (_tabs.Count == 0 || delta == 0) return;

            PauseTab tab = _tabs[_activeTab];

            if (tab.UsesSections)
            {
                tab.Section(delta);
                return;
            }

            SwitchTab(-delta);
        }

        /// <summary>+1 یعنی تب سمت چپ، -1 یعنی تب سمت راست (چیدمان راست‌به‌چپ).</summary>
        private void SwitchTab(int delta)
        {
            if (_tabs.Count == 0) return;

            SwitchTabTo((_activeTab + delta + _tabs.Count) % _tabs.Count);
        }

        private void SwitchTabTo(int index)
        {
            if (index < 0 || index >= _tabs.Count || index == _activeTab) return;

            _tabs[_activeTab].OnHide();
            _panels[_activeTab].SetActive(false);

            _activeTab = index;

            ShowTab(_activeTab);
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool active = i == index;

                _panels[i].SetActive(active);
                _tabUnderlines[i].SetActive(active);
            }

            UpdateTabColors();
            ClearHint();
            _tabs[index].OnShow();
        }

        private void UpdateTabColors()
        {
            for (int i = 0; i < _tabLabels.Count; i++)
            {
                TMP_Text label = _tabLabels[i];
                if (label == null) continue;

                label.color = i == _activeTab ? UiKit.Gold : (i == _hoverTab ? HoverColor : UiKit.Idle);
            }
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

            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeOut(0.45f, () => SceneManager.LoadScene(mainMenuSceneName));
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeOut(0.4f, QuitNow);
                return;
            }

            QuitNow();
        }

        private static void QuitNow()
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
            _tabs.Add(new PlaceholderPauseTab("map", "pause.tab.map"));
            _tabs.Add(new PlaceholderPauseTab("codex", "pause.tab.codex"));
            _tabs.Add(new PlaceholderPauseTab("cases", "pause.tab.cases"));
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

            _group = canvasObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

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

                TMP_Text label = UiKit.Text("Label", tab, TabFont, UiKit.Idle, Font, Persian);
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

                _tabRects.Add(tab);
                _tabUnderlines.Add(CreateUnderline(tab));

                x -= TabGap;
            }

            // Q با فاصله‌ی برابر از لبه‌ی چپ نوار، E از لبه‌ی راست
            CreateKeyBadge(strip, -TabBadgeGap - BadgeSize, "Q", BadgeTop, false);
            CreateKeyBadge(strip, total + TabBadgeGap, "E", BadgeTop, false);

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

            // چپ: بج‌ها، بعد متن — فاصله‌ها دقیق و ثابت
            float x = RuleMargin;

            x = CreateKeyBadge(bar, x, "Q", BottomBadgeTop, false);
            x = CreateKeyBadge(bar, x + BadgeGap, "E", BottomBadgeTop, false);

            CreateHintText(bar, x + TextGap, Loc.Get(Loc.Channel.Menu, "pause.switch"), TextAlignmentOptions.MidlineLeft);

            // راست: متن، بعد بج در سمت چپِ آن — فاصله با اندازه‌ی واقعی متن حساب می‌شود
            TMP_Text close = CreateHintTextRight(bar, RuleMargin,
                Loc.Get(Loc.Channel.Menu, "pause.close"), TextAlignmentOptions.MidlineRight);

            float closeWidth = close.rectTransform.rect.width;

            CreateKeyBadge(bar, -(RuleMargin + closeWidth + TextGap), "Esc", BottomBadgeTop, true);
        }

        private void BuildHint(RectTransform root)
        {
            RectTransform box = UiKit.Box("Hint", root, RowX, 900f, 1400f, 34f);

            _hintText = UiKit.Text("HintText", box, HintFont, UiKit.Dim, font, Persian);
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
        /// بج کلید. از چپ: x لبه‌ی چپ است و لبه‌ی راست برمی‌گردد.
        /// از راست: x لبه‌ی راست است و لبه‌ی چپ برمی‌گردد.
        /// </summary>
        private float CreateKeyBadge(RectTransform parent, float x, string key, float top, bool fromRight)
        {
            Sprite icon = UiKit.KeyIcon(key);

            float width = icon != null ? BadgeSize : BadgeWidth(key);

            RectTransform badge = UiKit.Node("Key_" + key, parent);
            badge.anchorMin = new Vector2(fromRight ? 1f : 0f, 1f);
            badge.anchorMax = badge.anchorMin;
            badge.pivot = new Vector2(fromRight ? 1f : 0f, 1f);
            badge.anchoredPosition = new Vector2(x, -top);
            badge.sizeDelta = new Vector2(width, BadgeSize);

            Image image = badge.gameObject.AddComponent<Image>();
            image.raycastTarget = false;

            if (icon != null)
            {
                image.sprite = icon;
                image.preserveAspect = true;

                return fromRight ? x - width : x + width;
            }

            image.sprite = UiKit.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            TMP_Text label = UiKit.Text("Key", badge, 18f, new Color(0.05f, 0.05f, 0.05f), font, false);
            label.alignment = TextAlignmentOptions.Center;
            label.text = key;

            return fromRight ? x - width : x + width;
        }

        /// <summary>متن راهنما از سمت چپ؛ عرض جعبه به اندازه‌ی واقعی متن می‌شود.</summary>
        private TMP_Text CreateHintText(RectTransform parent, float x, string text, TextAlignmentOptions alignment)
        {
            RectTransform box = UiKit.Node("Hint_" + text, parent);
            box.anchorMin = new Vector2(0f, 1f);
            box.anchorMax = new Vector2(0f, 1f);
            box.pivot = new Vector2(0f, 1f);
            box.anchoredPosition = new Vector2(x, -BottomBadgeTop);
            box.sizeDelta = new Vector2(600f, BadgeSize);

            TMP_Text label = UiKit.Text("Text", box, HintFont, Color.white, Font, Persian);
            label.alignment = alignment;
            label.text = text;
            label.ForceMeshUpdate();

            box.sizeDelta = new Vector2(Mathf.Ceil(label.preferredWidth) + 2f, BadgeSize);

            return label;
        }

        /// <summary>متن راهنما از سمت راست؛ لبه‌ی راستش دقیقاً روی حاشیه می‌نشیند.</summary>
        private TMP_Text CreateHintTextRight(RectTransform parent, float margin, string text, TextAlignmentOptions alignment)
        {
            RectTransform box = UiKit.Node("Hint_" + text, parent);
            box.anchorMin = new Vector2(1f, 1f);
            box.anchorMax = new Vector2(1f, 1f);
            box.pivot = new Vector2(1f, 1f);
            box.anchoredPosition = new Vector2(-margin, -BottomBadgeTop);
            box.sizeDelta = new Vector2(600f, BadgeSize);

            TMP_Text label = UiKit.Text("Text", box, HintFont, Color.white, Font, Persian);
            label.alignment = alignment;
            label.text = text;
            label.ForceMeshUpdate();

            box.sizeDelta = new Vector2(Mathf.Ceil(label.preferredWidth) + 2f, BadgeSize);

            return label;
        }

        private static float BadgeWidth(string key)
        {
            return Mathf.Max(BadgeSize, 12f * key.Length + 16f);
        }
    }
}
