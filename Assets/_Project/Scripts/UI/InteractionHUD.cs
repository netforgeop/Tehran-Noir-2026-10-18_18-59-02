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
    /// کارت تعامل، گوشه‌ی پایین-راست: پنل کرم با گرادیان، سرصفحه‌ی نام و نقش،
    /// ردیف‌های کار، و فوتر تیره با کلیدها.
    ///
    /// تمام رابط در زمان اجرا ساخته می‌شود. کانواس در ریشه‌ی صحنه است، نه زیر این آبجکت؛
    /// CanvasScaler فقط روی کانواس ریشه مقیاس ۱۹۲۰×۱۰۸۰ را اعمال می‌کند.
    ///
    /// هیچ متن و نامی این‌جا نوشته نشده: عنوان و نقش از خودِ آبجکت، و برچسب کارها از
    /// جدول مرکزی فعل‌ها می‌آید. فقط دو کلید ثابت فوتر از فایل زبان خوانده می‌شود.
    /// </summary>
    public class InteractionHUD : MonoBehaviour
    {
        /* ---------- چیدمان (واحد کانواس ۱۹۲۰×۱۰۸۰) ---------- */
        private const float Width = 440f;
        private const float Pad = 20f;
        private const float HeaderHeight = 78f;
        private const float RowHeight = 42f;
        private const float RowGap = 3f;
        private const float FooterHeight = 54f;
        private const float Corner = 12f;
        private const float StripGap = 8f;
        private const float PillHeight = 32f;
        private const float MarginX = 48f;
        private const float MarginY = 44f;
        private const float SlidePixels = 22f;
        private const int MaxRows = 4;
        private const int MaxPills = 4;
        private const int MaxPillChars = 18;

        /* ---------- رنگ ---------- */
        private static readonly Color PanelTop = new Color(0.80f, 0.72f, 0.61f, 0.93f);
        private static readonly Color PanelBottom = new Color(0.45f, 0.37f, 0.29f, 0.95f);
        private static readonly Color PanelEdge = new Color(0.82f, 0.73f, 0.63f, 0.60f);
        private static readonly Color FooterColor = new Color(0.07f, 0.06f, 0.055f, 0.96f);
        private static readonly Color TitleColor = new Color(0.97f, 0.94f, 0.89f, 1f);
        private static readonly Color RoleColor = new Color(0.78f, 0.71f, 0.61f, 1f);
        private static readonly Color RowIdleText = new Color(0.95f, 0.91f, 0.85f, 1f);
        private static readonly Color RowActiveText = new Color(0.10f, 0.09f, 0.07f, 1f);
        private static readonly Color RowActiveFill = new Color(0.92f, 0.88f, 0.81f, 0.95f);
        private static readonly Color RuleColor = new Color(0.82f, 0.73f, 0.63f, 0.30f);
        private static readonly Color FooterText = new Color(0.93f, 0.90f, 0.85f, 1f);
        private static readonly Color PillIdle = new Color(0.20f, 0.17f, 0.14f, 0.88f);
        private static readonly Color PillActive = new Color(0.92f, 0.88f, 0.81f, 0.95f);

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

        [Tooltip("مدت باز شدن کارت، ثانیه")]
        [SerializeField] private float openSeconds = 0.20f;

        [Tooltip("مدت بسته شدن کارت، ثانیه")]
        [SerializeField] private float closeSeconds = 0.14f;

        [Tooltip("اندازه در حالت بسته؛ کمتر از ۱ یعنی کارت هنگام آمدن کمی جمع است")]
        [SerializeField, Range(0.5f, 1f)] private float hiddenScale = 0.94f;

        private sealed class Row
        {
            public RectTransform Rect;
            public Image Background;
            public TMP_Text Label;
            public Image Arrow;
        }

        private sealed class Pill
        {
            public RectTransform Rect;
            public Image Background;
            public TMP_Text Label;
        }

        private readonly List<Row> _rows = new List<Row>(MaxRows);
        private readonly List<Pill> _pills = new List<Pill>(MaxPills);
        private readonly List<Interactable> _stripTargets = new List<Interactable>(MaxPills);
        private readonly List<InteractionAction> _verbs = new List<InteractionAction>(MaxRows);

        private GameObject _canvasObject;
        private CanvasGroup _group;
        private PanelMotion _motion;
        private RectTransform _root;
        private RectTransform _card;
        private RectTransform _strip;

        private Image _panelImage;
        private Image _footerImage;
        private RectTransform _footer;
        private Image _selectBadge;
        private TMP_Text _selectLetter;
        private TMP_Text _selectText;
        private TMP_Text _closeText;

        private TMP_Text _titleText;
        private TMP_Text _roleText;

        private Interactable _lastTarget;
        private int _lastVerb = -1;
        private int _lastSignature;
        private bool _lastGamepad;
        private bool _lastMulti;

        private static readonly Interactable[] NoTargets = new Interactable[0];

        /// <summary>آیا کارت همین حالا روی صفحه است. کمان این را می‌خواند.</summary>
        public static bool IsShown
        {
            get { return _active != null && _active._lastTarget != null; }
        }

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
                Highlight(verb);
            }

            SyncStrip(list, sensor.Index);

            // حرکت کارت همه‌اش در PanelMotion است: مقیاس + آلفا + جابه‌جایی از لبه
            _motion.SetVisible(current != null);
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

            string role = target.Subtitle;
            _roleText.text = role;
            _roleText.gameObject.SetActive(!string.IsNullOrEmpty(role));

            BuildRows(target);
            Layout();

            SetSelectKey(usingGamepad);

            Highlight(0);
        }

        /// <summary>ردیف‌های کار؛ حداکثر چهار تا مثل مرجع.</summary>
        private void BuildRows(Interactable target)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Rect == null) continue;

                _rows[i].Rect.gameObject.SetActive(false);
                Destroy(_rows[i].Rect.gameObject);
            }

            _rows.Clear();
            _verbs.Clear();

            IReadOnlyList<InteractionAction> verbs = target.Verbs;

            if (verbs != null)
                for (int i = 0; i < verbs.Count && i < MaxRows; i++) _verbs.Add(verbs[i]);

            if (_verbs.Count == 0) _verbs.Add(target.Action);

            for (int i = 0; i < _verbs.Count; i++)
            {
                Row row = new Row();

                row.Rect = UiKit.Box("Row_" + i, _card, 10f, 0f, Width - 20f, RowHeight);

                row.Background = row.Rect.gameObject.AddComponent<Image>();
                row.Background.sprite = UiKit.RoundedRect;
                row.Background.type = Image.Type.Sliced;
                row.Background.color = new Color(1f, 1f, 1f, 0f);
                row.Background.raycastTarget = false;

                float arrowX = Persian ? Width - 20f - 26f : 10f;
                float labelX = Persian ? 10f : 34f;
                float labelWidth = Width - 20f - labelX - 36f;

                RectTransform labelBox = UiKit.Box("Label", row.Rect, labelX, 0f, labelWidth, RowHeight);

                row.Label = UiKit.Text("Text", labelBox, 20f, RowIdleText, Font, Persian);
                row.Label.alignment = Align;
                row.Label.text = target.VerbLabel(_verbs[i]);

                RectTransform arrowBox = UiKit.Box("Arrow", row.Rect, arrowX, (RowHeight - 16f) * 0.5f, 16f, 16f);

                row.Arrow = arrowBox.gameObject.AddComponent<Image>();
                row.Arrow.sprite = HudArt.Icon(HudArt.Glyph.ArrowLeft, 48, RowActiveText);
                row.Arrow.raycastTarget = false;
                row.Arrow.gameObject.SetActive(false);

                _rows.Add(row);
            }
        }

        /// <summary>ردیف انتخاب‌شده کرم روشن با متن تیره و فلش؛ بقیه فقط متن.</summary>
        private void Highlight(int selected)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i == selected;

                _rows[i].Background.color = active ? RowActiveFill : new Color(1f, 1f, 1f, 0f);
                _rows[i].Label.color = active ? RowActiveText : RowIdleText;
                _rows[i].Label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                _rows[i].Arrow.gameObject.SetActive(active);
            }
        }

        /// <summary>قد کارت با تعداد کارها حساب می‌شود؛ فوتر همیشه ته آن می‌نشیند.</summary>
        private void Layout()
        {
            int rows = Mathf.Max(1, _verbs.Count);

            for (int i = 0; i < _rows.Count; i++)
            {
                float y = HeaderHeight + i * (RowHeight + RowGap);
                _rows[i].Rect.anchoredPosition = new Vector2(10f, -y);
            }

            float rowsBottom = HeaderHeight + rows * RowHeight + (rows - 1) * RowGap;
            float footerTop = rowsBottom + 12f;
            float height = footerTop + FooterHeight;

            _footer.anchoredPosition = new Vector2(0f, -footerTop);

            _card.sizeDelta = new Vector2(Width, height);

            ApplyPanel(Mathf.CeilToInt(height));

            _strip.anchoredPosition = new Vector2(0f, height + StripGap);
            _root.sizeDelta = new Vector2(Width, height + StripGap + PillHeight);
        }

        /// <summary>
        /// بافت پنل با قد تازه؛ یک بار در هر اندازه ساخته و کش می‌شود.
        /// گرادیان و حاشیه داخل خود بافت است تا گوشه‌ی گرد بیرون نزند.
        /// </summary>
        private void ApplyPanel(int height)
        {
            int width = Mathf.CeilToInt(Width);

            _panelImage.sprite = HudArt.Panel("hud.card." + height, width, height, Corner,
                PanelTop, PanelBottom, PanelEdge, 1.2f);

            if (_footerImage.sprite == null)
            {
                _footerImage.sprite = HudArt.Panel("hud.footer", width, Mathf.CeilToInt(FooterHeight), Corner,
                    FooterColor, FooterColor, PanelEdge, 1f, false, true);
            }
        }

        /// <summary>کلید انتخاب در فوتر: E روی کیبورد، X روی دسته.</summary>
        private void SetSelectKey(bool gamepad)
        {
            string key = gamepad ? "X" : "E";

            Sprite sprite = UiKit.KeyIcon(key);

            _selectBadge.sprite = sprite;
            _selectBadge.color = sprite != null ? Color.white : RowActiveFill;
            _selectBadge.preserveAspect = sprite != null;

            _selectLetter.text = key;
            _selectLetter.gameObject.SetActive(sprite == null);
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
            }

            if (multi) RebuildStripIfNeeded(list);

            for (int i = 0; i < _pills.Count; i++)
            {
                bool active = i == selected;

                _pills[i].Background.color = active ? PillActive : PillIdle;
                _pills[i].Label.color = active ? RowActiveText : RowIdleText;
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

                Image background = rect.gameObject.AddComponent<Image>();
                background.sprite = UiKit.RoundedRect;
                background.type = Image.Type.Sliced;
                background.color = PillIdle;
                background.raycastTarget = false;

                TMP_Text label = UiKit.Text("Text", rect, 16f, RowIdleText, Font, Persian);
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

            bool wasVisible = _motion != null && _motion.IsOpen;

            Build();

            _motion.SnapVisible(wasVisible);
            _lastTarget = null;
            _lastMulti = false;
        }

        private void Build()
        {
            _rows.Clear();
            _pills.Clear();
            _stripTargets.Clear();
            _verbs.Clear();
            _lastMulti = false;
            _lastVerb = -1;

            _canvasObject = MakeCanvas();

            _root = UiKit.Node("Root", _canvasObject.transform);
            _root.anchorMin = new Vector2(1f, 0f);
            _root.anchorMax = new Vector2(1f, 0f);
            _root.pivot = new Vector2(1f, 0f);
            _root.anchoredPosition = new Vector2(-MarginX, MarginY);
            _root.sizeDelta = new Vector2(Width, 240f);

            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            _motion = _root.gameObject.AddComponent<PanelMotion>();
            _motion.Configure(_root, _group, hiddenScale, SlidePixels, PanelMotion.Edge.Right,
                openSeconds, closeSeconds);
            _motion.SetRestPosition(new Vector2(-MarginX, MarginY));
            _motion.SnapVisible(false);

            // نوار بالا، کارت پایین؛ ترتیب ساخت یعنی کارت روی نوار کشیده می‌شود
            _strip = UiKit.Node("Strip", _root);
            _strip.anchorMin = new Vector2(0f, 0f);
            _strip.anchorMax = new Vector2(0f, 0f);
            _strip.pivot = new Vector2(0f, 0f);
            _strip.anchoredPosition = new Vector2(0f, 240f + StripGap);
            _strip.sizeDelta = new Vector2(Width, PillHeight);
            _strip.gameObject.SetActive(false);

            _card = UiKit.Node("Card", _root);
            _card.anchorMin = new Vector2(0f, 0f);
            _card.anchorMax = new Vector2(0f, 0f);
            _card.pivot = new Vector2(0f, 0f);
            _card.anchoredPosition = Vector2.zero;
            _card.sizeDelta = new Vector2(Width, 240f);

            BuildPanel();
            BuildHeader();
            BuildFooter();

            ApplyPanel(240);
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

        private void BuildPanel()
        {
            RectTransform panel = UiKit.Node("Panel", _card);
            UiKit.Stretch(panel);

            _panelImage = panel.gameObject.AddComponent<Image>();
            _panelImage.raycastTarget = false;
            _panelImage.type = Image.Type.Simple;
        }

        private void BuildHeader()
        {
            float iconX = Persian ? Width - Pad - 34f : Pad;

            RectTransform iconBox = UiKit.Box("HeaderIcon", _card, iconX, 14f, 34f, 34f);

            Image icon = iconBox.gameObject.AddComponent<Image>();
            icon.sprite = HudArt.Icon(HudArt.Glyph.Chat, 64, TitleColor);
            icon.raycastTarget = false;
            icon.color = new Color(1f, 1f, 1f, 0.85f);

            float textWidth = Width - Pad * 2f - 34f - 10f;

            RectTransform titleBox = UiKit.Box("Title", _card, Pad, 12f, textWidth, 32f);

            _titleText = UiKit.Text("Text", titleBox, 25f, TitleColor, Font, Persian);
            _titleText.alignment = Align;
            _titleText.fontStyle = FontStyles.Bold;

            RectTransform roleBox = UiKit.Box("Role", _card, Pad, 42f, textWidth, 24f);

            _roleText = UiKit.Text("Text", roleBox, 18f, RoleColor, Font, Persian);
            _roleText.alignment = Align;

            RectTransform rule = UiKit.Box("Rule", _card, Pad, 66f, Width - Pad * 2f, 1f);

            Image ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.color = RuleColor;
            ruleImage.raycastTarget = false;
        }

        private void BuildFooter()
        {
            _footer = UiKit.Box("Footer", _card, 0f, 0f, Width, FooterHeight);

            _footerImage = _footer.gameObject.AddComponent<Image>();
            _footerImage.raycastTarget = false;

            // در فارسی: «انتخاب» و کلیدش سمت راست، «بستن» و Esc سمت چپ
            float badgeY = (FooterHeight - 26f) * 0.5f;

            float selectBadgeX = Width - Pad - 26f;
            float closeBadgeX = Pad;

            RectTransform selectBadgeBox = UiKit.Box("SelectKey", _footer, selectBadgeX, badgeY, 26f, 26f);

            _selectBadge = selectBadgeBox.gameObject.AddComponent<Image>();
            _selectBadge.raycastTarget = false;
            _selectBadge.preserveAspect = true;

            _selectLetter = UiKit.Text("Letter", selectBadgeBox, 15f, RowActiveText, Font, false);
            _selectLetter.alignment = TextAlignmentOptions.Center;

            RectTransform closeBadgeBox = UiKit.Box("CloseKey", _footer, closeBadgeX, badgeY, 26f, 26f);

            Image closeBadge = closeBadgeBox.gameObject.AddComponent<Image>();
            closeBadge.sprite = UiKit.KeyIcon("Esc");
            closeBadge.raycastTarget = false;
            closeBadge.preserveAspect = true;
            closeBadge.color = closeBadge.sprite != null ? Color.white : RowActiveFill;

            if (closeBadge.sprite == null)
            {
                TMP_Text letter = UiKit.Text("Letter", closeBadgeBox, 13f, RowActiveText, Font, false);
                letter.alignment = TextAlignmentOptions.Center;
                letter.text = "Esc";
            }

            float selectLabelX = Width * 0.5f + 12f;
            float selectLabelWidth = selectBadgeX - 10f - selectLabelX;

            RectTransform selectLabelBox = UiKit.Box("Select", _footer, selectLabelX, 0f, selectLabelWidth, FooterHeight);

            _selectText = UiKit.Text("Text", selectLabelBox, 17f, FooterText, Font, Persian);
            _selectText.alignment = Align;
            _selectText.text = Settings.Loc.Get(Settings.Loc.Channel.Menu, "hud.select");

            float closeLabelX = closeBadgeX + 26f + 10f;
            float closeLabelWidth = Width * 0.5f - 12f - closeLabelX;

            RectTransform closeLabelBox = UiKit.Box("Close", _footer, closeLabelX, 0f, closeLabelWidth, FooterHeight);

            _closeText = UiKit.Text("Text", closeLabelBox, 17f, FooterText, Font, Persian);
            _closeText.alignment = TextAlignmentOptions.MidlineLeft;
            _closeText.text = Settings.Loc.Get(Settings.Loc.Channel.Menu, "pause.close");

            RectTransform divider = UiKit.Box("Divider", _footer, Width * 0.5f, 16f, 1f, 22f);

            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(0.93f, 0.90f, 0.85f, 0.18f);
            dividerImage.raycastTarget = false;
        }
    }
}
