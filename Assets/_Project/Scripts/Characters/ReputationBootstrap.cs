using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Characters
{
    /// <summary>
    /// آبرو را در صحنه‌های بازی زنده نگه می‌دارد.
    ///
    /// چرا خودکار و چرا با فاصله؟ چون هیچ صحنه‌ای نباید یادش باشد این کامپوننت
    /// را بگذارد، و چون بررسی چند شرط در هر فریم لازم نیست: نیم‌ثانیه کافی
    /// است و تغییرها همان‌جا در HUD دیده می‌شوند.
    ///
    /// زمان با `unscaledDeltaTime` جلو می‌رود، پس وقتی منوی توقف بازی را
    /// متوقف کرده هم آبرو به‌روز می‌شود — وگرنه برچسب تب کاراکتر تا لحظه‌ی
    /// بستن منو کهنه می‌ماند.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReputationBootstrap : MonoBehaviour
    {
        private const float Interval = 0.5f;

        private float _elapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindAnyObjectByType<ReputationBootstrap>() != null) return;
            if (Object.FindAnyObjectByType<WorldStateProvider>() == null) return;

            GameObject host = new GameObject("Reputation System");
            host.AddComponent<ReputationBootstrap>();
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            if (_elapsed < Interval) return;

            _elapsed = 0f;

            WorldStateProvider provider = WorldStateProvider.Instance;

            if (provider == null) return;

            Reputation.Tick(provider.State);
        }
    }
}
