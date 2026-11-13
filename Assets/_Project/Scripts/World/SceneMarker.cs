using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.World
{
    /// <summary>
    /// نام صحنه را در دنیا می‌نویسد: `place.hotel = true`.
    ///
    /// چرا لازم است؟ چون پرچم‌های دنیا در هر صحنه از صفر ساخته می‌شوند ولی
    /// شرط کارها و دیالوگ‌ها مشترک است. بدون این کلید، کارِ «اولین روز» که
    /// شرطش `true` بود، در هتل هم در گوشه‌ی صفحه می‌آمد و بازیکن را دنبال
    /// رضایی می‌فرستاد که آن صحنه اصلاً نبود.
    ///
    /// یک کلید به ازای هر صحنه، نه بیشتر. هر چیزی که «فقط در این مکان معنی
    /// دارد» با `place.&lt;id&gt;` شرط‌گذاری می‌شود.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneMarker : MonoBehaviour
    {
        [Tooltip("شناسه‌ی صحنه؛ در دنیا به place.<id> تبدیل می‌شود")]
        [SerializeField] private string placeId = "office";

        [Tooltip("در کنسول یک خط بنویس؛ برای تست مفید است، برای بازی نه")]
        [SerializeField] private bool logMarker = true;

        public string PlaceId { get { return placeId; } }

        private void Start()
        {
            WorldStateProvider provider = WorldStateProvider.Instance;

            if (provider == null)
            {
                Debug.LogWarning("[World] SceneMarker روی «" + name +
                                 "» است ولی WorldStateProvider در صحنه نیست؛ place." +
                                 placeId + " نوشته نشد.", this);
                return;
            }

            provider.Set("place." + placeId, true);

            if (logMarker) Debug.Log("[World] صحنه: " + placeId);
        }
    }
}
