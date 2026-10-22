using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// منوی اصلی. تمام رابط در زمان اجرا ساخته می‌شود؛
    /// در صحنه فقط یک آبجکت خالی با همین کامپوننت لازم است.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [Header("ظاهر")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private string title = "تهران نوآر";
        [SerializeField] private string subtitle = "تهران، ۱۳۵۲ تا ۱۳۵۷";
        [SerializeField] private string footer = "نسخه ۰.۱";
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.045f, 0.04f);

        [Header("صحنه بازی")]
        [Tooltip("نام فایل صحنه، بدون .unity")]
        [SerializeField] private string gameSceneName = "Sandbox";

        private static readonly Color Gold = new Color(0.85f, 0.74f, 0.45f);
        private static readonly Color Body = new Color(0.94f, 0.90f, 0.82f);
        private static readonly Color Muted = new Color(0.55f, 0.52f, 0.45f);
        private static readonly Color ButtonColor = new Color(0.13f, 0.12f, 0.10f, 0.96f);
        private static readonly Color ButtonHover = new Color(0.26f, 0.22f, 0.14f, 1f);
        private static readonly Color ButtonPressed = new Color(0.35f, 0.29f, 0.17f, 1f);

        private static Type _rtlType;
        private static bool _rtlSearched;

        private void Awake()
        {
            // اگر از بازی به منو برگشتیم، زمان ممکن است متوقف مانده باشد
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnsureCamera();
            EnsureEventSystem();
            Build();
        }

        /* ============================================================
           دکمه‌ها
           ============================================================ */

        public void StartGame()
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

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /* ============================================================
           صحنه
           ============================================================ */

        /// یک صحنه‌ی خالی هیچ دوربینی ندارد؛ بدون دوربین، پس‌زمینه رندر نمی‌شود.
        private void EnsureCamera()
        {
            if (Camera.main != null) return;

            GameObject cameraObject = new GameObject("MenuCamera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }

        /// برای کلیک موس روی دکمه‌ها به EventSystem نیاز است.
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject eventObject = new GameObject("EventSystem",
                typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(transform, false);
        }

        /* ============================================================
           ساخت رابط
           ============================================================ */

        private void Build()
        {
            if (font == null)
                Debug.LogWarning("[Menu] فونت TMP انتخاب نشده؛ متن فارسی ممکن است درست نمایش داده نشود.");

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

            RectTransform background = CreateNode("Background", canvasObject.transform);
            Stretch(background);

            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;

            RectTransform titleRect = CreateNode("Title", canvasObject.transform);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -170f);
            titleRect.sizeDelta = new Vector2(1200f, 130f);

            TMP_Text titleText = CreateText("TitleText", titleRect, 96f, FontStyles.Bold, Gold,
                TextAlignmentOptions.Center);
            titleText.text = title;
            titleText.characterSpacing = 4f;

            RectTransform subtitleRect = CreateNode("Subtitle", canvasObject.transform);
            subtitleRect.anchorMin = new Vector2(0.5f, 1f);
            subtitleRect.anchorMax = new Vector2(0.5f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);
            subtitleRect.anchoredPosition = new Vector2(0f, -306f);
            subtitleRect.sizeDelta = new Vector2(1000f, 44f);

            TMP_Text subtitleText = CreateText("SubtitleText", subtitleRect, 30f, FontStyles.Normal, Muted,
                TextAlignmentOptions.Center);
            subtitleText.text = subtitle;

            RectTransform dividerRect = CreateNode("Divider", canvasObject.transform);
            dividerRect.anchorMin = new Vector2(0.5f, 1f);
            dividerRect.anchorMax = new Vector2(0.5f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 1f);
            dividerRect.anchoredPosition = new Vector2(0f, -366f);
            dividerRect.sizeDelta = new Vector2(520f, 2f);

            Image divider = dividerRect.gameObject.AddComponent<Image>();
            divider.color = new Color(Gold.r, Gold.g, Gold.b, 0.45f);
            divider.raycastTarget = false;

            RectTransform buttonsRoot = CreateNode("Buttons", canvasObject.transform);
            buttonsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            buttonsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            buttonsRoot.pivot = new Vector2(0.5f, 0.5f);
            buttonsRoot.anchoredPosition = new Vector2(0f, -90f);
            buttonsRoot.sizeDelta = new Vector2(420f, 160f);

            VerticalLayoutGroup layout = buttonsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Button firstButton = CreateButton(buttonsRoot, "شروع بازی", StartGame);
            CreateButton(buttonsRoot, "خروج", QuitGame);

            RectTransform footerRect = CreateNode("Footer", canvasObject.transform);
            footerRect.anchorMin = new Vector2(0.5f, 0f);
            footerRect.anchorMax = new Vector2(0.5f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.anchoredPosition = new Vector2(0f, 34f);
            footerRect.sizeDelta = new Vector2(600f, 30f);

            TMP_Text footerText = CreateText("FooterText", footerRect, 20f, FontStyles.Normal, Muted,
                TextAlignmentOptions.Center);
            footerText.text = footer;

            // اولین دکمه انتخاب شود تا با کیبورد/دسته هم کار کند
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        private Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateNode("Button_" + label, parent);

            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 62f;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.pressedColor = new Color(2.2f, 2.2f, 2.2f, 1f);
            colors.selectedColor = new Color(1.4f, 1.4f, 1.4f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text text = CreateText("Label", rect, 30f, FontStyles.Normal, Body,
                TextAlignmentOptions.Center);
            text.text = label;

            button.onClick.AddListener(onClick);
            return button;
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

        private TMP_Text CreateText(string name, RectTransform parent, float size, FontStyles style,
            Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateNode(name, parent);
            Stretch(rect);

            TMP_Text text = AddTextComponent(rect.gameObject);

            if (font != null) text.font = font;

            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = alignment;

            return text;
        }

        /// اگر RTLTMPro نصب باشد از آن استفاده می‌کند؛ وگرنه TextMeshPro معمولی.
        private static TMP_Text AddTextComponent(GameObject gameObject)
        {
            Type rtl = FindRtlTextType();

            if (rtl != null)
            {
                Component component = gameObject.AddComponent(rtl);
                TMP_Text text = component as TMP_Text;

                if (text != null)
                {
                    SetBoolMember(component, "Farsi", true);
                    SetBoolMember(component, "FixTags", true);
                    return text;
                }
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
