using System.Collections.Generic;
using TehranNoir.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// تب اول: فهرست pause (ادامه/تنظیمات/منو/خروج) و صفحه‌ی تنظیمات داخلش.
    /// صفحه‌ی تنظیمات چهار بخش دارد: صدا، تصویر، گرافیک، زبان.
    /// </summary>
    public sealed class SystemPauseTab : PauseTab
    {
        private static int _lastSection;
        private static bool _lastInside;

        private const int SectionCount = 4;

        private RectTransform _rootPanel;
        private RectTransform _settingsPanel;
        private RectTransform _rowsRoot;
        private RectTransform _headerBox;

        private readonly List<GameObject> _railItems = new List<GameObject>();
        private readonly List<TMP_Text> _railLabels = new List<TMP_Text>();
        private readonly List<PauseRow> _rootRows = new List<PauseRow>();
        private readonly List<PauseRow> _settingRows = new List<PauseRow>();

        private TMP_Text _headerText;
        private int _section;
        private bool _inside;

        public override string Id => "system";

        public override string Label => Loc.Get(Loc.Channel.Menu, "pause.tab.system");

        protected override void OnBuild(RectTransform panel)
        {
            _rootPanel = UiKit.Node("Root", panel);
            UiKit.Stretch(_rootPanel);

            _settingsPanel = UiKit.Node("Settings", panel);
            UiKit.Stretch(_settingsPanel);

            BuildRoot();
            BuildSettings();

            _section = Mathf.Clamp(_lastSection, 0, SectionCount - 1);

            if (_lastInside) OpenSettings();
            else ShowRoot();
        }

        /* ============================================================
           فهرست اول
           ============================================================ */

        private void BuildRoot()
        {
            Rows.Clear();

            float y = PauseMenuUI.FirstRowY;

            AddRow(_rootPanel, Loc.Get(Loc.Channel.Menu, "pause.resume"), y, Menu.Resume);
            y += PauseMenuUI.RowHeight;

            AddRow(_rootPanel, Loc.Get(Loc.Channel.Menu, "pause.settings"), y, OpenSettings);
            y += PauseMenuUI.RowHeight;

            AddRow(_rootPanel, Loc.Get(Loc.Channel.Menu, "pause.mainmenu"), y, Menu.QuitToMainMenu);
            y += PauseMenuUI.RowHeight;

            AddRow(_rootPanel, Loc.Get(Loc.Channel.Menu, "pause.quit"), y, Menu.QuitGame);

            _rootRows.Clear();
            _rootRows.AddRange(Rows);
        }

        private void ShowRoot()
        {
            _inside = false;
            _lastInside = false;

            _rootPanel.gameObject.SetActive(true);
            _settingsPanel.gameObject.SetActive(false);

            SwapRows(_rootRows);
            Select(0);
        }

        private void OpenSettings()
        {
            _inside = true;
            _lastInside = true;

            _rootPanel.gameObject.SetActive(false);
            _settingsPanel.gameObject.SetActive(true);

            BuildSection(_section);
        }

        /* ============================================================
           صفحه‌ی تنظیمات
           ============================================================ */

        private void BuildSettings()
        {
            BuildRail();

            _rowsRoot = UiKit.Node("Rows", _settingsPanel);
            UiKit.Stretch(_rowsRoot);

            _headerBox = UiKit.Box("Header", _settingsPanel, SettingsLayout.RowX, 96f, SettingsLayout.RowWidth, 34f);
            _headerText = UiKit.Text("Text", _headerBox, SettingsLayout.HeaderFont, UiKit.Gold, Menu.Font, Menu.Persian);
            _headerText.alignment = TextAlignmentOptions.MidlineRight;
        }

        private void BuildRail()
        {
            for (int i = 0; i < SectionCount; i++)
            {
                float y = SettingsLayout.RailFirstY + i * (SettingsLayout.RailHeight + SettingsLayout.RailGap);

                RectTransform item = UiKit.Box("Rail_" + i, _settingsPanel, SettingsLayout.RailX, y,
                    SettingsLayout.RailWidth, SettingsLayout.RailHeight);

                TMP_Text label = UiKit.Text("Text", item, SettingsLayout.RowFont, UiKit.RowIdle, Menu.Font, Menu.Persian);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.text = SectionName(i);

                _railItems.Add(item.gameObject);
                _railLabels.Add(label);
            }
        }

        private static string SectionName(int section)
        {
            switch (section)
            {
                case 0: return Loc.Get(Loc.Channel.Menu, "settings.audio");
                case 1: return Loc.Get(Loc.Channel.Menu, "settings.video");
                case 2: return Loc.Get(Loc.Channel.Menu, "settings.graphics");
                default: return Loc.Get(Loc.Channel.Menu, "settings.language");
            }
        }

        private void BuildSection(int section)
        {
            _section = section;
            _lastSection = section;

            for (int i = _rowsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject old = _rowsRoot.GetChild(i).gameObject;
                old.SetActive(false);

                // این کلاس MonoBehaviour نیست، پس Destroy را باید از خود UnityEngine.Object بگیری
                UnityEngine.Object.Destroy(old);
            }

            Rows.Clear();

            _headerText.text = SectionName(section);

            switch (section)
            {
                case 0: BuildAudio(); break;
                case 1: BuildVideo(); break;
                case 2: BuildGraphics(); break;
                default: BuildLanguage(); break;
            }

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.back"), SettingsLayout.BackY, null, null);

            _settingRows.Clear();
            _settingRows.AddRange(Rows);

            HighlightRail();
            Select(0);
        }

        private void BuildAudio()
        {
            float y = SettingsLayout.FirstY;

            AddVolume("settings.master", GameSettings.Band.Master, y); y += SettingsLayout.RowHeight;
            AddVolume("settings.music", GameSettings.Band.Music, y); y += SettingsLayout.RowHeight;
            AddVolume("settings.fx", GameSettings.Band.FX, y); y += SettingsLayout.RowHeight;
            AddVolume("settings.dialogue", GameSettings.Band.Dialogue, y);
        }

        private void AddVolume(string key, GameSettings.Band band, float y)
        {
            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, key), y,
                () => Mathf.RoundToInt(GameSettings.Volume(band) * 100f) + "%",
                delta => GameSettings.StepVolume(band, delta * 0.05f));
        }

        private void BuildVideo()
        {
            float y = SettingsLayout.FirstY;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.resolution"), y, ResolutionText,
                GameSettings.StepResolution);
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.fullscreen"), y, ModeText,
                GameSettings.StepMode);
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.fps"), y, FpsText,
                GameSettings.StepFps);
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.vsync"), y, YesNoText(() => GameSettings.VSync),
                delta => GameSettings.ToggleVSync());
        }

        private static string ResolutionText()
        {
            System.Collections.Generic.IReadOnlyList<Vector2Int> list = GameSettings.Resolutions;
            if (list.Count == 0) return "-";

            Vector2Int size = list[Mathf.Clamp(GameSettings.ResolutionIndex, 0, list.Count - 1)];

            return size.x + "×" + size.y;
        }

        private static string ModeText()
        {
            switch (GameSettings.ModeIndex)
            {
                case 0: return Loc.Get(Loc.Channel.Menu, "settings.windowed");
                case 2: return Loc.Get(Loc.Channel.Menu, "settings.exclusive");
                default: return Loc.Get(Loc.Channel.Menu, "settings.borderless");
            }
        }

        private static string FpsText()
        {
            int fps = GameSettings.FpsValue(GameSettings.FpsIndex);

            return fps <= 0 ? Loc.Get(Loc.Channel.Menu, "settings.fps.unlimited") : fps.ToString();
        }

        private static System.Func<string> YesNoText(System.Func<bool> value)
        {
            string on = Loc.Get(Loc.Channel.Menu, "settings.on");
            string off = Loc.Get(Loc.Channel.Menu, "settings.off");

            return () => value() ? on : off;
        }

        private void BuildGraphics()
        {
            float y = SettingsLayout.FirstY;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.quality"), y, QualityText,
                GameSettings.StepQuality);
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.texture"), y, TextureText,
                GameSettings.StepTexture);
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.aa"), y, AntiAliasingText,
                GameSettings.StepAntiAliasing);
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.shadows"), y, ShadowText,
                GameSettings.StepShadows);
        }

        private static string QualityText()
        {
            string[] names = QualitySettings.names;
            if (names.Length == 0) return "-";

            return names[Mathf.Clamp(GameSettings.QualityIndex, 0, names.Length - 1)];
        }

        private static string TextureText()
        {
            switch (GameSettings.TextureIndex)
            {
                case 1: return Loc.Get(Loc.Channel.Menu, "settings.texture.half");
                case 2: return Loc.Get(Loc.Channel.Menu, "settings.texture.quarter");
                default: return Loc.Get(Loc.Channel.Menu, "settings.texture.full");
            }
        }

        private static string AntiAliasingText()
        {
            int[] values = { 0, 2, 4, 8 };
            int value = values[Mathf.Clamp(GameSettings.AntiAliasingIndex, 0, values.Length - 1)];

            return value <= 0 ? Loc.Get(Loc.Channel.Menu, "settings.off") : value + "x";
        }

        private static string ShadowText()
        {
            switch (GameSettings.ShadowIndex)
            {
                case 0: return Loc.Get(Loc.Channel.Menu, "settings.shadows.off");
                case 1: return Loc.Get(Loc.Channel.Menu, "settings.shadows.hard");
                default: return Loc.Get(Loc.Channel.Menu, "settings.shadows.all");
            }
        }

        private void BuildLanguage()
        {
            float y = SettingsLayout.FirstY;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.lang.master"), y, MasterLanguageText, StepMasterLanguage);
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.lang.menu"), y, () => Loc.Name(Loc.Code(Loc.Channel.Menu)),
                delta => Loc.Set(Loc.Channel.Menu, Loc.Next(Loc.Code(Loc.Channel.Menu), delta)));
            y += SettingsLayout.RowHeight;

            AddSetting(_rowsRoot, SettingsLayout.RowX, SettingsLayout.RowWidth,
                Loc.Get(Loc.Channel.Menu, "settings.lang.subtitles"), y, () => Loc.Name(Loc.Code(Loc.Channel.Subtitles)),
                delta => Loc.Set(Loc.Channel.Subtitles, Loc.Next(Loc.Code(Loc.Channel.Subtitles), delta)));
        }

        private static string MasterLanguageText()
        {
            string common = Loc.CommonCode();

            return common != null ? Loc.Name(common) : Loc.Get(Loc.Channel.Menu, "settings.mixed");
        }

        private static void StepMasterLanguage(int delta)
        {
            string baseCode = Loc.CommonCode() ?? Loc.Code(Loc.Channel.Master);

            Loc.Set(Loc.Channel.Master, Loc.Next(baseCode, delta));
        }

        private void HighlightRail()
        {
            for (int i = 0; i < _railLabels.Count; i++)
                _railLabels[i].color = i == _section ? UiKit.Gold : UiKit.RowIdle;
        }

        private void SwapRows(List<PauseRow> source)
        {
            Rows.Clear();
            Rows.AddRange(source);
        }

        /* ============================================================
           ناوبری
           ============================================================ */

        public override void Step(int delta)
        {
            if (!_inside)
            {
                base.Step(delta);
                return;
            }

            if (Rows.Count > 0 && Selected == Rows.Count - 1)
            {
                // ردیف «بازگشت»: چپ/راست هم برمی‌گردد
                ShowRoot();
                return;
            }

            base.Step(delta);
        }

        public override void Activate()
        {
            if (!_inside)
            {
                base.Activate();
                return;
            }

            if (Rows.Count > 0 && Selected == Rows.Count - 1) ShowRoot();
        }

        public override void NextSection()
        {
            if (!_inside) return;

            int next = (_section + 1) % SectionCount;
            BuildSection(next);
        }

        public override void OnShow()
        {
            base.OnShow();

            if (_lastInside) OpenSettings();
            else ShowRoot();
        }
    }
}
