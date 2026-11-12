using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Characters
{
    /// <summary>برچسبی که «از نگاه بقیه» به نادر می‌چسبد.</summary>
    public enum ReputationLabel
    {
        Normal = 0,
        Loyal,
        Traitor,
        Upright,
        Disgraced
    }

    /// <summary>
    /// یک قاعده‌ی آبرو: اگر شرط برقرار بود، یک بار این تغییرها را بده.
    ///
    /// «یک بار» مهم است. تغییر آبرو یک رویداد است، نه یک وضعیت: اگر هر فریم
    /// حساب شود، هر بار که منو باز شود یا صحنه لود شود، عدد بی‌نهایت بالا
    /// می‌رود. برای همین هر قاعده یک نشانه در دنیا می‌گذارد و دیگر تکرار نمی‌شود.
    /// </summary>
    public sealed class ReputationRule
    {
        public readonly string Id;
        public readonly string Marker;
        public readonly int Honor;
        public readonly int Honesty;
        public readonly int Loyalty;

        public ReputationRule(string id, int honor, int honesty, int loyalty)
        {
            Id = id;
            Marker = "flag.rep." + id;
            Honor = honor;
            Honesty = honesty;
            Loyalty = loyalty;
        }
    }

    /// <summary>
    /// سیستم آبرو — نوارهای شرافت، صداقت و وفاداری.
    ///
    /// قاعده‌ی طراحی (از `docs/19`): این سه عدد از **باور**ها حرکت می‌کنند، نه
    /// از واقعیت. دروغی که رضا باورش کرده و هرگز لو نرفته، آبرو را خراب نمی‌کند؛
    /// فقط خودِ بازیکن می‌داند. به همین دلیل تنها شرطی که صداقت را پایین می‌آورد
    /// `known.reza.lie` است، نه `flag.lie.seal`.
    ///
    /// چرا این کلاس روی WorldState کار می‌کند و نه روی CharacterStats؟ چون
    /// CharacterStats برای خواندن و نوشتن به WorldStateProvider صحنه نیاز دارد.
    /// این‌طوری همان قواعد را می‌شود بدون صحنه و بدون Unity آزمود — همان کاری
    /// که `ReputationSelfTest` می‌کند.
    ///
    /// سه عدد در همان کلیدهای `stat.honor` / `stat.honesty` / `stat.loyalty`
    /// می‌نشینند که از قبل وجود دارند. پس حلقه‌های HUD و تب کاراکتر خودبه‌خود
    /// همین‌ها را نشان می‌دهند و یک منبع حقیقت داریم، نه دو تا.
    /// </summary>
    public static class Reputation
    {
        public const string HonorKey = CharacterStats.HonorKey;
        public const string HonestyKey = CharacterStats.HonestyKey;
        public const string LoyaltyKey = CharacterStats.LoyaltyKey;

        /// <summary>
        /// همه‌ی قواعد، به ترتیبی که اعمال می‌شوند.
        ///
        /// عددها عمداً کوچک‌اند: یک تصمیم، آدم را عوض نمی‌کند؛ چند تصمیم عوض می‌کند.
        /// </summary>
        private static readonly ReputationRule[] Rules =
        {
            // کار را درست انجام دادی، وقتی کسی نگاه نمی‌کرد.
            new ReputationRule("logged", 6, 0, 0),

            // کارِ سپرده‌شده را تا آخر بردی.
            new ReputationRule("delivered", 0, 0, 8),

            // مهر شکسته را خودت گفتی، بدون این‌که گیر بیفتی.
            new ReputationRule("confess", 10, 12, 0),

            // رضا فهمید دروغ گفتی: هم دروغ، هم خیانت به همکار.
            new ReputationRule("caught", -6, -18, -14)
        };

        /* ============================================================
           اعمال
           ============================================================ */

        /// <summary>
        /// یک دور بررسی. هر قاعده‌ای که تازه برقرار شده باشد یک بار اعمال می‌شود.
        /// اگر دنیا null باشد، هیچ کاری نمی‌کند.
        /// </summary>
        public static void Tick(WorldState world)
        {
            if (world == null) return;

            for (int i = 0; i < Rules.Length; i++)
            {
                ReputationRule rule = Rules[i];

                if (world.Get(rule.Marker).Truthy()) continue;
                if (!Holds(rule.Id, world)) continue;

                Apply(world, rule);

                world.Set(rule.Marker, true);

                if (rule.Honor != 0 || rule.Honesty != 0 || rule.Loyalty != 0)
                    Debug.Log("[Reputation] " + rule.Id + "  →  شرافت " + Signed(rule.Honor) +
                              "، صداقت " + Signed(rule.Honesty) + "، وفاداری " + Signed(rule.Loyalty));
            }
        }

        /// <summary>شرط هر قاعده، به‌صورت خوانا و جدا از اعمال.</summary>
        private static bool Holds(string id, WorldState world)
        {
            switch (id)
            {
                case "logged":
                    return world.Get("flag.archive.logged").Truthy();

                case "delivered":
                    return world.Get("flag.quest.file").AsNumber() >= 2d;

                case "confess":
                    return world.Get("flag.claim.seal").AsText() == "opened";

                case "caught":
                    return world.Get("known.reza.lie").Truthy();

                default:
                    return false;
            }
        }

        private static void Apply(WorldState world, ReputationRule rule)
        {
            Add(world, HonorKey, rule.Honor);
            Add(world, HonestyKey, rule.Honesty);
            Add(world, LoyaltyKey, rule.Loyalty);
        }

        /// <summary>جمع با محدوده‌ی همان آمار؛ مقدار نبود، پیش‌فرض خودش.</summary>
        private static void Add(WorldState world, string key, int delta)
        {
            if (delta == 0) return;

            int min = 0;
            int max = 100;
            int fallback = 50;

            for (int i = 0; i < CharacterStats.All.Count; i++)
            {
                StatDefinition definition = CharacterStats.All[i];

                if (definition.Key != key) continue;

                min = definition.Min;
                max = definition.Max;
                fallback = definition.Default;
                break;
            }

            int current = fallback;

            if (world.Has(key))
            {
                double value = world.Get(key).AsNumber();

                if (!double.IsNaN(value)) current = Mathf.RoundToInt((float)value);
            }

            world.Set(key, (double)Mathf.Clamp(current + delta, min, max));
        }

        private static string Signed(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        /* ============================================================
           برچسب
           ============================================================ */

        /// <summary>
        /// برچسب از همان سه عدد حساب می‌شود، نوار جدا ندارد (`docs/19`).
        ///
        /// ترتیب بررسی عمدی است: برچسب بد زودتر از برچسب خوب بررسی می‌شود، تا
        /// کسی که هم شرافت ۸۰ دارد هم وفاداری ۲۰، «خیانت‌کار» دیده شود نه
        /// «درستکار». آبروی خراب چیزی است که بقیه اول می‌بینند.
        /// </summary>
        public static ReputationLabel LabelOf(int honor, int honesty, int loyalty)
        {
            if (loyalty <= 30) return ReputationLabel.Traitor;
            if (honor <= 30) return ReputationLabel.Disgraced;
            if (loyalty >= 70 && honesty >= 50) return ReputationLabel.Loyal;
            if (honor >= 70) return ReputationLabel.Upright;

            return ReputationLabel.Normal;
        }

        public static ReputationLabel Label(WorldState world)
        {
            if (world == null) return ReputationLabel.Normal;

            return LabelOf(Read(world, HonorKey), Read(world, HonestyKey), Read(world, LoyaltyKey));
        }

        public static string KeyOf(ReputationLabel label)
        {
            switch (label)
            {
                case ReputationLabel.Loyal: return "rep.label.loyal";
                case ReputationLabel.Traitor: return "rep.label.traitor";
                case ReputationLabel.Upright: return "rep.label.upright";
                case ReputationLabel.Disgraced: return "rep.label.disgraced";
                default: return "rep.label.normal";
            }
        }

        private static int Read(WorldState world, string key)
        {
            int fallback = 50;

            for (int i = 0; i < CharacterStats.All.Count; i++)
            {
                StatDefinition definition = CharacterStats.All[i];

                if (definition.Key != key) continue;

                fallback = definition.Default;
                break;
            }

            if (!world.Has(key)) return fallback;

            double value = world.Get(key).AsNumber();

            return double.IsNaN(value) ? fallback : Mathf.RoundToInt((float)value);
        }
    }
}
