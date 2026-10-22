using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.World
{
    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rain
    }

    /// هوا: یک مقدار در WorldState می‌نویسد که دیالوگ با آن شرط می‌گذارد.
    public sealed class WorldWeather : MonoBehaviour
    {
        [Header("هوا")]
        [SerializeField] private WeatherType startWeather = WeatherType.Clear;

        [Tooltip("هر چند ثانیه هوا خودش عوض شود؛ 0 = هرگز")]
        [SerializeField] private float changeIntervalSeconds = 240f;

        [Tooltip("در طول دیالوگ هوا عوض نمی‌شود")]
        [SerializeField] private bool freezeDuringDialogue = true;

        [SerializeField] private bool logChanges = true;

        private float _timer;
        private WeatherType _current;

        public WeatherType Current { get { return _current; } }

        private void Start()
        {
            Apply(startWeather);
        }

        private void Update()
        {
            if (changeIntervalSeconds <= 0f) return;
            if (freezeDuringDialogue && DialogueUI.Current != null) return;

            _timer += Time.deltaTime;
            if (_timer < changeIntervalSeconds) return;

            _timer = 0f;
            SetWeather(RandomOther());
        }

        public void SetWeather(WeatherType weather)
        {
            Apply(weather);
        }

        [ContextMenu("تغییر هوا")]
        private void ChangeWeatherNow()
        {
            SetWeather(RandomOther());
        }

        private WeatherType RandomOther()
        {
            WeatherType next = _current;

            for (int i = 0; i < 8 && next == _current; i++)
                next = (WeatherType)Random.Range(0, 3);

            return next;
        }

        private void Apply(WeatherType weather)
        {
            _current = weather;
            _timer = 0f;

            WorldStateProvider provider = WorldStateProvider.Instance;
            if (provider == null)
            {
                Debug.LogWarning("[World] WorldStateProvider در صحنه نیست؛ هوا جایی برای نوشتن ندارد.");
                return;
            }

            // نام enum با حروف کوچک: clear / cloudy / rain
            provider.Set("weather", weather.ToString().ToLowerInvariant());

            if (logChanges) Debug.Log("[World] هوا: " + weather);
        }
    }
}
