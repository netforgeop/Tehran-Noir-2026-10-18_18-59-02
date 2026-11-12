using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TehranNoir.Documents;
using TehranNoir.Quests;
using TehranNoir.Settings;

namespace TehranNoir.UI
{
    /// <summary>
    /// بخش «کارها» و بخش «پرونده‌ها» تب کاراکتر.
    ///
    /// هر دو یک شکل دارند: سرخط با شمار، بعد فهرست ردیف‌ها. تفاوت فقط در داده است:
    ///   کارها      ← QuestLog (که خودش وضعیت را از WorldState می‌خواند)
    ///   پرونده‌ها  ← DocumentCatalog + نشانه‌های `flag.seen.*` که DocumentLog می‌نویسد
    ///
    /// چرا یک کلاس برای دو بخش؟ چون چیدمان، ناوبری و ماوس‌شان یکی است؛ اگر جدا
    /// نوشته می‌شدند، هر اصلاح چیدمان باید دو بار انجام می‌شد و دو رفتار از هم
    /// جدا می‌افتاد.
    ///
    /// کلیک روی ردیف سند، همان سند را دوباره باز می‌کند. ورودی منو در آن لحظه
    /// کنار می‌رود (PauseMenuUI)، وگرنه Esc هم سند را می‌بست هم منو را.
    /// </summary>
    public sealed class DossierSection
    {
        public enum Mode
        {
            Jobs,
            Documents
        }

        /* ---------- اندازه‌ها؛ مبدأ، بالای ستون محتوا ---------- */

        private const float HeaderHeight = 40f;
        private const float RuleY = 48f;
        private const float RowTop = 64f;
        private const float RowHeight = 70f;
        private const float RowGap = 6f;

        /// <summary>بیشتر از این، در یک صفحه جا نمی‌شود؛ بقیه با سه‌نقطه علامت می‌خورند.</summary>
        private const int MaxRows = 7;

        private const float HeaderFont = 26f;
        private const float CountFont = 22f;
        private const float NameFont = 24f;
        private const float SubFont = 17f;
        private const float BadgeFont = 17f;
        private const float EmptyFont = 22f;

        private const float BadgeWidth = 150f;
        private const float BadgeHeight = 28f;
        private const float PadX = 18f;

        /// <summary>بلندی عنوانِ جایگزین (یادداشتِ بی‌سرصفحه)؛ بیشتر از این، ردیف را می‌شکند.</summary>
        private const int Cut = 46;

        private static readonly Color RowFill = new Color(0.094f, 0.089f, 0.082f, 0.55f);
        private static readonly Color RowFillOn = new Color(0.165f, 0.145f, 0.104f, 0.85f);
        private static readonly Color RowEdge = new Color(0.220f, 0.210f, 0.190f, 0.75f);
        private static readonly Color BadgeBody = new Color(0.118f, 0.110f, 0.098f, 0.95f);
        private static readonly Color BadgeEdge = new Color(0.270f, 0.255f, 0.230f, 0.85f);

        /* ---------- داده ---------- */

        private sealed class Entry
        {
            public string Title;
            public string Sub;
            public string Badge;
            public string Hint;
            public string Resource;
        }

        private sealed class RowView
        {
            public RectTransform Rect;
            public Image Fill;
            public TMP_Text Name;
            public TMP_Text Sub;
            public Image BadgeFill;
            public TMP_Text Badge;
        }

        private readonly Mode _mode;
        private readonly PauseMenuUI _menu;

        private readonly List<Entry> _entries = new List<Entry>(16);
        private readonly List<RowView> _rows = new List<RowView>(MaxRows);

        private RectTransform _content;

        private int _index;
        private int _signature = int.MinValue;
        private string _hintShown;

        public DossierSection(PauseMenuUI menu, Mode mode)
        {
            _menu = menu;
            _mode = mode;
        }

        private bool Rtl => _menu != null && _menu.Persian;

        private TMP_FontAsset Font => _menu != null ? _menu.Font : null;

        private string HeaderKey => _mode == Mode.Jobs ? "char.section.jobs" : "char.section.cases";

