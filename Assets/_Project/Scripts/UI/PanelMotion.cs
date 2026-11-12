using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// ورود و خروج نرم هر پنل HUD. یک کامپوننت کوچک که فقط سه چیز را می‌چرخاند:
    /// آلفا، مقیاس، و یک جابه‌جایی کوتاه از لبه.
    ///
    /// چرا جدا از خود پنل‌ها؟ چون InteractionHUD، کمان، و پنل‌های بعدی همه باید
    /// **دقیقاً یک حرکت** داشته باشند. اگر هر کدام Mathf.MoveTowards خودش را داشته باشد،
    /// نیم‌ثانیه‌ای که به «بسته شدن» نگاه می‌کنی سه حرکت متفاوت می‌بینی.
    ///
    /// کلید ظاهر: هنگام **باز شدن** هم موقع **بسته شدن** از همان منحنی EaseOutBack
    /// استفاده می‌شود. یعنی اول کمی باد می‌کند و بعد جمع می‌شود — همان «پاپ»ی که
    /// در فریم‌های مرجع بود. برای همین یک منحنی، دو جهت کافی است.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PanelMotion : MonoBehaviour
    {
        /// <summary>پنل از کدام لبه می‌آید و به کدام برمی‌گردد.</summary>
        public enum Edge
        {
            Right = 0,
            Left = 1,
            Top = 2,
            Bottom = 3,
            None = 4
        }

        [Header("هدف")]
        [Tooltip("مستطیلی که مقیاس و جابه‌جایی می‌گیرد. خالی = همین آبجکت")]
        [SerializeField] private RectTransform content;

        [Tooltip("خالی = خودکار از همین آبجکت یا فرزندش")]
        [SerializeField] private CanvasGroup group;

        [Header("زمان")]
        [SerializeField, Min(0.02f)] private float openSeconds = 0.20f;
        [SerializeField, Min(0.02f)] private float closeSeconds = 0.14f;

        [Header("مقیاس")]
        [Tooltip("اندازه در حالت بسته؛ ۱ = بدون تغییر اندازه")]
        [SerializeField, Range(0.5f, 1f)] private float hiddenScale = 0.94f;

        [Tooltip("چقدر اول کار باد کند؛ ۰ = بدون باد")]
        [SerializeField, Range(0f, 0.25f)] private float overshoot = 0.07f;

        [Header("جابه‌جایی")]
        [SerializeField] private Edge slideFrom = Edge.Right;
        [SerializeField, Min(0f)] private float slidePixels = 22f;

        [Header("شروع")]
        [SerializeField] private bool startHidden = true;

        private Vector2 _restPosition;
        private bool _restCaptured;
        private bool _configured;

        private float _progress;
        private float _target;

        /// <summary>آیا رو به باز شدن است (یا باز است).</summary>
        public bool IsOpen { get { return _target > 0.5f; } }

        /// <summary>۰ = کامل بسته، ۱ = کامل باز. برای پنلی که وسط راه است.</summary>
        public float Progress { get { return _progress; } }

        /* ============================================================
           تعریف از بیرون — پنل‌هایی که در زمان اجرا ساخته می‌شوند
           ============================================================ */

        /// <summary>
        /// برای پنل‌هایی که در زمان اجرا ساخته می‌شوند. قبل از هر Update صدا زده می‌شود،
        /// پس جای فعلی مستطیل همان جای «باز» است و هیچ حرکتی از دست نمی‌رود.
        /// </summary>
        public void Configure(RectTransform rect, CanvasGroup canvasGroup, float scaleWhenHidden,
            float pixels, Edge edge, float open, float close)
        {
            content = rect;
            group = canvasGroup;
            hiddenScale = Mathf.Clamp(scaleWhenHidden, 0.5f, 1f);
            slidePixels = Mathf.Max(0f, pixels);
            slideFrom = edge;
            openSeconds = Mathf.Max(0.02f, open);
            closeSeconds = Mathf.Max(0.02f, close);

            CaptureRest();
            _configured = true;
        }

        /// <summary>جای «باز» پنل. اگر بعد از ساخت عوض شود، همین را صدا بزن.</summary>
        public void SetRestPosition(Vector2 position)
        {
            _restPosition = position;
            _restCaptured = true;
        }

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            if (content == null) content = transform as RectTransform;
            if (group == null) group = GetComponent<CanvasGroup>();

            // پنل‌های داخل صحنه از قبل تنظیم شده‌اند؛ پنل‌های زمان اجرا
            // با Configure سر و شکل می‌گیرند و تا آن موقع نباید جابه‌جا شوند.
            if (!_configured && content == null) return;

            _configured = true;
            CaptureRest();

            if (!startHidden) return;

            _target = 0f;
            _progress = 0f;
            Apply();
        }

        private void CaptureRest()
        {
            if (content == null) content = transform as RectTransform;
            if (group == null) group = GetComponent<CanvasGroup>();

            if (_restCaptured || content == null) return;

            _restPosition = content.anchoredPosition;
            _restCaptured = true;
        }

        /* ============================================================
           کنترل
           ============================================================ */

        public void Show()
        {
            _target = 1f;
        }

        public void Hide()
        {
            _target = 0f;
        }

        public void SetVisible(bool visible)
        {
            _target = visible ? 1f : 0f;
        }

        /// <summary>بدون انیمیشن، همان‌جا باز یا بسته کن (برای بازسازی رابط و شروع صحنه).</summary>
        public void SnapVisible(bool visible)
        {
            _target = visible ? 1f : 0f;
            _progress = _target;
            Apply();
        }

        private void Update()
        {
            if (!_configured || content == null || group == null) return;

            if (!Mathf.Approximately(_progress, _target))
            {
                float duration = _target > _progress ? openSeconds : closeSeconds;
                _progress = Mathf.MoveTowards(_progress, _target, Time.unscaledDeltaTime / duration);
            }

            Apply();
        }

        /* ============================================================
           حرکت
           ============================================================ */

        private void Apply()
        {
            if (!_configured || content == null || group == null) return;

            if (!_restCaptured)
            {
                _restPosition = content.anchoredPosition;
                _restCaptured = true;
            }

            float eased = EaseOutBack(_progress, overshoot);

            content.localScale = Vector3.one * Mathf.LerpUnclamped(hiddenScale, 1f, eased);
            content.anchoredPosition = _restPosition + Direction * (slidePixels * (1f - eased));

            // آلفا فقط در انتهای حرکت کم می‌شود: پنل باید «جمع شود و برود»، نه اینکه محو شود.
            // تا ۴۰٪ آخرِ بسته شدن، کاملاً روشن است.
            group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_progress / 0.4f));
        }

        private Vector2 Direction
        {
            get
            {
                switch (slideFrom)
                {
                    case Edge.Left: return Vector2.left;
                    case Edge.Top: return Vector2.up;
                    case Edge.Bottom: return Vector2.down;
                    case Edge.None: return Vector2.zero;
                    default: return Vector2.right;
                }
            }
        }

        /// <summary>
        /// منحنی استاندارد «کمی رد شو، بعد برگرد». در t نزدیک ۱ مقدار کمی بالای ۱ است
        /// و همین است که باد کردن اول کار را می‌سازد — در باز شدن و بسته شدن، هر دو.
        /// </summary>
        private static float EaseOutBack(float t, float amplitude)
        {
            t = Mathf.Clamp01(t);

            float u = t - 1f;
            return 1f + (amplitude + 1f) * u * u * u + amplitude * u * u;
        }
    }
}
