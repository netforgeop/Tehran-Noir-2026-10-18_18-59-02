using UnityEngine;

namespace TehranNoir.Settings
{
    /// <summary>
    /// صدایی که به یک کانال تنظیمات وصل است. روی همان آبجکتی بنشیند که AudioSource دارد.
    /// حجم نهایی = حجم پایه × حجم کانال. برای FX و دیالوگ همین کافی است.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ChannelledAudio : MonoBehaviour
    {
        [Tooltip("کانال صدا در تنظیمات")]
        [SerializeField] private GameSettings.Band band = GameSettings.Band.FX;

        [Tooltip("حجم پایه‌ی همین صدا؛ کانال تنظیمات رویش ضرب می‌شود")]
        [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            if (baseVolume <= 0f) baseVolume = Mathf.Max(0.01f, _source.volume);

            Apply();
        }

        private void OnEnable()
        {
            GameSettings.VolumeChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.VolumeChanged -= Apply;
        }

        public void SetBaseVolume(float value)
        {
            baseVolume = Mathf.Clamp01(value);
            Apply();
        }

        private void Apply()
        {
            if (_source != null) _source.volume = baseVolume * GameSettings.SourceVolume(band);
        }
    }
}
