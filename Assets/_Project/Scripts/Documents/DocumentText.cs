using System;
using System.Collections.Generic;
using TehranNoir.Dialogue;

namespace TehranNoir.Documents
{
    /// <summary>
    /// یک سند کاغذی: نامه، پرونده، گزارش، یادداشت.
    /// فایلش متنی است و با تگ‌های ساده نوشته می‌شود؛ هیچ کد تازه‌ای برای سند بعدی لازم نیست.
    /// هر خط بدنه «فارسی | English» است. خط بدون «|» در هر دو زبان همان‌طور می‌آید.
    /// </summary>
    public sealed class DocumentText
    {
        /* ---------- سرصفحه ---------- */

        public string Agency = string.Empty;   // وزارت کشور | Ministry of the Interior
        public string Office = string.Empty;   // شهربانی کل کشور — اداره‌ی اطلاعات | ...
        public string Number = string.Empty;   // شماره
        public string Date = string.Empty;     // تاریخ
        public string Title = string.Empty;    // عنوان سند
        public string Stamp = string.Empty;    // متن مهر (پیش‌فرض: محرمانه)

        public bool ShowEmblem = true;
        public bool ShowStamp = true;

        /// <summary>سرستون‌های جدول «دفتر»، با «؛» جدا و هر سرستون «فارسی | English».</summary>
        public string Columns = string.Empty;

        /// <summary>پانویس برگه؛ یک خط کوچک زیر جدول.</summary>
        public string Footnote = string.Empty;

        /// <summary>
        /// خط‌های دست‌نویس. اگر روشن باشد، ردیف‌ها با لرزش و جوهر آبی-مشکی
        /// کشیده می‌شوند تا شکل نوشتن با خودکار را بگیرند.
        /// </summary>
        public bool Handwritten;

        /// <summary>قالب: نامه‌ی رسمی سرصفحه‌دار، یا یادداشت کوچکِ بی‌سرصفحه.</summary>
        public DocumentTemplate Template = DocumentTemplate.Letter;

        /// <summary>خطوط بدنه، به ترتیب. خط خالی یعنی فاصله‌ی بین پاراگراف‌ها.</summary>
        public readonly List<DocumentLine> Body = new List<DocumentLine>();

        /// <summary>ردیف‌های جدول؛ فقط در قالب «دفتر» استفاده می‌شود.</summary>
        public readonly List<DocumentRow> Rows = new List<DocumentRow>();

        /// <summary>خطاها و تذکرهای خواندن فایل؛ در Console چاپ می‌شوند.</summary>
        public readonly List<string> Issues = new List<string>();

        /// <summary>شرط‌های خوانده‌شده؛ هر شرط یک بار پارس می‌شود، نه هر بار باز شدن سند.</summary>
        private readonly Dictionary<string, Condition> _guards = new Dictionary<string, Condition>(4);

        public string FileName = string.Empty;

        public bool HasIssues { get { return Issues.Count > 0; } }

        /* ============================================================
           خواندن
           ============================================================ */

        /// <summary>
        /// متن فایل را می‌خواند. همیشه یک شیء برمی‌گرداند؛ اگر خطی مشکل داشت،
        /// در Issues می‌نویسد و از آن می‌گذرد تا بقیه‌ی سند از دست نرود.
        /// </summary>
        public static DocumentText Parse(string text, string fileName = null)
        {
            DocumentText document = new DocumentText();
            document.FileName = fileName ?? string.Empty;

            string normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');

            // نگهبانِ ردیف‌ها و خط‌های بعدی: با [when] روشن و با [always] خاموش می‌شود.
            // همان فکرِ [if] در دیالوگ؛ فقط برای کاغذ.
            string guard = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Length == 0)
                {
                    // خط خالی = پاراگراف تازه؛ دو تا پشت سر هم فقط یک فاصله می‌سازد
                    if (document.Body.Count > 0 && !document.Body[document.Body.Count - 1].IsBlank)
                        document.Body.Add(DocumentLine.Blank());
                    continue;
                }

                if (line[0] == '#') continue;

