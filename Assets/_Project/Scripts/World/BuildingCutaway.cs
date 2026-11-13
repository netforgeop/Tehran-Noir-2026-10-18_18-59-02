using System.Collections.Generic;
using TehranNoir.Player;
using UnityEngine;

namespace TehranNoir.World
{
    /// <summary>
    /// برش ساختمان: از بیرون کل ساختمان دیده می‌شود، از داخل فقط همان طبقه.
    ///
    /// چرا لازم شد؟ دوربین این بازی بالای سر بازیکن است و تقریباً عمود نگاه
    /// می‌کند. در ساختمانی که سقف دارد، دوربین سقف را می‌بیند و بازیکن فقط
    /// یک تخته‌ی قهوه‌ای می‌بیند — همان چیزی که در راهروی طبقه‌ی هشتم دیده
    /// شد. سیمز و پروجکت زامبوید همین را با برداشتن سقف و طبقه‌های بالایی حل
    /// می‌کنند؛ این هم همان.
    ///
    /// قاعده، دو خط است:
    ///   بیرون  → همه‌چیز روشن. ساختمان کامل است.
    ///   داخلِ طبقه‌ی n → تکه‌های بالاتر از n خاموش، سقفِ خودِ n هم خاموش.
    ///
    /// سقفِ هر طبقه، کفِ طبقه‌ی بالاست؛ پس با خاموش‌کردن طبقه‌ی بالایی، سقف
    /// خودش می‌رود. برای ساختمان‌هایی که سقفشان جدا ساخته شده (مثل راهروی
    /// هشتم) یک تکه‌ی `Ceiling` هم می‌شود گذاشت.
    ///
    /// این اسکریپت به هندسه دست نمی‌زند؛ فقط `Renderer.enabled` را عوض
    /// می‌کند. پس فیزیک، کولایدر و دیالوگ‌ها دست‌نخورده می‌مانند.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingCutaway : MonoBehaviour
    {
        [Header("بیننده")]
        [Tooltip("معمولاً Transform بازیکن. خالی بماند تا خودش پیدایش کند")]
        [SerializeField] private Transform viewer;

        [Header("حجم داخلی")]
        [Tooltip("مرکزِ جعبه‌ی داخلی، در فضای محلیِ همین آبجکت")]
        [SerializeField] private Vector3 interiorCenter = new Vector3(0f, 1.4f, 0f);

        [Tooltip("اندازه‌ی جعبه‌ی داخلی؛ بیرونِ آن یعنی «بیرون از ساختمان»")]
        [SerializeField] private Vector3 interiorSize = new Vector3(16f, 2.8f, 12f);

        [Tooltip("چند متر بیرون‌تر بروی تا «بیرون» حساب شود")]
        [SerializeField] private float exitMargin = 0.7f;

        [Header("طبقه‌ها")]
        [Tooltip("ارتفاع هر طبقه؛ طبقه = کفِ بازیکن تقسیم بر این عدد")]
        [SerializeField] private float floorHeight = 3f;

        [Tooltip("ارتفاعِ کفِ طبقه‌ی صفر در فضای محلی")]
        [SerializeField] private float groundLevel;

        [Header("اجرا")]
        [Tooltip("هر چند ثانیه یک بار حساب شود؛ هر فریم لازم نیست")]
        [SerializeField] private float checkInterval = 0.1f;

        [SerializeField] private bool logChanges = true;

        private readonly List<CutawayPiece> pieces = new List<CutawayPiece>();
        private float timer;
        private bool collected;
        private bool appliedOnce;
        private bool appliedInside;
        private int appliedLevel = int.MinValue;

        private void Awake()
        {
            Collect();
        }

        private void OnEnable()
        {
            Collect();
            Refresh(true);
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer < checkInterval) return;

            timer = 0f;
            Refresh(false);
        }

        /// <summary>
        /// برای وقتی که بازیکن را جابه‌جا می‌کنید (تلپورت، شروع صحنه) و
        /// نمی‌خواهید تا فریم بعد صبر کنید.
        /// </summary>
        public void RefreshNow()
        {
            Refresh(true);
        }

