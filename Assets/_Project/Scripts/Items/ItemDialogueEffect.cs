using System;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// گرامر تحویل در دیالوگ:
    ///
    ///     [do] item.give.&lt;شناسه‌ی آیتم&gt; = &lt;شناسه‌ی ظرف&gt;
    ///     [do] item.give.file.sealed = archive
    ///
    /// چرا لازم است؟ قبل از این، دیالوگ فقط `flag.file.taken = false` می‌نوشت؛
    /// یعنی «داستان فکر می‌کرد پرونده تحویل داده شده» ولی خودِ پرونده در «وسایل»
    /// بازیکن می‌ماند. حالا همان نمونه (با مهرِ شکسته و خوانده‌شدنش) از جیب
    /// بیرون می‌رود و در ظرف مقصد می‌نشیند.
    ///
    /// این کلاس در لایه‌ی آیتم‌ها زندگی می‌کند و خودش را به قلاب موتور دیالوگ
    /// وصل می‌کند؛ پس `DialogueRunner` هیچ `using`ای از آیتم‌ها ندارد.
    ///
    /// ظرف مقصد اگر نباشد ساخته می‌شود (مثل «بایگانی» یا «میز رضا») و با کلید
    /// ترجمه‌ی `item.container.&lt;شناسه&gt;` برچسب می‌خورد.
    /// </summary>
    public static class ItemDialogueEffect
    {
        public const string GivePrefix = "item.give.";

        /// <summary>
        /// وصل‌کردن قلاب. هم موقع شروع بازی خودکار اجرا می‌شود، هم تست
        /// خودکار صدایش می‌زند تا اگر با پیکربندی «بدون domain reload» اجرا شد
        /// هم مطمئن باشیم قلاب هست.
        /// </summary>
        public static void Install()
        {
            DialogueRunner.ExternalEffect = Handle;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            // اگر Play Mode Options روشن باشد، استاتیک‌ها بین دو Play زنده می‌مانند؛
            // پس اول پاک می‌کنیم و بعد در مرحله‌ی بعدی دوباره وصل می‌شود.
            DialogueRunner.ExternalEffect = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallOnLoad()
        {
            Install();
        }

        /// <summary>
        /// اگر متن با `item.give.` شروع شود، همان‌جا اجرا و `true` برمی‌گردد.
        /// بقیه‌ی اثرها دست‌نخورده به گرامر عادی `[do]` می‌روند.
        /// </summary>
        public static bool Handle(string effect, WorldState world)
        {
            if (string.IsNullOrEmpty(effect)) return false;

            string text = effect.Trim();

            if (text.Length == 0 || !text.StartsWith(GivePrefix, StringComparison.Ordinal)) return false;

            int sign = text.IndexOf('=');

            if (sign <= GivePrefix.Length)
            {
                Debug.LogError("[Items] تحویل بدخط: «" + text +
                               "» — باید به شکل item.give.<آیتم> = <ظرف> باشد.");
                return true;
            }

            string id = text.Substring(GivePrefix.Length, sign - GivePrefix.Length).Trim();
            string container = text.Substring(sign + 1).Trim();

            if (id.Length == 0 || container.Length == 0)
            {
                Debug.LogError("[Items] تحویل بی‌مقصد: «" + text + "» — شناسه‌ی آیتم یا ظرف خالی است.");
                return true;
            }

            // ظرف مقصد اگر نبود ساخته می‌شود؛ «بایگانی» باید از قبل وجود داشته باشد
            // تا بعد از تحویل هم همان‌جا بماند، نه اینکه هر بار ظرف تازه شود.
            InventoryManager.Ensure(container, "item.container." + ItemCatalog.Normalize(container));

            ItemResult result = InventoryManager.Give(id, container);

            if (result.Ok)
            {
                Debug.Log("[Items] تحویل شد: " + id + " → " + container);
            }
            else
            {
                // داستان نباید بشکند؛ فقط در Console می‌نویسیم که چرا جابه‌جا نشد.
                Debug.LogWarning("[Items] تحویل «" + id + "» به «" + container +
                                 "» انجام نشد: " + result.Outcome + " — «" + result.ReasonKey + "»");
            }

            return true;
        }
    }
}
