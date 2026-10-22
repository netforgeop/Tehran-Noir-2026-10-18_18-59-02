using System.Collections.Generic;

namespace TehranNoir.Dialogue
{
    /// یک متن دوزبانه. اگر یک زبان خالی باشد، زبان دیگر نمایش داده می‌شود.
    public struct LocalizedText
    {
        public string Fa;
        public string En;

        public LocalizedText(string fa, string en)
        {
            Fa = fa ?? string.Empty;
            En = en ?? string.Empty;
        }

        public string Get(bool persian)
        {
            if (persian) return string.IsNullOrEmpty(Fa) ? En : Fa;
            return string.IsNullOrEmpty(En) ? Fa : En;
        }
    }

    public sealed class PersonInfo
    {
        public string Id = string.Empty;
        public LocalizedText Name;
        public LocalizedText Role;
    }

    /// پایه‌ی همه‌ی خطوط یک گره.
    public abstract class DialogueItem
    {
        public int Line;
        public List<Condition> Guard = new List<Condition>();   // شرط‌های چسبان [if]
        public string GuardSource = string.Empty;

        public virtual void CollectConditions(List<Condition> result)
        {
            if (Guard != null) result.AddRange(Guard);
        }
    }

    /// [say] گوینده | فارسی | English
    public sealed class SayItem : DialogueItem
    {
        public string Speaker = string.Empty;
        public LocalizedText Text;
    }

    /// [opt] فارسی | English -> مقصد
    public sealed class OptionItem : DialogueItem
    {
        public LocalizedText Text;
        public string Target = string.Empty;
    }

    /// [go] مقصد
    public sealed class GoItem : DialogueItem
    {
        public string Target = string.Empty;
    }

    /// [if] شرط -> مقصد  (پرش شرطی؛ شرط خودش هم ارزیابی می‌شود)
    public sealed class RedirectItem : DialogueItem
    {
        public Condition When;
        public string WhenSource = string.Empty;
        public string Target = string.Empty;

        public override void CollectConditions(List<Condition> result)
        {
            base.CollectConditions(result);
            if (When != null) result.Add(When);
        }
    }

    /// [if] شرط  (شرط چسبان برای خطوط بعدی)
    public sealed class IfItem : DialogueItem
    {
        public Condition When;
        public string WhenSource = string.Empty;

        public override void CollectConditions(List<Condition> result)
        {
            base.CollectConditions(result);
            if (When != null) result.Add(When);
        }
    }

    /// [else] پایان شرط چسبان
    public sealed class ElseItem : DialogueItem
    {
    }

    /// [do] rel.reza +5
    public sealed class EffectItem : DialogueItem
    {
        public string Source = string.Empty;
    }

    /// [end]
    public sealed class EndItem : DialogueItem
    {
    }

    public sealed class DialogueLink
    {
        public string Target = string.Empty;
        public int Line;
        public bool IsOption;
        public bool IsAutomatic;
    }

    public sealed class DialogueNode
    {
        public string Id = string.Empty;
        public string BaseId = string.Empty;
        public int Line;
        public bool HasTerminator;
        public readonly List<DialogueItem> Items = new List<DialogueItem>();
        public readonly List<DialogueLink> Links = new List<DialogueLink>();
    }

    public sealed class DialogueIssue
    {
        public readonly int Line;
        public readonly string Message;

        public DialogueIssue(int line, string message)
        {
            Line = line;
            Message = message;
        }

        public override string ToString()
        {
            return "خط " + Line + ": " + Message;
        }
    }

    public sealed class DialogueDocument
    {
        public string File = string.Empty;
        public readonly PersonInfo Person = new PersonInfo();
        public readonly List<string> Cast = new List<string>();
        public readonly List<DialogueNode> Nodes = new List<DialogueNode>();
        public readonly List<DialogueIssue> Errors = new List<DialogueIssue>();
        public readonly List<DialogueIssue> Warnings = new List<DialogueIssue>();
        public readonly List<string> Variables = new List<string>();

        public bool HasErrors { get { return Errors.Count > 0; } }

        public DialogueNode FindNode(string id)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].Id == id) return Nodes[i];
            return null;
        }
    }
}
