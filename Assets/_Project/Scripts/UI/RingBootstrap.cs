using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.UI
{
    /// <summary>
    /// کمانِ استقامت/جان را خودکار می‌سازد: نه آبجکتی در صحنه، نه فیلدی دستی.
    /// مثل بقیه‌ی HUDها، در صحنه‌ی بازی (جایی که WorldStateProvider هست) و نه در منوی اصلی.
    /// </summary>
    public static class RingBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindAnyObjectByType<StatRingHud>(FindObjectsInactive.Include) != null) return;
            if (Object.FindAnyObjectByType<WorldStateProvider>(FindObjectsInactive.Include) == null) return;

            GameObject host = new GameObject("Stat Ring (runtime)");
            host.AddComponent<StatRingHud>();
        }
    }
}
