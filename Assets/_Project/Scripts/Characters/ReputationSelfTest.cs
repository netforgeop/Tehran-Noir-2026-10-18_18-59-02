using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Characters
{
    /// <summary>
    /// تست خودکار آبرو، بدون پکیج Test Framework.
    ///
    /// چه چیزی را ثابت می‌کند؟
    ///   ۱. قاعده‌ها یک بار اعمال می‌شوند، نه هر دور.
    ///   ۲. دروغِ لو نرفته آبرو را خراب نمی‌کند؛ فقط دروغِ فهمیده‌شده.
    ///   ۳. برچسب از همان سه عدد حساب می‌شود و برچسب بد بر برچسب خوب مقدم است.
    ///
    /// چرا مهم است؟ چون این سیستم مثل دفتر ثبت اشتباه‌های بی‌صداست: اگر دو بار
    /// اعمال شود، هیچ خطایی نمی‌دهد، فقط آبروی بازیکن بی‌دلیل خراب می‌شود.
    ///
    /// استفاده: یک GameObject خالی بساز، این کامپوننت را اضافه کن، Play.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReputationSelfTest : MonoBehaviour
    {
        [Header("اجرا")]
        [Tooltip("با شروع بازی خودکار اجرا شود")]
        [SerializeField] private bool runOnStart = true;

        private readonly List<string> _report = new List<string>(16);

        private int _passed;
        private int _failed;

        private void Start()
        {
            if (runOnStart) Run();
        }

        [ContextMenu("اجرای تست آبرو")]
        public void Run()
        {
            _report.Clear();
            _passed = 0;
            _failed = 0;

            // ۱) دنیای تازه: هر سه عدد ۵۰ و برچسب «معمولی».
            WorldState world = new WorldState();

            Check("شروع از پنجاه",
                Read(world, Reputation.HonorKey) == 50 &&
                Read(world, Reputation.HonestyKey) == 50 &&
                Read(world, Reputation.LoyaltyKey) == 50);

            Check("برچسب شروع معمولی",
                Reputation.Label(world) == ReputationLabel.Normal,
                Reputation.Label(world).ToString());

            // ۲) هیچ شرطی برقرار نیست: یک دور بی‌اثر است.
            Reputation.Tick(world);

            Check("دنیای خالی چیزی عوض نمی‌کند",
                Read(world, Reputation.HonorKey) == 50 && Read(world, Reputation.LoyaltyKey) == 50);

            // ۳) دروغِ لو نرفته: فقط flag.lie.seal روشن است.
            world.Set("flag.lie.seal", true);
            Reputation.Tick(world);

            Check("دروغ لو نرفته آبرو را خراب نمی‌کند",
                Read(world, Reputation.HonestyKey) == 50);

            // ۴) تحویل کار: وفاداری +۸.
            world.Set("flag.quest.file", 2d);
            Reputation.Tick(world);

            Check("تحویل کار وفاداری را بالا می‌برد", Read(world, Reputation.LoyaltyKey) == 58,
                Read(world, Reputation.LoyaltyKey).ToString());

            // ۵) یک بار، نه دو بار.
            Reputation.Tick(world);

            Check("قاعده دو بار اعمال نمی‌شود", Read(world, Reputation.LoyaltyKey) == 58,
                Read(world, Reputation.LoyaltyKey).ToString());

            // ۶) اعتراف: شرافت +۱۰ و صداقت +۱۲.
            world.Set("flag.claim.seal", "opened");
            Reputation.Tick(world);

            Check("اعتراف شرافت را بالا می‌برد", Read(world, Reputation.HonorKey) == 60,
                Read(world, Reputation.HonorKey).ToString());

            Check("اعتراف صداقت را بالا می‌برد", Read(world, Reputation.HonestyKey) == 62,
                Read(world, Reputation.HonestyKey).ToString());

            // ۷) لو رفتن: صداقت −۱۸، شرافت −۶، وفاداری −۱۴.
            world.Set("known.reza.lie", true);
            Reputation.Tick(world);

            Check("لو رفتن صداقت را پایین می‌آورد", Read(world, Reputation.HonestyKey) == 44,
                Read(world, Reputation.HonestyKey).ToString());

            Check("لو رفتن وفاداری را پایین می‌آورد", Read(world, Reputation.LoyaltyKey) == 44,
                Read(world, Reputation.LoyaltyKey).ToString());

            // ۸) محدوده: عدد از ۰ پایین‌تر نمی‌رود.
            WorldState floor = new WorldState();
            floor.Set(Reputation.HonestyKey, 5d);
            floor.Set("known.reza.lie", true);
            Reputation.Tick(floor);

            Check("صداقت زیر صفر نمی‌رود", Read(floor, Reputation.HonestyKey) == 0,
                Read(floor, Reputation.HonestyKey).ToString());

            // ۹) شاهد چای‌خانه: تهدید سه عدد را پایین می‌آورد، حمایت شرافت را بالا.
            WorldState threat = new WorldState();
            threat.Set("flag.witness.threat", true);
            Reputation.Tick(threat);

            Check("تهدید شاهد شرافت را پایین می‌آورد", Read(threat, Reputation.HonorKey) == 42,
                Read(threat, Reputation.HonorKey).ToString());

            Check("تهدید شاهد صداقت را پایین می‌آورد", Read(threat, Reputation.HonestyKey) == 45,
                Read(threat, Reputation.HonestyKey).ToString());

            Reputation.Tick(threat);

            Check("تهدید شاهد دوباره حساب نمی‌شود", Read(threat, Reputation.HonorKey) == 42,
                Read(threat, Reputation.HonorKey).ToString());

            WorldState protect = new WorldState();
            protect.Set("flag.witness.protect", true);
            Reputation.Tick(protect);

            Check("حمایت شاهد شرافت را بالا می‌برد", Read(protect, Reputation.HonorKey) == 58,
                Read(protect, Reputation.HonorKey).ToString());

            Check("حمایت شاهد وفاداری را بالا می‌برد", Read(protect, Reputation.LoyaltyKey) == 54,
                Read(protect, Reputation.LoyaltyKey).ToString());

            // ۱۰) برچسب‌ها.
            Check("برچسب درستکار", Reputation.LabelOf(72, 55, 55) == ReputationLabel.Upright);
            Check("برچسب وفادار", Reputation.LabelOf(55, 60, 72) == ReputationLabel.Loyal);
            Check("برچسب خیانت‌کار", Reputation.LabelOf(90, 55, 25) == ReputationLabel.Traitor,
                "شرافت بالا نباید خیانت را بپوشاند");
            Check("برچسب بدنام", Reputation.LabelOf(20, 55, 55) == ReputationLabel.Disgraced);

            Report();
        }

        /* ============================================================
           ابزار
           ============================================================ */

        /// <summary>
        /// خواندن با پیش‌فرض ۵۰. بدون این، کلیدِ نانوشته NaN می‌دهد و
        /// گرد کردنش صفر می‌شود — تست اول همین‌جا می‌شکست، نه در کد اصلی.
        /// </summary>
        private static int Read(WorldState world, string key)
        {
            if (!world.Has(key)) return 50;

            double value = world.Get(key).AsNumber();

            return double.IsNaN(value) ? 50 : Mathf.RoundToInt((float)value);
        }

        private void Check(string name, bool ok, string detail = "")
        {
            if (ok) _passed++;
            else _failed++;

            _report.Add((ok ? "PASS  " : "FAIL  ") + name +
                        (string.IsNullOrEmpty(detail) ? string.Empty : "  → " + detail));
        }

        private void Report()
        {
            Debug.Log("[Reputation] تست: " + _passed + " قبول، " + _failed + " رد\n" +
                      string.Join("\n", _report));
        }
    }
}
