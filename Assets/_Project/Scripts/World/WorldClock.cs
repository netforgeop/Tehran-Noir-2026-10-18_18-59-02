using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.World
{
    /// <summary>
    /// ساعت و تاریخ جهان. یک تاریخ شمسی را جلو می‌برد و هر تغییر را
    /// در WorldState می‌نویسد تا دیالوگ‌ها با آن شرط بگذارند.
    /// پرش زمانی (مثلاً «چهار ماه بعد») هم از همین‌جا انجام می‌شود.
    /// </summary>
    public sealed class WorldClock : MonoBehaviour
    {
        [Header("تاریخ")]
        [Tooltip("سال شمسی، ماه، روز، ساعت، دقیقه")]
        [SerializeField] private GameDate startDate = new GameDate(1352, 1, 1, 8, 0);

        [Tooltip("هر یک ساعتِ بازی، چند ثانیه‌ی واقعی طول بکشد")]
        [SerializeField] private float realSecondsPerGameHour = 25f;

        [Tooltip("در طول دیالوگ زمان نمی‌گذرد")]
        [SerializeField] private bool freezeDuringDialogue = true;

        [SerializeField] private bool logChanges = true;

        public static WorldClock Instance { get; private set; }

        private GameDate _now;
        private double _minuteAccumulator;
        private string _lastPhase = string.Empty;
        private int _lastHour = -1;
        private int _lastDay = -1;

        public GameDate Now { get { return _now; } }

        public SunState Sun { get { return SunPosition.Evaluate(_now); } }

        public string Phase { get { return SunPosition.PhaseOf(Sun); } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[World] بیش از یک WorldClock در صحنه است؛ دومی غیرفعال شد.");
                enabled = false;
                return;
            }

            Instance = this;
            _now = startDate;

            // اگر تاریخ در Inspector خالی یا نامعتبر ماند، به شروع داستان برمی‌گردیم
            if (_now.Year < 1200 || _now.Year > 1600)
            {
                Debug.LogWarning("[World] تاریخ شروع نامعتبر بود؛ به ۱۳۵۲/۰۱/۰۱ برگشت.");
                _now = new GameDate(1352, 1, 1, 8, 0);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            Push(true);
        }

        private void Update()
        {
            if (realSecondsPerGameHour <= 0f) return;
            if (freezeDuringDialogue && DialogueUI.Current != null) return;

            _minuteAccumulator += Time.deltaTime * (60.0 / realSecondsPerGameHour);

            if (_minuteAccumulator < 1.0) return;

            long wholeMinutes = (long)_minuteAccumulator;
            _minuteAccumulator -= wholeMinutes;

            _now = _now.AddMinutes(wholeMinutes);
            Push(false);
        }

        /* ============================================================
           پرش زمانی — کوئست‌ها و میان‌پرده‌ها این‌ها را صدا می‌زنند
           ============================================================ */

        public void SkipDays(int days)
        {
            if (days == 0) return;

            _now = _now.AddDays(days);
            Push(true);
        }

        public void SkipMonths(int months)
        {
            if (months == 0) return;

            _now = _now.AddMonths(months);
            Push(true);
        }

        public void SkipHours(float hours)
        {
            if (hours == 0f) return;

            _now = _now.AddHours(hours);
            Push(true);
        }

        public void SetDate(GameDate date)
        {
            _now = date;
            Push(true);
        }

        [ContextMenu("پرش یک ساعت")]
        private void SkipOneHour()
        {
            SkipHours(1f);
        }

        [ContextMenu("پرش یک روز")]
        private void SkipOneDay()
        {
            SkipDays(1);
        }

        [ContextMenu("پرش چهار ماه")]
        private void SkipFourMonths()
        {
            SkipMonths(4);
        }

        [ContextMenu("پرش یک سال")]
        private void SkipOneYear()
        {
            SkipMonths(12);
        }

        /* ============================================================
           فیلدهای تاریخ — تنها راه درست برای عوض کردنشان
           ============================================================ */

        /// <summary>
        /// این شش کلید در WorldState «نتیجه»‌اند، نه منبع: هر دقیقه‌ی بازی از روی
        /// _now دوباره نوشته می‌شوند. اگر کسی مستقیم در WorldState بنویسد،
        /// تا تیک بعدی دوام می‌آورد و بعد برمی‌گردد. تغییر باید از همین‌جا رد شود.
        /// </summary>
        public static bool IsClockField(string name)
        {
            switch (name)
            {
                case "year":
                case "month":
                case "day":
                case "hour":
                case "time":
                case "season":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>سال/ماه/روز/ساعت قابل تغییرند؛ فصل و بخش روز از تاریخ حساب می‌شوند.</summary>
        public static bool IsEditable(string name)
        {
            switch (name)
            {
                case "year":
                case "month":
                case "day":
                case "hour":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>مقدار فعلی برای نمایش. برای فیلد ناشناس false.</summary>
        public bool TryRead(string name, out string text)
        {
            switch (name)
            {
                case "year": text = _now.Year.ToString(); return true;
                case "month": text = _now.Month.ToString(); return true;
                case "day": text = _now.Day.ToString(); return true;
                case "hour": text = _now.Hour.ToString("00") + ":" + _now.Minute.ToString("00"); return true;
                case "season": text = _now.Season; return true;
                case "time": text = Phase; return true;
                default: text = string.Empty; return false;
            }
        }

        /// <summary>
        /// نوشتن یک فیلد. مقدار بیرون از بازه، پیچیده می‌شود (۲۵ ساعت = ۱ بامداد روز بعد)،
        /// نه رد. روز و ماه هم به آخرین روز معتبر همان ماه محدود می‌شوند.
        /// </summary>
        public bool SetField(string name, int value)
        {
            GameDate next;

            switch (name)
            {
                case "hour":
                    value = ((value % 24) + 24) % 24;
                    next = new GameDate(_now.Year, _now.Month, _now.Day, value, 0);
                    break;

                case "day":
                    value = Mathf.Clamp(value, 1, GameDate.DaysInMonth(_now.Year, _now.Month));
                    next = new GameDate(_now.Year, _now.Month, value, _now.Hour, _now.Minute);
                    break;

                case "month":
                    value = Mathf.Clamp(value, 1, 12);
                    next = new GameDate(_now.Year, value,
                        Mathf.Min(_now.Day, GameDate.DaysInMonth(_now.Year, value)), _now.Hour, _now.Minute);
                    break;

                case "year":
                    value = Mathf.Clamp(value, 1200, 1600);
                    next = new GameDate(value, _now.Month,
                        Mathf.Min(_now.Day, GameDate.DaysInMonth(value, _now.Month)), _now.Hour, _now.Minute);
                    break;

                default:
                    return false;
            }

            SetDate(next);
            return true;
        }

        /// <summary>کم و زیاد کردن یک فیلد؛ حول مرزها می‌پیچد (ساعت ۲۳ + ۲ = ۱).</summary>
        public bool AddField(string name, int delta)
        {
            int current;

            switch (name)
            {
                case "hour": current = _now.Hour; break;
                case "day": current = _now.Day; break;
                case "month": current = _now.Month; break;
                case "year": current = _now.Year; break;
                default: return false;
            }

            return SetField(name, current + delta);
        }

        /// <summary>تاریخ و ساعت را در WorldState می‌نویسد.</summary>
        private void Push(bool force)
        {
            SunState sun = SunPosition.Evaluate(_now);
            string phase = SunPosition.PhaseOf(sun);

            bool changed = force
                || phase != _lastPhase
                || _now.Hour != _lastHour
                || _now.Day != _lastDay;

            _lastPhase = phase;
            _lastHour = _now.Hour;
            _lastDay = _now.Day;

            WorldStateProvider provider = WorldStateProvider.Instance;
            if (provider == null)
            {
                if (force) Debug.LogWarning("[World] WorldStateProvider در صحنه نیست؛ تاریخ جایی برای نوشتن ندارد.");
                return;
            }

            provider.Set("time", phase);
            provider.Set("hour", (double)_now.Hour);
            provider.Set("day", (double)_now.Day);
            provider.Set("month", (double)_now.Month);
            provider.Set("year", (double)_now.Year);
            provider.Set("season", _now.Season);

            if (changed && logChanges)
                Debug.Log("[World] " + _now + "  " + phase + "  " + _now.Season +
                          "  (طلوع " + ToClock(sun.SunriseHour) + " / غروب " + ToClock(sun.SunsetHour) + ")");
        }

        private static string ToClock(float hour)
        {
            int totalMinutes = Mathf.RoundToInt(hour * 60f);
            if (totalMinutes < 0) totalMinutes += 1440;

            return (totalMinutes / 60).ToString("00") + ":" + (totalMinutes % 60).ToString("00");
        }
    }
}
