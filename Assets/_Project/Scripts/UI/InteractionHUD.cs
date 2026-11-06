using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TehranNoir.Core;
using TehranNoir.Inputs;
using TehranNoir.Interaction;

namespace TehranNoir.UI
{
    /// <summary>
    /// کارت تعامل، گوشه‌ی چپ-پایین: نوار اهداف نزدیک (بالا) + کارت کار (پایین).
    /// تمام رابط در زمان اجرا ساخته می‌شود تا چیدمانش دقیق و قابل تغییر باشد.
    /// هیچ اسم و متنی این‌جا نوشته نشده؛ همه از خودِ آبجکت و فایل زبان می‌آید.
    /// </summary>
    public class InteractionHUD : MonoBehaviour
    {
        /* ---------- چیدمان (واحد کانواس ۱۹۲۰×۱۰۸۰) ---------- */
        private const float Width = 470f;
        private const float CardHeight = 124f;
        private const float PillHeight = 34f;
        private const float PillGap = 8f;
        private const float StripGap = 8f;
        private const float MarginX = 48f;
        private const float MarginY = 44f;
        private const float Pad = 18f;
        private const float BadgeSize = 46f;
        private const float BadgeGap = 16f;
        private const int MaxPills = 4;
        private const int MaxPillChars = 16;

        /* ---------- رنگ ---------- */
        private static readonly Color CardColor = new Color(0.09f, 0.106f, 0.125f, 0.93f);
        private static readonly Color RaisedColor = new Color(0.145f, 0.165f, 0.188f, 0.95f);
        private static readonly Color PrimaryColor = new Color(0.941f, 0.933f, 0.914f);
        private static readonly Color SecondaryColor = new Color(0.655f, 0.663f, 0.62f);
        private static readonly Color MutedColor = new Color(0.42f, 0.44f, 0.40f);
        private static readonly Color AccentColor = new Color(0.541f, 0.604f, 0.357f);
        private static readonly Color BorderColor = new Color(0.23f, 0.25f, 0.22f, 0.9f);

        [Header("وابستگی‌ها")]
        [Tooltip("خالی بماند تا خودکار در صحنه پیدا شود")]
        [SerializeField] private InteractionSensor sensor;

        [Tooltip("برای نمایش آیکون دسته به‌جای کیبورد")]
        [SerializeField] private PlayerInputReader input;

        [Header("فونت")]
        [Tooltip("فونت فارسی — Vazirmatn")]
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("فونت لاتین — Roboto")]
        [SerializeField] private TMP_FontAsset latinFont;

        [Header("رفتار")]
        [Tooltip("بچه‌های قدیمی این آبجکت (کارت قدیمی) در Awake پاک شوند")]
        [SerializeField] private bool clearExistingChildren = true;

        [SerializeField] private float fadeSpeed = 12f;

        private sealed class Pill
        {
            public RectTransform Rect;
            public Image Background;
            public TMP_Text Label;
        }

        private readonly List<Pill> _pills = new List<Pill>();
        private readonly List<Interactable> _stripTargets = new List<Interactable>();

        private CanvasGroup _group;
        private RectTransform _canvasRect;
        private RectTransform _root;
        private RectTransform _strip;
        private RectTransform _card;
        private GameObject _keyIcon;

        private TMP_Text _keyText;
        private TMP_Text _verbText;
        private TMP_Text _titleText;
        private TMP_Text _subtitleText;
        private TMP_Text _hintText;

        private Interactable _lastTarget;
        private bool _lastGamepad;
        private bool _lastMulti;
        private float _alpha;

        private bool Persian => Localization.Persian;

        private TMP_FontAsset Font => Persian && font != null ? font : (latinFont != null ? latinFont : font);