        /* ============================================================
           ساخت
           ============================================================ */

        /// <summary>ستون محتوا را خالی و بخش را از نو می‌سازد.</summary>
        public void Build(RectTransform content)
        {
            _content = content;

            Clear(content);

            _rows.Clear();
            _entries.Clear();

            _signature = Signature();

            // رشته‌ی خالی با null یکی نیست: با این، اولین Refresh همیشه راهنما را
            // می‌نویسد، حتی وقتی راهنما خالی است و باید متن قبلی پاک شود.
            _hintShown = null;

            ReadEntries();
            ClampIndex();

            BuildHeader();
            BuildRows();

            Refresh();
        }

        /// <summary>هر فریم که منو باز است؛ فقط وقتی داده عوض شده باشد بازمی‌سازد.</summary>
        public void Tick()
        {
            if (_content == null) return;

            if (Signature() == _signature) return;

            Build(_content);
        }

        /// <summary>
        /// انتخاب را به ردیف اول برمی‌گرداند. عمداً چیزی روی صفحه دست نمی‌زند:
        /// این متد پیش از ساخت بخش صدا زده می‌شود و آن‌وقت ردیف‌های قبلی
        /// نابود شده‌اند؛ ساخت بعدی خودش با شماره‌ی صفر رنگ می‌گیرد.
        /// </summary>
        public void FocusFirst()
        {
            _index = 0;
        }

