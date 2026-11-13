using System;
using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.Settings
{
    /// <summary>
    /// مرکز زبان بازی. هر زبان یک فایل متنی در Resources/Localization است؛
    /// زبان تازه = یک فایل تازه، بدون دست‌زدن به هیچ اسکریپتی.
    /// متن‌ها با کلید خوانده می‌شوند: Loc.Get(Loc.Channel.Menu, "pause.resume").
    /// </summary>
    public static class Loc
    {
        /// <summary>کانال‌های زبان. برای کانال تازه، فقط یک عضو اینجا اضافه کن.</summary>
        public enum Channel
        {
            Master,
            Menu,
            Subtitles
        }

        public const string Fallback = "fa";
        private const string ResourcePath = "Localization";
        private const string PrefPrefix = "tehran.loc.";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>();

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>();
        private static readonly Dictionary<string, bool> Rtl = new Dictionary<string, bool>();
        private static readonly List<string> Codes = new List<string>();
        private static readonly Dictionary<Channel, string> Current = new Dictionary<Channel, string>();

        private static bool _loaded;

        /// <summary>هر بار که زبان عوض شود صدا زده می‌شود.</summary>
        public static event Action Changed;

        /* ============================================================
           خواندن
           ============================================================ */

        public static IReadOnlyList<string> Languages
        {
            get
            {
                EnsureLoaded();
                return Codes;
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;

            _loaded = true;

            LoadTables();
            LoadPreferences();
        }

        private static void LoadTables()
        {
            TextAsset[] files = Resources.LoadAll<TextAsset>(ResourcePath);

            for (int i = 0; i < files.Length; i++)
            {
                TextAsset file = files[i];
                Dictionary<string, string> table = Parse(file.text, file.name);

                if (table == null) continue;

                string code = file.name;
                Tables[code] = table;
                Codes.Add(code);

                Names[code] = table.TryGetValue("@name", out string name) ? name : code;
                Rtl[code] = !table.TryGetValue("@rtl", out string rtl) || rtl.Trim().ToLowerInvariant() != "false";
            }

            Codes.Sort(StringComparer.OrdinalIgnoreCase);

            if (Codes.Count == 0)
            {
                Debug.LogError("[Loc] هیچ فایل زبانی در Resources/" + ResourcePath + " پیدا نشد.");
                Tables[Fallback] = new Dictionary<string, string>();
                Names[Fallback] = Fallback;
                Rtl[Fallback] = true;
                Codes.Add(Fallback);
            }
        }

        private static Dictionary<string, string> Parse(string text, string fileName)
        {
            Dictionary<string, string> table = new Dictionary<string, string>();
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Length == 0 || line.StartsWith("#")) continue;

                int split = line.IndexOf('=');
                if (split <= 0)
                {
                    Debug.LogWarning("[Loc] خط " + (i + 1) + " فایل «" + fileName + "» فرمت درست ندارد: " + line);
                    continue;
                }

                string key = line.Substring(0, split).Trim();
                string value = line.Substring(split + 1).Trim().Replace("\\n", "\n");

                table[key] = value;
            }

            return table;
        }

        private static void LoadPreferences()
        {
            string master = PlayerPrefs.GetString(PrefPrefix + "master", Fallback);
            if (!Tables.ContainsKey(master)) master = Codes[0];

            SetSilent(Channel.Master, master);

            string menu = PlayerPrefs.GetString(PrefPrefix + "menu", master);
            string subtitles = PlayerPrefs.GetString(PrefPrefix + "subtitles", master);

            SetSilent(Channel.Menu, Tables.ContainsKey(menu) ? menu : master);
            SetSilent(Channel.Subtitles, Tables.ContainsKey(subtitles) ? subtitles : master);
        }

        private static void SetSilent(Channel channel, string code)
        {
            Current[channel] = code;
        }

        /* ============================================================
           خواندن متن
           ============================================================ */

        public static string Code(Channel channel)
        {
            EnsureLoaded();

            return Current.TryGetValue(channel, out string code) ? code : Fallback;
        }

        public static bool IsRtl(Channel channel)
        {
            return IsRtlCode(Code(channel));
        }

        public static bool IsRtlCode(string code)
        {
            EnsureLoaded();

            return !Rtl.TryGetValue(code, out bool rtl) || rtl;
        }

        /// <summary>
        /// نام زبان، به زبانِ منو: در منوی انگلیسی «Persian»، در منوی فارسی «فارسی».
        /// برای همین هیچ متن فارسی داخل منوی انگلیسی نمی‌افتد و مربع نمی‌شود.
        /// کلیدها: lang.fa، lang.en — برای زبان تازه هم یک کلید همنام اضافه کن.
        /// </summary>
        public static string Name(string code)
        {
            EnsureLoaded();

            string key = "lang." + code;

            if (Tables.TryGetValue(Code(Channel.Menu), out Dictionary<string, string> table) &&
                table.TryGetValue(key, out string label))
                return label;

            if (Tables.TryGetValue(Fallback, out Dictionary<string, string> fallback) &&
                fallback.TryGetValue(key, out string fallbackLabel))
                return fallbackLabel;

            return Names.TryGetValue(code, out string name) ? name : code;
        }

        public static string Get(Channel channel, string key)
        {
            EnsureLoaded();

            string code = Code(channel);

            if (Tables.TryGetValue(code, out Dictionary<string, string> table) &&
                table.TryGetValue(key, out string value))
                return value;

            if (Tables.TryGetValue(Fallback, out Dictionary<string, string> fallback) &&
                fallback.TryGetValue(key, out string fallbackValue))
                return fallbackValue;

            return "<" + key + ">";
        }

        /* ============================================================
           تغییر زبان
           ============================================================ */

        public static void Set(Channel channel, string code)
        {
            EnsureLoaded();

            if (!Tables.ContainsKey(code)) return;

            if (channel == Channel.Master)
            {
                SetSilent(Channel.Master, code);
                SetSilent(Channel.Menu, code);
                SetSilent(Channel.Subtitles, code);

                Save();
                Changed?.Invoke();
                return;
            }

            if (Code(channel) == code) return;

            SetSilent(channel, code);

            Save();
            Changed?.Invoke();
        }

        /// <summary>زبان بعدی در فهرست؛ برای کلیدهای چپ/راست.</summary>
        public static string Next(string code, int direction)
        {
            int index = Codes.IndexOf(code);
            if (index < 0) index = 0;

            index = (index + direction + Codes.Count) % Codes.Count;

            return Codes[index];
        }

        /// <summary>اگر همه‌ی کانال‌ها یکی باشند، همان زبان؛ وگرنه null (ترکیبی).</summary>
        public static string CommonCode()
        {
            string first = Code(Channel.Menu);

            foreach (KeyValuePair<Channel, string> pair in Current)
            {
                if (pair.Key == Channel.Master) continue;
                if (pair.Value != first) return null;
            }

            return first;
        }

        private static void Save()
        {
            PlayerPrefs.SetString(PrefPrefix + "master", Code(Channel.Master));
            PlayerPrefs.SetString(PrefPrefix + "menu", Code(Channel.Menu));
            PlayerPrefs.SetString(PrefPrefix + "subtitles", Code(Channel.Subtitles));
            PlayerPrefs.Save();
        }
    }
}
