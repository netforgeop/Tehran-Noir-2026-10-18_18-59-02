using System;
using System.Collections.Generic;

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

        /// <summary>قالب: نامه‌ی رسمی سرصفحه‌دار، یا یادداشت کوچکِ بی‌سرصفحه.</summary>
        public DocumentTemplate Template = DocumentTemplate.Letter;

        /// <summary>خطوط بدنه، به ترتیب. خط خالی یعنی فاصله‌ی بین پاراگراف‌ها.</summary>
        public readonly List<DocumentLine> Body = new List<DocumentLine>();

        /// <summary>خطاها و تذکرهای خواندن فایل؛ در Console چاپ می‌شوند.</summary>
        public readonly List<string> Issues = new List<string>();

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

                document.Body.Add(new DocumentLine(persian, english));
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
        Note
    }

    /// <summary>یک خط از بدنه‌ی سند، در هر دو زبان.</summary>
    public sealed class DocumentLine
    {
        public readonly string Persian;
        public readonly string English;
        public readonly bool IsBlank;

        public DocumentLine(string persian, string english)
        {
            Persian = persian ?? string.Empty;
            English = english ?? string.Empty;
            IsBlank = Persian.Length == 0 && English.Length == 0;
        }

        public static DocumentLine Blank()
        {
            return new DocumentLine(string.Empty, string.Empty);
        }
    }
}