        private static void Clear(RectTransform content)
        {
            if (content == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        /* ============================================================
           داده
           ============================================================ */

        /// <summary>
        /// اثر انگشت داده. اگر کارها عوض شوند یا سند تازه‌ای خوانده شود، عوض می‌شود؛
        /// وگرنه هر فریم ردیف تازه می‌ساختیم.
        /// </summary>
        private int Signature()
        {
            int hash = _mode == Mode.Jobs ? 31 : 67;

            if (_mode == Mode.Jobs)
            {
                QuestLog log = QuestLog.Instance;

                if (log == null) return hash;

                return hash * 31 + log.Signature;
            }

            IReadOnlyList<DocumentText> all = DocumentCatalog.All;

            for (int i = 0; i < all.Count; i++)
                if (DocumentLog.Seen(all[i], null)) hash = hash * 31 + i + 1;

            return hash;
        }

        private void ReadEntries()
        {
            if (_mode == Mode.Jobs) ReadJobs();
            else ReadDocuments();
        }

        private void ReadJobs()
        {
            QuestLog log = QuestLog.Instance;

            if (log == null) return;

            IReadOnlyList<QuestStatus> active = log.Active;

            for (int i = 0; i < active.Count; i++)
            {
                QuestStatus status = active[i];

                if (status == null || status.Quest == null) continue;

                Entry entry = new Entry();

                entry.Title = status.Quest.Name(Rtl);
                entry.Sub = status.Step != null ? status.Step.Text(Rtl) : string.Empty;
                entry.Badge = Loc.Get(Loc.Channel.Menu,
                    status.Quest.Kind == QuestKind.Main ? "quest.main" : "quest.side");
                entry.Hint = Summary(status.Quest);

                _entries.Add(entry);
            }
        }

        private void ReadDocuments()
        {
            IReadOnlyList<DocumentText> all = DocumentCatalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                DocumentText document = all[i];

                if (!DocumentLog.Seen(document, null)) continue;

                Entry entry = new Entry();

                entry.Title = Pick(document.Title);

                // یادداشت سرصفحه ندارد؛ به‌جای نام فایل، اولین خط خودش را نشان
                // می‌دهد تا در فهرست هم خواندنی باشد.
                if (entry.Title.Length == 0) entry.Title = FirstLine(document);
                if (entry.Title.Length == 0) entry.Title = document.FileName;

                string date = Pick(document.Date);
                string number = Pick(document.Number);

                if (date.Length > 0 && number.Length > 0) entry.Sub = date + "   ·   " + number;
                else entry.Sub = date.Length > 0 ? date : number;

                entry.Badge = Loc.Get(Loc.Channel.Menu, KindKey(document.Template));
                entry.Hint = Loc.Get(Loc.Channel.Menu, "cases.read");
                entry.Resource = "Documents/" + document.FileName;

                _entries.Add(entry);
            }
        }

        private string Summary(QuestDefinition quest)
        {
            string chosen = Rtl ? quest.SummaryPersian : quest.SummaryEnglish;

            if (!string.IsNullOrEmpty(chosen)) return chosen;

            return Rtl ? quest.SummaryEnglish : quest.SummaryPersian;
        }

        private string Pick(string raw)
        {
            return DocumentText.Pick(raw, Rtl);
        }

        /// <summary>اولین خط غیرخالیِ بدنه، کوتاه‌شده؛ برای سندی که عنوان ندارد.</summary>
        private string FirstLine(DocumentText document)
        {
            for (int i = 0; i < document.Body.Count; i++)
            {
                DocumentLine line = document.Body[i];

                if (line.IsBlank) continue;

                string text = (Rtl ? line.Persian : line.English).Trim();

                if (text.Length == 0) text = (Rtl ? line.English : line.Persian).Trim();
                if (text.Length == 0) continue;

                if (text.Length > Cut) text = text.Substring(0, Cut).TrimEnd() + "…";

                return text;
            }

            return string.Empty;
        }

        private static string KindKey(DocumentTemplate template)
        {
            if (template == DocumentTemplate.Note) return "cases.kind.note";
            if (template == DocumentTemplate.Ledger) return "cases.kind.ledger";

            return "cases.kind.letter";
        }

        private void ClampIndex()
        {
            int shown = Mathf.Min(_entries.Count, MaxRows);

            _index = shown == 0 ? 0 : Mathf.Clamp(_index, 0, shown - 1);
        }

        /* ============================================================
           ساختِ اجزا
           ============================================================ */

        private void BuildHeader()
        {
            float width = _content.rect.width;

            RectTransform header = UiKit.Box("Header", _content, 0f, 0f, width, HeaderHeight);

            TMP_Text title = UiKit.Text("Text", header, HeaderFont, UiKit.Gold, Font, Rtl);
            title.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.fontStyle = FontStyles.Bold;
            title.text = Loc.Get(Loc.Channel.Menu, HeaderKey);

            RectTransform countBox = UiKit.Box("Count", header, Rtl ? 0f : width - 80f, 6f, 80f, 28f);

            TMP_Text count = UiKit.Text("Text", countBox, CountFont, UiKit.Dim, Font, Rtl);
            count.alignment = Rtl ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            UiKit.ForceFixNumbers(count, Rtl);
            count.text = _entries.Count.ToString();

            RectTransform rule = UiKit.Box("Rule", _content, 0f, RuleY, width, 1f);

            Image line = rule.gameObject.AddComponent<Image>();
            line.color = RowEdge;
            line.raycastTarget = false;
        }

        private void BuildRows()
        {
            if (_entries.Count == 0)
            {
                BuildEmpty();
                return;
            }

            int shown = Mathf.Min(_entries.Count, MaxRows);

            for (int i = 0; i < shown; i++)
                AddRow(i, _entries[i], RowTop + i * (RowHeight + RowGap));

            if (_entries.Count <= shown) return;

            float y = RowTop + shown * (RowHeight + RowGap);

            RectTransform box = UiKit.Box("Older", _content, PadX, y, _content.rect.width - PadX * 2f, 26f);

            TMP_Text older = UiKit.Text("Text", box, SubFont, UiKit.Dim, Font, Rtl);
            older.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            older.text = "…";
        }

        private void BuildEmpty()
        {
            RectTransform box = UiKit.Box("Empty", _content, 0f, RowTop + 8f, _content.rect.width, 60f);

            TMP_Text empty = UiKit.Text("Text", box, EmptyFont, UiKit.Dim, Font, Rtl);
            empty.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            empty.text = Loc.Get(Loc.Channel.Menu,
                _mode == Mode.Jobs ? "cases.jobs.empty" : "cases.docs.empty");
        }

        private void AddRow(int index, Entry entry, float y)
        {
            float width = _content.rect.width;

            RectTransform box = UiKit.Box("Row_" + index, _content, 0f, y, width, RowHeight);

            RowView view = new RowView();
            view.Rect = box;

            view.Fill = box.gameObject.AddComponent<Image>();
            view.Fill.color = RowFill;
            view.Fill.raycastTarget = false;

            RectTransform rule = UiKit.Box("Rule", box, 0f, RowHeight - 1f, width, 1f);
            Image ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.color = RowEdge;
            ruleImage.raycastTarget = false;

            float badgeX = Rtl ? PadX : width - BadgeWidth - PadX;
            float nameX = Rtl ? PadX + BadgeWidth : PadX;
            float nameWidth = width - BadgeWidth - PadX * 2f;

            RectTransform nameBox = UiKit.Box("Name", box, nameX, 8f, nameWidth, 30f);

            view.Name = UiKit.Text("Text", nameBox, NameFont, UiKit.RowIdle, Font, Rtl);
            view.Name.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            view.Name.text = entry.Title;

            RectTransform subBox = UiKit.Box("Sub", box, nameX, 38f, nameWidth, 24f);

            view.Sub = UiKit.Text("Text", subBox, SubFont, UiKit.Dim, Font, Rtl);
            view.Sub.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            view.Sub.text = entry.Sub;

            RectTransform badgeBox = UiKit.Box("Badge", box, badgeX, (RowHeight - BadgeHeight) * 0.5f,
                BadgeWidth, BadgeHeight);

            view.BadgeFill = badgeBox.gameObject.AddComponent<Image>();
            view.BadgeFill.sprite = HudArt.Panel(
                "dossier.badge." + Mathf.RoundToInt(BadgeWidth) + "." + Mathf.RoundToInt(BadgeHeight),
                Mathf.RoundToInt(BadgeWidth), Mathf.RoundToInt(BadgeHeight), 4f,
                BadgeBody, BadgeBody, BadgeEdge, 1f);
            view.BadgeFill.raycastTarget = false;

            view.Badge = UiKit.Text("Text", badgeBox, BadgeFont, UiKit.Dim, Font, Rtl);
            view.Badge.alignment = TextAlignmentOptions.Center;
            view.Badge.text = entry.Badge;

            _rows.Add(view);
        }

        /* ============================================================
           رنگ و راهنما
           ============================================================ */

        private void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                RowView row = _rows[i];
                bool on = i == _index;

                row.Fill.color = on ? RowFillOn : RowFill;
                row.Name.color = on ? UiKit.Gold : UiKit.RowIdle;
                row.Sub.color = on ? UiKit.Idle : UiKit.Dim;
                row.Badge.color = on ? UiKit.Gold : UiKit.Dim;
            }

