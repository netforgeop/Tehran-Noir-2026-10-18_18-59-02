using System;
using System.Collections.Generic;
using System.Globalization;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// وضعیتِ یک نمونه، بدون کلاس جدا برای هر دسته.
    ///
    /// هر آیتم می‌تواند هر تعداد کلید وضعیت داشته باشد:
    ///   state.sealed = 1
    ///   state.read   = 0
    ///   state.ammo   = 8
    ///   state.owner  = "nader"
    ///
    /// قاعده‌ها همین کلیدها را می‌خوانند و می‌نویسند:
    ///   open.if = state.sealed = 1
    ///   open.do = state.sealed = 0
    ///
    /// چرا دنیای «شده»؟ چون شرط‌ها با گرامر دیالوگ نوشته می‌شوند و آن گرامر
    /// فقط نام متغیر می‌شناسد. برای هر بررسی، یک کپی از دنیا ساخته می‌شود و
    /// وضعیت همین نمونه با پیشوند `state.` رویش می‌نشیند. دنیای واقعی دست
    /// نمی‌خورد؛ اثرها جداگانه روی دنیای واقعی اجرا می‌شوند.
    /// </summary>
    public static class ItemState
    {
        public const string Prefix = "state.";

        private static readonly List<string> KeyBuffer = new List<string>(8);

        public static bool IsStateKey(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.StartsWith(Prefix, StringComparison.Ordinal) &&
                   name.Length > Prefix.Length;
        }

        /// <summary>«state.sealed» → «sealed»</summary>
        public static string ShortKey(string name)
        {
            if (!IsStateKey(name)) return name ?? string.Empty;

            return name.Substring(Prefix.Length);
        }

        public static string Get(ItemInstance instance, string key, string fallback = "")
        {
            if (instance == null || string.IsNullOrEmpty(key)) return fallback ?? string.Empty;

            return instance.GetState(ShortKey(key), fallback);
        }

        public static void Set(ItemInstance instance, string key, string value)
        {
            if (instance == null || string.IsNullOrEmpty(key)) return;

            instance.SetState(ShortKey(key), value);
        }

        public static int GetInt(ItemInstance instance, string key, int fallback = 0)
        {
            string raw = Get(instance, key, string.Empty);
            double number;

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                return (int)number;

            return fallback;
        }

        public static bool GetBool(ItemInstance instance, string key, bool fallback = false)
        {
            string raw = Get(instance, key, string.Empty).Trim().ToLowerInvariant();

            if (raw == "true" || raw == "1" || raw == "yes") return true;
            if (raw == "false" || raw == "0" || raw == "no") return false;

            return fallback;
        }

        /// <summary>وضعیت‌های اولیه‌ی تعریف را روی نمونه‌ی تازه می‌نویسد.</summary>
        public static void ApplyDefaults(ItemInstance instance)
        {
            if (instance == null) return;

            ItemDefinition definition = instance.Definition;

            if (definition == null || definition.InitialState == null) return;

            for (int i = 0; i < definition.InitialState.Count; i++)
                instance.SetState(definition.InitialState[i].Key, definition.InitialState[i].Value);
        }

        /// <summary>
        /// دنیای «شده»: کپی دنیای واقعی + `state.*` همین نمونه.
        /// هر فراخوانی یک کپی می‌سازد؛ فقط هنگام یک کلیک اتفاق می‌افتد، نه هر فریم.
        /// </summary>
        public static WorldState View(WorldState world, ItemInstance instance)
        {
            WorldState view = new WorldState();

            if (world != null)
            {
                foreach (string name in world.Names)
                    view.Set(name, world.Get(name));
            }

            if (instance == null) return view;

            KeyBuffer.Clear();
            instance.CopyStateKeys(KeyBuffer);

            for (int i = 0; i < KeyBuffer.Count; i++)
                view.Set(Prefix + KeyBuffer[i], instance.GetState(KeyBuffer[i]));

            return view;
        }

        /// <summary>
        /// یک اثر را روی نمونه اعمال می‌کند: `state.sealed = 0`، `state.ammo - 1`.
        /// برمی‌گرداند آیا این اثر مربوط به وضعیت نمونه بود.
        /// </summary>
        public static bool ApplyEffect(ItemInstance instance, string effect)
        {
            if (instance == null || string.IsNullOrEmpty(effect)) return false;

            string text = effect.Trim();

            if (!text.StartsWith(Prefix, StringComparison.Ordinal)) return false;

            int index = FindOperator(text);
            if (index <= Prefix.Length) return false;

            string key = text.Substring(0, index).Trim();
            char op = text[index];
            string raw = text.Substring(index + 1).Trim();

            if (!IsStateKey(key)) return false;

            if (op == '=')
            {
                instance.SetState(ShortKey(key), raw);
                return true;
            }

            double current = GetInt(instance, key);
            double delta;

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out delta)) return true;

            double next = op == '+' ? current + delta : current - delta;

            instance.SetState(ShortKey(key), next.ToString(CultureInfo.InvariantCulture));
            return true;
        }

        /// <summary>اولین عملگر (= یا + یا -) بعد از نام کلید.</summary>
        private static int FindOperator(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '=' || c == '+' || c == '-')
                {
                    if (i == 0) return -1;
                    return i;
                }
            }

            return -1;
        }
    }
}
