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
    /// کمانِ سمت راست صفحه، به سبک Mafia: The Old Country.
    ///
    ///   کمان بیرونی چپ — جان (سرخ) + زره (آبی). هر دو روی یک کمان‌اند و مجموعشان ۲۰۰ است،
    ///                    پس اگر جان ۱۰۰ و زره ۵۰ باشد، ۷۵٪ کمان پر است و از آن، یک‌سوم آبی.
    ///   کمان درونی چپ — استقامت (زرد). زیر صفر، همان کمان قرمز می‌شود و از پایین بالا می‌آید.
    ///   کمان راست      — یکی از پنج ویژگی. معمولاً شرافت؛ هر کدام عوض شود ۲۵ ثانیه جایش را می‌گیرد.
    ///
    /// هیچ عددی از خودش نگه نمی‌دارد: همه از CharacterStats و WorldState می‌آید.
    /// مثل بقیه‌ی HUD، هنگام دیالوگ/سند/منوی توقف محو می‌شود.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatRingHud : MonoBehaviour
    {
        /* ---------- چیدمان (واحد کانواس ۱۹۲۰×۱۰۸۰) ---------- */
        private const float CenterRightOffset = 360f;
        private const float RootSize = 420f;

        private const float OuterRadius = 140f;
        private const float OuterThickness = 9f;
        private const float InnerRadius = 118f;
        private const float InnerThickness = 7f;
        private const float PulseRadius = 162f;
        private const float PulseThickness = 2.5f;

        private const float LeftStart = 60f;
        private const float LeftEnd = 300f;
        private const float LeftSweep = LeftEnd - LeftStart;

        private const float RightStart = -40f;
        private const float RightEnd = 40f;
        private const float RightSweep = RightEnd - RightStart;

        /// <summary>لبه‌ی چپ جعبه‌ی برچسب در حالت انگلیسی: کمی بعد از کمان.</summary>
        private const float LabelOffset = 152f;

        /// <summary>لبه‌ی راست جعبه در حالت فارسی؛ متن راست‌چین از همین‌جا شروع می‌شود.</summary>
        private const float LabelRightEdge = 340f;

        private const float LabelWidth = 160f;

        /* ---------- رنگ‌ها ---------- */
        private static readonly Color HealthColor = new Color(0.72f, 0.20f, 0.16f, 1f);
        private static readonly Color ArmorColor = new Color(0.26f, 0.52f, 0.72f, 1f);
        private static readonly Color StaminaColor = new Color(0.90f, 0.74f, 0.28f, 1f);
        private static readonly Color ExhaustedColor = new Color(0.86f, 0.15f, 0.13f, 1f);
        private static readonly Color PulseColor = new Color(0.95f, 0.12f, 0.10f, 1f);
        private static readonly Color TrackColor = new Color(0.02f, 0.02f, 0.02f, 0.35f);
        private static readonly Color NameColor = new Color(0.86f, 0.86f, 0.83f, 1f);
        private static readonly Color ValueColor = new Color(0.96f, 0.95f, 0.92f, 1f);

        [Header("رفتار")]
        [Tooltip("چند ثانیه بعد از آخرین تغییر، کمان کم‌رنگ می‌شود")]
        [SerializeField] private float activeSeconds = 3.5f;

        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.45f;

        [Tooltip("چند ثانیه یک ویژگیِ تازه‌عوض‌شده جای شرافت را بگیرد")]
        [SerializeField] private float focusSeconds = 25f;

        [Tooltip("سرعت کم‌رنگ و پررنگ شدن در حالت بی‌حرکتی")]
        [SerializeField] private float fadeSpeed = 6f;

        [Tooltip("وقتی کارت تعامل باز است، کمان کنار برود (فریم‌های مرجع همین را نشان می‌دهند)")]
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
        private RectTransform _labelBox;
        private TMP_Text _nameText;
        private TMP_Text _valueText;

        private ArcGraphic _health;
        private ArcGraphic _armor;
        private ArcGraphic _stamina;
        private ArcGraphic _right;
        private ArcGraphic _pulse;

        private readonly float[] _seen = new float[9];
        private bool _seeded;

        private StatId _focus = StatId.Honor;
        private float _focusUntil;
        private float _activeUntil;
        private float _pulsePhase;
        private float _idleAlpha = 1f;
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
            LayoutLabel();
            UpdateLabel(true);
        }

        private void Update()
        {
            if (_group == null) return;

            float now = Time.time;

            UpdateVitals();
            UpdateStamina();
            UpdateFocus(now);
            UpdatePulse();
            UpdateLabel();
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

            // از بالای کمان به پایین پر می‌شود؛ پس پر شدن از بالا شروع می‌شود
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
           ویژگی سمت راست
           ============================================================ */

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

            float current = CharacterStats.GetFloat(_focus);
            float share = Mathf.Clamp01(current / 100f);

            _right.color = TraitColor(_focus);
            _right.SetAngles(RightStart, RightStart + share * RightSweep);

            _seeded = true;
        }

        private static Color TraitColor(StatId id)
        {
            switch (id)
            {
                case StatId.Honor: return new Color(0.85f, 0.78f, 0.52f, 1f);
                case StatId.Charisma: return new Color(0.74f, 0.56f, 0.74f, 1f);
                case StatId.Fame: return new Color(0.52f, 0.70f, 0.76f, 1f);
                case StatId.Honesty: return new Color(0.72f, 0.74f, 0.60f, 1f);
                case StatId.Loyalty: return new Color(0.80f, 0.55f, 0.44f, 1f);
                default: return NameColor;
            }
        }

        private StatId _shownStat = StatId.Money;
        private int _shownValue = int.MinValue;

        /// <summary>
        /// فقط وقتی مقدار یا ویژگی عوض شود متن بازنویسی می‌شود؛
        /// نوشتن هر فریم در TMP یعنی بازسازی مش هر فریم.
        /// </summary>
        private void UpdateLabel(bool force = false)
        {
            if (_nameText == null || _valueText == null) return;

            int value = CharacterStats.Get(_focus);

            if (!force && _focus == _shownStat && value == _shownValue) return;

            _shownStat = _focus;
            _shownValue = value;

            StatDefinition definition = CharacterStats.Definition(_focus);

            _nameText.text = definition.Label(_persian);
            _nameText.color = NameColor;
            _valueText.text = value.ToString();
            _valueText.color = TraitColor(_focus);
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
           تغییرها و محو شدن
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
        /// دو چیز جدا: «بودن» کمان (انیمیشن ورود و خروج) و «پررنگی» آن (بی‌حرکتی).
        /// اگر هر دو روی یک CanvasGroup بودند، محو شدن تدریجی، حرکت را خنثی می‌کرد.
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

            // ریل‌های تیره: بدون این‌ها کمان روی زمین روشن گم می‌شود
            MakeTrack(body, "Health Track", OuterRadius, OuterThickness, LeftStart, LeftEnd);
            MakeTrack(body, "Stamina Track", InnerRadius, InnerThickness, LeftStart, LeftEnd);
            MakeTrack(body, "Trait Track", OuterRadius, OuterThickness - 1f, RightStart, RightEnd);

            _health = MakeArc(body, "Health", OuterRadius, OuterThickness, HealthColor);
            _armor = MakeArc(body, "Armor", OuterRadius, OuterThickness, ArmorColor);
            _stamina = MakeArc(body, "Stamina", InnerRadius, InnerThickness, StaminaColor);
            _right = MakeArc(body, "Trait", OuterRadius, OuterThickness - 1f, NameColor);

            _pulse = MakeArc(body, "Pulse", PulseRadius, PulseThickness, new Color(PulseColor.r, PulseColor.g, PulseColor.b, 0f));
            _pulse.SetAngles(0f, 360f);

            BuildLabel(body);
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

        private static void MakeTrack(RectTransform parent, string name, float radius,
            float thickness, float start, float end)
        {
            ArcGraphic track = MakeArc(parent, name, radius, thickness, TrackColor);
            track.SetAngles(start, end);
        }

        private void BuildLabel(RectTransform root)
        {
            TMP_FontAsset font = null;

            DialogueUI ui = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
            if (ui != null) font = ui.Font;
            if (font == null) font = TMP_Settings.defaultFontAsset;

            RectTransform box = UiKit.Node("Trait Label", root);
            _labelBox = box;
            box.anchorMin = new Vector2(0.5f, 0.5f);
            box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.sizeDelta = new Vector2(LabelWidth, 60f);
            box.anchoredPosition = new Vector2(LabelOffset, 0f);

            RectTransform nameBox = UiKit.Node("Name", box);
            nameBox.anchorMin = new Vector2(0f, 0.5f);
            nameBox.anchorMax = new Vector2(0f, 0.5f);
            nameBox.pivot = new Vector2(0f, 0f);
            nameBox.sizeDelta = new Vector2(LabelWidth, 30f);
            nameBox.anchoredPosition = new Vector2(0f, 2f);

            _nameText = UiKit.MakeText(nameBox.gameObject, 21f, NameColor, font, _persian);
            _nameText.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform valueBox = UiKit.Node("Value", box);
            valueBox.anchorMin = new Vector2(0f, 0.5f);
            valueBox.anchorMax = new Vector2(0f, 0.5f);
            valueBox.pivot = new Vector2(0f, 1f);
            valueBox.sizeDelta = new Vector2(LabelWidth, 26f);
            valueBox.anchoredPosition = new Vector2(0f, -2f);

            _valueText = UiKit.MakeText(valueBox.gameObject, 17f, ValueColor, font, _persian);
            _valueText.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutLabel();
        }

        /// <summary>متن فارسی راست‌چین و سمت چپ کمان می‌نشیند؛ انگلیسی چپ‌چین و سمت راست.</summary>
        private void LayoutLabel()
        {
            RectTransform box = _labelBox;

            if (box == null) return;

            bool right = _persian;

            box.pivot = new Vector2(right ? 1f : 0f, 0.5f);
            box.sizeDelta = new Vector2(LabelWidth, 60f);
            box.anchoredPosition = new Vector2(right ? LabelRightEdge : LabelOffset, 0f);

            TextAlignmentOptions align = right ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

            if (_nameText != null) _nameText.alignment = align;
            if (_valueText != null) _valueText.alignment = align;
        }
    }
}
