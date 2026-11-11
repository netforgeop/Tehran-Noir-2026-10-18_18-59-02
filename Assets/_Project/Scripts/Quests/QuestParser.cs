using System.Collections.Generic;
using TehranNoir.Dialogue;

namespace TehranNoir.Quests
{
    /// <summary>
    /// خواندن فایل کارها. نحو همان نحو دیالوگ است تا چیز تازه‌ای یاد نگیری:
    /// هر خط با یک تگ شروع می‌شود، شرط‌ها همان زبان شرط دیالوگ‌اند.
    /// </summary>
    public static class QuestParser
    {
        public static QuestBook Parse(string text, string fileName, QuestBook book = null)
        {
            QuestBook result = book ?? new QuestBook();
            string name = fileName ?? string.Empty;

            string normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');

            QuestDefinition quest = null;
            QuestStep step = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int lineNumber = i + 1;

                if (line.Length == 0 || line[0] == '#') continue;

                if (line[0] != '[')
                {
                    result.Issues.Add(name + " خط " + lineNumber + ": هر خط باید با تگ شروع شود");
                    continue;
                }

                int close = line.IndexOf(']');
                if (close < 2)
                {
                    result.Issues.Add(name + " خط " + lineNumber + ": تگ بسته نشده");
                    continue;
                }

                string tag = line.Substring(1, close - 1).Trim().ToLowerInvariant();
                string value = line.Substring(close + 1).Trim();

                if (tag == "quest")
                {
                    quest = new QuestDefinition();
                    quest.Id = FirstToken(value);
                    quest.Order = result.Quests.Count;
                    result.Quests.Add(quest);

                    step = null;

                    if (quest.Id.Length == 0)
                        result.Issues.Add(name + " خط " + lineNumber + ": [quest] شناسه ندارد");

                    continue;
                }

                if (quest == null)
                {
                    result.Issues.Add(name + " خط " + lineNumber + ": این خط قبل از اولین [quest] آمده");
                    continue;
                }

                if (tag == "step")
                {
                    step = new QuestStep();
                    step.Id = FirstToken(value);
                    quest.Steps.Add(step);
                    continue;
                }

                string persian;
                string english;

                switch (tag)
                {
                    case "name":
                        Split(value, out persian, out english);
                        quest.Persian = persian;
                        quest.English = english;
                        break;

                    case "summary":
                        Split(value, out persian, out english);
                        quest.SummaryPersian = persian;
                        quest.SummaryEnglish = english;
                        break;

                    case "text":
                        if (step == null)
                        {
                            result.Issues.Add(name + " خط " + lineNumber + ": [text] قبل از [step] آمده");
                            break;
                        }

                        Split(value, out persian, out english);
                        step.Persian = persian;
                        step.English = english;
                        break;

                    case "type":
                        quest.Kind = value.Trim().ToLowerInvariant() == "side"
                            ? QuestKind.Side
                            : QuestKind.Main;
                        break;

                    case "track":
                        quest.Tracked = value.Trim().ToLowerInvariant() != "false";
                        break;

                    case "start":
                        quest.StartSource = value;
                        quest.Start = ParseCondition(result, name, lineNumber, value);
                        break;

                    case "done":
                        quest.DoneSource = value;
                        quest.Done = ParseCondition(result, name, lineNumber, value);
                        break;

                    case "when":
                        if (step == null)
                        {
                            result.Issues.Add(name + " خط " + lineNumber + ": [when] قبل از [step] آمده");
                            break;
                        }

                        step.WhenSource = value;
                        step.When = ParseCondition(result, name, lineNumber, value);
                        break;

                    default:
                        result.Issues.Add(name + " خط " + lineNumber + ": تگ ناشناخته [" + tag + "]");
                        break;
                }
            }

            for (int i = 0; i < result.Quests.Count; i++)
            {
                QuestDefinition item = result.Quests[i];

                if (item.Steps.Count == 0)
                    result.Issues.Add(name + ": کار «" + item.Id + "» هیچ قدمی ندارد");

                if (string.IsNullOrEmpty(item.DoneSource))
                    result.Issues.Add(name + ": کار «" + item.Id + "» شرط پایان ندارد ([done])");
            }

            return result;
        }

        private static Condition ParseCondition(QuestBook book, string file, int line, string source)
        {
            Condition condition;
            string error;

            if (ConditionParser.TryParse(source, out condition, out error)) return condition;

            book.Issues.Add(file + " خط " + line + ": شرط نامعتبر — " + error);
            return null;
        }

        private static string FirstToken(string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0) return string.Empty;

            int space = trimmed.IndexOf(' ');
            return space < 0 ? trimmed : trimmed.Substring(0, space).Trim();
        }

        /// <summary>«فارسی | English» — مثل همه‌ی متن‌های دوزبانه‌ی پروژه.</summary>
        private static void Split(string value, out string persian, out string english)
        {
            string trimmed = (value ?? string.Empty).Trim();
            persian = trimmed;
            english = trimmed;

            int bar = trimmed.IndexOf('|');
            if (bar < 0) return;

            persian = trimmed.Substring(0, bar).Trim();
            english = trimmed.Substring(bar + 1).Trim();
        }
    }
}
