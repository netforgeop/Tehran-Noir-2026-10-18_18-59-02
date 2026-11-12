using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Debugging
{
    /// <summary>
    /// پنل تست را خودکار می‌سازد: هیچ آبجکتی در صحنه لازم نیست، هیچ فیلدی هم دستی پر نمی‌شود.
    /// فقط جایی اجرا می‌شود که WorldStateProvider باشد (یعنی صحنه‌ی بازی، نه منوی اصلی).
    ///
    /// در Build نهایی وجود ندارد؛ فقط Editor و Development Build.
    /// </summary>
    public static class CheatBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Object.FindAnyObjectByType<CheatPanel>(FindObjectsInactive.Include) != null) return;
            if (Object.FindAnyObjectByType<WorldStateProvider>(FindObjectsInactive.Include) == null) return;

            GameObject host = new GameObject("Cheat Panel (runtime)");
            host.AddComponent<CheatPanel>();

            Debug.Log("[Cheat] پنل تست آماده است: F10");
#endif
        }
    }
}
