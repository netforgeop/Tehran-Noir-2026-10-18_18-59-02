using System;
using System.Collections.Generic;

namespace TehranNoir.Dialogue
{
    /// وضعیت جهان بازی: هر متغیری که دیالوگ می‌خواند یا تغییر می‌دهد.
    public sealed class WorldState
    {
        public static readonly string[] Scalars =
        {
            "time", "weather", "energy", "fear", "sanity", "trust", "playerAge"
        };

        public static readonly string[] Prefixes = { "rel.", "met.", "present.", "flag." };

        private readonly Dictionary<string, DialogueValue> _values = new Dictionary<string, DialogueValue>();

        public WorldState()
        {
            Set("time", "morning");
            Set("weather", "clear");
            Set("energy", 100d);
            Set("fear", 0d);
        }

        public static bool IsKnownVariable(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            for (int i = 0; i < Scalars.Length; i++)
                if (Scalars[i] == name) return true;

            for (int i = 0; i < Prefixes.Length; i++)
                if (name.Length > Prefixes[i].Length &&
                    name.StartsWith(Prefixes[i], StringComparison.Ordinal)) return true;

            return false;
        }

        public bool Has(string name)
        {
            return !string.IsNullOrEmpty(name) && _values.ContainsKey(name);
        }

        public DialogueValue Get(string name)
        {
            DialogueValue value;
            if (!string.IsNullOrEmpty(name) && _values.TryGetValue(name, out value)) return value;
            return DialogueValue.Undefined;
        }

        public void Set(string name, DialogueValue value)
        {
            if (string.IsNullOrEmpty(name)) return;
            _values[name] = value;
        }

        public void Set(string name, double value)
        {
            Set(name, DialogueValue.Number(value));
        }

        public void Set(string name, bool value)
        {
            Set(name, DialogueValue.Bool(value));
        }

        public void Set(string name, string value)
        {
            Set(name, DialogueValue.Text(value));
        }

        /// متغیرهای یک فایل دیالوگ را با مقدار پیش‌فرض پر می‌کند؛ به مقادیر موجود دست نمی‌زند.
        public void EnsureDefaults(IEnumerable<string> names)
        {
            if (names == null) return;

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name) || _values.ContainsKey(name)) continue;

                if (name.StartsWith("met.", StringComparison.Ordinal) ||
                    name.StartsWith("present.", StringComparison.Ordinal))
                    _values[name] = DialogueValue.Bool(false);
                else
                    _values[name] = DialogueValue.Number(0d);
            }
        }

        public static WorldState CreateDefault(DialogueDocument document)
        {
            WorldState world = new WorldState();
            if (document != null) world.EnsureDefaults(document.Variables);
            return world;
        }
    }
}
