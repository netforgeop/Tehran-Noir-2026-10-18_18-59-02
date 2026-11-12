using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TehranNoir.Characters;
using TehranNoir.Settings;
using TehranNoir.World;

namespace TehranNoir.UI
{
    /// <summary>
    /// تب «کاراکتر» منوی توقف.
    ///
    ///   خلاصه — یک نشان بزرگ (شرافت) و پنج ساعت گرد، یکی برای هر ویژگی.
    ///           کم = سرخ، زیاد = سفید؛ همان قاعده‌ی نشان شرافت در Red Dead Redemption 2.
    ///           درصدِ هر ساعت، خودِ مقدار ۰..۱۰۰ است.
    ///   وسایل / کارها / پرونده‌ها — تا منبع داده‌شان ساخته نشود، پیام صادقانه.
    ///
    /// عدد در این کلاس ساخته نمی‌شود: همه از CharacterStats می‌آید و به‌صورت ASCII
    /// به TMP داده می‌شود. تبدیل به رقم فارسی و جهت‌دهی کار RTLTMPro است؛ اگر خودمان
    /// رقم فارسی بسازیم، RTLTMPro آن را «حرف» می‌بیند و برعکس می‌چیند (۱۳۵۲ → ۲۵۳۱).
    /// </summary>
    public sealed class CharacterPauseTab : PauseTab
    {
        /* ---------- ریل بخش‌ها ---------- */
        private const float RailX = 130f;
        private const float RailWidth = 300f;
        private const float RailTop = 150f;
        private const float RailHeight = 46f;
        private const float RailGap = 12f;
        private const float RailFont = 26f;

        /* ---------- ستون محتوا ---------- */
        private const float PanelX = 500f;
        private const float PanelWidth = 1290f;

        private const float CrestSize = 96f;
        private const float NameY = 96f;
        private const float MetaY = 156f;
        private const float RuleY = 204f;

        /* ---------- ردیف ویژگی ---------- */
        private const float RowTop = 236f;
        private const float RowHeight = 66f;
        private const float RowGap = 8f;
        private const float RowFont = 26f;

        private const float PipSize = 46f;
        private const float PipRadius = 17f;
        private const float PipThickness = 3.5f;
        private const float IconSize = 20f;

        private const float ValueWidth = 140f;
        private const float PipX = 176f;
        private const float NameX = 240f;

        /// <summary>زیر این مقدار، سرخِ کامل؛ بالای آن، کم‌کم به استخوانی می‌رسد.</summary>
        private const float LowMark = 25f;

        private static readonly Color HighColor = new Color(0.96f, 0.95f, 0.92f, 1f);
        private static readonly Color LowColor = new Color(0.72f, 0.16f, 0.14f, 1f);
        private static readonly Color MidColor = new Color(0.80f, 0.77f, 0.70f, 1f);
        private static readonly Color TrackColor = new Color(0.25f, 0.24f, 0.22f, 0.75f);

        private static readonly string[] PersianMonths =
        {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        };

        private static readonly string[] EnglishMonths =
        {
            "Farvardin", "Ordibehesht", "Khordad", "Tir", "Mordad", "Shahrivar",
            "Mehr", "Aban", "Azar", "Dey", "Bahman", "Esfand"
        };

        private static readonly string[] SectionKeys =
        {
            "char.section.summary",
            "char.section.items",
            "char.section.jobs",
            "char.section.cases"
        };

        private static readonly StatId[] TraitOrder =
        {
            StatId.Honor, StatId.Charisma, StatId.Fame, StatId.Honesty, StatId.Loyalty
        };

        private readonly List<RectTransform> _railRects = new List<RectTransform>(4);
        private readonly List<TMP_Text> _railLabels = new List<TMP_Text>(4);

        private readonly List<ArcGraphic> _pips = new List<ArcGraphic>(5);
        private readonly List<ArcGraphic> _pipTracks = new List<ArcGraphic>(5);
        private readonly List<Image> _pipIcons = new List<Image>(5);
        private readonly List<TMP_Text> _valueTexts = new List<TMP_Text>(5);

        private RectTransform _content;
        private Image _crest;
        private Image _crestIcon;
        private TMP_Text _nameText;
        private TMP_Text _metaText;
        private string _metaShown;
        private TMP_Text _note;

        private int _section;

        public override string Id => "character";

