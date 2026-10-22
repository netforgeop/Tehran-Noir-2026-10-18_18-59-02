using System;

namespace TehranNoir.World
{
    /// <summary>
    /// تاریخ شمسی + ساعت. همه‌ی جابه‌جایی‌ها روی شماره‌ی روز جولیَن انجام می‌شود،
    /// پس هیچ‌جا لازم نیست طول ماه‌ها را دستی حساب کنیم.
    /// </summary>
    [Serializable]
    public struct GameDate : IEquatable<GameDate>
    {
        public int Year;    // شمسی
        public int Month;   // ۱ تا ۱۲
        public int Day;     // ۱ تا ۳۱
        public int Hour;    // ۰ تا ۲۳
        public int Minute;  // ۰ تا ۵۹

        public GameDate(int year, int month, int day, int hour = 0, int minute = 0)
        {
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Normalize();
        }

        /// ساعت محلی به صورت اعشاری: ۱۴:۳۰ → 14.5
        public float HourFraction { get { return Hour + Minute / 60f; } }

        public string Season
        {
            get
            {
                if (Month <= 3) return "spring";
                if (Month <= 6) return "summer";
                if (Month <= 9) return "autumn";
                return "winter";
            }
        }

        public int ToJulianDay()
        {
            return JalaliToJulianDay(Year, Month, Day);
        }

        /// روز سال میلادی؛ محاسبه‌ی خورشید به آن نیاز دارد.
        public int GregorianDayOfYear()
        {
            int jdn = ToJulianDay();

            int gy, gm, gd;
            JulianDayToGregorian(jdn, out gy, out gm, out gd);

            int[] monthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            bool leap = (gy % 4 == 0 && gy % 100 != 0) || gy % 400 == 0;

            int total = gd;
            for (int m = 1; m < gm; m++)
            {
                total += monthDays[m - 1];
                if (m == 2 && leap) total += 1;
            }

            return total;
        }

        public GameDate AddMinutes(long minutes)
        {
            long total = ToJulianDay() * 1440L + Hour * 60L + Minute + minutes;

            long days = FloorDiv(total, 1440L);
            long rest = total - days * 1440L;

            GameDate result = new GameDate();
            JulianDayToJalali((int)days, out result.Year, out result.Month, out result.Day);
            result.Hour = (int)(rest / 60L);
            result.Minute = (int)(rest % 60L);

            return result;
        }

        public GameDate AddDays(int days)
        {
            return AddMinutes(days * 1440L);
        }

        public GameDate AddHours(double hours)
        {
            return AddMinutes((long)Math.Round(hours * 60.0));
        }

        /// <summary>ماه‌ها را جلو می‌برد؛ اگر روز ۳۱ باشد و ماه بعدی ۳۰ روز، روی آخرین روز می‌نشیند.</summary>
        public GameDate AddMonths(int months)
        {
            int index = (Year * 12 + (Month - 1)) + months;
            int newYear = FloorDiv(index, 12);
            int newMonth = index - newYear * 12 + 1;

            int newDay = Math.Min(Day, DaysInMonth(newYear, newMonth));

            GameDate result = this;
            result.Year = newYear;
            result.Month = newMonth;
            result.Day = newDay;
            return result;
        }

        public static bool IsLeapYear(int jalaliYear)
        {
            return GetJalaliCalendar(jalaliYear).Leap == 0;
        }

        public static int DaysInMonth(int jalaliYear, int month)
        {
            if (month <= 6) return 31;
            if (month <= 11) return 30;
            return IsLeapYear(jalaliYear) ? 30 : 29;
        }

        public bool Equals(GameDate other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day &&
                   Hour == other.Hour && Minute == other.Minute;
        }

        public override bool Equals(object obj)
        {
            return obj is GameDate && Equals((GameDate)obj);
        }

        public override int GetHashCode()
        {
            return Year * 1000000 + Month * 10000 + Day * 100 + Hour;
        }

        public override string ToString()
        {
            return Year + "/" + Month.ToString("00") + "/" + Day.ToString("00") +
                   "  " + Hour.ToString("00") + ":" + Minute.ToString("00");
        }