            if (_menu == null) return;

            string hint = _entries.Count == 0 || _index >= _entries.Count
                ? string.Empty
                : _entries[_index].Hint;

            if (hint == _hintShown) return;

            _hintShown = hint;
            _menu.ShowHint(hint);
        }

        /* ============================================================
           ورودی
           ============================================================ */

        public void Move(int delta)
        {
            if (_rows.Count == 0) return;

            _index = Mathf.Clamp(_index + delta, 0, _rows.Count - 1);
            Refresh();
        }

        /// <summary>Enter یا کلیک. فقط سند کار دارد: همان کاغذ را دوباره باز می‌کند.</summary>
        public bool Activate()
        {
            if (_mode != Mode.Documents) return false;
            if (_index < 0 || _index >= _entries.Count) return false;

            string path = _entries[_index].Resource;

            if (string.IsNullOrEmpty(path)) return false;

            DocumentLayer.ShowResource(path, null);

            return true;
        }

        /// <summary>ماوس: اگر روی یکی از ردیف‌ها بود، همان را انتخاب می‌کند.</summary>
        public bool Pointer(Vector2 screenPoint, bool clicked)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                RectTransform rect = _rows[i].Rect;

                if (rect == null) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null)) continue;

                if (_index != i)
                {
                    _index = i;
                    Refresh();
                }

                if (clicked) Activate();

                return true;
            }

            return false;
        }
    }
}