        public override string Label => Loc.Get(Loc.Channel.Menu, "pause.tab.character");

        /// <summary>چپ/راست داخل این تب بخش را عوض می‌کند، نه تب را — مثل تنظیمات.</summary>
        public override bool UsesSections => true;

        /* ============================================================
           ساخت
           ============================================================ */

        protected override void OnBuild(RectTransform panel)
        {
            BuildRail(panel);
            BuildHeader(panel);
            BuildContent(panel);

            BuildSection();
            RefreshRail();
        }

        private void BuildRail(RectTransform panel)
        {
            _railRects.Clear();
            _railLabels.Clear();

            for (int i = 0; i < SectionKeys.Length; i++)
            {
                float y = RailTop + i * (RailHeight + RailGap);

                RectTransform rect = UiKit.Box("Section_" + i, panel, RailX, y, RailWidth, RailHeight);

                TMP_Text text = UiKit.Text("Label", rect, RailFont, UiKit.RowIdle, Menu.Font, Menu.Persian);
                text.alignment = TextAlignmentOptions.MidlineRight;
                text.text = Loc.Get(Loc.Channel.Menu, SectionKeys[i]);

                _railRects.Add(rect);
                _railLabels.Add(text);
            }
        }

        private void BuildHeader(RectTransform panel)
        {
            // نشان: المان امضای بازی، همان کمان شرافت با آیکون خودش
            RectTransform crestBox = UiKit.Box("Crest", panel, PanelX, NameY - 6f, CrestSize, CrestSize);

            _crest = crestBox.gameObject.AddComponent<Image>();
            _crest.sprite = HudArt.Disc("char.crest", Mathf.CeilToInt(CrestSize),
                new Color(0.06f, 0.05f, 0.045f, 0.95f), new Color(0.30f, 0.28f, 0.25f, 0.9f), 1.5f);
            _crest.raycastTarget = false;

            RectTransform crestIconBox = UiKit.Box("Icon", crestBox, (CrestSize - 46f) * 0.5f,
                (CrestSize - 46f) * 0.5f, 46f, 46f);

            _crestIcon = crestIconBox.gameObject.AddComponent<Image>();
            _crestIcon.sprite = HudArt.Icon(HudArt.Glyph.Star, 96, Color.white);
            _crestIcon.raycastTarget = false;
            _crestIcon.preserveAspect = true;

            float nameX = PanelX + CrestSize + 24f;

            RectTransform nameBox = UiKit.Box("Name", panel, nameX, NameY, PanelWidth - CrestSize - 24f, 44f);

            _nameText = UiKit.Text("Text", nameBox, 34f, UiKit.Gold, Menu.Font, Menu.Persian);
            _nameText.alignment = TextAlignmentOptions.MidlineRight;
            _nameText.fontStyle = FontStyles.Bold;

            RectTransform metaBox = UiKit.Box("Meta", panel, nameX, MetaY, PanelWidth - CrestSize - 24f, 30f);

            _metaText = UiKit.Text("Text", metaBox, 19f, UiKit.Dim, Menu.Font, Menu.Persian);
            _metaText.alignment = TextAlignmentOptions.MidlineRight;

            RectTransform rule = UiKit.Box("Rule", panel, PanelX, RuleY, PanelWidth, 2f);

            Image line = rule.gameObject.AddComponent<Image>();
            line.color = new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.55f);
            line.raycastTarget = false;

            RectTransform noteBox = UiKit.Box("Note", panel, PanelX, RowTop, PanelWidth, 90f);

            _note = UiKit.Text("Text", noteBox, 22f, UiKit.Dim, Menu.Font, Menu.Persian);
            _note.alignment = TextAlignmentOptions.MidlineRight;
            _note.gameObject.SetActive(false);
        }

        private void BuildContent(RectTransform panel)
        {
            _content = UiKit.Box("Content", panel, PanelX, RowTop, PanelWidth, 460f);
        }

        /* ============================================================
           محتوا
           ============================================================ */

        private void BuildSection()
        {
            Rows.Clear();
            _pips.Clear();
            _pipTracks.Clear();
            _pipIcons.Clear();
            _valueTexts.Clear();

            // ردیف‌های بخش پیشین: اول خاموش، بعد نابود
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }

            bool empty = _section != 0;

