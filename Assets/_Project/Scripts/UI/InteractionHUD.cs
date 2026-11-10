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
    /// کارت تعامل، گوشه‌ی راست-پایین صفحه: نوار اهداف نزدیک (بالای کارت) + کارت کار.
    /// تمام رابط در زمان اجرا ساخته می‌شود.
    /// کانواس در ریشه‌ی صحنه ساخته می‌شود؛ چون CanvasScaler فقط روی کانواس ریشه اثر دارد
    /// و کانواس تودرتو مقیاس ۱۹۲۰×۱۰۸۰ را نمی‌گیرد.
    /// هیچ اسم و متنی این‌جا نوشته نشده؛ همه از خودِ آبجکت و فایل زبان می‌آید.
    /// </summary>
    public class InteractionHUD : MonoBehaviour
    {
        /* ---------- چیدمان (واحد کانواس ۱۹۲۰×۱۰۸۰) ---------- */
        private const float Width = 470f;
        private const float CardHeight = 116f;
        private const float PillHeight = 32f;
        private const float StripGap = 8f;
        private const float MarginX = 48f;
        private const float MarginY = 44f;
        private const float Pad = 18f;
        private const float BadgeSize = 44f;
        private const float BadgeGap = 14f;
        private const float SlidePixels = 16f;
        private const float VerbTop = 10f;
        private const float VerbHeight = 24f;
        private const int MaxPills = 4;
        private const int MaxPillChars = 18;

        /* ---------- رنگ ---------- */
        private static readonly Color CardColor = new Color(0.09f, 0.106f, 0.125f, 0.93f);
        private static readonly Color RaisedColor = new Color(0.145f, 0.165f, 0.188f, 0.95f);
        private static readonly Color PillIdleColor = new Color(0.145f, 0.165f, 0.188f, 0.9f);
        private static readonly Color PillActiveColor = new Color(0.541f, 0.604f, 0.357f, 0.30f);
        private static readonly Color PrimaryColor = new Color(0.941f, 0.933f, 0.914f);
        private static readonly Color SecondaryColor = new Color(0.655f, 0.663f, 0.62f);
        private static readonly Color MutedColor = new Color(0.45f, 0.47f, 0.43f);
        private static readonly Color AccentColor = new Color(0.541f, 0.604f, 0.357f);
        private static readonly Color BorderColor = new Color(0.23f, 0.25f, 0.22f, 0.9f);

        private static InteractionHUD _active;

        [Header("وابستگی‌ها")]
        [Tooltip("خالی بماند تا خودکار در صحنه پیدا شود")]
        [SerializeField] private InteractionSensor sensor;

        [Tooltip("برای نمایش آیکون دسته به‌جای کیبورد")]
        [SerializeField] private PlayerInputReader input;

        [Header("فونت")]
        [Tooltip("فونت فارسی — Vazirmatn-Regular SDF (همان فونت دیالوگ)")]
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

        private GameObject _canvasObject;
        private CanvasGroup _group;
        private RectTransform _root;
        private RectTransform _strip;
        private RectTransform _card;
        private GameObject _keyIcon;

        private readonly List<TMP_Text> _verbRows = new List<TMP_Text>();
        private readonly List<InteractionAction> _verbs = new List<InteractionAction>(3);

        private TMP_Text _keyText;
        private TMP_Text _titleText;
        private TMP_Text _subtitleText;
        private TMP_Text _hintText;

        private RectTransform _titleBox;
        private RectTransform _subtitleBox;
        private RectTransform _hintBox;

        private Interactable _lastTarget;
        private int _lastVerb = -1;
        private int _lastSignature;
        private bool _lastGamepad;
        private bool _lastMulti;
        private float _alpha;

        private static readonly Interactable[] NoTargets = new Interactable[0];

        private bool Persian => Localization.Persian;

        private TMP_FontAsset Font => Persian && font != null ? font : (latinFont != null ? latinFont : font);

        private TextAlignmentOptions Align =>
            Persian ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            // دو نسخه از این اسکریپت یعنی دو کارت روی هم؛ دومی خاموش می‌شود
            if (_active != null && _active != this)
            {
                Debug.LogWarning("[HUD] بیش از یک Interaction HUD در صحنه است؛ دومی خاموش شد.", this);
                enabled = false;
                return;
            }

            _active = this;

            if (clearExistingChildren) ClearOldChildren();

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

        private void OnDestroy()
        {
            // کانواس دیگر فرزند این آبجکت نیست؛ باید خودمان پاکش کنیم
            if (_canvasObject != null) Destroy(_canvasObject);

            if (_active == this) _active = null;
        }

        private void Update()
        {
            // بدون سنسور هیچ‌چیز نیست؛ ولی نبودِ ورودی نباید HUD را کامل خاموش کند
            if (_group == null || sensor == null) return;

            // سنسور خاموش است یعنی چیزی برای تعامل نیست (سند باز، دیالوگ)؛
            // کارت باید برود، نه اینکه روی آخرین حالت یخ بزند.
            bool live = sensor.enabled;

            IReadOnlyList<Interactable> list = live ? sensor.Candidates : NoTargets;
            Interactable current = live ? sensor.Current : null;

            bool usingGamepad = input != null && input.UsingGamepad;

            int verb = sensor.VerbIndex;

            // فهرست کارهای هدف هم می‌تواند عوض شود (پرونده بعد از باز شدن «خواندن» می‌شود).
            // پس فقط تغییر هدف کافی نیست؛ امضای کارها هم باید بررسی شود.
            int signature = VerbSignature(current);

            // TMP برای هر نوشتن مِش از نو می‌سازد؛ فقط با تغییر می‌نویسیم
            if (current != _lastTarget || usingGamepad != _lastGamepad || signature != _lastSignature)
            {
                _lastTarget = current;
                _lastGamepad = usingGamepad;
                _lastSignature = signature;

                Apply(current, usingGamepad);
            }
            else if (verb != _lastVerb)
            {
                _lastVerb = verb;
                HighlightVerbs(verb);
            }

            SyncStrip(list, sensor.Index);

            float target = current != null ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, fadeSpeed * Time.unscaledDeltaTime);

            _group.alpha = _alpha;

            // پنهان: کمی به سمت لبه‌ی راست می‌رود؛ پیداشدن هم از همان‌جا برمی‌گردد
            _root.anchoredPosition = new Vector2(-MarginX + (1f - _alpha) * SlidePixels, MarginY);
        }

        /// <summary>بعد از عوض شدن زبان یا از بیرون صدا زده می‌شود.</summary>
        public void Refresh()
        {
            _lastTarget = null;
            _lastSignature = 0;
        }

        /// <summary>اثر انگشت فهرست کارهای هدف؛ با هر تغییر کارها عوض می‌شود.</summary>
        private static int VerbSignature(Interactable target)
        {
            if (target == null) return 0;

            IReadOnlyList<InteractionAction> verbs = target.Verbs;
            if (verbs == null || verbs.Count == 0) return 1;

            int hash = 17;
            for (int i = 0; i < verbs.Count; i++)
                hash = hash * 31 + (int)verbs[i];

            return hash;
        }

        /* ============================================================
           محتوا
           ============================================================ */

        private void Apply(Interactable target, bool usingGamepad)
        {
            if (target == null) return;

            // ردیف‌ها از نو ساخته می‌شوند؛ انتخاب همیشه از اول است
            _lastVerb = -1;

            _titleText.text = target.Title;

            string subtitle = target.Subtitle;
            _subtitleText.text = subtitle;
            _subtitleBox.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));

            BuildVerbs(target);
            ApplyHint();

            SetKey(usingGamepad ? "X" : "E");
        }

        /// <summary>
        /// کارهای هدف را به‌صورت ردیف می‌سازد: «باز کردن»، «برداشتن».
        /// ارتفاع کارت با تعداد کارها حساب می‌شود، پس یک کار همان کارت کوچک قبل است.
        /// </summary>
        private void BuildVerbs(Interactable target)
        {
            for (int i = 0; i < _verbRows.Count; i++)
            {
                if (_verbRows[i] == null) continue;

                _verbRows[i].transform.parent.gameObject.SetActive(false);
                Destroy(_verbRows[i].transform.parent.gameObject);
            }

            _verbRows.Clear();
            _verbs.Clear();

            IReadOnlyList<InteractionAction> verbs = target.Verbs;
            if (verbs != null)
                for (int i = 0; i < verbs.Count && i < 3; i++) _verbs.Add(verbs[i]);

            if (_verbs.Count == 0) _verbs.Add(target.Action);

            float textWidth = Width - Pad * 2f - BadgeSize - BadgeGap;
            float textX = Persian ? Pad : Pad + BadgeSize + BadgeGap;

            for (int i = 0; i < _verbs.Count; i++)
            {
                RectTransform box = UiKit.Box("Verb_" + i, _card, textX, VerbY(i), textWidth, VerbHeight);

                TMP_Text text = UiKit.Text("Text", box, 17f, SecondaryColor, Font, Persian);
                text.alignment = Align;
                text.text = (i + 1) + ". " + target.VerbLabel(_verbs[i]);

                _verbRows.Add(text);
            }

            Layout();
            HighlightVerbs(_lastVerb < 0 ? 0 : _lastVerb);
        }

        private static float VerbY(int index) => VerbTop + index * VerbHeight;

        /// <summary>کار انتخاب‌شده روشن و بقیه خاموش؛ بدون بازسازی متن.</summary>
        private void HighlightVerbs(int selected)
        {
            for (int i = 0; i < _verbRows.Count; i++)
            {
                bool active = i == selected;

                _verbRows[i].color = active ? AccentColor : SecondaryColor;
                _verbRows[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        /// <summary>هر تعداد کار، چیدمان عمودی از نو حساب می‌شود؛ یک قاعده برای هر حالت.</summary>
        private void Layout()
        {
            int rows = Mathf.Max(1, _verbs.Count);

            float titleY = VerbTop + rows * VerbHeight + 2f;
            float subtitleY = titleY + 30f;
            float hintY = subtitleY + 20f;

            _titleBox.anchoredPosition = new Vector2(_titleBox.anchoredPosition.x, -titleY);
            _subtitleBox.anchoredPosition = new Vector2(_subtitleBox.anchoredPosition.x, -subtitleY);
            _hintBox.anchoredPosition = new Vector2(_hintBox.anchoredPosition.x, -hintY);

            float height = hintY + 16f + 10f;

            _card.sizeDelta = new Vector2(Width, height);
            _strip.anchoredPosition = new Vector2(0f, height + StripGap);
            _root.sizeDelta = new Vector2(Width, height + StripGap + PillHeight);
        }

        /// <summary>راهنمای پایین کارت: با چند کار، کلیدهای عددی؛ با چند هدف، چرخ موس.</summary>
        private void ApplyHint()
        {
            bool manyVerbs = _verbs.Count > 1;

            _hintText.text = Settings.Loc.Get(Settings.Loc.Channel.Menu, manyVerbs ? "hud.verbs" : "hud.scroll");
            _hintBox.gameObject.SetActive(manyVerbs || _lastMulti);
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
                    rect.sizeDelta = new Vector2(26f, 26f);

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
                ApplyHint();
            }

            if (multi) RebuildStripIfNeeded(list);

            for (int i = 0; i < _pills.Count; i++)
            {
                bool active = i == selected;

                _pills[i].Background.color = active ? PillActiveColor : PillIdleColor;
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

            // فارسی: از لبه‌ی راست به چپ. انگلیسی: از لبه‌ی چپ به راست.
            float cursor = Persian ? Width : 0f;

            for (int i = 0; i < _stripTargets.Count; i++)
            {
                Interactable target = _stripTargets[i];

                RectTransform rect = UiKit.Box("Pill_" + i, _strip, 0f, 0f, 120f, PillHeight);

                Image background = Rounded(rect, PillIdleColor);

                TMP_Text label = UiKit.Text("Text", rect, 16f, SecondaryColor, Font, Persian);
                label.alignment = TextAlignmentOptions.Center;
                label.text = Shorten(target.Title, MaxPillChars);
                label.ForceMeshUpdate();

                float width = Mathf.Clamp(label.preferredWidth + 30f, 84f, 210f);
                rect.sizeDelta = new Vector2(width, PillHeight);

                float left = Persian ? cursor - width : cursor;

                rect.anchoredPosition = new Vector2(left, 0f);

                if (Persian) cursor -= width + StripGap;
                else cursor += width + StripGap;

                Pill pill = new Pill();
                pill.Rect = rect;
                pill.Background = background;
                pill.Label = label;

                _pills.Add(pill);
            }
        }

        /// <summary>بریدن نام‌های بلند؛ بدون وابستگی به فونت، پس همه‌جا یکسان است.</summary>
        private static string Shorten(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text;

            return text.Substring(0, max - 1).TrimEnd() + "…";
        }

        /* ============================================================
           ساخت
           ============================================================ */

        /// <summary>کارت قدیمی صحنه (قبل از بازنویسی) فقط مزاحم است.</summary>
        private void ClearOldChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject old = transform.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
        }

        private void Rebuild()
        {
            if (_canvasObject != null)
            {
                _canvasObject.SetActive(false);
                Destroy(_canvasObject);
                _canvasObject = null;
            }

            float previous = _alpha;

            Build();

            _alpha = previous;
            _lastTarget = null;
            _lastMulti = false;
        }

        private void Build()
        {
            _pills.Clear();
            _stripTargets.Clear();
            _verbRows.Clear();
            _verbs.Clear();
            _keyIcon = null;
            _lastMulti = false;
            _lastVerb = -1;

            _canvasObject = MakeCanvas();

            _root = UiKit.Node("Root", _canvasObject.transform);
            _root.anchorMin = new Vector2(1f, 0f);
            _root.anchorMax = new Vector2(1f, 0f);
            _root.pivot = new Vector2(1f, 0f);
            _root.anchoredPosition = new Vector2(-MarginX, MarginY);
            _root.sizeDelta = new Vector2(Width, CardHeight + StripGap + PillHeight);

            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            // نوار بالا، کارت پایین؛ ترتیب ساخت یعنی کارت روی نوار کشیده می‌شود
            _strip = UiKit.Node("Strip", _root);
            _strip.anchorMin = new Vector2(0f, 0f);
            _strip.anchorMax = new Vector2(0f, 0f);
            _strip.pivot = new Vector2(0f, 0f);
            _strip.anchoredPosition = new Vector2(0f, CardHeight + StripGap);
            _strip.sizeDelta = new Vector2(Width, PillHeight);
            _strip.gameObject.SetActive(false);

            _card = UiKit.Node("Card", _root);
            _card.anchorMin = new Vector2(0f, 0f);
            _card.anchorMax = new Vector2(0f, 0f);
            _card.pivot = new Vector2(0f, 0f);
            _card.anchoredPosition = Vector2.zero;
            _card.sizeDelta = new Vector2(Width, CardHeight);

            BuildCard();
            BuildTexts();
        }

        /// <summary>
        /// کانواس در ریشه‌ی صحنه، بدون والد. زیر آبجکت HUD ساخته شود، CanvasScaler
        /// نادیده گرفته می‌شود و کل کارت به اندازه‌ی مستطیل والد فشرده می‌شود.
        /// </summary>
        private GameObject MakeCanvas()
        {
            GameObject canvasObject = new GameObject("HUD Canvas (runtime)",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(null, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvasObject;
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

            float badgeX = Persian ? Width - Pad - BadgeSize : Pad;

            RectTransform badge = UiKit.Box("KeyBadge", _card, badgeX, 12f, BadgeSize, BadgeSize);
            Rounded(badge, RaisedColor);

            _keyText = UiKit.Text("Key", badge, 19f, PrimaryColor, Font, false);
            _keyText.alignment = TextAlignmentOptions.Center;
            _keyText.text = "E";
        }

        private void BuildTexts()
        {
            float textWidth = Width - Pad * 2f - BadgeSize - BadgeGap;
            float textX = Persian ? Pad : Pad + BadgeSize + BadgeGap;

            _titleBox = UiKit.Box("Title", _card, textX, 40f, textWidth, 30f);
            _titleText = UiKit.Text("Text", _titleBox, 24f, PrimaryColor, Font, Persian);
            _titleText.alignment = Align;

            _subtitleBox = UiKit.Box("Subtitle", _card, textX, 70f, textWidth, 20f);
            _subtitleText = UiKit.Text("Text", _subtitleBox, 15f, SecondaryColor, Font, Persian);
            _subtitleText.alignment = Align;

            _hintBox = UiKit.Box("Hint", _card, textX, 90f, textWidth, 16f);
            _hintText = UiKit.Text("Text", _hintBox, 12f, MutedColor, Font, Persian);
            _hintText.alignment = Align;

            _hintText.text = Settings.Loc.Get(Settings.Loc.Channel.Menu, "hud.scroll");
            _hintBox.gameObject.SetActive(false);

            Layout();
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
