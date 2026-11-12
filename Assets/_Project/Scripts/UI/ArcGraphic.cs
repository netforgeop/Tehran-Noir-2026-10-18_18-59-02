using UnityEngine;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// یک کمان دلخواه با شبکه‌ی ساخته‌شده در زمان اجرا — نه عکس، نه Texture.
    ///
    /// چرا Graphic سفارشی و نه Image با Fill Method = Radial360؟
    /// چون Image فقط کمان کامل ۳۶۰ درجه می‌کشد و ضخامتش از عکس می‌آید؛
    /// برای کمانی با زاویه‌ی دلخواه، ضخامت پارامتری و سرهای گرد، مش دستی ساده‌ترین راه است.
    /// هزینه‌اش هم ناچیز است: یک کمان ۲۴۰ درجه حدود ۱۰۰ رأس دارد.
    ///
    /// زاویه بر حسب درجه: ۰ = راست، ۹۰ = بالا، خلاف عقربه‌های ساعت مثبت است.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArcGraphic : MaskableGraphic
    {
        [SerializeField] private float startAngle = 60f;
        [SerializeField] private float endAngle = 300f;
        [SerializeField] private float radius = 150f;
        [SerializeField] private float thickness = 9f;

        [Tooltip("سرهای کمان گرد باشند")]
        [SerializeField] private bool roundCaps = true;

        [Tooltip("هر چند درجه یک رأس؛ کمتر = نرم‌تر، گران‌تر")]
        [SerializeField] private float degreesPerSegment = 2.5f;

        [Tooltip("چند درجه از هر سر کمان محو شود؛ صفر = بدون محو")]
        [SerializeField] private float fadeDegrees = 0f;

        public float Radius { get { return radius; } }

        /// <summary>زاویه‌ی شروع و پایان. اگر شروع از پایان بزرگ‌تر باشد، ساعت‌گرد کشیده می‌شود.</summary>
        public void SetAngles(float start, float end)
        {
            if (Mathf.Approximately(startAngle, start) && Mathf.Approximately(endAngle, end)) return;

            startAngle = start;
            endAngle = end;
            SetVerticesDirty();
        }

        /// <summary>محو شدن تدریجی دو سر کمان؛ با آلفای رأس، نه ماسک.</summary>
        public void SetFade(float degrees)
        {
            degrees = Mathf.Max(0f, degrees);

            if (Mathf.Approximately(fadeDegrees, degrees)) return;

            fadeDegrees = degrees;
            SetVerticesDirty();
        }

        public void SetGeometry(float newRadius, float newThickness)
        {
            if (Mathf.Approximately(radius, newRadius) && Mathf.Approximately(thickness, newThickness)) return;

            radius = newRadius;
            thickness = newThickness;
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float sweep = endAngle - startAngle;

            if (Mathf.Abs(sweep) < 0.05f || radius <= 0f || thickness <= 0f) return;

            int segments = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(sweep) / Mathf.Max(0.5f, degreesPerSegment)));

            float half = thickness * 0.5f;
            float outer = radius + half;
            float inner = radius - half;

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * (startAngle + sweep * i / segments);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                Color tint = Tint(segments == 0 ? 0f : i / (float)segments, sweep);

                vh.AddVert(direction * inner, tint, Vector2.zero);
                vh.AddVert(direction * outer, tint, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int index = i * 2;

                vh.AddTriangle(index, index + 1, index + 3);
                vh.AddTriangle(index, index + 3, index + 2);
            }

            // کمان کامل ۳۶۰ درجه دو سرِ روی‌هم‌افتاده دارد؛ سر گرد آن دو بار رنگ می‌شود و لکه می‌اندازد
            if (!roundCaps || Mathf.Abs(sweep) >= 359.5f) return;

            AddCap(vh, startAngle, Tint(0f, sweep));
            AddCap(vh, endAngle, Tint(1f, sweep));
        }

        /// <summary>آلفای هر رأس بر اساس فاصله‌اش از دو سر کمان.</summary>
        private Color Tint(float t, float sweep)
        {
            Color tint = color;

            if (fadeDegrees <= 0.01f) return tint;

            float span = Mathf.Abs(sweep);

            if (span < 1f) return tint;

            float fade = Mathf.Clamp01(fadeDegrees / span);

            if (fade <= 0f) return tint;

            float edge = Mathf.Min(t, 1f - t) / fade;

            tint.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge));

            return tint;
        }

        /// <summary>سر گرد = یک دایره‌ی پر به شعاع نصف ضخامت، وسط ضخامت، روی همان زاویه.</summary>
        private void AddCap(VertexHelper vh, float angle, Color tint)
        {
            float radians = Mathf.Deg2Rad * angle;
            Vector2 center = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            float capRadius = thickness * 0.5f;

            const int capSegments = 12;

            int first = vh.currentVertCount;

            vh.AddVert(center, tint, Vector2.zero);

            for (int i = 0; i <= capSegments; i++)
            {
                float t = Mathf.PI * 2f * i / capSegments;
                Vector2 point = center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * capRadius;

                vh.AddVert(point, tint, Vector2.zero);
            }

            for (int i = 0; i < capSegments; i++)
                vh.AddTriangle(first, first + 1 + i, first + 2 + i);
        }
    }
}