                if (line[0] == '[')
                {
                    int close = line.IndexOf(']');
                    if (close > 1)
                    {
                        string tag = line.Substring(1, close - 1).Trim().ToLowerInvariant();
                        string value = line.Substring(close + 1).Trim();

                        if (tag == "when")
                        {
                            guard = value;

                            if (value.Length == 0)
                                document.Issues.Add("خط " + (i + 1) + ": [when] بی‌شرط است");

                            continue;
                        }

                        if (tag == "always")
                        {
                            guard = string.Empty;
                            continue;
                        }

                        if (tag == "row")
                        {
                            DocumentRow row = DocumentRow.Parse(value, guard);

                            if (row.Cells.Count == 0)
                                document.Issues.Add("خط " + (i + 1) + ": [row] بی‌خانه است");
                            else
                                document.Rows.Add(row);

                            continue;
                        }

                        if (document.ApplyTag(tag, value, i + 1)) continue;

                        document.Issues.Add("خط " + (i + 1) + ": تگ ناشناخته [" + tag + "]");
                        continue;
                    }
                }

                string persian;
                string english;

                if (!Split(line, out persian, out english))
                {
                    document.Issues.Add("خط " + (i + 1) + ": متن خالی است");
                    continue;
                }

                document.Body.Add(new DocumentLine(persian, english, guard));
            }

            while (document.Body.Count > 0 && document.Body[document.Body.Count - 1].IsBlank)
                document.Body.RemoveAt(document.Body.Count - 1);

