using System;

namespace TehranNoir.World
{
    /// وضعیت خورشید در یک لحظه.
    public struct SunState
    {
        public float Elevation;      // درجه بالای افق؛ منفی = زیر افق
        public float Azimuth;        // درجه از شمال، ساعتگرد
        public float SunriseHour;    // ساعت محلی اعشاری
        public float SunsetHour;
        public float SolarNoonHour;
        public float DayProgress;    // ۰ تا ۱ در طول روز؛ قبل از طلوع منفی، بعد از غروب بزرگ‌تر از ۱

        public bool IsDaylight { get { return Elevation > -0.833f; } }
    }

    /// <summary>
    /// موقعیت خورشید برای یک تاریخ و ساعت محلی (فرمول‌های NOAA).
    /// دقتش برای بازی بیش از کافی است و به هیچ پکیج بیرونی وابسته نیست.
    /// </summary>
    public static class SunPosition
    {
        public const float TehranLatitude = 35.6892f;
        public const float TehranLongitude = 51.3890f;
        public const float IranStandardTime = 3.5f;   // UTC+3:30

        private const double Deg2Rad = Math.PI / 180.0;
        private const double Rad2Deg = 180.0 / Math.PI;

        /// <summary>موقعیت خورشید برای تهران.</summary>
        public static SunState Evaluate(GameDate date)
        {
            return Evaluate(date, TehranLatitude, TehranLongitude, IranStandardTime);
        }

        public static SunState Evaluate(GameDate date, float latitude, float longitude, float timeZone)
        {
            double lat = latitude * Deg2Rad;
            double dayOfYear = date.GregorianDayOfYear;
            double hour = date.HourFraction;

            // کسر سال
            double gamma = 2.0 * Math.PI / 365.0 * (dayOfYear - 1.0 + (hour - 12.0) / 24.0);

            // معادله‌ی زمان، دقیقه
            double eqTime = 229.18 * (0.000075
                + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
                - 0.014615 * Math.Cos(2.0 * gamma) - 0.040849 * Math.Sin(2.0 * gamma));

            // میل خورشید، رادیان
            double declination = 0.006918
                - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
                - 0.006758 * Math.Cos(2.0 * gamma) + 0.000907 * Math.Sin(2.0 * gamma)
                - 0.002697 * Math.Cos(3.0 * gamma) + 0.00148 * Math.Sin(3.0 * gamma);

            double timeOffset = eqTime + 4.0 * longitude - 60.0 * timeZone;
            double trueSolarTime = hour * 60.0 + timeOffset;
            double hourAngle = trueSolarTime / 4.0 - 180.0;

            // ارتفاع خورشید
            double cosZenith = Math.Sin(lat) * Math.Sin(declination)
                               + Math.Cos(lat) * Math.Cos(declination) * Math.Cos(hourAngle * Deg2Rad);
            cosZenith = Clamp(cosZenith, -1.0, 1.0);

            double zenith = Math.Acos(cosZenith);
            double elevation = 90.0 - zenith * Rad2Deg;

            // آزیموت: از شمال، ساعتگرد
            double azimuth;
            double sinZenith = Math.Sin(zenith);

            if (sinZenith > 1e-6)
            {
                double cosAzimuth = (Math.Sin(lat) * Math.Cos(zenith) - Math.Sin(declination))
                                    / (Math.Cos(lat) * sinZenith);

                azimuth = Math.Acos(Clamp(cosAzimuth, -1.0, 1.0)) * Rad2Deg;
                if (hourAngle > 0.0) azimuth = 360.0 - azimuth;
            }
            else
            {
                azimuth = 180.0;
            }

            // طلوع و غروب: ارتفاع ۰.۸۳۳- درجه
            double cosHourAngle0 = (Math.Cos(90.833 * Deg2Rad) - Math.Sin(lat) * Math.Sin(declination))
                                   / (Math.Cos(lat) * Math.Cos(declination));

            double hourAngle0 = Math.Acos(Clamp(cosHourAngle0, -1.0, 1.0)) * Rad2Deg;

            double sunriseMinutes = 720.0 - 4.0 * (longitude + hourAngle0) - eqTime + timeZone * 60.0;
            double sunsetMinutes = 720.0 - 4.0 * (longitude - hourAngle0) - eqTime + timeZone * 60.0;
            double noonMinutes = 720.0 - 4.0 * longitude - eqTime + timeZone * 60.0;

            double length = sunsetMinutes - sunriseMinutes;
            double progress = length > 1.0 ? (hour * 60.0 - sunriseMinutes) / length : 0.0;

            SunState state;
            state.Elevation = (float)elevation;
            state.Azimuth = (float)azimuth;
            state.SunriseHour = (float)(sunriseMinutes / 60.0);
            state.SunsetHour = (float)(sunsetMinutes / 60.0);
            state.SolarNoonHour = (float)(noonMinutes / 60.0);
            state.DayProgress = (float)progress;
            return state;
        }

        /// <summary>فاز روز برای دیالوگ: morning / day / evening / night.</summary>
        public static string PhaseOf(SunState sun)
        {
            if (sun.Elevation < -6f) return "night";
            if (sun.DayProgress < 0.40f) return "morning";
            if (sun.DayProgress < 0.72f) return "day";
            return "evening";
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
