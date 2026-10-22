using UnityEngine;

namespace TehranNoir.World
{
    /// <summary>
    /// نور خورشید را از تاریخ و ساعت واقعی بازی می‌چرخاند.
    /// زاویه، رنگ و شدت همه از موقعیت خورشید می‌آیند؛ هیچ کلید دستی‌ای نیست.
    /// </summary>
    public sealed class SunDriver : MonoBehaviour
    {
        [Header("مراجع")]
        [Tooltip("خالی بماند تا خودکار از Clock پر شود")]
        [SerializeField] private WorldClock clock;

        [Tooltip("خالی بماند تا از Render Settings گرفته شود")]
        [SerializeField] private Light sunLight;

        [Header("مکان")]
        [SerializeField] private float latitude = SunPosition.TehranLatitude;
        [SerializeField] private float longitude = SunPosition.TehranLongitude;
        [SerializeField] private float timeZone = SunPosition.IranStandardTime;

        [Header("نور روز")]
        [SerializeField] private float maxIntensity = 1.6f;
        [SerializeField] private Color noonColor = new Color(1f, 0.96f, 0.90f);
        [SerializeField] private Color horizonColor = new Color(1f, 0.55f, 0.28f);

        [Header("شب")]
        [Tooltip("ماه تقریباً مقابل خورشید گذاشته می‌شود؛ تا مرحله‌ی آسمان واقعی، جایگزین است")]
        [SerializeField] private bool useMoonlight = true;
        [SerializeField] private float nightIntensity = 0.10f;
        [SerializeField] private Color nightColor = new Color(0.45f, 0.55f, 0.85f);

        private SunState _sun;

        public SunState CurrentSun { get { return _sun; } }

        private bool _warnedNoLight;

        private void Awake()
        {
            if (clock == null) clock = WorldClock.Instance;
            if (sunLight == null) sunLight = RenderSettings.sun;
        }

        private void LateUpdate()
        {
            // ترتیب Awake تضمینی نیست؛ پس هر فریم اگر مرجع خالی بود دوباره تلاش می‌کنیم
            if (clock == null) clock = WorldClock.Instance;
            if (sunLight == null) sunLight = RenderSettings.sun;

            if (sunLight == null)
            {
                if (!_warnedNoLight)
                {
                    _warnedNoLight = true;
                    Debug.LogError("[Sun] Directional Light پیدا نشد. فیلد Sun Light را دستی پر کن.");
                }

                return;
            }

            // بدون ساعت، خورشید روی ظهر یک تاریخ ثابت می‌ماند (برای تست صحنه‌های بدون ساعت)
            GameDate date = clock != null ? clock.Now : new GameDate(1352, 1, 1, 12, 0);
            _sun = SunPosition.Evaluate(date, latitude, longitude, timeZone);

            Apply(_sun);
        }

        private void Apply(SunState sun)
        {
            if (sunLight == null) return;

            float elevation = sun.Elevation;
            float azimuth = sun.Azimuth;

            bool moonMode = useMoonlight && elevation < 0f;

            if (moonMode)
            {
                // ماه مقابل خورشید: همان ارتفاع، طرف دیگر آسمان
                elevation = Mathf.Min(60f, -elevation);
                azimuth = Mathf.Repeat(azimuth + 180f, 360f);
            }

            Vector3 forward = DirectionTo(elevation, azimuth);

            // اگر نور تقریباً عمودی بتابد، up پیش‌فرض یونیتی بی‌معنی می‌شود
            Vector3 up = Mathf.Abs(forward.y) > 0.995f ? Vector3.forward : Vector3.up;
            sunLight.transform.rotation = Quaternion.LookRotation(forward, up);

            if (moonMode)
            {
                sunLight.color = nightColor;
                sunLight.intensity = nightIntensity;
                return;
            }

            float t = Mathf.InverseLerp(0f, 25f, elevation);
            sunLight.color = Color.Lerp(horizonColor, noonColor, t);
            sunLight.intensity = Mathf.Lerp(nightIntensity, maxIntensity, t);
        }

        /// <summary>
        /// بردار از زمین به سمت خورشید. نور در یونیتی در جهت forward می‌تابد،
        /// پس منفی‌اش را برمی‌گردانیم تا نور از آسمان به زمین بیاید.
        /// </summary>
        private static Vector3 DirectionTo(float elevationDegrees, float azimuthDegrees)
        {
            float elevation = elevationDegrees * Mathf.Deg2Rad;
            float azimuth = azimuthDegrees * Mathf.Deg2Rad;

            float horizontal = Mathf.Cos(elevation);

            // شمال = +Z ، شرق = +X
            Vector3 toSun = new Vector3(
                Mathf.Sin(azimuth) * horizontal,
                Mathf.Sin(elevation),
                Mathf.Cos(azimuth) * horizontal);

            return -toSun;
        }
    }
}
