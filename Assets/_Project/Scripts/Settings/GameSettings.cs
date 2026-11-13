using System;
using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.Settings
{
    /// <summary>
    /// تنها جای خواندن و نوشتن تنظیمات بازی: صدا، تصویر، گرافیک.
    /// صدا فوراً اعمال می‌شود؛ تصویر و گرافیک اول در حافظه ذخیره می‌شوند
    /// و با GameSettings.Apply() — همان دکمه‌ی «اعمال تغییرات» — واقعاً اعمال می‌شوند.
    /// برای تنظیم تازه: یک مقدار، یک ذخیره، یک Apply — و یک ردیف در صفحه‌ی تنظیمات.
    /// </summary>
    public static class GameSettings
    {
        public enum Band
        {
            Master,
            Music,
            FX,
            Dialogue
        }

        private const string Prefix = "tehran.";

        private static readonly float[] VolumeValues = { 1f, 0.8f, 0.8f, 0.8f };
        private static readonly int[] FpsValues = { 30, 60, 90, 120, 144, 240, 0 };

        private static readonly FullScreenMode[] Modes =
        {
            FullScreenMode.Windowed,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.ExclusiveFullScreen
        };

        private static readonly List<Vector2Int> ResolutionList = new List<Vector2Int>();

        private static bool _loaded;
        private static bool _pending;

        public static event Action VolumeChanged;
        public static event Action VideoChanged;

        /* ---------- مقادیر ---------- */

        public static int FpsIndex { get; private set; } = 3;
        public static bool VSync { get; private set; } = true;
        public static int ResolutionIndex { get; private set; }
        public static int ModeIndex { get; private set; } = 1;
        public static int QualityIndex { get; private set; }
        public static int TextureIndex { get; private set; }

        /// <summary>تغییری هست که هنوز دکمه‌ی اعمال زده نشده.</summary>
        public static bool HasPending
        {
            get
            {
                EnsureLoaded();
                return _pending;
            }
        }

        public static IReadOnlyList<Vector2Int> Resolutions
        {
            get
            {
                EnsureLoaded();
                return ResolutionList;
            }
        }

        public static int FpsCount => FpsValues.Length;

        public static int ModeCount => Modes.Length;

        public static FullScreenMode Mode => Modes[Mathf.Clamp(ModeIndex, 0, Modes.Length - 1)];

        public static int FpsValue(int index) => FpsValues[Mathf.Clamp(index, 0, FpsValues.Length - 1)];

        /* ============================================================
           راه‌اندازی
           ============================================================ */

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            EnsureLoaded();
            ApplyAudio();
            ApplyVideo(applyResolution: false);
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;

            _loaded = true;

            Loc.EnsureLoaded();
            BuildResolutionList();
            Load();
        }

        private static void BuildResolutionList()
        {
            Resolution[] all = Screen.resolutions;

            for (int i = 0; i < all.Length; i++)
            {
                Vector2Int size = new Vector2Int(all[i].width, all[i].height);
                if (!ResolutionList.Contains(size)) ResolutionList.Add(size);
            }

            // در بعضی محیط‌ها فهرست خالی یا تک‌عضوی است؛ آن‌وقت ردیف رزولوشن هیچ‌وقت عوض نمی‌شود.
            if (ResolutionList.Count < 2)
            {
                AddResolution(1280, 720);
                AddResolution(1600, 900);
                AddResolution(1920, 1080);
            }

            if (ResolutionList.Count == 0)
                ResolutionList.Add(new Vector2Int(Screen.width, Screen.height));

            ResolutionList.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            ResolutionIndex = FindCurrentResolution();
        }

        private static void AddResolution(int width, int height)
        {
            Vector2Int size = new Vector2Int(width, height);
            if (!ResolutionList.Contains(size)) ResolutionList.Add(size);
        }

        private static int FindCurrentResolution()
        {
            Vector2Int current = new Vector2Int(Screen.width, Screen.height);
            int index = ResolutionList.IndexOf(current);

            return index >= 0 ? index : ResolutionList.Count - 1;
        }

        private static void Load()
        {
            for (int i = 0; i < VolumeValues.Length; i++)
                VolumeValues[i] = PlayerPrefs.GetFloat(Prefix + "vol." + i, VolumeValues[i]);

            FpsIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "fps", FpsIndex), 0, FpsValues.Length - 1);
            VSync = PlayerPrefs.GetInt(Prefix + "vsync", 1) == 1;
            ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "res", ResolutionIndex), 0, ResolutionList.Count - 1);
            ModeIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "mode", ModeIndex), 0, Modes.Length - 1);
            QualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "quality", QualitySettings.GetQualityLevel()),
                0, Mathf.Max(0, QualitySettings.names.Length - 1));
            TextureIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "texture", 0), 0, 2);
        }

        private static void Save()
        {
            for (int i = 0; i < VolumeValues.Length; i++)
                PlayerPrefs.SetFloat(Prefix + "vol." + i, VolumeValues[i]);

            PlayerPrefs.SetInt(Prefix + "fps", FpsIndex);
            PlayerPrefs.SetInt(Prefix + "vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "res", ResolutionIndex);
            PlayerPrefs.SetInt(Prefix + "mode", ModeIndex);
            PlayerPrefs.SetInt(Prefix + "quality", QualityIndex);
            PlayerPrefs.SetInt(Prefix + "texture", TextureIndex);

            PlayerPrefs.Save();
        }

        /* ============================================================
           صدا — فوری
           ============================================================ */

        public static float Volume(Band band)
        {
            EnsureLoaded();

            return VolumeValues[(int)band];
        }

        /// <summary>ضریبی که هر AudioSource باید در حجم پایه‌ی خودش ضرب کند.</summary>
        public static float SourceVolume(Band band)
        {
            EnsureLoaded();

            return band == Band.Master ? 1f : VolumeValues[(int)band];
        }

        public static void SetVolume(Band band, float value)
        {
            EnsureLoaded();

            VolumeValues[(int)band] = Mathf.Clamp01(value);

            ApplyAudio();
            Save();

            VolumeChanged?.Invoke();
        }

        public static void StepVolume(Band band, float amount)
        {
            SetVolume(band, Volume(band) + amount);
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = VolumeValues[(int)Band.Master];
        }

        /* ============================================================
           تصویر و گرافیک — با تأیید
           ============================================================ */

        /// <summary>مقادیر انتخاب‌شده را واقعاً روی بازی اعمال می‌کند.</summary>
        public static void Apply()
        {
            EnsureLoaded();

            ApplyVideo(applyResolution: true);

            _pending = false;

            Save();
            VideoChanged?.Invoke();
        }

        public static void StepResolution(int direction)
        {
            EnsureLoaded();

            ResolutionIndex = (ResolutionIndex + direction + ResolutionList.Count) % ResolutionList.Count;

            MarkPending();
        }

        public static void StepMode(int direction)
        {
            EnsureLoaded();

            ModeIndex = (ModeIndex + direction + Modes.Length) % Modes.Length;

            MarkPending();
        }

        public static void StepFps(int direction)
        {
            EnsureLoaded();

            FpsIndex = (FpsIndex + direction + FpsValues.Length) % FpsValues.Length;

            // با V-Sync روشن، سقف فریم اعمال نمی‌شود
            VSync = false;

            MarkPending();
        }

        public static void ToggleVSync()
        {
            EnsureLoaded();

            VSync = !VSync;

            MarkPending();
        }

        public static void StepQuality(int direction)
        {
            EnsureLoaded();

            int count = Mathf.Max(1, QualitySettings.names.Length);
            QualityIndex = (QualityIndex + direction + count) % count;

            MarkPending();
        }

        public static void StepTexture(int direction)
        {
            EnsureLoaded();

            TextureIndex = (TextureIndex + direction + 3) % 3;

            MarkPending();
        }

        /// <summary>
        /// فقط در حافظه؛ هیچ‌جا ذخیره نمی‌شود. اگر ذخیره شود، با یک بار خروج و ورود
        /// همان تغییرِ اعمال‌نشده روی بازی می‌نشیند و «اعمال» بی‌معنی می‌شود.
        /// </summary>
        private static void MarkPending()
        {
            _pending = true;

            VideoChanged?.Invoke();
        }

        private static void ApplyVideo(bool applyResolution)
        {
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(QualityIndex, 0, QualitySettings.names.Length - 1), true);

            Application.targetFrameRate = FpsValue(FpsIndex);
            QualitySettings.vSyncCount = VSync ? 1 : 0;

            QualitySettings.globalTextureMipmapLimit = TextureIndex;

            if (!applyResolution) return;
            if (ResolutionList.Count == 0) return;

            Vector2Int size = ResolutionList[Mathf.Clamp(ResolutionIndex, 0, ResolutionList.Count - 1)];
            Screen.SetResolution(size.x, size.y, Mode);

            // در ادیتور رزولوشنِ پنجره عوض نمی‌شود؛ این خط تنها مدرکِ اعمال‌شدن است.
            Debug.Log("[Settings] applied " + size.x + "\u00d7" + size.y + " · " + Mode +
                      " · " + Application.targetFrameRate + " fps · V-Sync " + (VSync ? "on" : "off"));
        }
    }
}
