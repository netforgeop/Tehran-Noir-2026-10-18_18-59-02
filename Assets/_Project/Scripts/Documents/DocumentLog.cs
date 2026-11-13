using System;
using TehranNoir.Dialogue;

namespace TehranNoir.Documents
{
    /// <summary>
    /// دفتر «کاغذهایی که دیده‌ای».
    ///
    /// هر سندی که یک بار روی صفحه بیاید، اینجا یک نشانه در WorldState می‌گذارد:
    ///
    ///     flag.seen.&lt;نام فایل&gt; = true
    ///
    /// پس جای دیگری لازم نیست فهرست نگه دارد: تب پرونده‌ها همین نشانه‌ها را
    /// می‌خواند و می‌داند چه چیزی پیدا شده. مزیتش این است که هر مسیری که سند را
    /// نشان دهد — تعامل روی میز، منوی ادیتور، پنل تست — خودکار ثبت می‌شود؛
    /// هیچ صحنه‌ای نباید یادش باشد چیزی را علامت بزند.
    ///
    /// چرا با پیشوند `flag.`؟ چون همان پیشوند شناخته‌شده‌ی دنیاست و پارسرها
    /// آن را می‌شناسند؛ پیشوند تازه یعنی دست‌زدن به `WorldState.IsKnownVariable`
    /// و به ابزار گیت، بدون هیچ سود واقعی.
    /// </summary>
    public static class DocumentLog
    {
        /// <summary>پیشوند کلیدها در WorldState.</summary>
        public const string Prefix = "flag.seen.";

        /// <summary>
        /// نام فایل سند → کلید دنیا. پسوند، مسیر و حروف بزرگ حذف می‌شوند تا
        /// «Documents/note_yazdani.txt» و «note_yazdani» یک کلید بدهند.
        /// </summary>
        public static string KeyOf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;

            string name = fileName.Trim();

            int slash = name.LastIndexOf('/');
            if (slash >= 0 && slash < name.Length - 1)
                name = name.Substring(slash + 1);

            int backslash = name.LastIndexOf('\\');
            if (backslash >= 0 && backslash < name.Length - 1)
                name = name.Substring(backslash + 1);

            if (name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            name = name.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');

            if (name.Length == 0) return string.Empty;

            return Prefix + name;
        }

        /// <summary>ثبت اینکه این سند دیده شد. اگر دنیا نباشد، بی‌صدا رد می‌شود.</summary>
        public static void Note(DocumentText document, WorldState world)
        {
            if (document == null) return;

            string key = KeyOf(document.FileName);

            if (key.Length <= Prefix.Length) return;

            if (world == null && WorldStateProvider.Instance != null)
                world = WorldStateProvider.Instance.State;

            if (world == null) return;

            world.Set(key, true);
        }

        /// <summary>آیا این سند قبلاً باز شده؟</summary>
        public static bool Seen(string fileName, WorldState world)
        {
            string key = KeyOf(fileName);

            if (key.Length <= Prefix.Length) return false;

            if (world == null && WorldStateProvider.Instance != null)
                world = WorldStateProvider.Instance.State;

            if (world == null) return false;

            return world.Get(key).Truthy();
        }

        public static bool Seen(DocumentText document, WorldState world)
        {
            if (document == null) return false;

            return Seen(document.FileName, world);
        }
    }
}
