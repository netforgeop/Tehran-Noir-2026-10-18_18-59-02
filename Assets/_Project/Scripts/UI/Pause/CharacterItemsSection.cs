using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TehranNoir.Items;
using TehranNoir.Settings;

namespace TehranNoir.UI
{
    /// <summary>
    /// بخش «وسایل» تب کاراکتر: نوار دسته‌ها، شبکه‌ی کارت‌ها و خط شرح.
    ///
    /// ساختار از فهرست موجودی بازی‌های مدرن آمده (دسته‌ها بالا، کارت‌ها در شبکه)،
    /// ولی زبان بصری همان زبان بازی است: تیره، حاشیه‌ی نازک، طلاییِ کم.
    ///
    /// این کلاس پنل خودش را زیر یک RectTransform می‌سازد و ورودی صفحه‌کلید و
    /// ماوس را خودش می‌گیرد؛ تب فقط صدا می‌زند. این‌طوری بخش وسایل هم یک
    /// تکه‌ی مستقل است، نه صد خط داخل تب.
    ///
    /// عدد اینجا نگه داشته نمی‌شود: همه‌چیز از Inventory (روی WorldState) خوانده می‌شود.
    /// </summary>
    public sealed class CharacterItemsSection
    {
        /* ============================================================
           اندازه‌ها (واحد کانواس ۱۹۲۰×۱۰۸۰، مبدأ بالا-چپِ ستون محتوا)
           ============================================================ */

        // مختصات نسبت به بالای ستون محتوا؛ ستون خودش از y=236 پنل شروع می‌شود
        private const float StripY = 0f;
        private const float ChipHeight = 40f;
        private const float ChipWidth = 128f;
        private const float ChipGap = 10f;
        private const float SortWidth = 196f;

        private const float GridTop = 64f;
        private const float CardWidth = 306f;
        private const float CardHeight = 148f;
        private const float CardGapX = 22f;
        private const float CardGapY = 18f;

        private const int Columns = 4;
        private const int Rows = 3;
        private const int PerPage = Columns * Rows;

        private const float DetailY = 570f;
        private const float DetailHeight = 36f;

        private const float NameFont = 22f;
        private const float KindFont = 15f;
        private const float CountFont = 26f;
        private const float ChipFont = 20f;
        private const float DetailFont = 19f;
        private const float IconSize = 52f;

        /// <summary>شماره‌ی دسته‌ها به ترتیب نوار. ۰ یعنی همه.</summary>
        private const int FilterAll = 0;
        private const int FilterDocument = 1;
        private const int FilterKey = 2;
        private const int FilterItem = 3;

        /// <summary>جایگاه ترتیب در همان نوار؛ بعد از چهار دسته.</summary>
        private const int SortChip = 4;

        private static readonly string[] FilterKeys =
        {
            "char.items.all", "char.items.docs", "char.items.keys", "char.items.misc"
        };

        /* ---------- رنگ‌ها ---------- */

        private static readonly Color CardTop = new Color(0.108f, 0.102f, 0.094f, 1f);
        private static readonly Color CardBottom = new Color(0.074f, 0.070f, 0.064f, 1f);
        private static readonly Color CardEdge = new Color(0.280f, 0.268f, 0.248f, 0.85f);
        private static readonly Color ChipTop = new Color(0.100f, 0.095f, 0.088f, 1f);
        private static readonly Color ChipBottom = new Color(0.070f, 0.066f, 0.061f, 1f);
        private static readonly Color ChipEdge = new Color(0.250f, 0.240f, 0.222f, 0.9f);
        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        /* ---------- وضعیت ---------- */

        private readonly List<InventorySlot> _all = new List<InventorySlot>(32);
        private readonly List<InventorySlot> _filtered = new List<InventorySlot>(32);
        private readonly List<CardView> _cards = new List<CardView>(PerPage);
        private readonly List<ChipView> _chips = new List<ChipView>(5);

