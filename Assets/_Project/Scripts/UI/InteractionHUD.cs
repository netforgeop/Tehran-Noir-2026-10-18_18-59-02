using TMPro;
using UnityEngine;
using TehranNoir.Core;
using TehranNoir.Inputs;
using TehranNoir.Interaction;

namespace TehranNoir.UI
{
    /// <summary>
    /// کارت گوشه‌ی راست-پایین: دکمه، کار، و یک خط توضیح.
    /// متن فقط وقتی هدف یا دستگاه ورودی عوض شود بازنویسی می‌شود.
    /// </summary>
    public class InteractionHUD : MonoBehaviour
    {
        [SerializeField] private InteractionSensor sensor;
        [SerializeField] private PlayerInputReader input;

        [Header("Visual")]
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private float fadeSpeed = 12f;

        private Interactable lastTarget;
        private bool lastUsingGamepad;

        private void Update()
        {
            if (sensor == null || input == null) return;

            Interactable current = sensor.Current;
            bool usingGamepad = input.UsingGamepad;

            // فقط در صورت تغییر می‌نویسیم؛ TMP برای هر نوشتن، مِش از نو می‌سازد
            if (current != lastTarget || usingGamepad != lastUsingGamepad)
            {
                lastTarget = current;
                lastUsingGamepad = usingGamepad;

                if (current != null)
                {
                    keyText.text = Localization.KeyHint(usingGamepad);
                    actionText.text = current.Prompt;
                    detailText.text = current.Detail;
                }
            }

            float targetAlpha = current != null ? 1f : 0f;
            panel.alpha = Mathf.MoveTowards(panel.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }

        /// <summary>بعد از عوض کردن زبان بازی صدا بزن.</summary>
        public void Refresh()
        {
            lastTarget = null;
        }
    }
}