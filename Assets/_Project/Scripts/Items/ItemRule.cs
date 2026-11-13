using System;
using System.Collections.Generic;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// یک «قاعده‌ی» آیتم و همه‌ی رفتار نقش‌محور بازی.
    ///
    /// قاعده = فعل + شرط + اثرها. هر سه‌تایشان از داده می‌آیند:
    ///
    ///   read.if = state.sealed = 0
    ///   read.do = state.read = 1; flag.letter.read = true
    ///   read.consume = 0
    ///
    /// شرط با همان گرامر دیالوگ نوشته می‌شود (`[if]`)، پس هر شرطی که در
    /// دیالوگ کار می‌کند — `rel.reza > 10`، `item.key.archive >= 1`،
    /// `known.reza.secret = true` — اینجا هم کار می‌کند. اثرها هم با همان
    /// گرامر `[do]` نوشته می‌شوند و با `;` جدا می‌شوند.
    ///
    /// هیچ کلاس C# به‌ازای هر آیتم یا هر دسته ساخته نمی‌شود.
    /// </summary>
    public sealed class ItemRule
    {
        public readonly ItemVerb Verb;

        /// <summary>متن اصلی شرط؛ برای لاگ و ابزار بررسی. خالی = همیشه.</summary>
        public readonly string WhenSource;

        /// <summary>شرط پارس‌شده؛ null یعنی همیشه.</summary>
        public readonly Condition When;

        /// <summary>اثرها به ترتیب نوشتن؛ هر کدام یک جمله مثل `flag.x = true`.</summary>
        public readonly string[] Effects;

        /// <summary>آیا با اجرا یک عدد از این آیتم کم شود؟</summary>
        public readonly bool Consume;

        /// <summary>قفل/هدف لازم؛ خالی = هر هدفی. برای کلید و در و ماشین.</summary>
        public readonly string TargetId;

        /// <summary>شماره‌ی قاعده در فایل؛ قاعده‌های هم‌فعل به ترتیب بررسی می‌شوند.</summary>
        public readonly int Order;

        public ItemRule(ItemVerb verb, string whenSource, Condition when, string[] effects, bool consume, string targetId, int order)
        {
            Verb = verb;
            WhenSource = whenSource ?? string.Empty;
            When = when;
            Effects = effects ?? Empty;
            Consume = consume;
            TargetId = targetId ?? string.Empty;
            Order = order;
        }

        public static readonly string[] Empty = new string[0];

        public bool HasCondition
        {
            get { return When != null; }
        }

        /// <summary>
        /// آیا این قاعده همین حالا اجرا می‌شود؟
        /// view دنیای «شده» است: دنیای واقعی + وضعیت همین نمونه (`state.*`).
        /// </summary>
        public bool Matches(WorldState view, string targetId)
        {
            if (!string.IsNullOrEmpty(TargetId))
            {
                if (!string.Equals(TargetId, targetId ?? string.Empty, StringComparison.Ordinal)) return false;
            }

            return When == null || When.IsTrue(view);
        }

        public bool RequiresTarget
        {
            get { return !string.IsNullOrEmpty(TargetId); }
        }

        public override string ToString()
        {
            return ItemVerbs.Name(Verb) + (HasCondition ? " if " + WhenSource : string.Empty);
        }
    }

    /// <summary>
    /// مجموعه‌ی قاعده‌های یک آیتم. جست‌وجو ارزان است (چند قاعده در هر آیتم).
    /// </summary>
    public sealed class ItemRuleSet
    {
        public static readonly ItemRuleSet Empty = new ItemRuleSet(new List<ItemRule>());

        private readonly List<ItemRule> _rules;

        public ItemRuleSet(List<ItemRule> rules)
        {
            _rules = rules ?? new List<ItemRule>();
            _rules.Sort(Compare);
        }

        public int Count
        {
            get { return _rules.Count; }
        }

        public IReadOnlyList<ItemRule> Rules
        {
            get { return _rules; }
        }

        /// <summary>اولین قاعده‌ای که فعلش می‌خورد و شرطش برقرار است.</summary>
        public ItemRule Find(ItemVerb verb, WorldState view, string targetId)
        {
            for (int i = 0; i < _rules.Count; i++)
            {
                ItemRule rule = _rules[i];

                if (rule.Verb != verb) continue;
                if (!rule.Matches(view, targetId)) continue;

                return rule;
            }

            return null;
        }

        /// <summary>آیا اصلاً قاعده‌ای برای این فعل نوشته شده؟ (برای تشخیص «تعریف نشده»)</summary>
        public bool HasVerb(ItemVerb verb)
        {
            for (int i = 0; i < _rules.Count; i++)
                if (_rules[i].Verb == verb) return true;

            return false;
        }

        private static int Compare(ItemRule left, ItemRule right)
        {
            if (left.Order != right.Order) return left.Order.CompareTo(right.Order);

            return left.Verb.CompareTo(right.Verb);
        }
    }

    /// <summary>
    /// نام جایگزین آیتم بر اساس وضعیت.
    ///
    /// پرونده‌ای که مهرش باز شده نباید هنوز «پرونده‌ی مهرشده» نام داشته باشد.
    /// قاعده‌اش مثل قاعده‌های فعل است، فقط نتیجه‌اش متن است:
    ///
    ///   title.if=state.sealed = 0
    ///   title.fa=پرونده‌ی بازشده
    ///   title.en=Opened file
    ///
    /// اگر شرط برقرار نباشد، نام پایه (ستون دوم و سوم فایل) استفاده می‌شود.
    /// </summary>
    public sealed class ItemTitleVariant
    {
        public readonly Condition When;
        public readonly string WhenSource;
        public readonly string Persian;
        public readonly string English;
        public readonly int Order;

        public ItemTitleVariant(Condition when, string whenSource, string persian, string english, int order)
        {
            When = when;
            WhenSource = whenSource ?? string.Empty;
            Persian = persian ?? string.Empty;
            English = english ?? string.Empty;
            Order = order;
        }

        public bool Matches(WorldState view)
        {
            return When == null || When.IsTrue(view);
        }

        public string Text(bool persian)
        {
            string chosen = persian ? Persian : English;
            if (!string.IsNullOrEmpty(chosen)) return chosen;

            return persian ? English : Persian;
        }

        public bool HasText
        {
            get { return !string.IsNullOrEmpty(Persian) || !string.IsNullOrEmpty(English); }
        }
    }

    /// <summary>یک جفت کلید/مقدار اولیه‌ی وضعیت، مثل `state.sealed = 1`.</summary>
    public sealed class ItemStatePair
    {
        public readonly string Key;
        public readonly string Value;

        public ItemStatePair(string key, string value)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
        }
    }
}