        private RectTransform _content;
        private PauseMenuUI _menu;

        private TMP_Text _detail;
        private TMP_Text _pageText;
        private TMP_Text _note;

        private int _filter = FilterAll;
        private bool _sortByCount;
        private int _index;
        private bool _focusFilter;
        private int _filterFocus;
        private int _signature = int.MinValue;
        private int _shownPage;
        private bool _built;

        public CharacterItemsSection(PauseMenuUI menu)
        {
            _menu = menu;
        }

        private bool Rtl => _menu != null && _menu.Persian;

        private TMP_FontAsset Font => _menu != null ? _menu.Font : null;

        /* ============================================================
           ساخت
           ============================================================ */

        /// <summary>
        /// ستون محتوا را خالی و بخش را از نو می‌سازد. فقط تب این را صدا می‌زند
        /// (خودِ تب قبلش بچه‌های ستون را پاک می‌کند؛ این متد هم برای بازسازی
        /// داخلی از همان مسیر استفاده می‌کند).
        /// </summary>
        public void Build(RectTransform content)
        {
            _content = content;

            Clear(content);

            _cards.Clear();
            _chips.Clear();

            _signature = Inventory.Signature();

            ReadSlots();
            ClampState();

            BuildChips();
            BuildNote();
            BuildDetail();
            BuildCards();

            _built = true;
        }

        /// <summary>هر فریم که منو باز است. فقط وقتی تعدادها عوض شده باشد بازمی‌سازد.</summary>
        public void Tick()
        {
            if (!_built || _content == null) return;

            if (Inventory.Signature() == _signature) return;

            Build(_content);
        }

        private static void Clear(RectTransform content)
        {
            if (content == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        /* ============================================================
           داده
           ============================================================ */

        private void ReadSlots()
        {
            Inventory.Slots(_all);

            _filtered.Clear();

            for (int i = 0; i < _all.Count; i++)
            {
                if (!Matches(_all[i].Definition.Kind)) continue;

                _filtered.Add(_all[i]);
            }

            if (_sortByCount) _filtered.Sort(CompareByCount);
        }

        private bool Matches(ItemKind kind)
        {
            switch (_filter)
            {
                case FilterDocument: return kind == ItemKind.Document;
                case FilterKey: return kind == ItemKind.Key;
                case FilterItem: return kind == ItemKind.Item;
                default: return true;
            }
        }

        /// <summary>تعداد زیاد اول؛ با تعداد برابر، ترتیب فایل حفظ می‌شود (پایدار).</summary>
        private static int CompareByCount(InventorySlot a, InventorySlot b)
        {
            return b.Count.CompareTo(a.Count);
        }

        private void ClampState()
        {
            _filter = Mathf.Clamp(_filter, FilterAll, FilterItem);
            _filterFocus = Mathf.Clamp(_filterFocus, 0, SortChip);

            if (_filtered.Count == 0)
            {
                _index = 0;
                _focusFilter = true;
                return;
            }

            _index = Mathf.Clamp(_index, 0, _filtered.Count - 1);
        }

        private int Page => _filtered.Count == 0 ? 0 : _index / PerPage;

        private int PageCount => Mathf.Max(1, (_filtered.Count + PerPage - 1) / PerPage);

        /* ============================================================
           نوار دسته‌ها
           ============================================================ */

        private void BuildChips()
        {
            for (int i = 0; i < FilterKeys.Length; i++)
            {
                float x = Rtl
                    ? _content.rect.width - ChipWidth - i * (ChipWidth + ChipGap)
                    : i * (ChipWidth + ChipGap);

                AddChip(i, x, Loc.Get(Loc.Channel.Menu, FilterKeys[i]));
            }

            float sortX = Rtl ? 0f : _content.rect.width - SortWidth;

            AddChip(SortChip, sortX, Loc.Get(Loc.Channel.Menu, _sortByCount ? "char.items.sort.count" : "char.items.sort.name"), SortWidth);

            RectTransform pageBox = UiKit.Box("Page", _content, SortWidth + ChipGap, StripY, 420f, ChipHeight);

            _pageText = UiKit.Text("Text", pageBox, ChipFont, UiKit.Dim, Font, Rtl);
            _pageText.alignment = TextAlignmentOptions.Center;
            UiKit.ForceFixNumbers(_pageText, Rtl);

            RefreshChips();
        }

        private void AddChip(int index, float x, string label, float width = ChipWidth)
        {
            RectTransform box = UiKit.Box("Chip_" + index, _content, x, StripY, width, ChipHeight);

            ChipView chip = new ChipView();
            chip.Rect = box;
            chip.Image = box.gameObject.AddComponent<Image>();
            chip.Image.raycastTarget = false;
            chip.Label = UiKit.Text("Text", box, ChipFont, UiKit.RowIdle, Font, Rtl);
            chip.Label.alignment = TextAlignmentOptions.Center;
            chip.Label.text = label;
            chip.Width = Mathf.RoundToInt(width);
            chip.Index = index;

            _chips.Add(chip);
        }

        private void RefreshChips()
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                ChipView chip = _chips[i];

                bool active = chip.Index == SortChip ? false : _filter == chip.Index;
                bool focused = _focusFilter && _filterFocus == chip.Index;

                chip.Image.sprite = HudArt.Panel(
                    "item.chip." + chip.Width + "." + (active ? "on" : focused ? "focus" : "idle"),
                    chip.Width, Mathf.RoundToInt(ChipHeight), 5f,
                    active || focused ? new Color(0.135f, 0.122f, 0.098f, 1f) : ChipTop,
                    active || focused ? new Color(0.098f, 0.088f, 0.070f, 1f) : ChipBottom,
                    active || focused ? UiKit.Gold : ChipEdge,
                    1f);

                chip.Label.color = active || focused ? UiKit.Gold : UiKit.RowIdle;
            }
        }

