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
        /// <summary>شناسه‌ی لاتین برای نام‌گذاری آبجکت‌ها.</summary>
        public abstract string Id { get; }

        /// <summary>نامی که روی نوار بالا نوشته می‌شود.</summary>
        public abstract string Label { get; }

        protected readonly List<PauseRow> Rows = new List<PauseRow>();

        protected PauseMenuUI Menu { get; private set; }

        private int _selected;

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

        public void Move(int delta)
        {
            if (Rows.Count == 0) return;

            _selected = Mathf.Clamp(_selected + delta, 0, Rows.Count - 1);
            Highlight();
        }

        public void Activate()
        {
            if (Rows.Count == 0) return;

            Action action = Rows[_selected].Action;
            if (action == null) return;

            action();
        }

        /// <summary>موس: هاور انتخاب می‌کند، کلیک اجرا.</summary>
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
                if (Rows[i].Underline != null) Rows[i].Underline.SetActive(active);
            }
        }

        /// <summary>یک ردیف فهرست می‌سازد: متن چپ‌چین و خط طلایی زیر گزینه‌ی فعال.</summary>
        protected void AddRow(RectTransform panel, string label, float y, Action action)
        {
            RectTransform row = UiKit.Box("Row_" + label, panel, PauseMenuUI.RowX, y, PauseMenuUI.RowWidth, PauseMenuUI.RowHeight);

            TMP_Text text = UiKit.Text("Label", row, PauseMenuUI.RowFont, UiKit.RowIdle, Menu.Font, true);
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
    }

    /// <summary>یک ردیف قابل انتخاب در فهرست یک تب.</summary>
    public sealed class PauseRow
    {
        public RectTransform Rect;
        public TMP_Text Label;
        public GameObject Underline;
        public Action Action;
    }
}
