using System.Collections.Generic;
using TehranNoir.Dialogue;

namespace TehranNoir.Quests
{
    /// <summary>نوع کار. «اصلی» خط داستان است، «جانبی» کارهای گوشه‌ی نقشه.</summary>
    public enum QuestKind
    {
        Main,
        Side
    }

    /// <summary>یک قدم از کار: یک جمله که به بازیکن می‌گوید همین حالا چه کار کند.</summary>
    public sealed class QuestStep
    {
        public string Id = string.Empty;
        public string Persian = string.Empty;
        public string English = string.Empty;

        public string WhenSource = string.Empty;
        public Condition When;

        public string Text(bool persian)
        {
            string chosen = persian ? Persian : English;
            if (!string.IsNullOrEmpty(chosen)) return chosen;

            return persian ? English : Persian;
        }
    }

    /// <summary>
    /// یک کار کامل، خوانده‌شده از فایل متنی.
    /// وضعیتش از WorldState می‌آید: «شروع شده» و «تمام شده» دو شرط‌اند،
    /// و قدم فعلی اولین قدمی است که شرطش برقرار است.
    /// </summary>
    public sealed class QuestDefinition
    {
        public string Id = string.Empty;
        public string Persian = string.Empty;
        public string English = string.Empty;
        public string SummaryPersian = string.Empty;
        public string SummaryEnglish = string.Empty;

        public QuestKind Kind = QuestKind.Main;
        public bool Tracked = true;
        public int Order;

        public string StartSource = "true";
        public string DoneSource = string.Empty;

        public Condition Start;
        public Condition Done;

        public readonly List<QuestStep> Steps = new List<QuestStep>();

        public string Name(bool persian)
        {
            string chosen = persian ? Persian : English;
            if (!string.IsNullOrEmpty(chosen)) return chosen;

            return persian ? English : Persian;
        }

        public bool IsStarted(WorldState world)
        {
            if (Start == null) return true;
            return Start.IsTrue(world);
        }

        public bool IsDone(WorldState world)
        {
            if (Done == null) return false;
            return Done.IsTrue(world);
        }

        /// <summary>
        /// قدم فعلی: اولین قدمی که شرطش برقرار است. اگر هیچ‌کدام برقرار نبود،
        /// قدم آخر می‌ماند تا کار ناگهان بی‌متن نشود.
        /// </summary>
        public QuestStep CurrentStep(WorldState world)
        {
            if (Steps.Count == 0) return null;

            for (int i = 0; i < Steps.Count; i++)
            {
                QuestStep step = Steps[i];
                if (step.When == null) continue;

                if (step.When.IsTrue(world)) return step;
            }

            return Steps[Steps.Count - 1];
        }
    }

    /// <summary>همه‌ی کارهای یک فایل یا یک پوشه.</summary>
    public sealed class QuestBook
    {
        public readonly List<QuestDefinition> Quests = new List<QuestDefinition>();
        public readonly List<string> Issues = new List<string>();

        public bool IsEmpty { get { return Quests.Count == 0; } }
    }
}