            return document;
        }

        private bool ApplyTag(string tag, string value, int lineNumber)
        {
            switch (tag)
            {
                case "agency": Agency = value; return true;
                case "office": Office = value; return true;
                case "number": Number = value; return true;
                case "date": Date = value; return true;
                case "title": Title = value; return true;
                case "stamp": Stamp = value; return true;
                case "columns": Columns = value; return true;
                case "footnote": Footnote = value; return true;

                case "hand":
                    Handwritten = ParseBool(value, true);
                    return true;

                case "template":
                    Template = ParseTemplate(value);
                    if (value.Trim().ToLowerInvariant() == "note") ShowEmblem = false;
                    return true;

                case "emblem":
                    ShowEmblem = ParseBool(value, true);
                    return true;

                case "seal":
                    ShowStamp = ParseBool(value, true);
                    return true;

                default:
                    return false;
            }
        }

        private static DocumentTemplate ParseTemplate(string value)
        {
            string lowered = (value ?? string.Empty).Trim().ToLowerInvariant();

            if (lowered == "note" || lowered == "یادداشت") return DocumentTemplate.Note;
            if (lowered == "ledger" || lowered == "دفتر") return DocumentTemplate.Ledger;
            return DocumentTemplate.Letter;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;

            string lowered = value.Trim().ToLowerInvariant();
            if (lowered == "true" || lowered == "1" || lowered == "yes") return true;
            if (lowered == "false" || lowered == "0" || lowered == "no") return false;

            return fallback;
        }

        /// <summary>«فارسی | English» را به دو نیم می‌کند؛ اگر «|» نبود، هر دو یکی‌اند.</summary>
        public static bool Split(string source, out string persian, out string english)
        {
            persian = (source ?? string.Empty).Trim();
            english = persian;

            int bar = persian.IndexOf('|');
            if (bar < 0) return persian.Length > 0;

            english = persian.Substring(bar + 1).Trim();
            persian = persian.Substring(0, bar).Trim();

            return persian.Length > 0 || english.Length > 0;
        }

        /* ============================================================
           نمایش
           ============================================================ */

        public static string Pick(string raw, bool persian)
        {
            string fa;
            string en;
            if (!Split(raw, out fa, out en)) return string.Empty;
            return persian ? fa : en;
        }

        /// <summary>
        /// خط‌ها و ردیف‌هایی که شرطشان الان برقرار است.
        ///
        /// چرا اینجا و نه در نمای سند؟ چون شرط، بخشی از **خواندن داده** است، نه
        /// بخشی از نقاشی. با این جدایی، همان منطق بدون Unity هم آزمون‌پذیر است.
        /// </summary>
        public void Resolve(WorldState world, List<DocumentLine> body, List<DocumentRow> rows)
        {
            if (body != null)
            {
                body.Clear();

                for (int i = 0; i < Body.Count; i++)
                    if (IsVisible(Body[i].Guard, world)) body.Add(Body[i]);
            }

            if (rows != null)
            {
                rows.Clear();

                for (int i = 0; i < Rows.Count; i++)
                    if (IsVisible(Rows[i].Guard, world)) rows.Add(Rows[i]);
            }
        }

        /// <summary>شرط یک ردیف یا خط. شرط بدخط یعنی «دیده نشود»، نه «سند نشکند».</summary>
        public bool IsVisible(string guard, WorldState world)
        {
            if (string.IsNullOrEmpty(guard)) return true;

            Condition condition;

            if (!_guards.TryGetValue(guard, out condition))
            {
                string error;

                if (!ConditionParser.TryParse(guard, out condition, out error))
                {
                    Issues.Add("شرط بدخط: «" + guard + "» — " + error);
                    condition = null;
                }

                _guards[guard] = condition;
            }

            if (condition == null) return false;

            return world != null && condition.IsTrue(world);
        }

        /// <summary>همان متن، از فهرستی که پیش‌تر صافی خورده است.</summary>
        public static string BuildBody(List<DocumentLine> lines, bool persian)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            if (lines == null) return string.Empty;

            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) builder.Append('\n');

                DocumentLine line = lines[i];
                builder.Append(persian ? line.Persian : line.English);
            }

            return builder.ToString();
        }

        /// <summary>بدنه را به یک متن آماده‌ی TMP تبدیل می‌کند.</summary>
        public string BuildBody(bool persian)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < Body.Count; i++)
            {
                if (i > 0) builder.Append('\n');

                DocumentLine line = Body[i];
                builder.Append(persian ? line.Persian : line.English);
            }

            return builder.ToString();
        }
    }

    /// <summary>شکل برگه. «نامه» سرصفحه و نشان دارد؛ «یادداشت» فقط متن است.</summary>
    public enum DocumentTemplate
    {
        Letter,
        Note,
        Ledger
    }

    /// <summary>یک خط از بدنه‌ی سند، در هر دو زبان.</summary>
    public sealed class DocumentLine
    {
        public readonly string Persian;
        public readonly string English;
        public readonly bool IsBlank;

        /// <summary>شرطِ نمایش این خط؛ خالی یعنی همیشه. با گرامر [if] دیالوگ.</summary>
        public readonly string Guard;

        public DocumentLine(string persian, string english)
            : this(persian, english, string.Empty)
        {
        }

        public DocumentLine(string persian, string english, string guard)
        {
            Persian = persian ?? string.Empty;
            English = english ?? string.Empty;
            Guard = guard ?? string.Empty;
            IsBlank = Persian.Length == 0 && English.Length == 0;
        }

        public static DocumentLine Blank()
        {
            return new DocumentLine(string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// یک ردیف جدول. هر خانه «فارسی | English» است و خانه‌ها با «؛» جدا می‌شوند.
    ///
    /// چرا «؛»؟ چون «|» برای جدا کردن دو زبان استفاده می‌شود و اگر برای خانه‌ها
    /// هم به کار می‌رفت، مرزها گم می‌شدند.
    /// </summary>
    public sealed class DocumentRow
    {
        public readonly List<string> Cells;

        /// <summary>شرطِ نمایش این ردیف؛ خالی یعنی همیشه.</summary>
        public readonly string Guard;

        public DocumentRow(List<string> cells, string guard)
        {
            Cells = cells ?? new List<string>(0);
            Guard = guard ?? string.Empty;
        }

        public static DocumentRow Parse(string source, string guard)
        {
            List<string> cells = new List<string>(6);
            string text = source ?? string.Empty;
            int start = 0;

            for (int i = 0; i <= text.Length; i++)
            {
                if (i < text.Length && text[i] != '؛') continue;

                string cell = text.Substring(start, i - start).Trim();
                cells.Add(cell);
                start = i + 1;
            }

            if (cells.Count == 1 && cells[0].Length == 0) cells.Clear();

            return new DocumentRow(cells, guard);
        }

        /// <summary>متن یک خانه در زبان خواسته‌شده.</summary>
        public string Cell(int index, bool persian)
        {
            if (index < 0 || index >= Cells.Count) return string.Empty;

            return DocumentText.Pick(Cells[index], persian);
        }
    }
}