        /* ============================================================
           شبکه‌
           ============================================================ */

        private void BuildCards()
        {
            if (_filtered.Count == 0)
            {
                _note.text = Loc.Get(Loc.Channel.Menu, _all.Count == 0 ? "char.items.empty" : "char.items.none");
                _note.gameObject.SetActive(true);

                _pageText.text = string.Empty;
                _detail.text = string.Empty;
                return;
            }

            _note.gameObject.SetActive(false);

            _shownPage = Page;

            int first = Page * PerPage;
            int shown = Mathf.Min(PerPage, _filtered.Count - first);

            for (int slot = 0; slot < shown; slot++)
                AddCard(_filtered[first + slot], slot);

            _pageText.text = Loc.Get(Loc.Channel.Menu, "char.items.page") + " " + (Page + 1) + " / " + PageCount;

            RefreshCards();
            RefreshDetail();
        }

        /// <summary>کارت یک آیتم: زمینه، نوار رنگ دسته، آیکون، نام، نوع و تعداد.</summary>
        private void AddCard(InventorySlot slot, int position)
        {
            int column = position % Columns;
            int row = position / Columns;

            float x = Rtl
                ? _content.rect.width - CardWidth - column * (CardWidth + CardGapX)
                : column * (CardWidth + CardGapX);

            float y = GridTop + row * (CardHeight + CardGapY);

            RectTransform card = UiKit.Box("Card_" + position, _content, x, y, CardWidth, CardHeight);
            card.gameObject.AddComponent<Image>().raycastTarget = false;

            CardView view = new CardView();
            view.Rect = card;
            view.Background = card.GetComponent<Image>();
            view.Background.sprite = HudArt.Panel("item.card", Mathf.RoundToInt(CardWidth), Mathf.RoundToInt(CardHeight),
                6f, CardTop, CardBottom, CardEdge, 1f);

            Color accent = AccentFor(slot.Definition.Kind);

            RectTransform bar = UiKit.Box("Accent", card, Rtl ? CardWidth - 4f : 0f, 10f, 4f, CardHeight - 20f);

            Image barImage = bar.gameObject.AddComponent<Image>();
            barImage.color = accent;
            barImage.raycastTarget = false;

            RectTransform iconBox = UiKit.Box("Icon", card, Rtl ? CardWidth - 20f - IconSize : 20f,
                (CardHeight - IconSize) * 0.5f, IconSize, IconSize);

            Image icon = iconBox.gameObject.AddComponent<Image>();
            icon.sprite = HudArt.Icon(GlyphFor(slot.Definition.Kind), 96, Color.white);
            icon.color = accent;
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            const float textWidth = CardWidth - 24f - IconSize - 32f;
            float textX = Rtl ? 18f : CardWidth - 18f - textWidth;

            RectTransform nameBox = UiKit.Box("Name", card, textX, 16f, textWidth, 30f);

            TMP_Text nameText = UiKit.Text("Text", nameBox, NameFont, UiKit.RowIdle, Font, Rtl);
            nameText.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            nameText.text = Fit(slot.Definition.Name(Rtl), 22);

            RectTransform kindBox = UiKit.Box("Kind", card, textX, 52f, textWidth, 22f);

            TMP_Text kind = UiKit.Text("Text", kindBox, KindFont, UiKit.Dim, Font, Rtl);
            kind.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            kind.text = Loc.Get(Loc.Channel.Menu, "char.items.kind." + slot.Definition.Kind.ToString().ToLowerInvariant());

            RectTransform countBox = UiKit.Box("Count", card, textX, 88f, textWidth, 40f);

            TMP_Text count = UiKit.Text("Text", countBox, CountFont, UiKit.Gold, Font, Rtl);
            count.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

            // فقط عدد است؛ بی ForceFix به رقم فارسی تبدیل نمی‌شد
            UiKit.ForceFixNumbers(count, Rtl);

            count.text = slot.Count.ToString();

            RectTransform border = UiKit.Box("Select", card, 0f, 0f, CardWidth, CardHeight);

            Image borderImage = border.gameObject.AddComponent<Image>();
            borderImage.sprite = HudArt.Panel("item.card.select", Mathf.RoundToInt(CardWidth), Mathf.RoundToInt(CardHeight),
                6f, Clear, Clear, UiKit.Gold, 2f);
            borderImage.raycastTarget = false;
            border.gameObject.SetActive(false);

            view.Icon = icon;
            view.Name = nameText;
            view.Count = count;
            view.Border = borderImage.gameObject;

            _cards.Add(view);
        }