        private void Normalize()
        {
            long total = JalaliToJulianDay(Year, Month, Day) * 1440L + Hour * 60L + Minute;

            long days = FloorDiv(total, 1440L);
            long rest = total - days * 1440L;

            JulianDayToJalali((int)days, out Year, out Month, out Day);
            Hour = (int)(rest / 60L);
            Minute = (int)(rest % 60L);
        }

        private static long FloorDiv(long a, long b)
        {
            return a >= 0 ? a / b : -((-a + b - 1) / b);
        }

        private static int FloorDiv(int a, int b)
        {
            return a >= 0 ? a / b : -((-a + b - 1) / b);
        }

        /* ============================================================
           تبدیل تقویم شمسی  ↔  شماره‌ی روز جولیَن
           الگوریتم استاندارد جلالی (همان که jalaali-js استفاده می‌کند)
           ============================================================ */

        private struct JalaliCalendar
        {
            public int Leap;   // 0 = سال کبیسه
            public int Gy;
            public int March;  // روز مارس که ۱ فروردین است
        }

        private static readonly int[] Breaks =
        {
            -61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210,
            1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178
        };

        private static JalaliCalendar GetJalaliCalendar(int jy)
        {
            int gy = jy + 621;
            int leapJ = -14;
            int jp = Breaks[0];
            int jump = 0;

            for (int i = 1; i < Breaks.Length; i++)
            {
                int jm = Breaks[i];
                jump = jm - jp;
                if (jy < jm) break;

                leapJ += (jump / 33) * 8 + ((jump % 33) / 4);
                jp = jm;
            }

            int n = jy - jp;
            leapJ += (n / 33) * 8 + ((n % 33) + 3) / 4;

            if ((jump % 33) == 4 && jump - n == 4) leapJ += 1;

            int leapG = (gy / 4) - ((gy / 100 + 1) * 3) / 4 - 150;
            int march = 20 + leapJ - leapG;

            if (jump - n < 6) n = n - jump + ((jump + 4) / 33) * 33;

            int leap = (((n + 1) % 33) - 1) % 4;
            if (leap == -1) leap = 4;

            JalaliCalendar result = new JalaliCalendar();
            result.Leap = leap;
            result.Gy = gy;
            result.March = march;
            return result;
        }

        private static int GregorianToJulianDay(int gy, int gm, int gd)
        {
            long d = ((gy + (gm - 8) / 6 + 100100) * 1461) / 4
                     + (153 * ((gm + 9) % 12) + 2) / 5
                     + gd - 34840408;

            d = d - ((gy + 100100 + (gm - 8) / 6) / 100 * 3) / 4 + 752;
            return (int)d;
        }

        private static void JulianDayToGregorian(int jdn, out int gy, out int gm, out int gd)
        {
            long j = 4L * jdn + 139361631;
            j = j + ((4L * jdn + 183187720) / 146097 * 3) / 4 * 4 - 3908;

            long i = ((j % 1461) / 4) * 5 + 308;
            gd = (int)(((i % 153) / 5) + 1);
            gm = (int)(((i / 153) % 12) + 1);

            long year = j / 1461 - 100100 + (8 - gm) / 6;
            gy = (int)year;
        }

        private static int JalaliToJulianDay(int jy, int jm, int jd)
        {
            JalaliCalendar cal = GetJalaliCalendar(jy);

            return GregorianToJulianDay(cal.Gy, 3, cal.March)
                   + (jm - 1) * 31
                   - (jm / 7) * (jm - 7)
                   + jd - 1;
        }

        private static void JulianDayToJalali(int jdn, out int jy, out int jm, out int jd)
        {
            int gy, gm, gd;
            JulianDayToGregorian(jdn, out gy, out gm, out gd);

            jy = gy - 621;

            JalaliCalendar cal = GetJalaliCalendar(jy);
            int jdnFirstFarvardin = GregorianToJulianDay(gy, 3, cal.March);

            int k = jdn - jdnFirstFarvardin;

            if (k >= 0)
            {
                if (k <= 185)
                {
                    jm = 1 + k / 31;
                    jd = k % 31 + 1;
                    return;
                }

                k -= 186;
            }
            else
            {
                jy -= 1;
                k += 179;
                if (cal.Leap == 1) k += 1;
            }

            jm = 7 + k / 30;
            jd = k % 30 + 1;
        }
    }
}