            _note.gameObject.SetActive(empty);
            _note.text = Loc.Get(Loc.Channel.Menu, "pause.soon");

            if (empty) return;

            for (int i = 0; i < TraitOrder.Length; i++)
                AddTraitRow(TraitOrder[i], i);
        }

        /// <summary>
        /// یک ردیف ویژگی: ساعت گرد با آیکون، نام، و عدد.
        /// ساعت = یک ریل کامل + یک کمان به اندازه‌ی مقدار؛ رنگ از سرخ (کم) به سفید (زیاد).
        /// </summary>
        private void AddTraitRow(StatId id, int index)
        {
            float y = index * (RowHeight + RowGap);

            RectTransform row = UiKit.Box("Row_" + index, _content, 0f, y, PanelWidth, RowHeight);

            bool rtl = Menu.Persian;

            float valueX = rtl ? 0f : PanelWidth - ValueWidth;
            float pipX = rtl ? PipX : PanelWidth - PipX - PipSize;
            float nameX = rtl ? NameX : 0f;
            float nameWidth = PanelWidth - NameX;

            RectTransform valueBox = UiKit.Box("Value", row, valueX, 0f, ValueWidth, RowHeight);

            TMP_Text valueText = UiKit.Text("Text", valueBox, RowFont, UiKit.Dim, Menu.Font, Menu.Persian);
            valueText.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

            // عدد خالص است و هیچ حرفی ندارد؛ RTLTMPro بدون ForceFix آن را تبدیل نمی‌کند
            UiKit.ForceFixNumbers(valueText, Menu.Persian);

            RectTransform pip = UiKit.Node("Pip", row);
            pip.anchorMin = new Vector2(0f, 1f);
            pip.anchorMax = new Vector2(0f, 1f);
            pip.pivot = new Vector2(0f, 1f);
            pip.anchoredPosition = new Vector2(pipX, -(RowHeight - PipSize) * 0.5f);
            pip.sizeDelta = new Vector2(PipSize, PipSize);

            ArcGraphic track = MakeArc(pip, "Track", PipRadius, PipThickness, TrackColor);
            track.SetAngles(0f, 360f);

            ArcGraphic fill = MakeArc(pip, "Fill", PipRadius, PipThickness, HighColor);

            RectTransform iconBox = UiKit.Node("Icon", pip);
            iconBox.anchorMin = new Vector2(0.5f, 0.5f);
            iconBox.anchorMax = new Vector2(0.5f, 0.5f);
            iconBox.pivot = new Vector2(0.5f, 0.5f);
            iconBox.anchoredPosition = Vector2.zero;
            iconBox.sizeDelta = new Vector2(IconSize, IconSize);

            Image icon = iconBox.gameObject.AddComponent<Image>();
            icon.sprite = HudArt.Icon(IconFor(id), 48, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            RectTransform labelBox = UiKit.Box("Label", row, nameX, 0f, nameWidth, RowHeight);

            TMP_Text labelText = UiKit.Text("Text", labelBox, RowFont, UiKit.RowIdle, Menu.Font, Menu.Persian);
            labelText.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            labelText.text = CharacterStats.Definition(id).Label(Menu.Persian);

            _pipTracks.Add(track);
            _pips.Add(fill);
            _pipIcons.Add(icon);

            PauseRow data = new PauseRow();
            data.Rect = row;
            data.Label = labelText;

            Rows.Add(data);
            _valueTexts.Add(valueText);
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

        private static HudArt.Glyph IconFor(StatId id)
        {
            switch (id)
            {
                case StatId.Charisma: return HudArt.Glyph.Crown;
                case StatId.Fame: return HudArt.Glyph.People;
                case StatId.Honesty: return HudArt.Glyph.Document;
                case StatId.Loyalty: return HudArt.Glyph.Shield;
                default: return HudArt.Glyph.Star;
            }
        }

        /// <summary>
        /// رنگ از سرخ به سفید. زیر ۲۵ سرخِ کامل است تا «وضعیت بد» با یک نگاه فهمیده شود؛
        /// بعد به استخوانی و در آخر سفید می‌رود. ۵۰ نقطه‌ی شروع بازی است، پس بی‌طرف.
        /// </summary>
        private static Color TraitColor(float value)
        {
            // ۵۰ = نقطه‌ی شروع بازی؛ باید استخوانی و بی‌طرف دیده شود، نه سرخ.
            if (value <= LowMark) return LowColor;

            if (value < 50f) return Color.Lerp(LowColor, MidColor, Mathf.InverseLerp(LowMark, 50f, value));

            return Color.Lerp(MidColor, HighColor, Mathf.InverseLerp(50f, 100f, value));
        }

        /// <summary>نقش و تاریخ؛ فقط وقتی متن عوض شده باشد دوباره نوشته می‌شود.</summary>
        private void RefreshMeta()
        {
            string text = Loc.Get(Loc.Channel.Menu, "char.role") + "   ·   " + FormatDate();

            if (text == _metaShown) return;

            _metaShown = text;
            _metaText.text = text;
        }

        /// <summary>هر فریم صدا زده می‌شود تا منو با مقدارهای تازه هم‌خوان بماند.</summary>
        public override void TickValues()
        {
            RefreshMeta();

            for (int i = 0; i < TraitOrder.Length && i < _pips.Count; i++)
            {
                float value = CharacterStats.GetFloat(TraitOrder[i]);
                Color color = TraitColor(value);

                float sweep = Mathf.Clamp(value, 0f, 100f) * 3.6f;

                // از بالای ساعت، ساعت‌گرد پر می‌شود: ۹۰ درجه شروع، پایان کمتر
                _pips[i].SetAngles(90f, 90f - sweep);
                _pips[i].color = color;

                if (_pipIcons[i] != null) _pipIcons[i].color = color;

                if (_valueTexts[i] != null)
                {
                    // عدد ASCII؛ تبدیل به رقم فارسی کار RTLTMPro است (ForceFix)
                    _valueTexts[i].text = CharacterStats.Get(TraitOrder[i]).ToString();
                    _valueTexts[i].color = color;
                }
            }

            if (_crestIcon != null)
            {
                // نشان، همان شرافت است: سرخ در پایین، سفید در بالا
                _crestIcon.color = TraitColor(CharacterStats.GetFloat(StatId.Honor));
            }
        }

        private void RefreshRail()
        {
            for (int i = 0; i < _railLabels.Count; i++)
                _railLabels[i].color = i == _section ? UiKit.Gold : UiKit.RowIdle;
        }

        /* ============================================================
           چرخه و ناوبری
           ============================================================ */

        public override void OnShow()
        {
            base.OnShow();

            _nameText.text = Loc.Get(Loc.Channel.Menu, "char.name");

            BuildSection();
            RefreshRail();
            Select(0);

            RefreshMeta();
            TickValues();
        }

        public override void Section(int delta)
        {
            if (delta == 0) return;

            _section = (_section + delta + SectionKeys.Length) % SectionKeys.Length;

            OnShow();
        }

        public override void NextSection()
        {
            Section(1);
        }

        /// <summary>Esc اول به خلاصه برمی‌گردد؛ بار دوم منو را می‌بندد.</summary>
        public override bool Back()
        {
            if (_section == 0) return false;

            _section = 0;
            OnShow();

            return true;
        }

        /// <summary>کلیک روی ریل بخش، همان کاری را می‌کند که Q/E.</summary>
        public override void Pointer(Vector2 screenPoint, bool clicked)
        {
            for (int i = 0; i < _railRects.Count; i++)
            {
                if (_railRects[i] == null) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(_railRects[i], screenPoint, null)) continue;

                if (clicked && _section != i)
                {
                    _section = i;
                    OnShow();
                }

                return;
            }

            base.Pointer(screenPoint, clicked);
        }

        /* ============================================================
           تاریخ
           ============================================================ */

        /// <summary>
        /// عددها ASCII می‌مانند؛ RTLTMPro خودش هم رقم فارسی می‌کند هم ترتیب را نگه می‌دارد.
        /// </summary>
        private string FormatDate()
        {
            WorldClock clock = WorldClock.Instance;

            if (clock == null) return Loc.Get(Loc.Channel.Menu, "char.date.unknown");

            GameDate now = clock.Now;

            int month = Mathf.Clamp(now.Month - 1, 0, 11);

            string monthName = Menu.Persian ? PersianMonths[month] : EnglishMonths[month];

            string time = now.Hour.ToString("00") + ":" + now.Minute.ToString("00");

            return now.Day + " " + monthName + " " + now.Year + "   —   " + time;
        }
    }
}