        private void Collect()
        {
            pieces.Clear();

            CutawayPiece[] found = GetComponentsInChildren<CutawayPiece>(true);

            for (int i = 0; i < found.Length; i++)
            {
                CutawayPiece piece = found[i];

                if (piece == null) continue;

                // تکه‌ی تودرتو یعنی یک رندرر دو بار خاموش و روشن می‌شود و
                // آخرین حرف برنده است. اصلاً واردش نمی‌شویم.
                if (IsNested(piece)) continue;

                piece.Cache();
                pieces.Add(piece);
            }

            collected = pieces.Count > 0;

            // سکوت بدترین حالت است: اگر تکه‌ها پیدا نشوند، برش هیچ کاری
            // نمی‌کند و بازیکن فقط یک تخته‌ی قهوه‌ای می‌بیند.
            if (!collected)
            {
                Debug.LogWarning("[World] BuildingCutaway روی «" + name +
                                 "» هیچ CutawayPiece زیر خودش پیدا نکرد. " +
                                 "مدیر باید روی ریشه‌ی ساختمان باشد، نه روی یک آبجکت تازه.",
                                 this);
            }
        }

        private bool IsNested(CutawayPiece piece)
        {
            Transform parent = piece.transform.parent;

            while (parent != null && parent != transform)
            {
                if (parent.GetComponent<CutawayPiece>() != null) return true;
                parent = parent.parent;
            }

            return false;
        }

        private void Refresh(bool force)
        {
            if (!collected) Collect();

            if (viewer == null) viewer = FindViewer();

            if (viewer == null) return;

            Vector3 local = transform.InverseTransformPoint(viewer.position);

            bool inside = Contains(local, appliedInside);
            int level = Mathf.FloorToInt((local.y - groundLevel) / Mathf.Max(0.1f, floorHeight));

            if (!force && appliedOnce && inside == appliedInside && level == appliedLevel)
                return;

            appliedOnce = true;
            appliedInside = inside;
            appliedLevel = level;

            Apply(inside, level);
        }

        private bool Contains(Vector3 local, bool inflated)
        {
            return Contains(local, interiorCenter, interiorSize,
                            inflated ? exitMargin : 0f);
        }

        /// <summary>
        /// آیا این نقطه (در فضای محلیِ ساختمان) داخل جعبه‌ی داخلی است؟
        /// `margin` مثبت، جعبه را بزرگ می‌کند؛ همان کاری که «تا یک قدم
        /// بیرونِ دیوار، هنوز داخل حساب شود» می‌کند و جلوی چشمک‌زدن برش را
        /// روی آستانه می‌گیرد.
        /// </summary>
        public static bool Contains(Vector3 local, Vector3 center, Vector3 size, float margin)
        {
            Vector3 half = size * 0.5f;
            Vector3 offset = local - center;

            return Mathf.Abs(offset.x) <= half.x + margin
                && Mathf.Abs(offset.y) <= half.y + margin
                && Mathf.Abs(offset.z) <= half.z + margin;
        }

        private void Apply(bool inside, int level)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                CutawayPiece piece = pieces[i];

                if (piece == null) continue;

                piece.SetVisible(piece.IsVisible(inside, level));
            }

            if (logChanges)
            {
                Debug.Log("[World] برش ساختمان: " +
                          (inside ? "داخل، طبقه‌ی " + level : "بیرون"), this);
            }
        }

        /// <summary>
        /// بازیکن تگ ندارد، پس با نوعش پیدا می‌شود. `FindObjectOfType` در
        /// Unity 6 منسوخ است؛ `FindFirstObjectByType` جانشینش است.
        /// </summary>
        private static Transform FindViewer()
        {
            PlayerController controller = FindFirstObjectByType<PlayerController>();

            return controller != null ? controller.transform : null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireCube(interiorCenter, interiorSize);
        }
    }
}