        private TextAlignmentOptions Align =>
            Persian ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            if (clearExistingChildren)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    GameObject old = transform.GetChild(i).gameObject;
                    old.SetActive(false);
                    Destroy(old);
                }
            }

            if (sensor == null) sensor = FindAnyObjectByType<InteractionSensor>();
            if (input == null && sensor != null) input = sensor.GetComponent<PlayerInputReader>();

            Build();
        }

        private void OnEnable()
        {
            Settings.Loc.Changed += Rebuild;
        }

        private void OnDisable()
        {
            Settings.Loc.Changed -= Rebuild;
        }

        private void Update()
        {
            // بدون سنسور هیچ‌چیز نیست؛ ولی نبودِ ورودی نباید HUD را کامل خاموش کند
            if (_group == null || sensor == null) return;

            IReadOnlyList<Interactable> list = sensor.Candidates;
            Interactable current = sensor.Current;

            bool usingGamepad = input != null && input.UsingGamepad;

            // TMP برای هر نوشتن مِش از نو می‌سازد؛ فقط با تغییر می‌نویسیم
            if (current != _lastTarget || usingGamepad != _lastGamepad)
            {
                _lastTarget = current;
                _lastGamepad = usingGamepad;

                Apply(current, usingGamepad);
            }

            SyncStrip(list, sensor.Index);

            float target = current != null ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, fadeSpeed * Time.unscaledDeltaTime);

            _group.alpha = _alpha;
            _root.anchoredPosition = new Vector2(MarginX - (1f - _alpha) * 18f, MarginY);
        }

        /// <summary>بعد از عوض شدن زبان یا از بیرون صدا زده می‌شود.</summary>
        public void Refresh()
        {
            _lastTarget = null;
        }

        /* ============================================================
           محتوا
           ============================================================ */

        private void Apply(Interactable target, bool usingGamepad)
        {
            if (target == null) return;

            _verbText.text = target.Prompt;
            _titleText.text = target.Title;

            string subtitle = target.Subtitle;
            _subtitleText.text = subtitle;
            _subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));

            SetKey(usingGamepad ? "X" : "E");
        }

        /// <summary>آیکون واقعی کلید اگر در Resources باشد؛ وگرنه حرف.</summary>
        private void SetKey(string key)
        {
            Sprite sprite = UiKit.KeyIcon(key);

            if (sprite != null)
            {
                if (_keyIcon == null)
                {
                    RectTransform rect = UiKit.Node("KeyIcon", _keyText.transform.parent);
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(28f, 28f);

                    Image image = rect.gameObject.AddComponent<Image>();
                    image.raycastTarget = false;

                    _keyIcon = rect.gameObject;
                }

                _keyIcon.GetComponent<Image>().sprite = sprite;

                _keyIcon.SetActive(true);
                _keyText.gameObject.SetActive(false);
                return;
            }

            if (_keyIcon != null) _keyIcon.SetActive(false);

            _keyText.gameObject.SetActive(true);
            _keyText.text = key;
        }

        /* ============================================================
           نوار اهداف
           ============================================================ */

        private void SyncStrip(IReadOnlyList<Interactable> list, int selected)
        {
            bool multi = list.Count > 1;

            if (multi != _lastMulti)
            {
                _lastMulti = multi;
                _strip.gameObject.SetActive(multi);
                _hintText.gameObject.SetActive(multi);
            }

            if (multi) RebuildStripIfNeeded(list);

            for (int i = 0; i < _pills.Count; i++)
            {
                bool active = i == selected;

                _pills[i].Background.color = active ? new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.22f) : RaisedColor;
                _pills[i].Label.color = active ? PrimaryColor : SecondaryColor;
            }
        }

        private void RebuildStripIfNeeded(IReadOnlyList<Interactable> list)
        {
            int shown = Mathf.Min(list.Count, MaxPills);

            bool same = _stripTargets.Count == shown;

            for (int i = 0; same && i < shown; i++)
                if (_stripTargets[i] != list[i]) same = false;

            if (same) return;

            _stripTargets.Clear();
            for (int i = 0; i < shown; i++) _stripTargets.Add(list[i]);

            BuildPills();
        }

        private void BuildPills()
        {
            for (int i = 0; i < _pills.Count; i++)
            {
                if (_pills[i].Rect == null) continue;
                _pills[i].Rect.gameObject.SetActive(false);
                Destroy(_pills[i].Rect.gameObject);
            }

            _pills.Clear();

            float cursor = Persian ? Width : 0f;

            for (int i = 0; i < _stripTargets.Count; i++)
            {
                Interactable target = _stripTargets[i];

                RectTransform rect = UiKit.Box("Pill_" + i, _strip, 0f, 0f, 120f, PillHeight);
                Image background = Rounded(rect, RaisedColor);

                TMP_Text label = UiKit.Text("Text", rect, 17f, SecondaryColor, Font, Persian);
                label.alignment = TextAlignmentOptions.Center;
                label.text = Shorten(target.Title, MaxPillChars);
                label.ForceMeshUpdate();

                float width = Mathf.Clamp(label.preferredWidth + 34f, 86f, 210f);
                rect.sizeDelta = new Vector2(width, PillHeight);

                if (Persian)
                {
                    cursor -= width;
                    rect.anchoredPosition = new Vector2(cursor, 0f);
                    cursor -= PillGap;
                }
                else
                {
                    rect.anchoredPosition = new Vector2(cursor, 0f);
                    cursor += width + PillGap;
                }

                Pill pill = new Pill();
                pill.Rect = rect;
                pill.Background = background;
                pill.Label = label;

                _pills.Add(pill);
            }
        }

        /// <summary>بریدن نام‌های بلند؛ به فونت و TMP وابسته نیست، پس همه‌جا یکسان است.</summary>
        private static string Shorten(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text;

            return text.Substring(0, max - 1).TrimEnd() + "…";
        }

        /* ============================================================
           ساخت
           ============================================================ */

        private void Rebuild()
        {
            if (_canvasRect != null)
            {
                _canvasRect.gameObject.SetActive(false);
                Destroy(_canvasRect.gameObject);
                _canvasRect = null;
            }

            float previous = _alpha;

            Build();

            _alpha = previous;
            _lastTarget = null;
            _lastMulti = false;
        }

        private void Build()
        {
            GameObjects();

            _canvasRect = MakeCanvas();

            _root = UiKit.Node("Root", _canvasRect);
            _root.anchorMin = new Vector2(0f, 0f);
            _root.anchorMax = new Vector2(0f, 0f);
            _root.pivot = new Vector2(0f, 0f);
            _root.anchoredPosition = new Vector2(MarginX, MarginY);
            _root.sizeDelta = new Vector2(Width, PillHeight + StripGap + CardHeight);

            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            _strip = UiKit.Box("Strip", _root, 0f, 0f, Width, PillHeight);
            _strip.gameObject.SetActive(false);

            _card = UiKit.Box("Card", _root, 0f, PillHeight + StripGap, Width, CardHeight);

            BuildCard();
            BuildTexts();
        }

        private void GameObjects()
        {
            _pills.Clear();
            _stripTargets.Clear();
            _keyIcon = null;
            _lastMulti = false;
        }

        private RectTransform MakeCanvas()
        {
            GameObject canvasObject = new GameObject("HudCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvasObject.GetComponent<RectTransform>();
        }

        private void BuildCard()
        {
            Image background = _card.gameObject.AddComponent<Image>();
            background.color = CardColor;
            background.raycastTarget = false;

            // خط باریک بالا: تنها عنصر تزئینی کارت
            RectTransform accent = UiKit.Box("Accent", _card, 0f, 0f, Width, 2f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.85f);
            accentImage.raycastTarget = false;

            RectTransform bottom = UiKit.Box("Rule", _card, 0f, CardHeight - 1f, Width, 1f);
            Image bottomImage = bottom.gameObject.AddComponent<Image>();
            bottomImage.color = BorderColor;
            bottomImage.raycastTarget = false;

            float badgeX = Persian ? Width - Pad - BadgeSize : Pad;

            RectTransform badge = UiKit.Box("KeyBadge", _card, badgeX, 14f, BadgeSize, BadgeSize);
            Rounded(badge, RaisedColor);

            _keyText = UiKit.Text("Key", badge, 20f, PrimaryColor, Font, false);
            _keyText.alignment = TextAlignmentOptions.Center;
            _keyText.text = "E";
        }

        private void BuildTexts()
        {
            float textX = Persian ? Pad : Pad + BadgeSize + BadgeGap;
            float textWidth = Width - Pad * 2f - BadgeSize - BadgeGap;

            _verbText = MakeLine("Verb", 12f, 24f, 19f, AccentColor, textX, textWidth);
            _titleText = MakeLine("Title", 38f, 32f, 26f, PrimaryColor, textX, textWidth);
            _subtitleText = MakeLine("Subtitle", 74f, 22f, 17f, SecondaryColor, textX, textWidth);
            _hintText = MakeLine("Hint", 100f, 18f, 13f, MutedColor, textX, textWidth);

            _hintText.text = Settings.Loc.Get(Settings.Loc.Channel.Menu, "hud.scroll");
            _hintText.gameObject.SetActive(false);
        }

        private TMP_Text MakeLine(string name, float y, float height, float size, Color color, float x, float width)
        {
            RectTransform box = UiKit.Box(name, _card, x, y, width, height);
            TMP_Text text = UiKit.Text("Text", box, size, color, Font, Persian);

            text.alignment = Align;
            return text;
        }

        private static Image Rounded(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiKit.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;

            return image;
        }
    }
}
