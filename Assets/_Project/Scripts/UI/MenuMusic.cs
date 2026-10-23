using System.Collections;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// موسیقی صحنه. با فید نرم شروع می‌شود، لوپ می‌شود و در خروج از صحنه
    /// خودش را قطع می‌کند. هم برای منو، هم بعداً برای صحنه‌های بازی.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusic : MonoBehaviour
    {
        [Header("آهنگ")]
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        [Header("فید")]
        [Tooltip("چند ثانیه طول بکشد تا صدا به حجم کامل برسد")]
        [SerializeField] private float fadeInSeconds = 2.5f;

        [Tooltip("اگر صحنه عوض شد، صدا قطع شود")]
        [SerializeField] private bool stopOnSceneChange = true;

        private AudioSource _source;
        private Coroutine _fade;

        public AudioSource Source { get { return _source; } }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;   // دو بعدی؛ با دوربین جابه‌جا نمی‌شود
            _source.volume = 0f;
        }

        private void Start()
        {
            if (clip == null)
            {
                Debug.LogWarning("[Music] هیچ AudioClip انتخاب نشده.");
                return;
            }

            _source.clip = clip;
            _source.Play();

            _fade = StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _source.volume = Mathf.Lerp(0f, volume, elapsed / fadeInSeconds);
                yield return null;
            }

            _source.volume = volume;
            _fade = null;
        }

        private void OnDestroy()
        {
            if (_fade != null) StopCoroutine(_fade);

            if (!stopOnSceneChange || _source == null) return;

            _source.Stop();
        }

        /// <summary>برای منوی توقف: موسیقی را کم یا زیاد کن.</summary>
        public void SetVolume(float target)
        {
            volume = Mathf.Clamp01(target);
            if (_source != null) _source.volume = volume;
        }
    }
}
