using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Quests
{
    /// <summary>
    /// سیستم کارها را در صحنه‌های بازی خودکار بالا می‌آورد.
    ///
    /// چرا؟ چون هر آبجکت دستی در صحنه یعنی یک قدم دیگر که ممکن است فراموش شود.
    /// این کلاس فقط در صحنه‌ای کار می‌کند که WorldStateProvider دارد؛
    /// یعنی منوی اصلی و صحنه‌های بی‌دنیا دست نمی‌خورند.
    /// اگر خودت یک QuestLog در صحنه بگذاری، دوتایی ساخته نمی‌شود.
    /// </summary>
    public static class QuestBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindAnyObjectByType<QuestLog>() != null) return;
            if (Object.FindAnyObjectByType<WorldStateProvider>() == null) return;

            GameObject host = new GameObject("Quest System");
            host.AddComponent<QuestLog>();
            host.AddComponent<QuestTracker>();

            Debug.Log("[Quest] سیستم کارها خودکار ساخته شد.");
        }
    }
}