        private void BuildDetail()
        {
            RectTransform box = UiKit.Box("Detail", _content, 0f, DetailY, _content.rect.width, DetailHeight);

            _detail = UiKit.Text("Text", box, DetailFont, UiKit.Dim, Font, Rtl);
            _detail.alignment = Rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        }

        private void BuildNote()
        {
            RectTransform box = UiKit.Box("Empty", _content, 0f, GridTop, _content.rect.width, 80f);

            _note = UiKit.Text("Text", box, 22f, UiKit.Dim, Font, Rtl);
            _note.alignment = TextAlignmentOptions.Center;
            _note.gameObject.SetActive(false);
        }

        private void RefreshCards()
        {
            int first = Page * PerPage;

            for (int i = 0; i < _cards.Count; i++)
            {
                CardView card = _cards[i];

                bool active = !_focusFilter && first + i == _index;

                card.Border.SetActive(active);
                card.Name.color = active ? UiKit.Gold : UiKit.RowIdle;
                card.Background.sprite = HudArt.Panel(
                    active ? "item.card.on" : "item.card",
                    Mathf.RoundToInt(CardWidth), Mathf.RoundToInt(CardHeight), 6f,
                    active ? new Color(0.135f, 0.126f, 0.110f, 1f) : CardTop,
                    active ? new Color(0.092f, 0.085f, 0.072f, 1f) : CardBottom,
                    active ? UiKit.Gold : CardEdge,
                    1f);
            }
        }

