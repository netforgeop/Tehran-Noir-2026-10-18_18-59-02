using System.Collections.Generic;
using TehranNoir.Settings;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// تب اول منوی توقف: فهرست ادامه/تنظیمات/منو/خروج.
    /// خودِ صفحه‌ی تنظیمات این‌جا نوشته نشده؛ از MenuSettings می‌آید تا
    /// منوی اصلی هم همان صفحه را دوباره استفاده کند.
    /// </summary>
    public sealed class SystemPauseTab : PauseTab
    {
        private RectTransform _panel;
        private RectTransform _rootPanel;

        private readonly List<PauseRow> _rootRows = new List<PauseRow>();

        private MenuSettings _settings;

        public override string Id => "system";

        public override string Label => Loc.Get(Loc.Channel.Menu, "pause.tab.system");

        protected override void OnBuild(RectTransform panel)
        {
            _panel = panel;

            _rootPanel = UiKit.Node("Root", panel);
            UiKit.Stretch(_rootPanel);

            BuildRoot();
            ShowRoot();
        }

        /* ============================================================
           فهرست
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
            if (_settings != null) _settings.Root.gameObject.SetActive(false);

            _rootPanel.gameObject.SetActive(true);

            SwapRows(_rootRows);
            Select(0);
        }

        private void OpenSettings()
        {
            if (_settings == null)
            {
                // یک بار ساخته می‌شود؛ عوض کردن بخش‌ها خودش ردیف‌ها را از نو می‌سازد
                _settings = new MenuSettings(_panel, Menu.Font, Menu.Persian, ShowRoot);
            }

            _rootPanel.gameObject.SetActive(false);
            _settings.Root.gameObject.SetActive(true);

            Rows.Clear();
        }

        /// <summary>activeInHierarchy، نه activeSelf: وقتی کل پنل تب خاموش است، صفحه هم خاموش حساب شود.</summary>
        private bool SettingsVisible => _settings != null && _settings.Root.gameObject.activeInHierarchy;

        private void SwapRows(List<PauseRow> source)
        {
            Rows.Clear();
            Rows.AddRange(source);
        }

        /* ============================================================
           ناوبری — داخل تنظیمات به MenuSettings سپرده می‌شود
           ============================================================ */

        public override void Move(int delta)
        {
            if (SettingsVisible)
            {
                _settings.Move(delta);
                return;
            }

            base.Move(delta);
        }

        public override void Step(int delta)
        {
            if (SettingsVisible)
            {
                _settings.Step(delta);
                return;
            }

            base.Step(delta);
        }

        public override void Activate()
        {
            if (SettingsVisible)
            {
                _settings.Activate();
                return;
            }

            base.Activate();
        }

        public override void NextSection()
        {
            if (SettingsVisible) _settings.NextSection();
        }

        public override bool UsesSections => SettingsVisible;

        public override void Section(int delta)
        {
            if (SettingsVisible) _settings.Section(delta);
        }

        /// <summary>Esc داخل تنظیمات یعنی برگشت به فهرست، نه بستن منوی توقف.</summary>
        public override bool Back()
        {
            if (!SettingsVisible) return false;

            ShowRoot();
            return true;
        }

        public override void Pointer(Vector2 screenPoint, bool clicked)
        {
            if (SettingsVisible)
            {
                _settings.Pointer(screenPoint, clicked);
                return;
            }

            base.Pointer(screenPoint, clicked);
        }

        public override void OnShow()
        {
            base.OnShow();
            ShowRoot();
        }

        /* ============================================================
           نگه‌داشتن وضعیت بین دو بازسازی
           ============================================================ */

        /// <summary>بخش و ردیفی که کاربر در صفحه‌ی تنظیمات داشته.</summary>
        private sealed class SettingsState
        {
            public int Section;
            public int Row;
        }

        public override object SaveState()
        {
            if (!SettingsVisible) return null;

            SettingsState state = new SettingsState();
            state.Section = _settings.SectionIndex;
            state.Row = _settings.SelectedIndex;

            return state;
        }

        public override void LoadState(object state)
        {
            SettingsState saved = state as SettingsState;
            if (saved == null) return;

            OpenSettings();

            _settings.ShowSection(saved.Section);
            _settings.Select(saved.Row);
        }
    }
}
