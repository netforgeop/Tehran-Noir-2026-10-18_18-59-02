using System.Collections;
using TehranNoir.Settings;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// موسیقی صحنه. با فید نرم شروع می‌شود، لوپ می‌شود و در خروج از صحنه
    /// خودش را قطع می‌کند. حجم نهایی از کانال «موسیقی» در تنظیمات می‌آید.
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

        /// <summary>حجم پایه ضربدر کانال موسیقی در تنظیمات.</summary>
        private float TargetVolume => volume * GameSettings.SourceVolume(GameSettings.Band.Music);

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;   // دو بعدی؛ با دوربین جابه‌جا نمی‌شود
            _source.volume = 0f;
        }

        private void OnEnable()
        {
            GameSettings.VolumeChanged += OnVolumeChanged;
        }

        private void OnDisable()
        {
            GameSettings.VolumeChanged -= OnVolumeChanged;
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
                _source.volume = Mathf.Lerp(0f, TargetVolume, elapsed / fadeInSeconds);
                yield return null;
            }

            _source.volume = TargetVolume;
            _fade = null;
        }

        /// <summary>اگر کاربر اسلایدر موسیقی را عوض کرد، همان لحظه اثر کند.</summary>
        private void OnVolumeChanged()
        {
            if (_source == null || _fade != null) return;

            _source.volume = TargetVolume;
        }

        private void OnDestroy()
        {
            if (_fade != null) StopCoroutine(_fade);

            if (!stopOnSceneChange || _source == null) return;

            _source.Stop();
        }

        /// <summary>
        /// محو شدن و قطع تدریجی؛ برای خروج از منو.
        /// بدون این، با عوض‌شدن صحنه صدا یک‌دفعه قطع می‌شود و تو گوش می‌خورد.
        /// </summary>
        public void FadeOut(float seconds)
        {
            if (_source == null) return;

            if (_fade != null) StopCoroutine(_fade);

            _fade = StartCoroutine(FadeOutRoutine(Mathf.Max(0.01f, seconds)));
        }

        private IEnumerator FadeOutRoutine(float seconds)
        {
            float from = _source.volume;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;

                _source.volume = Mathf.Lerp(from, 0f, elapsed / seconds);
                yield return null;
            }

            _source.volume = 0f;
            _source.Stop();

            _fade = null;
        }

        /// <summary>برای تغییر حجم از بیرون.</summary>
        public void SetVolume(float target)
        {
            volume = Mathf.Clamp01(target);
            if (_source != null && _fade == null) _source.volume = TargetVolume;
        }
    }
}
