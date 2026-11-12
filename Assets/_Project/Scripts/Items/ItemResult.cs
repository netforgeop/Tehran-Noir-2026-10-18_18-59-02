namespace TehranNoir.Items
{
    /// <summary>نتیجه‌ی یک کار روی آیتم.</summary>
    public enum ItemOutcome
    {
        Success = 0,
        Invalid = 1,
        NotFound = 2,
        NotAllowed = 3,
        ConditionFailed = 4,
        TargetMismatch = 5,
        ContainerFull = 6,
        NothingToDo = 7
    }

    /// <summary>
    /// نتیجه‌ی یک تراکنش. هیچ‌جا فقط `bool` برنمی‌گردانیم، چون رابط کاربری باید
    /// بداند چرا نشد و همان دلیل را ترجمه کند، نه یک پیام کلی بسازد.
    ///
    /// `ReasonKey` یک کلید ترجمه است، نه جمله؛ جمله از Localization می‌آید.
    /// </summary>
    public sealed class ItemResult
    {
        public readonly bool Ok;

        public readonly ItemOutcome Outcome;

        public readonly string ReasonKey;

        public readonly ItemVerb Verb;

        private ItemResult(bool ok, ItemOutcome outcome, string reasonKey, ItemVerb verb)
        {
            Ok = ok;
            Outcome = outcome;
            ReasonKey = reasonKey ?? string.Empty;
            Verb = verb;
        }

        public static ItemResult Success(ItemVerb verb = ItemVerb.None)
        {
            return new ItemResult(true, ItemOutcome.Success, string.Empty, verb);
        }

        public static ItemResult Fail(ItemOutcome outcome, string reasonKey, ItemVerb verb = ItemVerb.None)
        {
            return new ItemResult(false, outcome, reasonKey, verb);
        }

        public override string ToString()
        {
            return Ok ? "ok" : "fail(" + Outcome + ")";
        }
    }
}
