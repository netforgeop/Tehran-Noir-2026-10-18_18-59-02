using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TehranNoir.Dialogue
{
    /// خواندن فایل متنی دیالوگ و ساختن DialogueDocument.
    public static class DialogueParser
    {
        private static readonly string[] Tags =
        {
            "person", "name", "role", "cast", "node", "say", "opt", "go", "if", "else", "do", "end"
        };

        private static readonly Regex TagPattern = new Regex(@"^\[([A-Za-z]+)\]\s*(.*)$");
        private static readonly char[] SpaceSplit = { ' ', '\t' };

        public static DialogueDocument Parse(string text, string fileName = null)
        {
            DialogueDocument document = new DialogueDocument();
            document.File = fileName ?? string.Empty;

            List<Condition> pendingGuard = new List<Condition>();
            string pendingGuardSource = string.Empty;
            DialogueNode current = null;

            string normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] raw = normalized.Split('\n');

            for (int i = 0; i < raw.Length; i++)
            {
                int lineNumber = i + 1;
                string line = raw[i].Trim();

                if (line.Length == 0 || line[0] == '#') continue;

                Match match = TagPattern.Match(line);
                if (!match.Success)
                {
                    document.Errors.Add(new DialogueIssue(lineNumber, "هر خط باید با یک تگ شروع شود، مثل [say]"));
                    continue;
                }

                string tag = match.Groups[1].Value.ToLowerInvariant();
                string rest = match.Groups[2].Value.Trim();

                if (Array.IndexOf(Tags, tag) < 0)
                {
                    document.Errors.Add(new DialogueIssue(lineNumber, "تگ ناشناخته: [" + tag + "]"));
                    continue;
                }

                if (tag == "person")
                {
                    document.Person.Id = FirstToken(rest).ToLowerInvariant();
                    continue;
                }

                if (tag == "name" || tag == "role")
                {
                    List<string> parts = SplitPipe(rest);
                    if (parts.Count < 2)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[" + tag + "] باید «فارسی | English» باشد"));
                        continue;
                    }

                    LocalizedText value = new LocalizedText(parts[0], parts[1]);
                    if (tag == "name") document.Person.Name = value;
                    else document.Person.Role = value;
                    continue;
                }

                if (tag == "cast")
                {
                    string castId = FirstToken(rest).ToLowerInvariant();
                    if (castId.Length == 0)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[cast] بدون شناسه"));
                        continue;
                    }
                    if (!document.Cast.Contains(castId)) document.Cast.Add(castId);
                    continue;
                }

                if (tag == "node")
                {
                    string id = FirstToken(rest);
                    if (id.Length == 0)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[node] بدون شناسه"));
                        continue;
                    }

                    int duplicates = 0;
                    for (int n = 0; n < document.Nodes.Count; n++)
                        if (document.Nodes[n].BaseId == id) duplicates++;

                    if (duplicates > 0)
                        document.Errors.Add(new DialogueIssue(lineNumber, "شناسه تکراری: «" + id + "»"));

                    current = new DialogueNode();
                    current.Id = duplicates > 0 ? id + "#" + (duplicates + 1) : id;
                    current.BaseId = id;
                    current.Line = lineNumber;
                    document.Nodes.Add(current);

                    pendingGuard.Clear();
                    pendingGuardSource = string.Empty;
                    continue;
                }

                if (current == null)
                {
                    document.Errors.Add(new DialogueIssue(lineNumber, "این خط قبل از اولین [node] آمده"));
                    continue;
                }

                List<Condition> guard = new List<Condition>(pendingGuard);
                string guardSource = pendingGuardSource;

                if (tag == "if")
                {
                    int arrow = rest.LastIndexOf("->", StringComparison.Ordinal);

                    if (arrow >= 0)
                    {
                        string conditionSource = rest.Substring(0, arrow).Trim();
                        string destination = FirstToken(rest.Substring(arrow + 2).Trim());

                        if (destination.Length == 0)
                            document.Errors.Add(new DialogueIssue(lineNumber, "[if] مقصد ندارد"));

                        RedirectItem redirect = new RedirectItem();
                        redirect.Line = lineNumber;
                        redirect.When = ParseCondition(document, lineNumber, conditionSource);
                        redirect.WhenSource = conditionSource;
                        redirect.Target = destination;
                        redirect.Guard = guard;
                        redirect.GuardSource = guardSource;

                        current.Items.Add(redirect);
                        current.Links.Add(new DialogueLink
                        {
                            Target = destination,
                            Line = lineNumber,
                            IsAutomatic = true
                        });
                    }
                    else
                    {
                        Condition condition = ParseCondition(document, lineNumber, rest);

                        IfItem ifItem = new IfItem();
                        ifItem.Line = lineNumber;
                        ifItem.When = condition;
                        ifItem.WhenSource = rest;
                        current.Items.Add(ifItem);

                        pendingGuard.Clear();
                        pendingGuardSource = string.Empty;

                        if (condition != null)
                        {
                            pendingGuard.Add(condition);
                            pendingGuardSource = rest;
                        }
                    }
                    continue;
                }

                if (tag == "say")
                {
                    List<string> parts = SplitPipe(rest);
                    if (parts.Count < 3)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[say] باید «گوینده | فارسی | English» باشد"));
                        continue;
                    }

                    string english = string.Join(" | ", parts.GetRange(2, parts.Count - 2).ToArray());

                    if (parts[0].Length == 0)
                        document.Warnings.Add(new DialogueIssue(lineNumber, "گوینده خالی است"));
                    if (parts[1].Length == 0)
                        document.Warnings.Add(new DialogueIssue(lineNumber, "ترجمه‌ی فارسی خالی است"));
                    if (english.Trim().Length == 0)
                        document.Warnings.Add(new DialogueIssue(lineNumber, "ترجمه‌ی انگلیسی خالی است"));

                    SayItem say = new SayItem();
                    say.Line = lineNumber;
                    say.Speaker = parts[0].ToLowerInvariant();
                    say.Text = new LocalizedText(parts[1], english);
                    say.Guard = guard;
                    say.GuardSource = guardSource;
                    current.Items.Add(say);
                    continue;
                }

                if (tag == "opt")
                {
                    int arrow = rest.LastIndexOf("->", StringComparison.Ordinal);
                    if (arrow < 0)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[opt] باید با  -> مقصد  تمام شود"));
                        continue;
                    }

                    string label = rest.Substring(0, arrow).Trim();
                    string destination = FirstToken(rest.Substring(arrow + 2).Trim());
                    List<string> parts = SplitPipe(label);

                    if (parts.Count < 2)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[opt] باید «فارسی | English -> مقصد» باشد"));
                        continue;
                    }
                    if (destination.Length == 0)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[opt] مقصد خالی است"));
                        continue;
                    }

                    OptionItem option = new OptionItem();
                    option.Line = lineNumber;
                    option.Text = new LocalizedText(parts[0], parts[1]);
                    option.Target = destination;
                    option.Guard = guard;
                    option.GuardSource = guardSource;
                    current.Items.Add(option);

                    current.Links.Add(new DialogueLink { Target = destination, Line = lineNumber });
                    current.HasTerminator = true;
                    continue;
                }

                if (tag == "go")
                {
                    string destination = FirstToken(rest);
                    if (destination.Length == 0)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[go] مقصد ندارد"));
                        continue;
                    }

                    GoItem go = new GoItem();
                    go.Line = lineNumber;
                    go.Target = destination;
                    go.Guard = guard;
                    go.GuardSource = guardSource;
                    current.Items.Add(go);

                    current.Links.Add(new DialogueLink { Target = destination, Line = lineNumber, IsAutomatic = true });
                    current.HasTerminator = true;
                    continue;
                }

                if (tag == "else")
                {
                    ElseItem elseItem = new ElseItem();
                    elseItem.Line = lineNumber;
                    current.Items.Add(elseItem);

                    pendingGuard.Clear();
                    pendingGuardSource = string.Empty;
                    continue;
                }

                if (tag == "do")
                {
                    if (rest.Length == 0)
                    {
                        document.Errors.Add(new DialogueIssue(lineNumber, "[do] خالی است"));
                        continue;
                    }

                    EffectItem effect = new EffectItem();
                    effect.Line = lineNumber;
                    effect.Source = rest;
                    effect.Guard = guard;
                    effect.GuardSource = guardSource;
                    current.Items.Add(effect);
                    continue;
                }

                if (tag == "end")
                {
                    EndItem end = new EndItem();
                    end.Line = lineNumber;
                    end.Guard = guard;
                    end.GuardSource = guardSource;
                    current.Items.Add(end);
                    current.HasTerminator = true;
                    continue;
                }
            }

            if (document.Person.Id.Length == 0)
                document.Warnings.Add(new DialogueIssue(1, "[person] تعریف نشده"));

            if (document.Nodes.Count == 0)
                document.Errors.Add(new DialogueIssue(1, "هیچ [node] پیدا نشد"));

            ValidateNodes(document);
            ValidateLinks(document);
            ValidateSpeakers(document);
            CollectVariables(document);

            return document;
        }

        private static void ValidateNodes(DialogueDocument document)
        {
            for (int i = 0; i < document.Nodes.Count; i++)
            {
                DialogueNode node = document.Nodes[i];
                bool hasLink = node.Links.Count > 0;
                bool hasEnd = false;
                bool hasUnconditionalExit = false;

                for (int j = 0; j < node.Items.Count; j++)
                {
                    DialogueItem item = node.Items[j];

                    if (item is EndItem) hasEnd = true;

                    bool isExit = item is OptionItem || item is GoItem || item is RedirectItem;
                    if (isExit && (item.Guard == null || item.Guard.Count == 0))
                        hasUnconditionalExit = true;
                }

                if (!hasLink && !hasEnd)
                    document.Errors.Add(new DialogueIssue(node.Line,
                        "node «" + node.Id + "» نه [opt]/[go]/[end] دارد — بن‌بست"));

                if (hasLink && !hasUnconditionalExit && !hasEnd)
                    document.Warnings.Add(new DialogueIssue(node.Line,
                        "node «" + node.Id + "» فقط مسیرهای شرطی دارد؛ اگر هیچ شرطی برقرار نشود، دیالوگ تمام می‌شود"));
            }
        }

        private static void ValidateLinks(DialogueDocument document)
        {
            for (int i = 0; i < document.Nodes.Count; i++)
            {
                DialogueNode node = document.Nodes[i];

                for (int j = 0; j < node.Links.Count; j++)
                {
                    DialogueLink link = node.Links[j];
                    if (document.FindNode(link.Target) == null)
                        document.Errors.Add(new DialogueIssue(link.Line, "مقصد پیدا نشد: «" + link.Target + "»"));
                }
            }
        }

        private static void ValidateSpeakers(DialogueDocument document)
        {
            if (document.Cast.Count == 0) return;

            for (int i = 0; i < document.Nodes.Count; i++)
            {
                DialogueNode node = document.Nodes[i];

                for (int j = 0; j < node.Items.Count; j++)
                {
                    SayItem say = node.Items[j] as SayItem;
                    if (say == null) continue;

                    if (!document.Cast.Contains(say.Speaker))
                        document.Warnings.Add(new DialogueIssue(say.Line,
                            "گوینده «" + say.Speaker + "» در [cast] نیست"));
                }
            }
        }

        private static void CollectVariables(DialogueDocument document)
        {
            List<string> used = new List<string>();

            for (int i = 0; i < document.Nodes.Count; i++)
            {
                DialogueNode node = document.Nodes[i];

                for (int j = 0; j < node.Items.Count; j++)
                {
                    List<Condition> conditions = new List<Condition>();
                    node.Items[j].CollectConditions(conditions);

                    for (int c = 0; c < conditions.Count; c++)
                        conditions[c].CollectVariables(used);
                }
            }

            used.Sort(StringComparer.Ordinal);
            document.Variables.AddRange(used);
        }

        private static Condition ParseCondition(DialogueDocument document, int lineNumber, string source)
        {
            if (string.IsNullOrEmpty(source) || source.Trim().Length == 0)
            {
                document.Errors.Add(new DialogueIssue(lineNumber, "شرط خالی است"));
                return null;
            }

            Condition condition;
            string error;

            if (ConditionParser.TryParse(source, out condition, out error)) return condition;

            document.Errors.Add(new DialogueIssue(lineNumber, "شرط نامعتبر: " + error));
            return null;
        }

        private static string FirstToken(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            string[] parts = value.Trim().Split(SpaceSplit, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? string.Empty : parts[0];
        }

        private static List<string> SplitPipe(string value)
        {
            List<string> result = new List<string>();
            string[] parts = (value ?? string.Empty).Split('|');

            for (int i = 0; i < parts.Length; i++)
                result.Add(parts[i].Trim());

            return result;
        }
    }
}
