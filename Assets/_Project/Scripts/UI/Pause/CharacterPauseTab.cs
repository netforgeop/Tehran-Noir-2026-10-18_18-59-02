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
    /// تب «کاراکتر» منوی توقف — ساختار Cyberpunk: ریل بخش‌ها در یک طرف، محتوا در طرف دیگر.
    ///
    ///   خلاصه — نام، تاریخ و ساعت بازی، و نُه آماری که واقعاً وجود دارند.
    ///   وسایل / کارها / پرونده‌ها — تا وقتی منبع داده‌شان ساخته نشود، پیام صادقانه.
    ///
    /// هیچ عددی در این کلاس نگه داشته نمی‌شود: همه از CharacterStats و WorldClock خوانده
    /// می‌شود. برای همین با تغییر پول در پنل تقلب، این صفحه هم عوض می‌شود.
    ///
    /// ریل بخش‌ها عمداً از چیدمان ردیف‌های منو (PauseMenuUI.RowX) استفاده نمی‌کند؛
    /// آن ثابت‌ها برای ستون چپ فهرست تنظیمات‌اند، نه برای یک صفحه‌ی دوستونی.
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

        private const float NameY = 108f;
        private const float MetaY = 156f;
        private const float RuleY = 200f;

        private const float RowTop = 236f;
        private const float RowHeight = 50f;
        private const float RowGap = 4f;
        private const float RowFont = 26f;
        private const float ValueWidth = 360f;

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

        private static readonly StatId[] SummaryStats =
        {
            StatId.Money, StatId.Health, StatId.Armor, StatId.Stamina,
            StatId.Honor, StatId.Charisma, StatId.Fame, StatId.Honesty, StatId.Loyalty
        };

        private readonly List<RectTransform> _railRects = new List<RectTransform>(4);
        private readonly List<TMP_Text> _railLabels = new List<TMP_Text>(4);

        private RectTransform _content;
        private TMP_Text _nameText;
        private TMP_Text _metaText;
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
            RectTransform nameBox = UiKit.Box("Name", panel, PanelX, NameY, PanelWidth, 42f);

            _nameText = UiKit.Text("Text", nameBox, 34f, UiKit.Gold, Menu.Font, Menu.Persian);
            _nameText.alignment = TextAlignmentOptions.MidlineRight;
            _nameText.fontStyle = FontStyles.Bold;

            RectTransform metaBox = UiKit.Box("Meta", panel, PanelX, MetaY, PanelWidth, 30f);

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
            _content = UiKit.Box("Content", panel, PanelX, RowTop, PanelWidth, 500f);
        }

        /* ============================================================
           محتوا
           ============================================================ */

        private void BuildSection()
        {
            Rows.Clear();

            // ردیف‌های تب پیشین: اول خاموش، بعد نابود؛ Destroy تا پایان فریم صبر می‌کند
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }

            bool empty = _section != 0;

            if (_note != null)
            {
                _note.gameObject.SetActive(empty);
                _note.text = Loc.Get(Loc.Channel.Menu, "pause.soon");
            }

            if (empty) return;

            for (int i = 0; i < SummaryStats.Length; i++)
            {
                StatId id = SummaryStats[i];

                AddValueRow(CharacterStats.Definition(id).Label(Menu.Persian),
                    () => CharacterStats.Format(id, Menu.Persian));
            }
        }

        /// <summary>ردیف فقط‌خواندنی: برچسب یک طرف، مقدار طرف دیگر.</summary>
        private void AddValueRow(string label, Func<string> read)
        {
            int index = Rows.Count;

            float y = index * (RowHeight + RowGap);

            RectTransform row = UiKit.Box("Row_" + index, _content, 0f, y, PanelWidth, RowHeight);

            bool rtl = Menu.Persian;

            float labelX = rtl ? ValueWidth : 0f;
            float labelWidth = PanelWidth - ValueWidth;

            RectTransform labelBox = UiKit.Box("Label", row, labelX, 0f, labelWidth, RowHeight);

            TMP_Text labelText = UiKit.Text("Text", labelBox, RowFont, UiKit.RowIdle, Menu.Font, Menu.Persian);
            labelText.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            labelText.text = label;

            float valueX = rtl ? 0f : PanelWidth - ValueWidth;

            RectTransform valueBox = UiKit.Box("Value", row, valueX, 0f, ValueWidth, RowHeight);

            TMP_Text valueText = UiKit.Text("Text", valueBox, RowFont, UiKit.Dim, Menu.Font, Menu.Persian);
            valueText.alignment = rtl ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            valueText.text = read != null ? read() : string.Empty;

            PauseRow data = new PauseRow();
            data.Rect = row;
            data.Label = labelText;
            data.ValueText = valueText;
            data.ReadValue = read;

            Rows.Add(data);
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
            _metaText.text = Loc.Get(Loc.Channel.Menu, "char.role") + "   ·   " + FormatDate();

            BuildSection();
            RefreshRail();
            Select(0);
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

        private string FormatDate()
        {
            WorldClock clock = WorldClock.Instance;

            if (clock == null) return Loc.Get(Loc.Channel.Menu, "char.date.unknown");

            GameDate now = clock.Now;

            int month = Mathf.Clamp(now.Month - 1, 0, 11);

            string monthName = Menu.Persian ? PersianMonths[month] : EnglishMonths[month];

            string day = HudArt.Digits(now.Day, Menu.Persian);
            string year = HudArt.Digits(now.Year, Menu.Persian);

            string time = HudArt.Digits(now.Hour.ToString("00") + ":" + now.Minute.ToString("00"), Menu.Persian);

            return day + " " + monthName + " " + year + "   —   " + time;
        }
    }
}
