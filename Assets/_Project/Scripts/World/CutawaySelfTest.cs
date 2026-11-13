using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.World
{
    /// <summary>
    /// تست خودکار برش ساختمان، بدون پکیج Test Framework.
    ///
    /// چه چیزی را ثابت می‌کند؟
    ///   ۱. بیرون از ساختمان هیچ‌چیز پنهان نمی‌شود؛ ساختمان کامل است.
    ///   ۲. داخل طبقه‌ی n، سقفِ همان طبقه دیده نمی‌شود.
    ///   ۳. داخل طبقه‌ی n، طبقه‌های پایین‌تر دیده می‌شوند و بالاترها نه.
    ///   ۴. جعبه‌ی داخلی حاشیه دارد: یک قدم بیرونِ دیوار هنوز «داخل» است.
    ///
    /// چرا مهم است؟ چون این سیستم فقط رندرر خاموش می‌کند و اگر برعکس عمل کند،
    /// هیچ خطایی نمی‌دهد — فقط بازیکن یک تخته‌ی قهوه‌ای می‌بیند. همان چیزی
    /// که در راهروی طبقه‌ی هشتم دیده شد.
    ///
    /// استفاده: با باز شدن صحنه خودش اجرا می‌شود (روی `SceneMarker` هم می‌شود
    /// گذاشتش). گزارش در Console می‌آید. در بیلد، `runOnStart` را خاموش کن.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutawaySelfTest : MonoBehaviour
    {
        [Header("اجرا")]
        [Tooltip("با شروع بازی خودکار اجرا شود")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("هر چند طبقه بالا و پایین تست شود")]
        [SerializeField] private int levels = 3;

        private readonly List<string> _report = new List<string>(32);

        private int _passed;
        private int _failed;

        private void Start()
        {
            if (runOnStart) Run();
        }

        [ContextMenu("اجرای تست برش")]
        public void Run()
        {
            _report.Clear();
            _passed = 0;
            _failed = 0;

            GameObject root = new GameObject("CutawaySelfTest_Root");

            CutawayPiece[] floors = new CutawayPiece[levels + 1];
            CutawayPiece[] ceilings = new CutawayPiece[levels + 1];

            for (int level = 0; level <= levels; level++)
            {
                floors[level] = MakePiece(root.transform, CutawayRole.Floor, level);
                ceilings[level] = MakePiece(root.transform, CutawayRole.Ceiling, level);
            }

            // ۱) بیرون: همه‌چیز روشن، در هر طبقه‌ای که باشیم.
            for (int level = 0; level <= levels; level++)
            {
                Check("بیرون: کفِ " + level + " دیده می‌شود",
                      floors[level].IsVisible(false, int.MinValue));
                Check("بیرون: سقفِ " + level + " دیده می‌شود",
                      ceilings[level].IsVisible(false, int.MinValue));
            }

            // ۲) و ۳) داخل هر طبقه.
            for (int current = 0; current <= levels; current++)
            {
                Check("داخل " + current + ": سقفِ خودش پنهان است",
                      !ceilings[current].IsVisible(true, current));

                for (int level = 0; level <= levels; level++)
                {
                    bool floorExpected = level <= current;
                    bool ceilingExpected = level > current;

                    Check("داخل " + current + ": کفِ " + level +
                          (floorExpected ? " دیده می‌شود" : " پنهان است"),
                          floors[level].IsVisible(true, current) == floorExpected);

                    Check("داخل " + current + ": سقفِ " + level +
                          (ceilingExpected ? " دیده می‌شود" : " پنهان است"),
                          ceilings[level].IsVisible(true, current) == ceilingExpected);
                }
            }

            // ۴) حاشیه‌ی داخلی — با همان تابعی که خودِ مدیر استفاده می‌کند.
            Vector3 center = new Vector3(0f, 1.4f, 0f);
            Vector3 size = new Vector3(16.4f, 2.8f, 12.4f);

            Check("داخل: وسط ساختمان",
                  BuildingCutaway.Contains(new Vector3(0f, 1.4f, 0f), center, size, 0f));
            Check("حاشیه: یک قدم بیرونِ دیوار هنوز داخل است",
                  BuildingCutaway.Contains(new Vector3(0f, 1.4f, 6.5f), center, size, 0.7f));
            Check("حاشیه: دو قدم بیرون، بیرون است",
                  !BuildingCutaway.Contains(new Vector3(0f, 1.4f, 7.2f), center, size, 0.7f));
            Check("بالای بام: بیرون است",
                  !BuildingCutaway.Contains(new Vector3(0f, 5.0f, 0f), center, size, 0.7f));

            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);

            Debug.Log("[World] تست برش ساختمان — " + _passed + " قبول، " + _failed + " رد" +
                      (_failed > 0 ? "\n" + string.Join("\n", _report.ToArray()) : ""), this);
        }

        private CutawayPiece MakePiece(Transform parent, CutawayRole role, int level)
        {
            GameObject go = new GameObject(role + "_" + level);
            go.transform.SetParent(parent, false);

            CutawayPiece piece = go.AddComponent<CutawayPiece>();
            piece.Configure(role, level);

            return piece;
        }

        private void Check(string what, bool passed)
        {
            if (passed)
            {
                _passed++;
                return;
            }

            _failed++;
            _report.Add("✗ " + what);
        }
    }
}
