using UnityEngine;
using UnityEngine.SceneManagement;
using TehranNoir.Dialogue;
using TehranNoir.UI;

namespace TehranNoir.World
{
    /// <summary>
    /// در یا آسانسوری که صحنه را عوض می‌کند.
    ///
    /// چرا یک کامپوننت جدا؟ چون تا امروز هتل و راهروی طبقه‌ی هشتم دو فایل جدا
    /// بودند و هیچ راهی بین‌شان نبود: کار می‌گفت «با مرادی در ۸۱۴ حرف بزن» و
    /// بازیکن در لابی ایستاده بود. این کامپوننت همان راه است.
    ///
    /// سه کار می‌کند، به همین ترتیب:
    ///   ۱. خط‌های `effects` را در دنیا می‌نویسد (همان نحو `[do]` دیالوگ).
    ///   ۲. `entry.from` را می‌نویسد تا صحنه‌ی مقصد بداند بازیکن از کدام در آمد
    ///      و او را همان‌جا بگذارد، نه سر جای پیش‌فرضِ صحنه.
    ///   ۳. پرده را سیاه می‌کند و صحنه را بارگذاری می‌کند.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneDoor : Interaction.Interactable
    {
        [Header("مقصد")]
        [Tooltip("نام صحنه؛ باید در Build Settings باشد")]
        [SerializeField] private string targetScene = "Hilton814";

        [Tooltip("با این شناسه در صحنه‌ی مقصد پیدا می‌شود. خالی = مکان پیش‌فرض صحنه")]
        [SerializeField] private string entryId = "hotel_lift";

        [Header("دنیا")]
        [Tooltip("هر خط مثل [do] دیالوگ:  flag.hilton.floor = 8")]
        [SerializeField] private string[] effects = new string[0];

        [Header("پرده")]
        [SerializeField, Min(0f)] private float fadeSeconds = 0.45f;

        [Tooltip("در بازی، بعد از رفتن، این آبجکت نابود می‌شود (صحنه عوض می‌شود)")]
        [SerializeField] private bool logDeparture = true;

        private bool _leaving;

        // فعل این در «ورود» است، نه «استفاده»
        public override Interaction.InteractionAction Action =>
            Interaction.InteractionAction.Enter;

        public override void Interact(GameObject interactor)
        {
            if (_leaving) return;

            if (string.IsNullOrWhiteSpace(targetScene))
            {
                Debug.LogWarning("[World] SceneDoor روی «" + name + "» صحنه‌ی مقصد ندارد.", this);
                return;
            }

            _leaving = true;

            ApplyEffects();
            RememberEntry();

            if (logDeparture)
                Debug.Log("[World] رفتن به " + targetScene + " از " + name, this);

            if (ScreenFade.Instance != null)
                ScreenFade.Instance.FadeOut(fadeSeconds, Load);
            else
                Load();
        }

        private void Load()
        {
            SceneManager.LoadScene(targetScene);
        }

        private void ApplyEffects()
        {
            if (WorldStateProvider.Instance == null) return;

            for (int i = 0; i < effects.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(effects[i])) continue;

                DialogueRunner.ApplyEffect(effects[i], WorldStateProvider.Instance.State);
            }
        }

        /// <summary>
        /// `entry.from` یک رشته است، نه پرچم. صحنه‌ی مقصد با همان مقایسه‌ی
        /// متنیِ شرط‌های دیالوگ می‌خواندش: `entry.from == hotel_lift`.
        /// </summary>
        private void RememberEntry()
        {
            if (WorldStateProvider.Instance == null) return;

            WorldStateProvider.Instance.Set("entry.from", entryId ?? string.Empty);
        }
    }
}
