using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TehranNoir.Core;
using TehranNoir.Dialogue;
using TehranNoir.Documents;
using TehranNoir.Settings;
using TehranNoir.UI;

namespace TehranNoir.Quests
{
    /// <summary>
    /// فهرست کوچک کارها در گوشه‌ی بالا: دسته (اصلی/جانبی)، عنوان کار، و جمله‌ی
    /// «همین حالا چه کار کنم» زیرش به رنگ کم‌رنگ — همان الگوی فهرست کارها در
    /// Assassin's Creed، با ظاهر آرامِ Mafia.
    ///
    /// فقط از QuestLog می‌خواند و خودش هیچ وضعیتی نگه نمی‌دارد.
    /// هنگام دیالوگ، سند یا منوی توقف محو می‌شود.
    /// </summary>
    public sealed class QuestTracker : MonoBehaviour
    {
        private enum Corner
        {
            Auto,
            Left,
            Right
        }

        [Header("فونت")]
        [Tooltip("خالی بماند تا از Dialogue UI صحنه گرفته شود")]
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("برای متن انگلیسی؛ خالی = همان فونت بالا")]
        [SerializeField] private TMP_FontAsset latinFont;

        [Header("چیدمان")]
        [Tooltip("Auto یعنی فارسی بالا-راست، انگلیسی بالا-چپ")]
        [SerializeField] private Corner corner = Corner.Auto;

        [SerializeField] private int maxEntries = 3;
        [SerializeField] private float margin = 48f;

        [Header("رفتار")]
        [SerializeField] private float fadeSpeed = 8f;

        /* ---------- اندازه‌ها (واحد کانواس ۱۹۲۰×۱۰۸۰) ---------- */
        private const float Width = 460f;
        private const float EntryHeight = 68f;
        private const float EntryGap = 14f;
        private const float BarWidth = 2f;
        private const float TextInset = 16f;

        /* ---------- رنگ ---------- */
        private static readonly Color TitleColor = new Color(0.941f, 0.933f, 0.914f, 1f);
        private static readonly Color StepColor = new Color(0.655f, 0.663f, 0.620f, 1f);
        private static readonly Color KindColor = new Color(0.480f, 0.500f, 0.455f, 1f);
        private static readonly Color AccentColor = new Color(0.541f, 0.604f, 0.357f, 0.9f);

        private sealed class Entry
        {
            public RectTransform Rect;
            public TMP_Text Kind;
            public TMP_Text Title;
            public TMP_Text Step;
        }

        private readonly List<Entry> _entries = new List<Entry>(3);

        private GameObject _canvasObject;
        private CanvasGroup _group;
        private RectTransform _root;

        private int _signature = -1;
        private bool _persian;
        private float _alpha;

        private QuestLog Log
        {
            get { return QuestLog.Instance; }
        }

        private TMP_FontAsset Font
        {
            get
            {
                if (_persian && font != null) return font;
                if (!_persian && latinFont != null) return latinFont;
                if (font != null) return font;

                DialogueUI ui = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
                if (ui != null) font = ui.Font;

                return font;
            }
        }

        private bool AnchorRight
        {
            get
            {
                if (corner == Corner.Right) return true;
                if (corner == Corner.Left) return false;

                return Localization.Persian;
            }
        }

        private TextAlignmentOptions Align
        {
            get
            {
                return AnchorRight
                    ? TextAlignmentOptions.TopRight
                    : TextAlignmentOptions.TopLeft;
            }
        }

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            _persian = Localization.Persian;

