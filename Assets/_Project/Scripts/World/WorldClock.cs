using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.World
{
    /// ساعت جهان: یک روز کامل در چند ثانیه‌ی واقعی می‌گذرد و فاز روز را
    /// در WorldState می‌نویسد: morning / day / evening / night
    public sealed class WorldClock : MonoBehaviour
    {
        [Header("زمان")]
        [Tooltip("چند ثانیه‌ی واقعی برای یک روز ۲۴ ساعته")]
        [SerializeField] private float dayLengthSeconds = 600f;

        [Tooltip("ساعت شروع، بین ۰ و ۲۴")]
        [SerializeField, Range(0f, 24f)] private float startHour = 8f;

        [Tooltip("در طول دیالوگ زمان نمی‌گذرد")]
        [SerializeField] private bool freezeDuringDialogue = true;

        [SerializeField] private bool logPhaseChanges = true;

        [Header("آزمایش")]
        [SerializeField, Range(0f, 24f)] private float debugHour = 22f;

        private float _hour;
        private string _lastPhase;

        public float Hour { get { return _hour; } }

        public string Phase { get { return PhaseOf(_hour); } }

        private void Start()
        {
            SetHour(startHour);
        }

        private void Update()
        {
            if (dayLengthSeconds <= 0f) return;
            if (freezeDuringDialogue && DialogueUI.Current != null) return;

            _hour += Time.deltaTime * (24f / dayLengthSeconds);

            if (_hour >= 24f) _hour -= 24f;

            Apply();
        }

        /// <summary>ساعت را جابه‌جا می‌کند و فاز را فوراً در جهان می‌نویسد.</summary>
        public void SetHour(float hour)
        {
            _hour = Mathf.Repeat(hour, 24f);
            _lastPhase = string.Empty;
            Apply();
        }

        [ContextMenu("پرش به ساعت آزمایشی")]
        private void JumpToDebugHour()
        {
            SetHour(debugHour);
        }

        /// <summary>فاز روز از ساعت. مرزها عمداً ساده‌اند.</summary>
        public static string PhaseOf(float hour)
        {
            if (hour >= 5f && hour < 11f) return "morning";
            if (hour >= 11f && hour < 17f) return "day";
            if (hour >= 17f && hour < 20f) return "evening";
            return "night";
        }

        private void Apply()
        {
            string phase = PhaseOf(_hour);
            if (phase == _lastPhase) return;

            _lastPhase = phase;

            WorldStateProvider provider = WorldStateProvider.Instance;
            if (provider == null)
            {
                Debug.LogWarning("[World] WorldStateProvider در صحنه نیست؛ ساعت جایی برای نوشتن ندارد.");
                return;
            }

            provider.Set("time", phase);

            if (logPhaseChanges)
                Debug.Log("[World] " + phase + " — ساعت " + Mathf.FloorToInt(_hour).ToString("00") + ":00");
        }
    }
}
