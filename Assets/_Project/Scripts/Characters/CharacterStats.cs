using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Characters
{
    /// <summary>عددی که شخصیت نادر را می‌سازد.</summary>
    public enum StatId
    {
        Money = 0,
        Health,
        Armor,
        Stamina,
        Honor,
        Charisma,
        Fame,
        Honesty,
        Loyalty
    }

    /// <summary>
    /// تعریف یک آمار: کلیدش در WorldState، نام دوزبانه، بازه، مقدار پیش‌فرض،
    /// و گام تغییر (برای پنل تست و بعداً برای هر جای دیگری که عدد را کم و زیاد می‌کند).
    /// </summary>
    public sealed class StatDefinition
    {
        public readonly StatId Id;
        public readonly string Key;
        public readonly string Persian;
        public readonly string English;
        public readonly int Min;
        public readonly int Max;
        public readonly int Default;
        public readonly int Step;
        public readonly int BigStep;

        public StatDefinition(StatId id, string key, string persian, string english,
            int min, int max, int defaultValue, int step, int bigStep)
        {
            Id = id;
            Key = key;
            Persian = persian;
            English = english;
            Min = min;
            Max = max;
            Default = defaultValue;
            Step = step;
            BigStep = bigStep;
        }

        public string Label(bool persian)
        {
            return persian ? Persian : English;
        }
    }

    /// <summary>
    /// آمار کاراکتر، تنها روی WorldState: هیچ عددی جای دیگری نگه داشته نمی‌شود.
    /// چرا؟ چون دیالوگ‌ها و کارها از قبل با همان زبان شرط کار می‌کنند:
    ///   [if] stat.honor >= 60
    ///   [do] stat.money + 500
    /// پس آمار از روز اول بخشی از دنیاست، نه یک HUD تزئینی.
    ///
    /// مقیاس‌ها:
    ///   پول: تومان، بی‌سقف
    ///   جان و زره: ۰ تا ۱۰۰
    ///   استقامت: -۱۵۰ تا ۱۰۰ (منفی یعنی مصرف بیش از حد)
    ///   پنج ویژگی: ۰ تا ۱۰۰
    /// </summary>
    public static class CharacterStats
    {
        public const string MoneyKey = "stat.money";
        public const string HealthKey = "stat.health";
        public const string ArmorKey = "stat.armor";
        public const string StaminaKey = "stat.stamina";
        public const string HonorKey = "stat.honor";
        public const string CharismaKey = "stat.charisma";
        public const string FameKey = "stat.fame";
        public const string HonestyKey = "stat.honesty";
        public const string LoyaltyKey = "stat.loyalty";

        /// <summary>جمع جان و زره؛ کمانِ چپ همین ۲۰۰ را نشان می‌دهد.</summary>
        public const int VitalMax = 200;

        private static readonly StatDefinition[] Definitions =
        {
            new StatDefinition(StatId.Money, MoneyKey, "پول", "Money", 0, 99999999, 4500, 500, 5000),
            new StatDefinition(StatId.Health, HealthKey, "جان", "Health", 0, 100, 100, 5, 25),
            new StatDefinition(StatId.Armor, ArmorKey, "زره", "Armor", 0, 100, 0, 5, 25),
            new StatDefinition(StatId.Stamina, StaminaKey, "استقامت", "Stamina", -150, 100, 100, 10, 25),
            new StatDefinition(StatId.Honor, HonorKey, "شرافت", "Honor", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Charisma, CharismaKey, "جذبه", "Charisma", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Fame, FameKey, "شهرت", "Fame", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Honesty, HonestyKey, "صداقت", "Honesty", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Loyalty, LoyaltyKey, "وفاداری", "Loyalty", 0, 100, 50, 5, 25)
        };

        /// <summary>پنج ویژگی‌ای که کمان سمت راست یکی‌یکی نشانشان می‌دهد.</summary>
        public static readonly StatId[] Traits =
        {
            StatId.Honor, StatId.Charisma, StatId.Fame, StatId.Honesty, StatId.Loyalty
        };

        public static IReadOnlyList<StatDefinition> All
        {
            get { return Definitions; }
        }

        public static StatDefinition Definition(StatId id)
        {
            return Definitions[(int)id];
        }

        private static WorldState World
        {
            get
            {
                return WorldStateProvider.Instance != null
                    ? WorldStateProvider.Instance.State
                    : null;
            }
        }

        /// <summary>خواندن؛ اگر کلید در دنیا نبود، مقدار پیش‌فرض نوشته می‌شود.</summary>
        public static int Get(StatId id)
        {
            StatDefinition definition = Definitions[(int)id];
            WorldState world = World;

            if (world == null) return definition.Default;

            if (!world.Has(definition.Key))
            {
                world.Set(definition.Key, (double)definition.Default);
                return definition.Default;
            }

            double value = world.Get(definition.Key).AsNumber();

            if (double.IsNaN(value)) return definition.Default;

            return Mathf.Clamp(Mathf.RoundToInt((float)value), definition.Min, definition.Max);
        }

        /// <summary>خواندن اعشاری — استقامت بین دو عدد صحیح هم معنی دارد.</summary>
        public static float GetFloat(StatId id)
        {
            StatDefinition definition = Definitions[(int)id];
            WorldState world = World;

            if (world == null) return definition.Default;

            if (!world.Has(definition.Key))
            {
                world.Set(definition.Key, (double)definition.Default);
                return definition.Default;
            }

            double value = world.Get(definition.Key).AsNumber();

            if (double.IsNaN(value)) return definition.Default;

            return Mathf.Clamp((float)value, definition.Min, definition.Max);
        }

        public static void Set(StatId id, int value)
        {
            StatDefinition definition = Definitions[(int)id];
            WorldState world = World;

            if (world == null) return;

            world.Set(definition.Key, (double)Mathf.Clamp(value, definition.Min, definition.Max));
        }

        public static void SetFloat(StatId id, float value)
        {
            StatDefinition definition = Definitions[(int)id];
            WorldState world = World;

            if (world == null) return;

            world.Set(definition.Key, (double)Mathf.Clamp(value, definition.Min, definition.Max));
        }

        public static void Add(StatId id, int delta)
        {
            Set(id, Get(id) + delta);
        }

        /* ============================================================
           آسیب و درمان — زره اول می‌گیرد، بعد جان
           ============================================================ */

        /// <summary>
        /// مقدار واقعی آسیب پس از کسر زره را برمی‌گرداند.
        /// زره هر چه دارد جذب می‌کند؛ باقی‌مانده از جان کم می‌شود.
        /// </summary>
        public static int Damage(int amount)
        {
            if (amount <= 0) return 0;

            int armor = Get(StatId.Armor);
            int absorbed = Mathf.Min(armor, amount);

            if (absorbed > 0) Set(StatId.Armor, armor - absorbed);

            int rest = amount - absorbed;
            if (rest <= 0) return 0;

            Set(StatId.Health, Get(StatId.Health) - rest);
            return rest;
        }

        public static void Heal(int amount)
        {
            if (amount <= 0) return;

            Set(StatId.Health, Get(StatId.Health) + amount);
        }

        public static bool IsDead
        {
            get { return Get(StatId.Health) <= 0; }
        }

        /// <summary>پول واحد دارد، بقیه عدد خالی‌اند.</summary>
        public static string Format(StatId id, bool persian)
        {
            string number = Get(id).ToString("N0", CultureInfo.InvariantCulture);

            if (id != StatId.Money) return number;

            return persian ? number + " تومان" : number + " toman";
        }

        /// <summary>هر کلید را در دنیا بساز تا از همان اول در فهرست متغیرها دیده شوند.</summary>
        public static void EnsureDefaults()
        {
            for (int i = 0; i < Definitions.Length; i++)
                Get(Definitions[i].Id);
        }
    }
}