        private void RefreshDetail()
        {
            if (_filtered.Count == 0 || _index < 0 || _index >= _filtered.Count)
            {
                _detail.text = string.Empty;
                return;
            }

            InventorySlot slot = _filtered[_index];

            string kind = Loc.Get(Loc.Channel.Menu,
                "char.items.kind." + slot.Definition.Kind.ToString().ToLowerInvariant());

            // عددها ASCII؛ تبدیل و جهت‌دهی کار RTLTMPro است
            _detail.text = slot.Definition.Name(Rtl) + "   ·   " + kind + "   ·   "
                + slot.Count + " / " + slot.Definition.MaxStack;
        }

        /* ============================================================
           ورودی
           ============================================================ */

        /// <summary>بالا/پایین. از ردیف اول به نوار دسته‌ها می‌رسد.</summary>
        public void Move(int delta)
        {
            if (_content == null || delta == 0) return;

            if (_filtered.Count == 0)
            {
                _focusFilter = true;
                RefreshChips();
                return;
            }

            if (_focusFilter)
            {
                if (delta > 0)
                {
                    _focusFilter = false;
                    RefreshChips();
                    RefreshCards();
                    RefreshDetail();
                }

                return;
            }

            if (delta < 0)
            {
                if (_index >= Columns)
                {
                    _index -= Columns;
                }
                else
                {
                    _focusFilter = true;
                    _filterFocus = Mathf.Min(_filter, FilterItem);
                }
            }
            else
            {
                int target = _index + Columns;

                if (target < _filtered.Count)
                {
                    _index = target;
                }
                else if (Page < PageCount - 1)
                {
                    // ردیف اولِ صفحه‌ی بعد ممکن است نصفه باشد؛ آخرین آیتم نزدیک‌ترین جای درست است
                    _index = _filtered.Count - 1;
                }
            }

            // اگر انتخاب به صفحه‌ی دیگری رفت، کارت‌های همان صفحه ساخته شوند
            if (Page != _shownPage)
            {
                Build(_content);
                return;
            }

            RefreshChips();
            RefreshCards();
            RefreshDetail();
        }

        /// <summary>چپ/راست: در شبکه حرکت، در نوار دسته تغییر دسته.</summary>
        public void Step(int delta)
        {
            if (_content == null || delta == 0) return;

            if (_focusFilter)
            {
                _filterFocus = Mathf.Clamp(_filterFocus + delta, 0, SortChip);
                RefreshChips();
                return;
            }

            if (_filtered.Count == 0) return;

            _index = (_index + delta + _filtered.Count) % _filtered.Count;

            // چپ از اولین کارت یا راست از آخرین کارت، صفحه را عوض می‌کند
            if (Page != _shownPage)
            {
                Build(_content);
                return;
            }

            RefreshCards();
            RefreshDetail();
        }

        /// <summary>Enter: روی نوار یعنی «همین دسته» یا «عوض کردن ترتیب».</summary>
        public bool Activate()
        {
            if (!_focusFilter) return false;

            if (_filterFocus == SortChip)
            {
                // بعد از عوض شدن ترتیب، همان آیتم زیر نشانگر بماند، نه همان شماره
                string keep = SelectedId();

                _sortByCount = !_sortByCount;

                RebuildKeepingFocus(SortChip);
                SelectId(keep);

                return true;
            }

            _filter = _filterFocus;
            _index = 0;

            RebuildKeepingFocus(_filterFocus);
            return true;
        }

        private void RebuildKeepingFocus(int focus)
        {
            RectTransform content = _content;

            Build(content);

            _filterFocus = focus;
            RefreshChips();
            RefreshCards();
            RefreshDetail();
        }

