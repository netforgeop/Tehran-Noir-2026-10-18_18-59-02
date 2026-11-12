using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// بافت‌سازِ کوچک HUD: پنل شیشه‌ای با گرادیان و حاشیه، دیسک تیره، و آیکون‌های خطی.
    /// همه در زمان اجرا ساخته و کش می‌شوند — هیچ PNG‌ای لازم نیست.
    ///
    /// چرا بافت و نه چند Image روی هم؟
    /// چون گرادیان داخل گوشه‌ی گرد بدون ماسک واقعی از گوشه بیرون می‌زند و ماسک یونیتی
    /// به شکل مستطیل است، نه به شکل آلفای اسپرایت. یک بافت با آلفای نرم، هم گوشه را
    /// درست می‌کند هم حاشیه و گرادیان را، با یک draw call.
    ///
    /// لبه‌ها با فاصله‌ی علامت‌دار (SDF) حساب می‌شوند: یک محاسبه‌ی ساده برای هر پیکسل،
    /// نه چند نمونه در هر پیکسل. برای پنل ۴۴۰×۲۸۵ این تفاوت چند صد میلی‌ثانیه است.
    ///
    /// هر بافت یک بار ساخته می‌شود؛ کلیدِ کش شامل اندازه است، پس تغییر قد پنل
    /// فقط یک بافت تازه می‌سازد، نه هر فریم.
    /// </summary>
    public static class HudArt
    {
        /// <summary>شکل آیکون. «Glyph» نام دارد چون متدِ ساختش «Icon» است و هم‌نامی، خطای CS0102 می‌داد.</summary>
        public enum Glyph
        {
            Chat,
            Star,
            Crown,
            People,
            ArrowLeft,
            Document
        }

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(32);

        /* ============================================================
           پنل
           ============================================================ */

        /// <summary>
        /// پنل با گرادیان عمودی، گوشه‌ی گرد و حاشیه‌ی نازک.
        /// <paramref name="roundTop"/> یا <paramref name="roundBottom"/> خاموش یعنی آن لبه صاف باشد
        /// (فوتر تیره‌ی پایین کارت فقط گوشه‌ی پایین گرد دارد).
        /// </summary>
        public static Sprite Panel(string key, int width, int height, float radius,
            Color top, Color bottom, Color border, float borderWidth,
            bool roundTop = true, bool roundBottom = true)
        {
            Sprite cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);

            float rTop = roundTop ? radius : 0f;
            float rBottom = roundBottom ? radius : 0f;

            float[] outer = new float[width * height];
            float[] inner = new float[width * height];

            RoundedRect(outer, width, height, 0f, 0f, width, height, rTop, rBottom);
            RoundedRect(inner, width, height, borderWidth, borderWidth,
                width - borderWidth, height - borderWidth,
                Mathf.Max(0f, rTop - borderWidth), Mathf.Max(0f, rBottom - borderWidth));

            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                Color fill = Color.Lerp(bottom, top, (y + 0.5f) / height);

                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    float coverage = outer[index];
                    float edge = Mathf.Clamp01(coverage - inner[index]);

                    Color pixel = Color.Lerp(fill, border, edge);
                    float alpha = Mathf.Max(coverage * fill.a, edge * border.a);

                    pixel.a = alpha;
                    pixels[index] = ToColor32(pixel);
                }
            }

            return Store(key, pixels, width, height);
        }

        /* ============================================================
           دیسک
           ============================================================ */

        /// <summary>دایره‌ی پر با یک رینگ نازک دور آن؛ پایه‌ی گیج دایره‌ای.</summary>
        public static Sprite Disc(string key, int size, Color fill, Color rim, float rimWidth)
        {
            Sprite cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            size = Mathf.Max(8, size);

            Color32[] pixels = new Color32[size * size];

            float radius = size * 0.5f;
            float bodyRadius = radius - 0.5f;
            float holeRadius = Mathf.Max(0f, bodyRadius - rimWidth);

            for (int y = 0; y < size; y++)
            {
                float dy = y + 0.5f - radius;

                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float coverage = Mathf.Clamp01(0.5f + bodyRadius - distance);
                    float edge = Mathf.Clamp01(0.5f + holeRadius - distance);

                    Color pixel = Color.Lerp(fill, rim, edge);
                    pixel.a = Mathf.Max(coverage * fill.a, edge * rim.a);

                    pixels[y * size + x] = ToColor32(pixel);
                }
            }

            return Store(key, pixels, size, size);
        }

        /* ============================================================
           آیکون
           ============================================================ */

        /// <summary>
        /// آیکون خطی. رنگ را داخل بافت می‌پزد؛ برای تغییر رنگ بدون ساخت بافت تازه،
        /// سفید بساز و با Image.color رنگ بزن.
        /// </summary>
        public static Sprite Icon(Glyph icon, int size, Color color)
        {
            string key = "icon." + icon + "." + size + "." + Mathf.RoundToInt(color.r * 255f) + "."
                + Mathf.RoundToInt(color.g * 255f) + "." + Mathf.RoundToInt(color.b * 255f);

            Sprite cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            size = Mathf.Max(8, size);

            float[] mask = new float[size * size];

            switch (icon)
            {
                case Glyph.Chat:
                    Chat(mask, size);
                    break;
                case Glyph.Star:
                    Star(mask, size);
                    break;
                case Glyph.Crown:
                    Crown(mask, size);
                    break;
                case Glyph.People:
                    People(mask, size);
                    break;
                case Glyph.ArrowLeft:
                    ArrowLeft(mask, size);
                    break;
                default:
                    Document(mask, size);
                    break;
            }

            Color32[] pixels = new Color32[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                float alpha = Mathf.Clamp01(mask[i]) * color.a;
                pixels[i] = new Color32(
                    (byte)Mathf.RoundToInt(color.r * 255f),
                    (byte)Mathf.RoundToInt(color.g * 255f),
                    (byte)Mathf.RoundToInt(color.b * 255f),
                    (byte)Mathf.RoundToInt(alpha * 255f));
            }

            return Store(key, pixels, size, size);
        }

        /* ============================================================
           شکل آیکون‌ها (مختصات نسبی ۰..۱)
           ============================================================ */

        private static void Chat(float[] mask, int s)
        {
            RoundedRect(mask, s, s, 0.10f * s, 0.30f * s, 0.90f * s, 0.86f * s, 0.16f * s, 0.16f * s);
            CutRoundedRect(mask, s, s, 0.17f * s, 0.37f * s, 0.83f * s, 0.79f * s, 0.10f * s, 0.10f * s);

            Polygon(mask, s, s, new[]
            {
                new Vector2(0.26f * s, 0.36f * s),
                new Vector2(0.45f * s, 0.36f * s),
                new Vector2(0.30f * s, 0.10f * s)
            });

            CutPolygon(mask, s, s, new[]
            {
                new Vector2(0.31f * s, 0.36f * s),
                new Vector2(0.39f * s, 0.36f * s),
                new Vector2(0.32f * s, 0.21f * s)
            });
        }

        private static void Star(float[] mask, int s)
        {
            Vector2[] points = new Vector2[10];

            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.Deg2Rad * (90f + i * 36f);
                float radius = (i % 2 == 0 ? 0.46f : 0.20f) * s;

                points[i] = new Vector2(0.5f * s + Mathf.Cos(angle) * radius,
                    0.48f * s + Mathf.Sin(angle) * radius);
            }

            Polygon(mask, s, s, points);
        }

        private static void Crown(float[] mask, int s)
        {
            Polygon(mask, s, s, new[]
            {
                new Vector2(0.10f, 0.30f) * s,
                new Vector2(0.10f, 0.68f) * s,
                new Vector2(0.28f, 0.46f) * s,
                new Vector2(0.50f, 0.76f) * s,
                new Vector2(0.72f, 0.46f) * s,
                new Vector2(0.90f, 0.68f) * s,
                new Vector2(0.90f, 0.30f) * s
            });

            Rect(mask, s, s, 0.10f * s, 0.18f * s, 0.90f * s, 0.28f * s);
        }

        private static void People(float[] mask, int s)
        {
            Circle(mask, s, s, 0.30f * s, 0.66f * s, 0.115f * s);
            RoundedRect(mask, s, s, 0.10f * s, 0.14f * s, 0.50f * s, 0.48f * s, 0.19f * s, 0.19f * s);

            Circle(mask, s, s, 0.70f * s, 0.66f * s, 0.115f * s);
            RoundedRect(mask, s, s, 0.50f * s, 0.14f * s, 0.90f * s, 0.48f * s, 0.19f * s, 0.19f * s);

            // شکاف باریک بین دو شانه تا دو آدم جدا خوانده شوند
            CutRect(mask, s, s, 0.485f * s, 0.10f * s, 0.515f * s, 0.52f * s);
        }

        private static void ArrowLeft(float[] mask, int s)
        {
            Polygon(mask, s, s, new[]
            {
                new Vector2(0.64f, 0.16f) * s,
                new Vector2(0.64f, 0.84f) * s,
                new Vector2(0.24f, 0.50f) * s
            });
        }

        private static void Document(float[] mask, int s)
        {
            RoundedRect(mask, s, s, 0.18f * s, 0.12f * s, 0.82f * s, 0.88f * s, 0.10f * s, 0.10f * s);
            CutRoundedRect(mask, s, s, 0.24f * s, 0.19f * s, 0.76f * s, 0.81f * s, 0.06f * s, 0.06f * s);

            Rect(mask, s, s, 0.32f * s, 0.64f * s, 0.68f * s, 0.70f * s);
            Rect(mask, s, s, 0.32f * s, 0.48f * s, 0.68f * s, 0.54f * s);
            Rect(mask, s, s, 0.32f * s, 0.32f * s, 0.58f * s, 0.38f * s);
        }

        /* ============================================================
           ابزارهای شکل
           ============================================================ */

        private static void Rect(float[] mask, int w, int h, float x0, float y0, float x1, float y1)
        {
            AddRect(mask, w, h, x0, y0, x1, y1, false);
        }

        private static void CutRect(float[] mask, int w, int h, float x0, float y0, float x1, float y1)
        {
            AddRect(mask, w, h, x0, y0, x1, y1, true);
        }

        private static void AddRect(float[] mask, int w, int h, float x0, float y0, float x1, float y1, bool cut)
        {
            int i0 = Mathf.Max(0, Mathf.FloorToInt(x0));
            int i1 = Mathf.Min(w - 1, Mathf.CeilToInt(x1));
            int j0 = Mathf.Max(0, Mathf.FloorToInt(y0));
            int j1 = Mathf.Min(h - 1, Mathf.CeilToInt(y1));

            for (int y = j0; y <= j1; y++)
            {
                for (int x = i0; x <= i1; x++)
                {
                    float coverage = Mathf.Clamp01(0.5f + Mathf.Min(
                        Mathf.Min(x + 0.5f - x0, x1 - (x + 0.5f)),
                        Mathf.Min(y + 0.5f - y0, y1 - (y + 0.5f))));

                    int index = y * w + x;

                    mask[index] = cut ? mask[index] * (1f - coverage) : Mathf.Max(mask[index], coverage);
                }
            }
        }

        private static void Circle(float[] mask, int w, int h, float cx, float cy, float radius)
        {
            if (radius <= 0f) return;

            int i0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius) - 1);
            int i1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + radius) + 1);
            int j0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius) - 1);
            int j1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + radius) + 1);

            for (int y = j0; y <= j1; y++)
            {
                float dy = y + 0.5f - cy;

                for (int x = i0; x <= i1; x++)
                {
                    float dx = x + 0.5f - cx;
                    float coverage = Mathf.Clamp01(0.5f + radius - Mathf.Sqrt(dx * dx + dy * dy));

                    if (coverage <= 0f) continue;

                    int index = y * w + x;
                    if (coverage > mask[index]) mask[index] = coverage;
                }
            }
        }

        /// <summary>
        /// مستطیل گوشه‌گرد با شعاع متفاوت بالا و پایین.
        /// فاصله‌ی علامت‌دار: یک بار برای هر پیکسل، بدون نمونه‌برداری چندگانه.
        /// </summary>
        private static void RoundedRect(float[] mask, int w, int h, float x0, float y0, float x1, float y1,
            float rTop, float rBottom)
        {
            int i0 = Mathf.Max(0, Mathf.FloorToInt(x0) - 1);
            int i1 = Mathf.Min(w - 1, Mathf.CeilToInt(x1) + 1);
            int j0 = Mathf.Max(0, Mathf.FloorToInt(y0) - 1);
            int j1 = Mathf.Min(h - 1, Mathf.CeilToInt(y1) + 1);

            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float halfW = (x1 - x0) * 0.5f;
            float halfH = (y1 - y0) * 0.5f;

            float limit = Mathf.Min(halfW, halfH);

            rTop = Mathf.Clamp(rTop, 0f, limit);
            rBottom = Mathf.Clamp(rBottom, 0f, limit);

            for (int y = j0; y <= j1; y++)
            {
                float py = y + 0.5f - cy;

                // y به سمت بالا؛ بالا = شعاع بالا، پایین = شعاع پایین
                float radius = py > 0f ? rTop : rBottom;

                for (int x = i0; x <= i1; x++)
                {
                    float px = x + 0.5f - cx;

                    float coverage = RoundCoverage(px, py, halfW, halfH, radius);

                    if (coverage <= 0f) continue;

                    int index = y * w + x;
                    if (coverage > mask[index]) mask[index] = coverage;
                }
            }
        }

        private static float RoundCoverage(float px, float py, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(px) - halfW + radius;
            float qy = Mathf.Abs(py) - halfH + radius;

            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);

            float distance = Mathf.Min(Mathf.Max(qx, qy), 0f) + Mathf.Sqrt(ox * ox + oy * oy) - radius;

            return Mathf.Clamp01(0.5f - distance);
        }

        private static void CutRoundedRect(float[] mask, int w, int h, float x0, float y0, float x1, float y1,
            float rTop, float rBottom)
        {
            float[] part = new float[mask.Length];

            RoundedRect(part, w, h, x0, y0, x1, y1, rTop, rBottom);

            for (int i = 0; i < mask.Length; i++) mask[i] *= 1f - part[i];
        }

        private static void Polygon(float[] mask, int w, int h, Vector2[] points)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                minX = Mathf.Min(minX, points[i].x);
                minY = Mathf.Min(minY, points[i].y);
                maxX = Mathf.Max(maxX, points[i].x);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            int i0 = Mathf.Max(0, Mathf.FloorToInt(minX) - 1);
            int i1 = Mathf.Min(w - 1, Mathf.CeilToInt(maxX) + 1);
            int j0 = Mathf.Max(0, Mathf.FloorToInt(minY) - 1);
            int j1 = Mathf.Min(h - 1, Mathf.CeilToInt(maxY) + 1);

            for (int y = j0; y <= j1; y++)
            {
                for (int x = i0; x <= i1; x++)
                {
                    float coverage = Inside4(x, y, points);

                    if (coverage <= 0f) continue;

                    int index = y * w + x;
                    if (coverage > mask[index]) mask[index] = coverage;
                }
            }
        }

        private static void CutPolygon(float[] mask, int w, int h, Vector2[] points)
        {
            float[] part = new float[mask.Length];

            Polygon(part, w, h, points);

            for (int i = 0; i < mask.Length; i++) mask[i] *= 1f - part[i];
        }

        /// <summary>چهار نمونه در هر پیکسل؛ فقط برای چندضلعی‌های کوچک آیکون به‌کار می‌رود.</summary>
        private static float Inside4(int x, int y, Vector2[] polygon)
        {
            int hits = 0;

            for (int sy = 0; sy < 2; sy++)
            {
                for (int sx = 0; sx < 2; sx++)
                {
                    Vector2 point = new Vector2(x + (sx + 0.5f) * 0.5f, y + (sy + 0.5f) * 0.5f);

                    if (Inside(point, polygon)) hits++;
                }
            }

            return hits * 0.25f;
        }

        /// <summary>الگوریتم پرتوی فرد-زوج؛ برای ستاره و تاج کافی است.</summary>
        private static bool Inside(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (polygon[i].y > point.y != polygon[j].y > point.y &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                    (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /* ============================================================
           بافت و کش
           ============================================================ */

        private static Sprite Store(string key, Color32[] pixels, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = key;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

            sprite.name = key;

            Cache[key] = sprite;

            return sprite;
        }

        private static Color32 ToColor32(Color color)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255f));
        }

        /* ============================================================
           ارقام فارسی
           ============================================================ */

        private static readonly char[] PersianDigits = { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };

        /// <summary>«55» را «۵۵» می‌کند؛ بقیه‌ی کاراکترها دست‌نخورده می‌مانند.</summary>
        public static string Digits(string text, bool persian)
        {
            if (!persian || string.IsNullOrEmpty(text)) return text;

            char[] chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '0' && chars[i] <= '9')
                    chars[i] = PersianDigits[chars[i] - '0'];
            }

            return new string(chars);
        }

        public static string Digits(int value, bool persian)
        {
            return Digits(value.ToString(), persian);
        }
    }
}
