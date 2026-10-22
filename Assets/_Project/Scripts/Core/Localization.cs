using TehranNoir.Interaction;

namespace TehranNoir.Core
{
    public enum Language
    {
        English,
        Persian
    }

    /// <summary>
    /// تنها جایی که متن UI تعریف می‌شود.
    /// برای اضافه کردن متن یا زبان جدید، فقط همین فایل باز می‌شود.
    /// </summary>
    public static class Localization
    {
        public static Language Current = Language.Persian;

        public static string Action(InteractionAction action)
        {
            switch (action)
            {
                case InteractionAction.Use: return Current == Language.Persian ? "استفاده" : "Use";
                case InteractionAction.Open: return Current == Language.Persian ? "باز کردن" : "Open";
                case InteractionAction.Close: return Current == Language.Persian ? "بستن" : "Close";
                case InteractionAction.Talk: return Current == Language.Persian ? "صحبت" : "Talk";
                case InteractionAction.Take: return Current == Language.Persian ? "برداشتن" : "Take";
                case InteractionAction.Read: return Current == Language.Persian ? "خواندن" : "Read";
                case InteractionAction.Enter: return Current == Language.Persian ? "سوار شدن" : "Enter";
                case InteractionAction.Drive: return Current == Language.Persian ? "رانندگی" : "Drive";
                default: return action.ToString();
            }
        }

        /// <summary>برچسب دکمه؛ بعداً از روی بایندینگ واقعی خوانده می‌شود.</summary>
        public static string KeyHint(bool gamepad) => gamepad ? "X" : "E";
    }
}