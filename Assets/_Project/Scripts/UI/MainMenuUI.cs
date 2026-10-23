using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// منوی اصلی به سبک Mafia: پس‌زمینه‌ی تمام‌صفحه، عنوان طلایی، آیتم‌های
    /// خاکستری با نوار تیره زیر آیتم انتخاب‌شده و لوزی + خط ثابت زیر لیست.
    /// تمام رابط در زمان اجرا ساخته می‌شود.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [Header("تصاویر")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite markerSprite;

        [Header("فونت")]
        [SerializeField] private MenuFonts englishFonts = new MenuFonts();
        [SerializeField] private MenuFonts persianFonts = new MenuFonts();

        [Header("زبان")]
        [SerializeField] private bool persian;

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

        private static readonly Color GoldColor = new Color(0.659f, 0.502f, 0.188f);
        private static readonly Color ItemNormalColor = new Color(0.588f, 0.573f, 0.541f);
        private static readonly Color ItemSelectedColor = new Color(0.933f, 0.937f, 0.925f);
        private static readonly Color BarColor = new Color(0.10f, 0.09f, 0.07f, 0.82f);
        private static readonly Color BackgroundFallback = new Color(0.05f, 0.045f, 0.04f);

        private sealed class MenuItem
        {
            public string Key;
            public RectTransform Rect;
            public Image Bar;
            public TMP_Text Label;
            public Action Action;
        }

        private readonly List<MenuItem> _items = new List<MenuItem>();

        private TMP_Text _titleText;
        private TMP_Text _hintText;
        private TMP_Text _languageKey;
        private TMP_Text _languageName;
        private int _selected;

        private static Type _rtlType;
        private static bool _rtlSearched;

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnsureCamera();
            Build();
            ApplyLanguage();
            SelectIndex(0);
        }

        private void Update()
        {
            HandleInput();
            AnimateBars();
        }

        /* ============================================================
           ورودی
           ============================================================ */

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) SelectIndex(_selected + 1);
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) SelectIndex(_selected - 1);

                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame) Activate(_selected);

                if (keyboard.f1Key.wasPressedThisFrame) ToggleLanguage();
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

        /// <summary>پرش نرم نوار انتخاب؛ هر فریم آلفا را به هدف نزدیک می‌کند.</summary>
        private void AnimateBars()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                MenuItem item = _items[i];
                if (item.Bar == null) continue;

                float target = i == _selected ? BarColor.a : 0f;
                Color color = item.Bar.color;
                color.a = Mathf.MoveTowards(color.a, target, 8f * Time.unscaledDeltaTime);
                item.Bar.color = color;

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

            SceneManager.LoadScene(gameSceneName);
        }

        private void Options()
        {
            ShowHint(persian ? "تنظیمات هنوز ساخته نشده است." : "Options are not built yet.");
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ToggleLanguage()
        {
            persian = !persian;
            ApplyLanguage();
        }

        private void ShowHint(string message)
        {
            if (_hintText == null) return;

            _hintText.text = message;
            SetRtl(_hintText, persian);
        }

        /* ============================================================
           ساخت رابط
           ============================================================ */

        private void EnsureCamera()
        {
            if (Camera.main != null) return;

            GameObject cameraObject = new GameObject("MenuCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundFallback;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }

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

            BuildBackground(canvasObject.transform);
            BuildMenu(canvasObject.transform);
        }

        private void BuildBackground(Transform parent)
        {
            RectTransform rect = CreateNode("Background", parent);
            Stretch(rect);

            Image image = rect.gameObject.AddComponent<Image>();

            if (backgroundSprite != null)
            {
                image.sprite = backgroundSprite;
                image.preserveAspect = false;
            }
            else
            {
                Debug.LogWarning("[Menu] Background Sprite خالی است؛ پس‌زمینه تک‌رنگ می‌شود.");
                image.color = BackgroundFallback;
            }

            image.raycastTarget = false;

            // در Godot همین عکس با flip_h آینه می‌شد تا پرچم سمت چپ بیفتد
            rect.localScale = new Vector3(-1f, 1f, 1f);
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

            // سوییچ زبان، پایین-راست؛ دو خط جدا تا هیچ متنی دوخطی نشود
            RectTransform keyRect = CreateNode("LanguageKey", parent);
            keyRect.anchorMin = new Vector2(1f, 0f);
            keyRect.anchorMax = new Vector2(1f, 0f);
            keyRect.pivot = new Vector2(1f, 0f);
            keyRect.anchoredPosition = new Vector2(-48f, 62f);
            keyRect.sizeDelta = new Vector2(260f, 26f);

            _languageKey = CreateText("LanguageKeyText", keyRect, 20f, ItemNormalColor);
            _languageKey.alignment = TextAlignmentOptions.BottomRight;

            RectTransform nameRect = CreateNode("LanguageName", parent);
            nameRect.anchorMin = new Vector2(1f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(1f, 0f);
            nameRect.anchoredPosition = new Vector2(-48f, 32f);
            nameRect.sizeDelta = new Vector2(260f, 32f);

            _languageName = CreateText("LanguageNameText", nameRect, 24f, ItemSelectedColor);
            _languageName.alignment = TextAlignmentOptions.BottomRight;
        }

        private void CreateItem(RectTransform parent, float y, string key, Action action)
        {
            RectTransform rect = CreateNode("Item_" + key, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(ColumnWidth, ItemHeight);

            Image bar = rect.gameObject.AddComponent<Image>();
            bar.color = new Color(BarColor.r, BarColor.g, BarColor.b, 0f);
            bar.raycastTarget = false;

            RectTransform labelRect = CreateNode("Label", rect);
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(TextLeftPadding, 0f);

            TMP_Text label = CreateText("LabelText", labelRect, ItemSize, ItemNormalColor);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            MenuItem item = new MenuItem();
            item.Key = key;
            item.Rect = rect;
            item.Bar = bar;
            item.Label = label;
            item.Action = action;
            _items.Add(item);
        }

        /* ============================================================
           زبان
           ============================================================ */

        private void ApplyLanguage()
        {
            MenuFonts fonts = persian ? persianFonts : englishFonts;

            SetText(_titleText, persian ? "خانه" : "HOME", fonts.title != null ? fonts.title : fonts.label, persian);

            for (int i = 0; i < _items.Count; i++)
            {
                MenuItem item = _items[i];
                SetText(item.Label, LabelFor(item.Key, persian), fonts.label, persian);
            }

            if (_hintText != null) SetText(_hintText, string.Empty, fonts.label, persian);

            // کلید F1 همیشه لاتین است، پس همیشه با فونت انگلیسی نوشته می‌شود
            if (_languageKey != null) SetText(_languageKey, "F1", englishFonts.label, false);

            // نام زبانِ مقصد، با فونت و جهت خودش
            if (_languageName != null)
            {
                MenuFonts targetFonts = persian ? englishFonts : persianFonts;
                SetText(_languageName, persian ? "ENGLISH" : "فارسی", targetFonts.label, !persian);
            }
        }

        private static string LabelFor(string key, bool fa)
        {
            switch (key)
            {
                case "sandbox": return fa ? "تست سندباکس" : "SANDBOX TEST";
                case "options": return fa ? "تنظیمات" : "OPTIONS";
                case "quit": return fa ? "خروج" : "QUIT";
            }

            return key;
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