            Build();
            _group.alpha = 0f;
        }

        private void OnEnable()
        {
            Loc.Changed += HandleLanguage;
        }

        private void OnDisable()
        {
            Loc.Changed -= HandleLanguage;
        }

        private void OnDestroy()
        {
            if (_canvasObject != null) Destroy(_canvasObject);
        }

        /// <summary>
        /// زبان که عوض می‌شود، چیدمان هم عوض می‌شود (راست/چپ).
        /// بازسازی کامل ساده‌تر و بی‌باگ‌تر از جابه‌جا کردن تک‌تک خط‌هاست.
        /// </summary>
        private void HandleLanguage()
        {
            bool persian = Localization.Persian;
            if (persian == _persian) return;

            _persian = persian;
            Rebuild();
        }

        private void Update()
        {
            if (_group == null) return;

            QuestLog log = Log;
            int signature = log != null ? log.Signature : -1;

            if (signature != _signature)
            {
                _signature = signature;
                Refresh(log);
            }

            bool visible = log != null && log.Active.Count > 0 && !Blocked();

            _alpha = Mathf.MoveTowards(_alpha, visible ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            _group.alpha = _alpha;
        }

        private static bool Blocked()
        {
            return PauseMenuUI.IsOpen || DialogueUI.Current != null || DocumentView.IsOpen;
        }

        /* ============================================================
           محتوا
           ============================================================ */

        private void Refresh(QuestLog log)
        {
            int count = log == null ? 0 : Mathf.Min(log.Active.Count, Mathf.Max(0, maxEntries));

            EnsureEntries(count);

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                bool used = i < count;

                entry.Rect.gameObject.SetActive(used);

                if (!used) continue;

                QuestStatus status = log.Active[i];

                entry.Kind.text = Localization.Get(
                    status.Quest.Kind == QuestKind.Main ? "quest.main" : "quest.side");

                entry.Title.text = status.Quest.Name(_persian);

                entry.Step.text = status.Step != null
                    ? status.Step.Text(_persian)
                    : ( _persian ? status.Quest.SummaryPersian : status.Quest.SummaryEnglish);

                entry.Rect.anchoredPosition = new Vector2(0f, -i * (EntryHeight + EntryGap));
            }

            if (_root != null)
                _root.sizeDelta = new Vector2(Width, Mathf.Max(0, count) * (EntryHeight + EntryGap));
        }

        private void EnsureEntries(int needed)
        {
            while (_entries.Count < needed)
            {
                _entries.Add(CreateEntry(_entries.Count));
            }
        }

        private Entry CreateEntry(int index)
        {
            RectTransform rect = UiKit.Node("Quest_" + index, _root);
            rect.anchorMin = new Vector2(AnchorRight ? 1f : 0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(AnchorRight ? 1f : 0f, 1f);
            rect.sizeDelta = new Vector2(Width, EntryHeight);
            rect.anchoredPosition = new Vector2(0f, -index * (EntryHeight + EntryGap));

            // خط نازک کنار هر کار؛ تنها عنصر تزئینی
            RectTransform bar = UiKit.Node("Bar", rect);
            bar.anchorMin = new Vector2(AnchorRight ? 1f : 0f, 0f);
            bar.anchorMax = new Vector2(AnchorRight ? 1f : 0f, 1f);
            bar.pivot = new Vector2(AnchorRight ? 1f : 0f, 0.5f);
            bar.sizeDelta = new Vector2(BarWidth, -6f);
            bar.anchoredPosition = Vector2.zero;

            Image barImage = bar.gameObject.AddComponent<Image>();
            barImage.color = AccentColor;
            barImage.raycastTarget = false;

            float textWidth = Width - TextInset - 6f;

            Entry entry = new Entry();
            entry.Rect = rect;
            entry.Kind = MakeLine("Kind", rect, textWidth, 0f, 13f, KindColor);
            entry.Title = MakeLine("Title", rect, textWidth, 17f, 19f, TitleColor);
            entry.Step = MakeLine("Step", rect, textWidth, 43f, 15f, StepColor);

            return entry;
        }

        private TMP_Text MakeLine(string name, RectTransform parent, float width, float top,
            float size, Color color)
        {
            RectTransform box = UiKit.Node(name, parent);
            box.anchorMin = new Vector2(AnchorRight ? 1f : 0f, 1f);
            box.anchorMax = box.anchorMin;
            box.pivot = new Vector2(AnchorRight ? 1f : 0f, 1f);
            box.sizeDelta = new Vector2(width, size * 1.7f);
            box.anchoredPosition = new Vector2(
                AnchorRight ? -TextInset : TextInset, -top);

            TMP_Text text = UiKit.MakeText(box.gameObject, size, color, Font, _persian);
            text.alignment = Align;

            return text;
        }

        private void Rebuild()
        {
            if (_canvasObject != null)
            {
                _canvasObject.SetActive(false);
                Destroy(_canvasObject);
                _canvasObject = null;
            }

            _entries.Clear();
            _signature = -1;
            _alpha = 0f;

            Build();
            _group.alpha = 0f;
        }

        /* ============================================================
           ساخت
           ============================================================ */

        private void Build()
        {
            _canvasObject = new GameObject("Quest Canvas (runtime)",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvasObject.transform.SetParent(null, false);

            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _root = UiKit.Node("Root", _canvasObject.transform);
            _root.sizeDelta = new Vector2(Width, 0f);

            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 1f;

            LayoutRoot();
        }

        private void LayoutRoot()
        {
            if (_root == null) return;

            _root.anchorMin = new Vector2(AnchorRight ? 1f : 0f, 1f);
            _root.anchorMax = _root.anchorMin;
            _root.pivot = new Vector2(AnchorRight ? 1f : 0f, 1f);
            _root.anchoredPosition = new Vector2(AnchorRight ? -margin : margin, -margin);
        }
    }
}
