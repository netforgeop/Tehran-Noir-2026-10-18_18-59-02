using System;
using System.Collections.Generic;
using System.Reflection;
using TehranNoir.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// منوی اصلی به سبک Mafia: پس‌زمینه‌ی تمام‌صفحه، عنوان طلایی، آیتم‌های
    /// خاکستری با کادر نازک طلایی (بالا، پایین، چپ) زیر آیتم انتخاب‌شده.
    /// زبان از Loc می‌آید و فقط از داخل «تنظیمات» عوض می‌شود، نه با F1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [Header("تصاویر")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("مثل flip_h در Godot؛ پرچم را از راست به چپ می‌آورد")]
        [SerializeField] private bool mirrorBackground = true;
        [SerializeField] private Sprite markerSprite;

        [Header("فونت")]
        [SerializeField] private MenuFonts englishFonts = new MenuFonts();
        [SerializeField] private MenuFonts persianFonts = new MenuFonts();

        [Header("صحنه بازی")]
        [Tooltip("نام فایل صحنه، بدون .unity")]
        [SerializeField] private string gameSceneName = "Sandbox";

        [Serializable]
        public sealed class MenuFonts
        {
            [Tooltip("وزن Black — عنوان")]
            public TMP_FontAsset title;
            [Tooltip("وزن Bold — آیتم‌ها")]
            public TMP_FontAsset label;
        }

        /* ============================================================
           اندازه‌ها — از main_menu.tscn در Godot برداشته شده
           ============================================================ */

        private const float MenuX = 270f;
        private const float MenuY = -286f;
        private const float ColumnWidth = 640f;
        private const float TitleHeight = 118f;
        private const float ItemHeight = 76f;
        private const float ItemGap = 8f;
        private const float MarkerGap = 26f;
        private const float TitleSize = 90f;
        private const float ItemSize = 56f;
        private const float TextLeftPadding = 54f;
        private const float FrameLine = 2f;

        private static readonly Color GoldColor = new Color(0.659f, 0.502f, 0.188f);
        private static readonly Color ItemNormalColor = new Color(0.588f, 0.573f, 0.541f);
        private static readonly Color ItemSelectedColor = new Color(0.933f, 0.937f, 0.925f);
        private static readonly Color FrameLineColor = new Color(0.78f, 0.64f, 0.34f, 0.95f);
        private static readonly Color FrameFillColor = new Color(0.72f, 0.60f, 0.33f, 0.16f);
        private static readonly Color BackgroundFallback = new Color(0.05f, 0.045f, 0.04f);

        private sealed class MenuItem
        {
            public string Key;
            public RectTransform Rect;
            public CanvasGroup Frame;
            public TMP_Text Label;
            public Action Action;
        }

        private readonly List<MenuItem> _items = new List<MenuItem>();

        private RectTransform _canvasRect;
        private RectTransform _background;
        private Vector2 _lastCanvasSize;

        private TMP_Text _titleText;
        private TMP_Text _hintText;
        private int _selected;

        private GameObject _settingsOverlay;
        private MenuSettings _settingsPage;

        /// <summary>در حال رفتن از این صحنه؛ تا محو تمام نشود، هیچ کلیدی کار نمی‌کند.</summary>
        private bool _leaving;

        private static Type _rtlType;
        private static bool _rtlSearched;
        private static Sprite _fadeSprite;

        /// <summary>زبان رابط از کانال «منو» می‌آید؛ همان کانالی که منوی توقف استفاده می‌کند.</summary>
        private bool Persian => Loc.IsRtl(Loc.Channel.Menu);

        private TMP_FontAsset LabelFont => Pick(englishFonts, persianFonts);

        private TMP_FontAsset TitleFont => PickTitle(englishFonts, persianFonts);

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Camera menuCamera = EnsureCamera();
            EnsureAudioListener(menuCamera);

            Build();
            ApplyLanguage();
            SelectIndex(0);

            Loc.Changed += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
        }

        private void Update()
        {
            FitBackground();
            AnimateFrames();

            // وسط محو شدن، ورودی نمی‌گیریم؛ وگرنه دو بار Enter یعنی دو بار لود
            if (_leaving) return;

            if (_settingsPage != null) HandleSettingsInput();
            else HandleInput();
        }

        /// <summary>
        /// تصویر پس‌زمینه را طوری اندازه می‌کند که کل کانواس پوشیده شود، بدون کشیدگی.
        /// اندازه‌گیری با واحدِ کانواس انجام می‌شود، نه پیکسل صفحه؛ چون CanvasScaler
        /// مقیاس را عوض می‌کند و این دو یکی نیستند.
        /// </summary>
        private void FitBackground()
        {
            if (_canvasRect == null || _background == null || backgroundSprite == null) return;

            Vector2 canvasSize = _canvasRect.rect.size;
            if (canvasSize.x <= 0f || canvasSize.y <= 0f) return;

            if (canvasSize == _lastCanvasSize) return;

            _lastCanvasSize = canvasSize;

            Vector2 sprite = new Vector2(backgroundSprite.rect.width, backgroundSprite.rect.height);
            if (sprite.x <= 0f || sprite.y <= 0f) return;

            // بزرگ‌ترین ضریب: هر دو بُعد پوشیده می‌شوند و اضافه از لبه‌ها بیرون می‌زند
            float scale = Mathf.Max(canvasSize.x / sprite.x, canvasSize.y / sprite.y);
            _background.sizeDelta = sprite * scale;
        }

        /* ============================================================
           ورودی منو
           ============================================================ */

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
                    keyboard.dKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame) SelectIndex(_selected + 1);

                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame ||
                    keyboard.aKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame) SelectIndex(_selected - 1);

                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame) Activate(_selected);

                if (keyboard.escapeKey.wasPressedThisFrame) QuitGame();
            }

            if (gamepad != null)
            {
                if (gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame) SelectIndex(_selected + 1);
                if (gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame) SelectIndex(_selected - 1);

                if (gamepad.buttonSouth.wasPressedThisFrame) Activate(_selected);
            }

            if (mouse == null) return;

            Vector2 pointer = mouse.position.ReadValue();
            bool clicked = mouse.leftButton.wasPressedThisFrame;

            for (int i = 0; i < _items.Count; i++)
            {
                if (!PointerInside(_items[i].Rect, pointer)) continue;

                SelectIndex(i);
                if (clicked) Activate(i);

                return;
            }
        }

        /* ============================================================
           ورودی صفحه‌ی تنظیمات
           ============================================================ */

        private void HandleSettingsInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    CloseSettings();
                    return;
                }

                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) _settingsPage.Move(1);
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) _settingsPage.Move(-1);

                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) _settingsPage.Step(-1);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) _settingsPage.Step(1);

                if (keyboard.qKey.wasPressedThisFrame) _settingsPage.Section(-1);
                if (keyboard.eKey.wasPressedThisFrame) _settingsPage.Section(1);
                if (keyboard.tabKey.wasPressedThisFrame) _settingsPage.NextSection();

                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame) _settingsPage.Activate();
            }

            if (gamepad != null)
            {
                if (gamepad.startButton.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame)
                {
                    CloseSettings();
                    return;
                }

                if (gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame) _settingsPage.Move(1);
                if (gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame) _settingsPage.Move(-1);

                if (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame) _settingsPage.Step(-1);
                if (gamepad.dpad.right.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame) _settingsPage.Step(1);

                if (gamepad.buttonNorth.wasPressedThisFrame) _settingsPage.NextSection();
                if (gamepad.buttonSouth.wasPressedThisFrame) _settingsPage.Activate();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            _settingsPage.Pointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame);
        }

        private static bool PointerInside(RectTransform rect, Vector2 screenPoint)
        {
            if (rect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        private void SelectIndex(int index)
        {
            if (_items.Count == 0) return;

            _selected = Mathf.Clamp(index, 0, _items.Count - 1);
        }

        private void Activate(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            Action action = _items[index].Action;
            if (action == null) return;

            action();
        }

        /// <summary>کادر انتخاب هر فریم به سمت هدف می‌رود؛ پرش ندارد.</summary>
        private void AnimateFrames()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                MenuItem item = _items[i];

                if (item.Frame != null)
                {
                    float target = i == _selected ? 1f : 0f;
                    item.Frame.alpha = Mathf.MoveTowards(item.Frame.alpha, target, 9f * Time.unscaledDeltaTime);
                }

                item.Label.color = i == _selected ? ItemSelectedColor : ItemNormalColor;
            }
        }

        /* ============================================================
           دکمه‌ها
           ============================================================ */

        private void SandboxTest()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogError("[Menu] نام صحنه‌ی بازی خالی است.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError("[Menu] صحنه‌ی «" + gameSceneName + "» در Build Profiles نیست. " +
                               "File > Build Profiles و صحنه‌ها را اضافه کن.");
                return;
            }

            LeaveTo(gameSceneName);
        }

        /// <summary>
        /// هر خروجی از منو از این‌جا رد می‌شود: اول تصویر و موزیک محو می‌شوند،
        /// بعد صحنه عوض می‌شود. صحنه‌ی تازه خودش از سیاه روشن می‌شود (ScreenFade).
        /// </summary>
        private void LeaveTo(string scene)
        {
            if (_leaving) return;

            _leaving = true;

            ShowHint(string.Empty);
            FadeMusic();

            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeOut(0.45f, () => SceneManager.LoadScene(scene));
                return;
            }

            SceneManager.LoadScene(scene);
        }

        private void FadeMusic()
        {
            MenuMusic music = GetComponent<MenuMusic>();

            if (music != null)
            {
                music.FadeOut(0.45f);
                return;
            }

            // موزیک ممکن است روی یک فرزند نشسته باشد
            music = GetComponentInChildren<MenuMusic>();
            if (music != null) music.FadeOut(0.45f);
        }

        /// <summary>تنظیمات، همان صفحه‌ای است که منوی توقف هم استفاده می‌کند.</summary>
        private void Options()
        {
            if (_settingsOverlay != null) return;

            ShowHint(string.Empty);

            RectTransform overlay = CreateNode("SettingsOverlay", _canvasRect);
            Stretch(overlay);

            Image backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.02f, 0.02f, 0.02f, 0.97f);
            backdrop.raycastTarget = false;

            _settingsOverlay = overlay.gameObject;
            _settingsPage = new MenuSettings(overlay, LabelFont, Persian, CloseSettings);
        }

        private void CloseSettings()
        {
            if (_settingsOverlay == null) return;

            Destroy(_settingsOverlay);

            _settingsOverlay = null;
            _settingsPage = null;
        }

        private void QuitGame()
        {
            if (_leaving) return;

            _leaving = true;

            FadeMusic();

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

        private void ShowHint(string message)
        {
            if (_hintText == null) return;

            _hintText.text = message;
            SetRtl(_hintText, Persian);
        }

        /* ============================================================
           صحنه
           ============================================================ */

        private Camera EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera != null) return camera;

            GameObject cameraObject = new GameObject("MenuCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);

            camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundFallback;
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            return camera;
        }

        /// بدون AudioListener هیچ صدایی پخش نمی‌شود؛ صحنه‌ی خالی هیچ ندارد.
        private static void EnsureAudioListener(Camera camera)
        {
            if (camera == null) return;

            if (camera.GetComponent<AudioListener>() != null) return;

            camera.gameObject.AddComponent<AudioListener>();
        }

        /* ============================================================
           ساخت رابط
           ============================================================ */

        private void Build()
        {
            GameObject canvasObject = new GameObject("MenuCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = canvasObject.GetComponent<RectTransform>();

            BuildBackground(canvasObject.transform);
            BuildMenu(canvasObject.transform);
        }

        private void BuildBackground(Transform parent)
        {
            RectTransform rect = CreateNode("Background", parent);

            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;

            if (backgroundSprite == null)
            {
                Debug.LogWarning("[Menu] Background Sprite خالی است؛ پس‌زمینه تک‌رنگ می‌شود.");
                image.color = BackgroundFallback;
                return;
            }

            image.sprite = backgroundSprite;

            // تصویر به نسبت خودش بزرگ می‌شود تا کل صفحه را بپوشاند؛
            // اضافه‌اش از لبه‌ها بیرون می‌زند و هیچ‌وقت کشیده نمی‌شود.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            _background = rect;
            _lastCanvasSize = Vector2.zero;
            FitBackground();

            // در Godot همین عکس با flip_h آینه می‌شد تا پرچم سمت چپ بیفتد
            if (mirrorBackground) rect.localScale = new Vector3(-1f, 1f, 1f);
        }

        private void BuildMenu(Transform parent)
        {
            RectTransform menuRoot = CreateNode("Menu", parent);
            menuRoot.anchorMin = new Vector2(0f, 1f);
            menuRoot.anchorMax = new Vector2(0f, 1f);
            menuRoot.pivot = new Vector2(0f, 1f);
            menuRoot.anchoredPosition = new Vector2(MenuX, MenuY);
            menuRoot.sizeDelta = new Vector2(ColumnWidth, TitleHeight + 3f * ItemHeight + 3f * ItemGap + MarkerGap + 60f);

            // عنوان
            RectTransform titleRect = CreateNode("Title", menuRoot);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(ColumnWidth, TitleHeight);

            _titleText = CreateText("TitleText", titleRect, TitleSize, GoldColor);
            _titleText.characterSpacing = 2f;

            // آیتم‌ها
            float y = -(TitleHeight + ItemGap);

            CreateItem(menuRoot, y, "sandbox", SandboxTest);
            y -= ItemHeight + ItemGap;

            CreateItem(menuRoot, y, "options", Options);
            y -= ItemHeight + ItemGap;

            CreateItem(menuRoot, y, "quit", QuitGame);
            y -= ItemHeight + MarkerGap;

            // لوزی + خط، ثابت زیر لیست (مثل Godot)
            RectTransform markerRect = CreateNode("Marker", menuRoot);
            markerRect.anchorMin = new Vector2(0f, 1f);
            markerRect.anchorMax = new Vector2(0f, 1f);
            markerRect.pivot = new Vector2(0f, 1f);
            markerRect.anchoredPosition = new Vector2(0f, y);
            markerRect.sizeDelta = new Vector2(ColumnWidth, 44f);

            Image marker = markerRect.gameObject.AddComponent<Image>();
            marker.raycastTarget = false;

            if (markerSprite != null)
            {
                marker.sprite = markerSprite;
                marker.preserveAspect = false;
            }
            else
            {
                marker.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.35f);
            }

            // پیام موقت، پایین-چپ
            RectTransform hintRect = CreateNode("Hint", parent);
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(0f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.anchoredPosition = new Vector2(MenuX, 90f);
            hintRect.sizeDelta = new Vector2(900f, 34f);

            _hintText = CreateText("HintText", hintRect, 24f, ItemNormalColor);
            _hintText.text = string.Empty;
        }

        private void CreateItem(RectTransform parent, float y, string key, Action action)
        {
            RectTransform rect = CreateNode("Item_" + key, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(ColumnWidth, ItemHeight);

            CanvasGroup frame = CreateFrame(rect);

            RectTransform labelRect = CreateNode("Label", rect);
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(TextLeftPadding, 0f);

            TMP_Text label = CreateText("LabelText", labelRect, ItemSize, ItemNormalColor);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            MenuItem item = new MenuItem();
            item.Key = key;
            item.Rect = rect;
            item.Frame = frame;
            item.Label = label;
            item.Action = action;
            _items.Add(item);
        }

        /// کادر انتخاب: پرکردن محو + سه خط نازک (چپ، بالا، پایین). سمت راست باز است.
        private static CanvasGroup CreateFrame(RectTransform parent)
        {
            RectTransform root = CreateNode("Frame", parent);
            Stretch(root);

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // پرکردن: از چپ روشن، به سمت راست محو
            RectTransform fillRect = CreateNode("Fill", root);
            Stretch(fillRect);

            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = FadeSprite;
            fill.type = Image.Type.Simple;
            fill.color = FrameFillColor;
            fill.raycastTarget = false;

            // خط چپ: یکدست
            RectTransform leftRect = CreateNode("LineLeft", root);
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.offsetMin = new Vector2(0f, 0f);
            leftRect.offsetMax = new Vector2(FrameLine, 0f);

            Image left = leftRect.gameObject.AddComponent<Image>();
            left.color = FrameLineColor;
            left.raycastTarget = false;

            CreateLine(root, "LineTop", true);
            CreateLine(root, "LineBottom", false);

            return group;
        }

        /// خط افقی؛ از چپ پر و به راست محو می‌شود.
        private static void CreateLine(RectTransform parent, string name, bool top)
        {
            RectTransform rect = CreateNode(name, parent);

            if (top)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.offsetMin = new Vector2(0f, -FrameLine);
                rect.offsetMax = new Vector2(0f, 0f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.offsetMin = new Vector2(0f, 0f);
                rect.offsetMax = new Vector2(0f, FrameLine);
            }

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = FadeSprite;
            image.color = FrameLineColor;
            image.raycastTarget = false;
        }

        /// بافت محو از چپ به راست؛ یک بار ساخته و همه‌جا استفاده می‌شود.
        private static Sprite FadeSprite
        {
            get
            {
                if (_fadeSprite != null) return _fadeSprite;

                const int width = 256;
                Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.name = "MenuFade";

                for (int x = 0; x < width; x++)
                {
                    float t = 1f - x / (float)(width - 1);
                    float alpha = Mathf.Pow(t, 1.8f);
                    texture.SetPixel(x, 0, new Color(1f, 1f, 1f, alpha));
                }

                texture.Apply();

                _fadeSprite = Sprite.Create(texture, new Rect(0f, 0f, width, 1f), new Vector2(0.5f, 0.5f), 100f);
                _fadeSprite.name = "MenuFade";
                return _fadeSprite;
            }
        }

        /* ============================================================
           زبان
           ============================================================ */

        private void OnLanguageChanged()
        {
            ApplyLanguage();

            if (_settingsOverlay == null) return;

            // صفحه‌ی تنظیمات با فونت و متن زبان تازه از نو ساخته می‌شود،
            // ولی کاربر باید توی همان بخش و همان ردیف بماند — نه اینکه بپرد به بخش صدا
            int section = _settingsPage.SectionIndex;
            int row = _settingsPage.SelectedIndex;

            CloseSettings();
            Options();

            _settingsPage.ShowSection(section);
            _settingsPage.Select(row);
        }

        private void ApplyLanguage()
        {
            TMP_FontAsset title = TitleFont;
            TMP_FontAsset label = LabelFont;

            SetText(_titleText, Loc.Get(Loc.Channel.Menu, "menu.title"), title, Persian);

            for (int i = 0; i < _items.Count; i++)
            {
                MenuItem item = _items[i];
                SetText(item.Label, Loc.Get(Loc.Channel.Menu, "menu." + item.Key), label, Persian);
            }

            if (_hintText != null) SetText(_hintText, string.Empty, label, Persian);
        }

        private TMP_FontAsset Pick(MenuFonts english, MenuFonts persian)
        {
            MenuFonts fonts = Persian ? persian : english;
            if (fonts.label != null) return fonts.label;
            if (fonts.title != null) return fonts.title;

            MenuFonts other = Persian ? english : persian;

            return other.label != null ? other.label : other.title;
        }

        private TMP_FontAsset PickTitle(MenuFonts english, MenuFonts persian)
        {
            MenuFonts fonts = Persian ? persian : english;
            if (fonts.title != null) return fonts.title;
            if (fonts.label != null) return fonts.label;

            MenuFonts other = Persian ? english : persian;

            return other.title != null ? other.title : other.label;
        }

        private static void SetText(TMP_Text text, string value, TMP_FontAsset font, bool farsi)
        {
            if (text == null) return;

            text.text = value;
            if (font != null) text.font = font;

            SetRtl(text, farsi);
        }

        /// هر متن، جهت خودش را دارد؛ برای همین یک متن انگلیسی همیشه لاتین می‌ماند.
        private static void SetRtl(TMP_Text text, bool farsi)
        {
            if (text == null) return;

            Component rtl = FindRtlComponent(text.gameObject);
            if (rtl == null) return;

            SetBoolMember(rtl, "Farsi", farsi);
            SetBoolMember(rtl, "FixTags", true);
        }

        /* ============================================================
           ابزار ساخت
           ============================================================ */

        private static RectTransform CreateNode(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
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

        private static TMP_Text CreateText(string name, RectTransform parent, float size, Color color)
        {
            RectTransform rect = CreateNode(name, parent);
            Stretch(rect);

            TMP_Text text = AddTextComponent(rect.gameObject);

            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            return text;
        }

        private static TMP_Text AddTextComponent(GameObject gameObject)
        {
            Type rtl = FindRtlTextType();

            if (rtl != null)
            {
                Component component = gameObject.AddComponent(rtl);
                TMP_Text text = component as TMP_Text;

                if (text != null) return text;
            }

            return gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static Type FindRtlTextType()
        {
            if (_rtlSearched) return _rtlType;

            _rtlSearched = true;

            _rtlType = typeof(MainMenuUI).Assembly.GetType("RTLTMPro.RTLTextMeshPro", false);
            if (_rtlType != null) return _rtlType;

            _rtlType = Type.GetType("RTLTMPro.RTLTextMeshPro, RTLTMPro", false);
            return _rtlType;
        }

        private static Component FindRtlComponent(GameObject gameObject)
        {
            Type rtl = FindRtlTextType();
            if (rtl == null) return null;

            return gameObject.GetComponent(rtl);
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
    }
}
