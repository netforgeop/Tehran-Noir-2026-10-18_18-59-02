using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TehranNoir.Characters;
using TehranNoir.Core;
using TehranNoir.Dialogue;
using TehranNoir.Documents;
using TehranNoir.Settings;
using TehranNoir.World;

namespace TehranNoir.UI
{
    /// <summary>
    /// گیج دایره‌ای سمت راست صفحه.
    ///
    ///   دیسک تیره — پشت همه چیز، با یک رینگ نازک خاکستری دور آن.
    ///   کمان بیرونی چپ — جان (سرخ) + زره (آبی). هر دو روی یک مقیاس ۲۰۰ هستند،
    ///                    پس جان ۱۰۰ و زره ۵۰ یعنی ۷۵٪ کمان پر، و یک‌سومش آبی.
    ///   کمان درونی چپ — استقامت (زرد). زیر صفر همان کمان قرمز می‌شود و از پایین بالا می‌آید.
    ///   سه قطعه‌ی لبه‌ی راست — سه ویژگیِ برگه. ویژگیِ فعال کرم، بقیه خاکستری.
    ///   برگه‌ی پشت دیسک — سه ردیف: آیکون، نام، مقدار.
    ///
    /// هیچ عددی از خودش نگه نمی‌دارد: همه از CharacterStats و WorldState می‌آید.
    /// مثل بقیه‌ی HUD، هنگام دیالوگ/سند/منوی توقف کنار می‌رود.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatRingHud : MonoBehaviour
    {
        /* ---------- چیدمان (واحد کانواس ۱۹۲۰×۱۰۸۰) ---------- */
        private const float CenterRightOffset = 360f;
        private const float RootSize = 460f;

        private const float DiscSize = 330f;
        private const float OuterRadius = 138f;
        private const float OuterThickness = 9f;
        private const float InnerRadius = 118f;
        private const float InnerThickness = 7f;
        private const float PulseRadius = 158f;
        private const float PulseThickness = 2.5f;

        private const float SegmentRadius = 150f;
        private const float SegmentThickness = 7f;

        /// <summary>چقدر از دو سر هر کمان محو شود.</summary>
        private const float FadeDegrees = 26f;

        private const float LeftStart = 60f;
        private const float LeftEnd = 300f;
        private const float LeftSweep = LeftEnd - LeftStart;

        /// <summary>مرکز سه قطعه‌ی لبه‌ی راست، بالای افق و پایین آن.</summary>
        private static readonly float[] SegmentCenter = { 15f, 0f, -15f };

        private const float SheetX = 142f;
        private const float SheetWidth = 190f;
        private const float SheetHeight = 196f;
        private const float SheetRowHeight = 46f;
        private const float SheetRowTop = 26f;

        /* ---------- رنگ‌ها ---------- */
        private static readonly Color HealthColor = new Color(0.72f, 0.20f, 0.16f, 1f);
        private static readonly Color ArmorColor = new Color(0.26f, 0.52f, 0.72f, 1f);
        private static readonly Color StaminaColor = new Color(0.90f, 0.74f, 0.28f, 1f);
        private static readonly Color ExhaustedColor = new Color(0.86f, 0.15f, 0.13f, 1f);
        private static readonly Color PulseColor = new Color(0.95f, 0.12f, 0.10f, 1f);
        private static readonly Color TrackColor = new Color(0.02f, 0.02f, 0.02f, 0.45f);
        private static readonly Color DiscFill = new Color(0.043f, 0.035f, 0.030f, 0.97f);
        private static readonly Color DiscRim = new Color(0.26f, 0.25f, 0.23f, 0.85f);
        private static readonly Color SheetFill = new Color(0.075f, 0.061f, 0.052f, 0.94f);
        private static readonly Color SheetEdge = new Color(0.26f, 0.23f, 0.20f, 0.55f);
        private static readonly Color Cream = new Color(0.93f, 0.90f, 0.84f, 1f);
        private static readonly Color Grey = new Color(0.56f, 0.54f, 0.49f, 1f);
        private static readonly Color Dim = new Color(0.44f, 0.42f, 0.38f, 1f);

        [Header("رفتار")]
        [Tooltip("چند ثانیه بعد از آخرین تغییر، کمان کم‌رنگ می‌شود")]
        [SerializeField] private float activeSeconds = 3.5f;

        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.45f;

        [Tooltip("چند ثانیه یک ویژگیِ تازه‌عوض‌شده جای شرافت را بگیرد")]
        [SerializeField] private float focusSeconds = 25f;

        [Tooltip("سرعت کم‌رنگ و پررنگ شدن در حالت بی‌حرکتی")]
        [SerializeField] private float fadeSpeed = 6f;

        [Tooltip("وقتی کارت تعامل باز است، کمان کنار برود")]
        [SerializeField] private bool hideWhileCardShown = true;

        [Header("حرکت")]
        [SerializeField, Min(0.02f)] private float openSeconds = 0.22f;
        [SerializeField, Min(0.02f)] private float closeSeconds = 0.16f;

        [Tooltip("اندازه در حالت بسته")]
        [SerializeField, Range(0.5f, 1f)] private float hiddenScale = 0.9f;

        private GameObject _canvasObject;
        private CanvasGroup _group;
        private CanvasGroup _idle;
        private PanelMotion _motion;

        private ArcGraphic _health;
        private ArcGraphic _armor;
        private ArcGraphic _stamina;
        private ArcGraphic _pulse;

        private readonly ArcGraphic[] _segments = new ArcGraphic[3];
        private readonly Image[] _sheetIcons = new Image[3];
        private readonly TMP_Text[] _sheetLabels = new TMP_Text[3];
        private readonly TMP_Text[] _sheetValues = new TMP_Text[3];
        private readonly StatId[] _sheetIds = { StatId.Honor, StatId.Charisma, StatId.Fame };

        private TMP_FontAsset _font;

        private readonly float[] _seen = new float[9];
        private bool _seeded;

        private StatId _focus = StatId.Honor;
        private float _focusUntil;
        private float _activeUntil;
        private float _pulsePhase;
        private float _idleAlpha = 1f;
        private int _sheetSignature = int.MinValue;
        private bool _persian;

        private void Awake()
        {
            _persian = Localization.Persian;

            CharacterStats.EnsureDefaults();

            Build();

            _motion.SnapVisible(false);
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
            if (_canvasObject != null) Destroy(_canvasObject);
        }

        private void HandleLanguage()
        {
            if (Localization.Persian == _persian) return;

            _persian = Localization.Persian;
            _sheetSignature = int.MinValue;
        }

        private void Update()
        {
            if (_group == null) return;

            float now = Time.time;

            UpdateVitals();
            UpdateStamina();
            UpdateFocus(now);
            UpdatePulse();
            UpdateSheet();
            UpdatePresence();
        }

        /* ============================================================
           جان + زره
           ============================================================ */

        private void UpdateVitals()
        {
            float health = CharacterStats.GetFloat(StatId.Health);
            float armor = CharacterStats.GetFloat(StatId.Armor);

            float healthShare = health / CharacterStats.VitalMax;
            float armorShare = armor / CharacterStats.VitalMax;

            // از بالای کمان به پایین پر می‌شود
            float top = LeftEnd;
            float healthBottom = top - healthShare * LeftSweep;
            float armorBottom = healthBottom - armorShare * LeftSweep;

            _health.SetAngles(healthBottom, top);
            _armor.SetAngles(armorBottom, healthBottom);

            Watch(StatId.Health, health);
            Watch(StatId.Armor, armor);
        }

        /* ============================================================
           استقامت
           ============================================================ */

        private void UpdateStamina()
        {
            float stamina = CharacterStats.GetFloat(StatId.Stamina);

            if (stamina >= 0f)
            {
                float share = stamina / 100f;
                _stamina.color = StaminaColor;
                _stamina.SetAngles(LeftStart, LeftStart + share * LeftSweep);
            }
            else
            {
                // زیر صفر: همان کمان، قرمز، و از پایین بالا می‌آید تا ۱۵۰
                float share = Mathf.Clamp01(-stamina / 150f);
                _stamina.color = ExhaustedColor;
                _stamina.SetAngles(LeftStart, LeftStart + share * LeftSweep);
            }

            Watch(StatId.Stamina, stamina);
        }

        /* ============================================================
           برگه‌ی ویژگی‌ها
           ============================================================ */

        private static readonly StatId[] SheetTrio = { StatId.Honor, StatId.Charisma, StatId.Fame };

        private void UpdateFocus(float now)
        {
            for (int i = 0; i < CharacterStats.Traits.Length; i++)
            {
                StatId trait = CharacterStats.Traits[i];
                float value = CharacterStats.GetFloat(trait);

                if (Watch(trait, value) && _seeded)
                {
                    _focus = trait;
                    _focusUntil = now + focusSeconds;
                }
            }

            if (now >= _focusUntil) _focus = StatId.Honor;

            _seeded = true;

            UpdateSegments();
        }

        /// <summary>سه ردیف: ویژگیِ فعال بالا، دو تای دیگر زیرش.</summary>
        private void FillSheetIds()
        {
            _sheetIds[0] = _focus;

            int next = 1;

            for (int i = 0; i < SheetTrio.Length && next < 3; i++)
            {
                if (SheetTrio[i] == _focus) continue;

                _sheetIds[next++] = SheetTrio[i];
            }

            while (next < 3) _sheetIds[next++] = StatId.Honor;
        }

        /// <summary>سه قطعه‌ی لبه‌ی راست؛ بلندی هر کدام از مقدار ویژگی می‌آید.</summary>
        private void UpdateSegments()
        {
            FillSheetIds();

            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i] == null) continue;

