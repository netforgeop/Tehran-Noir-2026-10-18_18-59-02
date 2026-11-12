using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Characters
{
    /// <summary>هفت عددی که شخصیت نادر را می‌سازند.</summary>
    public enum StatId
    {
        Money = 0,
        Health,
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
    /// مقیاس: پول تومان است (بی‌سقف)، بقیه ۰ تا ۱۰۰.
    /// </summary>
    public static class CharacterStats
    {
        public const string MoneyKey = "stat.money";
        public const string HealthKey = "stat.health";
        public const string HonorKey = "stat.honor";
        public const string CharismaKey = "stat.charisma";
        public const string FameKey = "stat.fame";
        public const string HonestyKey = "stat.honesty";
        public const string LoyaltyKey = "stat.loyalty";

        private static readonly StatDefinition[] Definitions =
        {
            new StatDefinition(StatId.Money, MoneyKey, "پول", "Money", 0, 99999999, 4500, 500, 5000),
            new StatDefinition(StatId.Health, HealthKey, "جان", "Health", 0, 100, 100, 5, 25),
            new StatDefinition(StatId.Honor, HonorKey, "شرافت", "Honor", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Charisma, CharismaKey, "جذبه", "Charisma", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Fame, FameKey, "شهرت", "Fame", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Honesty, HonestyKey, "صداقت", "Honesty", 0, 100, 50, 5, 25),
            new StatDefinition(StatId.Loyalty, LoyaltyKey, "وفاداری", "Loyalty", 0, 100, 50, 5, 25)
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

        public static void Set(StatId id, int value)
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

        /// <summary>پول واحد دارد، بقیه عدد خالی‌اند.</summary>
        public static string Format(StatId id, bool persian)
        {
            string number = Get(id).ToString("N0", CultureInfo.InvariantCulture);

            if (id != StatId.Money) return number;

            return persian ? number + " تومان" : number + " toman";
        }

        /// <summary>هر هفت کلید را در دنیا بساز تا از همان اول در فهرست متغیرها دیده شوند.</summary>
        public static void EnsureDefaults()
        {
            for (int i = 0; i < Definitions.Length; i++)
                Get(Definitions[i].Id);
        }
    }
}
