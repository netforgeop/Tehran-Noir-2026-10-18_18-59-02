using System;
using System.Collections.Generic;
using TMPro;
using TehranNoir.Settings;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// صفحه‌ی تنظیمات، مستقل از جایی که باز می‌شود.
    /// منوی توقف و منوی اصلی هر دو همین کلاس را می‌سازند: یک پیاده‌سازی، دو محل استفاده.
    /// چهار بخش دارد: صدا، تصویر، گرافیک، زبان — همه با کیبورد، دسته و ماوس کار می‌کنند.
    /// </summary>
    public sealed class MenuSettings
    {
        public const int SectionCount = 4;

        private readonly List<PauseRow> _rows = new List<PauseRow>();
        private readonly List<GameObject> _railItems = new List<GameObject>();
        private readonly List<TMP_Text> _railLabels = new List<TMP_Text>();

        private readonly RectTransform _root;
        private readonly RectTransform _rowsRoot;
        private readonly TMP_Text _header;
        private readonly TMP_FontAsset _font;
        private readonly bool _persian;
        private readonly Action _back;

        private int _section;
        private int _selected;
        private bool _applied;

        public MenuSettings(RectTransform parent, TMP_FontAsset font, bool persian, Action back)
        {
            _font = font;
            _persian = persian;
            _back = back;

            _root = UiKit.Node("SettingsPage", parent);
            UiKit.Stretch(_root);

            BuildRail();

            _rowsRoot = UiKit.Node("Rows", _root);
            UiKit.Stretch(_rowsRoot);

            RectTransform headerBox = UiKit.Box("Header", _root, SettingsLayout.RowX, 96f, SettingsLayout.RowWidth, 34f);
            _header = UiKit.Text("Text", headerBox, SettingsLayout.HeaderFont, UiKit.Gold, _font, _persian);
            _header.alignment = TextAlignmentOptions.MidlineRight;

            BuildSection(0);
        }

        /// <summary>ریشه‌ی صفحه؛ میزبان با فعال/غیرفعال کردنش نشانش می‌دهد یا پنهانش می‌کند.</summary>
        public RectTransform Root => _root;

        /// <summary>شماره‌ی بخش باز. میزبان قبل از بازسازی رابط نگهش می‌دارد.</summary>
        public int SectionIndex => _section;

        /// <summary>شماره‌ی ردیف انتخاب‌شده.</summary>
        public int SelectedIndex => _selected;

        /// <summary>رفتن به یک بخش مشخص؛ برای برگشتن به همان‌جا بعد از تغییر زبان.</summary>
        public void ShowSection(int index)
        {
            BuildSection(Mathf.Clamp(index, 0, SectionCount - 1));
        }

        /// <summary>انتخاب یک ردیف مشخص.</summary>
        public void Select(int index)
        {
            if (_rows.Count == 0) return;

            _selected = Mathf.Clamp(index, 0, _rows.Count - 1);
            Highlight();
        }

        /* ============================================================
           ساخت
           ============================================================ */

        private void BuildRail()
        {
            for (int i = 0; i < SectionCount; i++)
            {
                float y = SettingsLayout.RailFirstY + i * (SettingsLayout.RailHeight + SettingsLayout.RailGap);

                RectTransform item = UiKit.Box("Rail_" + i, _root, SettingsLayout.RailX, y,
                    SettingsLayout.RailWidth, SettingsLayout.RailHeight);

                TMP_Text label = UiKit.Text("Text", item, SettingsLayout.RowFont, UiKit.RowIdle, _font, _persian);
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

            for (int i = _rowsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject old = _rowsRoot.GetChild(i).gameObject;
                old.SetActive(false);

                // این کلاس MonoBehaviour نیست، پس Destroy را باید از خود UnityEngine.Object بگیری
                UnityEngine.Object.Destroy(old);
            }

            _rows.Clear();

            _header.text = SectionName(section);

            switch (section)
            {
                case 0: BuildAudio(); break;
                case 1: BuildVideo(); break;
                case 2: BuildGraphics(); break;
                default: BuildLanguage(); break;
            }

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.back"), SettingsLayout.BackY, null, null, _back);

            _applied = false;

            HighlightRail();
            _selected = 0;
            Highlight();
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
            AddSetting(Loc.Get(Loc.Channel.Menu, key), y,
                () => Mathf.RoundToInt(GameSettings.Volume(band) * 100f) + "%",
                delta => GameSettings.StepVolume(band, delta * 0.05f));
        }

        private void BuildVideo()
        {
            float y = SettingsLayout.FirstY;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.resolution"), y, ResolutionText, GameSettings.StepResolution);
            y += SettingsLayout.RowHeight;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.fullscreen"), y, ModeText, GameSettings.StepMode);
            y += SettingsLayout.RowHeight;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.fps"), y, FpsText, GameSettings.StepFps);
            y += SettingsLayout.RowHeight;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.vsync"), y, YesNoText(() => GameSettings.VSync),
                delta => GameSettings.ToggleVSync());
            y += SettingsLayout.RowHeight;

            AddApply(y);
        }

        private void BuildGraphics()
        {
            float y = SettingsLayout.FirstY;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.quality"), y, QualityText, GameSettings.StepQuality);
            y += SettingsLayout.RowHeight;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.texture"), y, TextureText, GameSettings.StepTexture);
            y += SettingsLayout.RowHeight;

            AddApply(y);
        }

        private void BuildLanguage()
        {
            float y = SettingsLayout.FirstY;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.lang.master"), y, MasterLanguageText, StepMasterLanguage);
            y += SettingsLayout.RowHeight;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.lang.menu"), y, () => Loc.Name(Loc.Code(Loc.Channel.Menu)),
                delta => Loc.Set(Loc.Channel.Menu, Loc.Next(Loc.Code(Loc.Channel.Menu), delta)));
            y += SettingsLayout.RowHeight;

            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.lang.subtitles"), y, () => Loc.Name(Loc.Code(Loc.Channel.Subtitles)),
                delta => Loc.Set(Loc.Channel.Subtitles, Loc.Next(Loc.Code(Loc.Channel.Subtitles), delta)));
        }

        /// <summary>ردیف «اعمال تغییرات»: تا وقتی زده نشود، تصویر و گرافیک عوض نمی‌شوند.</summary>
        private void AddApply(float y)
        {
            AddSetting(Loc.Get(Loc.Channel.Menu, "settings.apply"), y, ApplyText, null, ApplyAll);
        }

        /// <summary>
        /// اعمال واقعی. بعد از آن ردیف باید فوراً بگوید «اعمال شد»؛
        /// وگرنه کاربر فکر می‌کند دکمه کار نکرده است.
        /// </summary>
        private void ApplyAll()
        {
            GameSettings.Apply();

            _applied = true;
        }

        private string ApplyText()
        {
            if (GameSettings.HasPending)
            {
                _applied = false;

                return Loc.Get(Loc.Channel.Menu, "settings.apply.pending");
            }

            return Loc.Get(Loc.Channel.Menu, _applied ? "settings.applied" : "settings.apply.done");
        }

        /* ============================================================
           مقادیر
           ============================================================ */

        private static string ResolutionText()
        {
            IReadOnlyList<Vector2Int> list = GameSettings.Resolutions;
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

        private static Func<string> YesNoText(Func<bool> value)
        {
            string on = Loc.Get(Loc.Channel.Menu, "settings.on");
            string off = Loc.Get(Loc.Channel.Menu, "settings.off");

            return () => value() ? on : off;
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

        /* ============================================================
           ردیف‌ها
           ============================================================ */

        private void AddSetting(string label, float y, Func<string> readValue, Action<int> step, Action action = null)
        {
            const float valueWidth = 380f;

            RectTransform row = UiKit.Box("Setting_" + label, _rowsRoot,
                SettingsLayout.RowX, y, SettingsLayout.RowWidth, SettingsLayout.RowHeight);

            RectTransform valueBox = UiKit.Box("Value", row, 0f, 0f, valueWidth, SettingsLayout.RowHeight);
            TMP_Text valueText = UiKit.Text("Text", valueBox, SettingsLayout.RowFont, UiKit.Dim, _font, _persian);
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.text = readValue != null ? readValue() : string.Empty;

            RectTransform labelBox = UiKit.Box("Label", row, valueWidth + 20f, 0f,
                SettingsLayout.RowWidth - valueWidth - 20f, SettingsLayout.RowHeight);

            TMP_Text text = UiKit.Text("Text", labelBox, SettingsLayout.RowFont, UiKit.RowIdle, _font, _persian);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.text = label;

            PauseRow data = new PauseRow();
            data.Rect = row;
            data.Label = text;
            data.ValueText = valueText;
            data.ReadValue = readValue;
            data.Step = step;
            data.Action = action;
            _rows.Add(data);
        }

        private void HighlightRail()
        {
            for (int i = 0; i < _railLabels.Count; i++)
                _railLabels[i].color = i == _section ? UiKit.Gold : UiKit.RowIdle;
        }

        private void Highlight()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i == _selected;

                _rows[i].Label.color = active ? UiKit.Gold : UiKit.RowIdle;
                if (_rows[i].ValueText != null) _rows[i].ValueText.color = active ? UiKit.Gold : UiKit.Dim;
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                PauseRow row = _rows[i];
                if (row.ValueText == null || row.ReadValue == null) continue;

                row.ValueText.text = row.ReadValue();
            }
        }

        /* ============================================================
           ناوبری — همان قرارداد PauseTab، تا میزبان‌ها یکسان صدا بزنند
           ============================================================ */

        public void Move(int delta)
        {
            if (_rows.Count == 0) return;

            _selected = Mathf.Clamp(_selected + delta, 0, _rows.Count - 1);
            Highlight();
        }

        public void Step(int delta)
        {
            if (_rows.Count == 0) return;

            Action<int> step = _rows[_selected].Step;
            if (step == null) return;

            step(delta);
            Refresh();
        }

        public void Activate()
        {
            if (_rows.Count == 0) return;

            PauseRow row = _rows[_selected];

            if (row.Action != null)
            {
                row.Action();
                Refresh();
                return;
            }

            if (row.Step != null)
            {
                row.Step(1);
                Refresh();
            }
        }

        public void NextSection()
        {
            Section(1);
        }

        /// <summary>بخش بعدی/قبلی؛ Q و E و Tab همه از همین راه می‌روند.</summary>
        public void Section(int delta)
        {
            if (delta == 0 || SectionCount == 0) return;

            int index = (_section + delta) % SectionCount;
            if (index < 0) index += SectionCount;

            BuildSection(index);
        }

        /// <summary>ماوس: ریل بخش‌ها اول، بعد ردیف‌ها. هاور انتخاب می‌کند، کلیک اجرا.</summary>
        public void Pointer(Vector2 screenPoint, bool clicked)
        {
            for (int i = 0; i < _railItems.Count; i++)
            {
                RectTransform item = (RectTransform)_railItems[i].transform;

                if (!RectTransformRectangleContains(item, screenPoint)) continue;

                if (clicked && i != _section) BuildSection(i);

                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                RectTransform rect = _rows[i].Rect;
                if (rect == null) continue;
                if (!RectTransformRectangleContains(rect, screenPoint)) continue;

                if (_selected != i)
                {
                    _selected = i;
                    Highlight();
                }

                if (clicked) Activate();
                return;
            }
        }

        private static bool RectTransformRectangleContains(RectTransform rect, Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }
    }
}
