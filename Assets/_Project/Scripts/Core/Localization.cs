using TehranNoir.Interaction;
using TehranNoir.Settings;

namespace TehranNoir.Core
{
    /// <summary>
    /// متن‌های رابط که به سیستم ترجمه وصل‌اند.
    /// قبلاً یک زبان ثابت داشت و با عوض‌شدن زبان منو هم‌قدم نمی‌شد؛ حالا از Loc می‌خواند.
    /// </summary>
    public static class Localization
    {
        public static bool Persian => Loc.IsRtl(Loc.Channel.Menu);

        /// <summary>متن دکمه‌ی تعامل، از کلیدهای action.* در فایل زبان.</summary>
        public static string Action(InteractionAction action)
        {
            return Loc.Get(Loc.Channel.Menu, "action." + action.ToString().ToLowerInvariant());
        }

        public static string Get(string key)
        {
            return Loc.Get(Loc.Channel.Menu, key);
        }

        /// <summary>
        /// متن دوزبانه در یک فیلد Inspector: «فارسی|English».
        /// یک زبان یعنی متن ساده. این‌طوری اسم آبجکت‌ها هم با زبان بازی عوض می‌شود.
        /// </summary>
        public static string Pick(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            int bar = raw.IndexOf('|');
            if (bar < 0) return raw;

            return Persian ? raw.Substring(0, bar).Trim() : raw.Substring(bar + 1).Trim();
        }

        /// <summary>برچسب دکمه؛ بعداً از روی بایندینگ واقعی خوانده می‌شود.</summary>
        public static string KeyHint(bool gamepad)
        {
            return gamepad ? "X" : "E";
        }
    }
}