                StatId id = _sheetIds[i];
                float share = Mathf.Clamp01(CharacterStats.GetFloat(id) / 100f);

                float half = 3f + 8f * share;

                _segments[i].color = i == 0 ? Cream : Grey;
                _segments[i].SetAngles(SegmentCenter[i] - half, SegmentCenter[i] + half);
            }
        }

        /// <summary>
        /// فقط با تغییر مقدار یا ویژگی، متن بازنویسی می‌شود؛
        /// نوشتن هر فریم در TMP یعنی بازسازی مش هر فریم.
        /// </summary>
        private void UpdateSheet()
        {
            FillSheetIds();

            int honor = CharacterStats.Get(StatId.Honor);
            int charisma = CharacterStats.Get(StatId.Charisma);
            int fame = CharacterStats.Get(StatId.Fame);

            int signature = (int)_focus * 1000000 + honor * 10000 + charisma * 100 + fame;

            if (signature == _sheetSignature) return;

            _sheetSignature = signature;

            for (int i = 0; i < _sheetIds.Length; i++)
            {
                StatId id = _sheetIds[i];
                bool lead = i == 0;

                if (_sheetLabels[i] != null)
                {
                    _sheetLabels[i].text = CharacterStats.Definition(id).Label(_persian);
                    _sheetLabels[i].color = lead ? Cream : Grey;
                    _sheetLabels[i].fontStyle = lead ? FontStyles.Bold : FontStyles.Normal;
                    _sheetLabels[i].alignment = _persian
                        ? TextAlignmentOptions.MidlineRight
                        : TextAlignmentOptions.MidlineLeft;
                }

                if (_sheetValues[i] != null)
                {
                    _sheetValues[i].text = HudArt.Digits(CharacterStats.Get(id), _persian);
                    _sheetValues[i].color = lead ? Cream : Dim;
                }

                if (_sheetIcons[i] != null)
                {
                    // آیکون سفید ساخته می‌شود و رنگش با Image.color داده می‌شود،
                    // وگرنه هر رنگ یک بافت تازه در کش می‌ساخت.
                    _sheetIcons[i].sprite = HudArt.Icon(IconFor(id), 48, Color.white);
                    _sheetIcons[i].color = lead ? Cream : new Color(Grey.r, Grey.g, Grey.b, 0.8f);
                }
            }
        }

        private static HudArt.Glyph IconFor(StatId id)
        {
            switch (id)
            {
                case StatId.Charisma: return HudArt.Glyph.Crown;
                case StatId.Fame: return HudArt.Glyph.People;
                case StatId.Honesty: return HudArt.Glyph.Document;
                case StatId.Loyalty: return HudArt.Glyph.People;
                default: return HudArt.Glyph.Star;
            }
        }

        /* ============================================================
           نبض فرسودگی
           ============================================================ */

        private void UpdatePulse()
        {
            float stamina = CharacterStats.GetFloat(StatId.Stamina);

            if (stamina >= 0f)
            {
                if (_pulse.color.a > 0f)
                    _pulse.color = new Color(PulseColor.r, PulseColor.g, PulseColor.b, 0f);

                return;
            }

            _pulsePhase += Time.unscaledDeltaTime * 3.2f;

            float strength = Mathf.Clamp01(-stamina / 150f);
            float wave = 0.5f + 0.5f * Mathf.Sin(_pulsePhase);

            _pulse.color = new Color(PulseColor.r, PulseColor.g, PulseColor.b,
                (0.15f + 0.75f * wave) * Mathf.Lerp(0.35f, 1f, strength));

            _pulse.SetGeometry(PulseRadius + wave * 3f, PulseThickness + wave * 0.8f);
        }

        /* ============================================================
           تغییرها و حضور
           ============================================================ */

        /// <summary>اگر مقدار عوض شده باشد true و زمان «فعال ماندن» تمدید می‌شود.</summary>
        private bool Watch(StatId id, float value)
        {
            int index = (int)id;

            if (Mathf.Abs(_seen[index] - value) < 0.05f) return false;

            _seen[index] = value;

            if (_seeded) _activeUntil = Time.time + activeSeconds;

            return true;
        }

        /// <summary>
        /// دو چیز جدا: «بودن» گیج (انیمیشن ورود و خروج) و «پررنگی» آن (بی‌حرکتی).
        /// اگر هر دو روی یک CanvasGroup بودند، کم‌رنگ شدن، حرکت را خنثی می‌کرد.
        /// </summary>
        private void UpdatePresence()
        {
            bool blocked = DialogueUI.Current != null || DocumentView.IsOpen || PauseMenuUI.IsOpen;

            bool visible = !blocked && !(hideWhileCardShown && InteractionHUD.IsShown);

            _motion.SetVisible(visible);

            float target = Time.time < _activeUntil ? 1f : idleAlpha;

            _idleAlpha = Mathf.MoveTowards(_idleAlpha, target, fadeSpeed * Time.unscaledDeltaTime);

            if (_idle != null) _idle.alpha = _idleAlpha;
        }

        /* ============================================================
           ساخت
           ============================================================ */

        private void Build()
        {
            _canvasObject = new GameObject("Stat Ring (runtime)",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvasObject.transform.SetParent(null, false);

            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 35;

            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = UiKit.Node("Ring", _canvasObject.transform);
            root.anchorMin = new Vector2(1f, 0.5f);
            root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(RootSize, RootSize);
            root.anchoredPosition = new Vector2(-CenterRightOffset, 0f);

            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _motion = root.gameObject.AddComponent<PanelMotion>();
            _motion.Configure(root, _group, hiddenScale, 26f, PanelMotion.Edge.Right,
                openSeconds, closeSeconds);
            _motion.SetRestPosition(new Vector2(-CenterRightOffset, 0f));

            // بدنه: هر چیزی که با بی‌حرکتی کم‌رنگ می‌شود. مقیاس و ورود/خروج روی ریشه است.
            RectTransform body = UiKit.Node("Body", root);
            body.anchorMin = new Vector2(0.5f, 0.5f);
            body.anchorMax = new Vector2(0.5f, 0.5f);
            body.pivot = new Vector2(0.5f, 0.5f);
            body.anchoredPosition = Vector2.zero;
            body.sizeDelta = new Vector2(RootSize, RootSize);

            _idle = body.gameObject.AddComponent<CanvasGroup>();
            _idle.blocksRaycasts = false;
            _idle.interactable = false;
            _idle.alpha = 1f;

            _font = FindFont();

            // برگه اول ساخته می‌شود تا پشت دیسک بماند
            BuildSheet(body);
            BuildDisc(body);
            BuildArcs(body);
            BuildSegments(body);
        }

        private void BuildDisc(RectTransform parent)
        {
            RectTransform disc = UiKit.Node("Disc", parent);
            disc.anchorMin = new Vector2(0.5f, 0.5f);
            disc.anchorMax = new Vector2(0.5f, 0.5f);
            disc.pivot = new Vector2(0.5f, 0.5f);
            disc.anchoredPosition = Vector2.zero;
            disc.sizeDelta = new Vector2(DiscSize, DiscSize);

            Image image = disc.gameObject.AddComponent<Image>();
            image.sprite = HudArt.Disc("hud.disc", Mathf.CeilToInt(DiscSize), DiscFill, DiscRim, 1.3f);
            image.raycastTarget = false;
        }

        private void BuildArcs(RectTransform parent)
        {
            // ریل‌های تیره: بدون این‌ها کمان روی دیسک گم می‌شود
            ArcGraphic healthTrack = MakeArc(parent, "Health Track", OuterRadius, OuterThickness, TrackColor);
            healthTrack.SetAngles(LeftStart, LeftEnd);

            ArcGraphic staminaTrack = MakeArc(parent, "Stamina Track", InnerRadius, InnerThickness, TrackColor);
            staminaTrack.SetAngles(LeftStart, LeftEnd);

            _pulse = MakeArc(parent, "Pulse", PulseRadius, PulseThickness,
                new Color(PulseColor.r, PulseColor.g, PulseColor.b, 0f));

            _pulse.SetAngles(0f, 360f);

            _health = MakeArc(parent, "Health", OuterRadius, OuterThickness, HealthColor);
            _armor = MakeArc(parent, "Armor", OuterRadius, OuterThickness, ArmorColor);
            _stamina = MakeArc(parent, "Stamina", InnerRadius, InnerThickness, StaminaColor);

            _health.SetFade(FadeDegrees);
            _armor.SetFade(FadeDegrees);
            _stamina.SetFade(FadeDegrees);
        }

        private void BuildSegments(RectTransform parent)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i] = MakeArc(parent, "Trait_" + i, SegmentRadius, SegmentThickness, Grey);
                _segments[i].SetFade(0f);
                _segments[i].SetAngles(SegmentCenter[i] - 3f, SegmentCenter[i] + 3f);
            }
        }

        private void BuildSheet(RectTransform parent)
        {
            RectTransform sheet = UiKit.Node("Sheet", parent);
            sheet.anchorMin = new Vector2(0.5f, 0.5f);
            sheet.anchorMax = new Vector2(0.5f, 0.5f);
            sheet.pivot = new Vector2(0f, 0.5f);
            sheet.anchoredPosition = new Vector2(SheetX, 0f);
            sheet.sizeDelta = new Vector2(SheetWidth, SheetHeight);

            Image background = sheet.gameObject.AddComponent<Image>();
            background.sprite = HudArt.Panel("hud.sheet", Mathf.CeilToInt(SheetWidth), Mathf.CeilToInt(SheetHeight),
                14f, SheetFill, SheetFill, SheetEdge, 1f);
            background.raycastTarget = false;

            for (int i = 0; i < 3; i++)
            {
                float y = SheetRowTop + i * SheetRowHeight;

                // آیکون، سمت راست ردیف
                RectTransform iconBox = UiKit.Box("Icon_" + i, sheet, SheetWidth - 42f, y + 12f, 22f, 22f);

                _sheetIcons[i] = iconBox.gameObject.AddComponent<Image>();
                _sheetIcons[i].sprite = HudArt.Icon(HudArt.Glyph.Star, 48, Color.white);
                _sheetIcons[i].color = Grey;
                _sheetIcons[i].raycastTarget = false;
                _sheetIcons[i].preserveAspect = true;

                // نام، راست‌چین تا کنار آیکون
                RectTransform labelBox = UiKit.Box("Label_" + i, sheet, 66f, y, 76f, SheetRowHeight);

                _sheetLabels[i] = UiKit.Text("Text", labelBox, 19f, Grey, _font, _persian);
                _sheetLabels[i].alignment = TextAlignmentOptions.MidlineRight;

                // مقدار، کم‌رنگ، سمت چپ نام
                RectTransform valueBox = UiKit.Box("Value_" + i, sheet, 24f, y, 38f, SheetRowHeight);

                _sheetValues[i] = UiKit.Text("Text", valueBox, 18f, Dim, _font, _persian);
                _sheetValues[i].alignment = TextAlignmentOptions.Center;
            }
        }

        private static ArcGraphic MakeArc(RectTransform parent, string name, float radius,
            float thickness, Color color)
        {
            RectTransform rect = UiKit.Node(name, parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(2f, 2f);

            ArcGraphic arc = rect.gameObject.AddComponent<ArcGraphic>();
            arc.color = color;
            arc.raycastTarget = false;
            arc.SetGeometry(radius, thickness);

            return arc;
        }

        /// <summary>همان فونت دیالوگ؛ اگر نبود، فونت پیش‌فرض TMP.</summary>
        private static TMP_FontAsset FindFont()
        {
            DialogueUI ui = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);

            if (ui != null && ui.Font != null) return ui.Font;

            return TMP_Settings.defaultFontAsset;
        }
    }
}
