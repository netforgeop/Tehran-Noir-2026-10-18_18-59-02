using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// پرده‌ی سیاه روی همه‌ی صحنه‌ها. یک نسخه در کل بازی، بین صحنه‌ها می‌ماند.
    /// هر صحنه که بارگذاری شود، از سیاه به تصویر روشن می‌شود؛
    /// و قبل از رفتن به صحنه‌ی دیگر، FadeOut صدا زده می‌شود تا تصویر آرام سیاه شود.
    /// همه‌چیز با زمان واقعی کار می‌کند، پس در منوی توقف (timeScale = 0) هم روان است.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFade : MonoBehaviour
    {
        /// <summary>همیشه یک نسخه؛ اگر نبود، خودش در اولین صحنه ساخته می‌شود.</summary>
        public static ScreenFade Instance { get; private set; }

        [Tooltip("مدت روشن‌شدن پرده بعد از ورود به هر صحنه")]
        [SerializeField, Min(0f)] private float sceneFadeInSeconds = 0.6f;

        private CanvasGroup _group;
        private float _alpha = 1f;
        private float _target;
        private float _speed = 2f;
        private Action _onDone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Instance != null) return;

            GameObject host = new GameObject("ScreenFade");
            DontDestroyOnLoad(host);
            host.AddComponent<ScreenFade>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Build();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_group == null) return;
            if (Mathf.Approximately(_alpha, _target)) return;

            _alpha = Mathf.MoveTowards(_alpha, _target, Time.unscaledDeltaTime * _speed);
            _group.alpha = _alpha;

            if (!Mathf.Approximately(_alpha, _target)) return;

            Action done = _onDone;
            _onDone = null;

            if (done != null) done();
        }

        /* ============================================================
           کارهای عمومی
           ============================================================ */

        /// <summary>از سیاه به تصویر. برای ورود به صحنه یا برگشتن از منو.</summary>
        public void FadeIn(float duration)
        {
            _onDone = null;

            if (duration <= 0f)
            {
                Snap(0f);
                return;
            }

            _target = 0f;
            _speed = 1f / duration;
        }

        /// <summary>از تصویر به سیاه؛ بعد از تمام‌شدن، onDone صدا زده می‌شود.</summary>
        public void FadeOut(float duration, Action onDone = null)
        {
            _onDone = onDone;

            if (duration <= 0f)
            {
                Snap(1f);

                Action done = _onDone;
                _onDone = null;

                if (done != null) done();
                return;
            }

            _target = 1f;
            _speed = 1f / duration;
        }

        private void Snap(float value)
        {
            _alpha = value;
            _target = value;
            _group.alpha = value;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FadeIn(sceneFadeInSeconds);
        }

        /* ============================================================
           ساخت
           ============================================================ */

        private void Build()
        {
            GameObject canvasObject = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;

            GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            RectTransform imageRect = image.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            _group = canvasObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _alpha = 1f;
            _target = 1f;
        }
    }
}