        /// <summary>
        /// کلیک و هاور. true یعنی همین بخش ورودی را مصرف کرد و بقیه‌ی تب نباید
        /// دوباره روی آن تصمیم بگیرد.
        /// </summary>
        public bool Pointer(Vector2 screenPoint, bool clicked)
        {
            if (_content == null) return false;

            for (int i = 0; i < _chips.Count; i++)
            {
                ChipView chip = _chips[i];
                if (chip.Rect == null) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(chip.Rect, screenPoint, null)) continue;

                bool changed = _filterFocus != chip.Index || !_focusFilter;

                _focusFilter = true;
                _filterFocus = chip.Index;

                if (clicked)
                {
                    if (chip.Index == SortChip)
                    {
                        _sortByCount = !_sortByCount;
                        Build(_content);
                        _filterFocus = SortChip;
                        _focusFilter = true;
                    }
                    else if (_filter != chip.Index)
                    {
                        _filter = chip.Index;
                        _index = 0;
                        Build(_content);
                        _filterFocus = chip.Index;
                        _focusFilter = true;
                    }
                }
                else if (changed)
                {
                    RefreshChips();
                }

                if (clicked) RefreshChips();

                return true;
            }

            int first = Page * PerPage;

            for (int i = 0; i < _cards.Count; i++)
            {
                CardView card = _cards[i];
                if (card.Rect == null) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(card.Rect, screenPoint, null)) continue;

                int target = first + i;

                if (target != _index || _focusFilter)
                {
                    _index = target;
                    _focusFilter = false;

                    RefreshChips();
                    RefreshCards();
                    RefreshDetail();
                }

                // کلیک روی کارت فعلاً یعنی «انتخاب»؛ تا وقتی مصرف آیتم ساخته نشود
                return true;
            }

            return false;
        }

        private string SelectedId()
        {
            if (_filtered.Count == 0 || _index < 0 || _index >= _filtered.Count) return string.Empty;

            return _filtered[_index].Definition.Id;
        }

        private void SelectId(string id)
        {
            if (string.IsNullOrEmpty(id) || _filtered.Count == 0) return;

            for (int i = 0; i < _filtered.Count; i++)
            {
                if (_filtered[i].Definition.Id != id) continue;

                _index = i;
                return;
            }
        }

        /// <summary>انتخاب را به ردیف اول بخش برمی‌گرداند؛ هنگام ورود به بخش صدا زده می‌شود.</summary>
        public void FocusFirst()
        {
            // اگر دسته خالی باشد، ClampState هنگام ساخت خودش نشانگر را روی نوار می‌برد
            _index = 0;
            _focusFilter = false;
        }

        /* ============================================================
           رنگ و آیکون
           ============================================================ */

        /// <summary>نام بلند را با سه‌نقطه کوتاه می‌کند؛ کارت یک خط جا دارد.</summary>
        private static string Fit(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text;

            return text.Substring(0, Mathf.Max(1, max - 1)).TrimEnd() + "…";
        }

        private static Color AccentFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Document: return new Color(0.86f, 0.83f, 0.76f, 1f);
                case ItemKind.Key: return new Color(0.941f, 0.847f, 0.349f, 1f);
                default: return new Color(0.62f, 0.60f, 0.56f, 1f);
            }
        }

        private static HudArt.Glyph GlyphFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Key: return HudArt.Glyph.Key;
                case ItemKind.Item: return HudArt.Glyph.Case;
                default: return HudArt.Glyph.Document;
            }
        }

        /* ============================================================
           نماهای کوچک
           ============================================================ */

        private sealed class CardView
        {
            public RectTransform Rect;
            public Image Background;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text Count;
            public GameObject Border;
        }

        private sealed class ChipView
        {
            public RectTransform Rect;
            public Image Image;
            public TMP_Text Label;
            public int Index;
            public int Width;
        }
    }
}
