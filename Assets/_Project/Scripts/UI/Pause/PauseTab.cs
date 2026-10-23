using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TehranNoir.UI
{
    /// <summary>
    /// یک تب از منوی توقف. هر تب یک کلاس است، نه یک شرط داخل منو؛
    /// افزودن تب تازه یعنی یک فایل جدید، نه دست‌زدن به PauseMenuUI.
    /// </summary>
    public abstract class PauseTab
    {
        public abstract string Id { get; }

        public abstract string Label { get; }

        protected readonly List<PauseRow> Rows = new List<PauseRow>();

        protected PauseMenuUI Menu { get; private set; }

        private int _selected;

        public int Selected => _selected;

        /// <summary>ساخت پنل؛ از بیرون فقط همین صدا زده می‌شود.</summary>
        public void Build(RectTransform panel, PauseMenuUI menu)
        {
            Menu = menu;
            OnBuild(panel);
            Highlight();
        }

        protected abstract void OnBuild(RectTransform panel);

        public virtual void OnShow()
        {
            _selected = 0;
            Highlight();
        }

        public virtual void OnHide()
        {
        }

        /* ---------- ناوبری ---------- */

        public virtual void Move(int delta)
        {
            if (Rows.Count == 0) return;

            _selected = Mathf.Clamp(_selected + delta, 0, Rows.Count - 1);
            Highlight();
        }

        /// <summary>کلید چپ/راست: تغییر مقدار ردیف انتخاب‌شده.</summary>
        public virtual void Step(int delta)
        {
            if (Rows.Count == 0) return;

            Action<int> step = Rows[_selected].Step;
            if (step == null) return;

            step(delta);
            Refresh();
        }

        /// <summary>کلید Tab: بخش بعدی (فقط تب‌هایی که بخش دارند).</summary>
        public virtual void NextSection()
        {
        }

        /// <summary>
        /// Enter یا کلیک: ردیف منو کارش را انجام می‌دهد؛
        /// ردیف تنظیمات دکمه ندارد، پس مقدارش یک پله جلو می‌رود.
        /// </summary>
        public virtual void Activate()
        {
            if (Rows.Count == 0) return;

            PauseRow row = Rows[_selected];

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

        public void Select(int index)
        {
            if (Rows.Count == 0) return;

            _selected = Mathf.Clamp(index, 0, Rows.Count - 1);
            Highlight();
        }

        /// <summary>مقادیر ردیف‌ها را از منبعشان دوباره می‌خواند.</summary>
        public void Refresh()
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                PauseRow row = Rows[i];
                if (row.ValueText == null || row.ReadValue == null) continue;

                row.ValueText.text = row.ReadValue();
            }
        }

        /// <summary>
        /// ماوس: اول به تب فرصت داده می‌شود خودش تصمیم بگیرد (مثلاً ریل بخش‌ها)،
        /// بعد ردیف‌های معمولی. هاور انتخاب می‌کند، کلیک اجرا.
        /// </summary>
        public virtual void Pointer(Vector2 screenPoint, bool clicked)
        {
            Tick(screenPoint, clicked);
        }

        public void Tick(Vector2 pointer, bool clicked)
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                RectTransform rect = Rows[i].Rect;
                if (rect == null) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, null)) continue;

                if (_selected != i)
                {
                    _selected = i;
                    Highlight();
                }

                if (clicked) Activate();
                return;
            }
        }

        protected void Highlight()
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                bool active = i == _selected;

                Rows[i].Label.color = active ? UiKit.Gold : UiKit.RowIdle;
                if (Rows[i].ValueText != null) Rows[i].ValueText.color = active ? UiKit.Gold : UiKit.Dim;
                if (Rows[i].Underline != null) Rows[i].Underline.SetActive(active);
            }
        }

        /* ---------- ساخت ردیف ---------- */

        /// <summary>ردیف منو: متن چپ‌چین و خط طلایی زیر گزینه‌ی فعال.</summary>
        protected void AddRow(RectTransform panel, string label, float y, Action action)
        {
            RectTransform row = UiKit.Box("Row_" + label, panel, PauseMenuUI.RowX, y, PauseMenuUI.RowWidth, PauseMenuUI.RowHeight);

            TMP_Text text = UiKit.Text("Label", row, PauseMenuUI.RowFont, UiKit.RowIdle, Menu.Font, Menu.Persian);
            text.text = label;
            text.ForceMeshUpdate();

            float width = Mathf.Max(20f, text.preferredWidth);

            RectTransform underline = UiKit.Node("Underline", row);
            underline.anchorMin = new Vector2(0f, 0f);
            underline.anchorMax = new Vector2(0f, 0f);
            underline.pivot = new Vector2(0f, 0f);
            underline.anchoredPosition = new Vector2(0f, 6f);
            underline.sizeDelta = new Vector2(width, 2f);

            Image line = underline.gameObject.AddComponent<Image>();
            line.color = UiKit.Gold;
            line.raycastTarget = false;

            PauseRow data = new PauseRow();
            data.Rect = row;
            data.Label = text;
            data.Underline = underline.gameObject;
            data.Action = action;
            Rows.Add(data);

            underline.gameObject.SetActive(false);
        }

        /// <summary>
        /// ردیف تنظیمات: برچسب سمت راست، مقدار سمت چپش. مقدارها فقط با چپ/راست عوض می‌شوند.
        /// </summary>
        protected void AddSetting(RectTransform panel, float x, float width, string label, float y,
            Func<string> readValue, Action<int> step, Action action = null)
        {
            const float valueWidth = 380f;
            const float rowHeight = SettingsLayout.RowHeight;

            RectTransform row = UiKit.Box("Setting_" + label, panel, x, y, width, rowHeight);

            RectTransform valueBox = UiKit.Box("Value", row, 0f, 0f, valueWidth, rowHeight);
            TMP_Text valueText = UiKit.Text("Text", valueBox, SettingsLayout.RowFont, UiKit.Dim, Menu.Font, Menu.Persian);
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.text = readValue != null ? readValue() : string.Empty;

            RectTransform labelBox = UiKit.Box("Label", row, valueWidth + 20f, 0f, width - valueWidth - 20f, rowHeight);
            TMP_Text text = UiKit.Text("Text", labelBox, SettingsLayout.RowFont, UiKit.RowIdle, Menu.Font, Menu.Persian);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.text = label;

            PauseRow data = new PauseRow();
            data.Rect = row;
            data.Label = text;
            data.ValueText = valueText;
            data.ReadValue = readValue;
            data.Step = step;
            data.Action = action;
            Rows.Add(data);
        }

        /// <summary>عنوان بخش؛ انتخاب‌شدنی نیست.</summary>
        protected void AddHeader(RectTransform panel, string label, float y)
        {
            RectTransform box = UiKit.Box("Header_" + label, panel, SettingsLayout.RowX, y, SettingsLayout.RowWidth, 34f);

            TMP_Text text = UiKit.Text("Text", box, SettingsLayout.HeaderFont, UiKit.Gold, Menu.Font, Menu.Persian);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.text = label;
        }
    }

    /// <summary>یک ردیف قابل انتخاب در فهرست یک تب.</summary>
    public sealed class PauseRow
    {
        public RectTransform Rect;
        public TMP_Text Label;
        public GameObject Underline;
        public Action Action;

        public TMP_Text ValueText;
        public Func<string> ReadValue;
        public Action<int> Step;
    }
}
