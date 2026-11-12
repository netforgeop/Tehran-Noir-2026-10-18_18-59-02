using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// یک نقطه‌ی دسترسی به دنیای بازی برای سیستم آیتم.
    ///
    /// چرا جدا؟ چون تست باید بتواند یک `WorldState` موقت بدهد بدون اینکه
    /// صحنه یا `WorldStateProvider` لازم باشد. در بازی واقعی همان provider
    /// صحنه برمی‌گردد؛ در تست، `Override`.
    /// </summary>
    public static class ItemWorld
    {
        /// <summary>فقط در تست پر می‌شود؛ در بازی null است.</summary>
        public static WorldState Override;

        /// <summary>دنیای فعلی یا null اگر هنوز providerی در صحنه نیست.</summary>
        public static WorldState Current
        {
            get
            {
                if (Override != null) return Override;

                WorldStateProvider provider = WorldStateProvider.Instance;

                return provider != null ? provider.State : null;
            }
        }

        public static void ClearOverride()
        {
            Override = null;
        }
    }
}
