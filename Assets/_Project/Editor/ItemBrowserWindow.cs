using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TehranNoir.Items;

namespace TehranNoir.EditorTools
{
    /// <summary>
    /// پنجره‌ی مرور آیتم‌ها.
    ///
    /// کارش این است که طراحی بدون کد آیتم بسازد: فهرست، جست‌وجو، فیلتر دسته،
    /// جزئیات (قاعده‌ها و وضعیت اولیه)، بررسی داده و افزودن خط تازه به
    /// `items.txt`.
    ///
    /// عمداً ScriptableObject نیست: منبع داده همان فایل متنی است و این پنجره
    /// فقط روی آن می‌نشیند. اگر روزی منبع عوض شد، این پنجره هم با آن عوض می‌شود.
    /// </summary>
    public sealed class ItemBrowserWindow : EditorWindow
    {
        private const string ItemsPath = "Assets/_Project/Resources/Items/items.txt";

        private readonly List<ItemDefinition> _shown = new List<ItemDefinition>(32);
        private readonly List<string> _issues = new List<string>(16);

        private Vector2 _listScroll;
        private Vector2 _detailScroll;

        private string _search = string.Empty;
        private int _categoryFilter;

        private ItemDefinition _selected;

        private string _newId = "item.new_01";
        private string _newPersian = string.Empty;
        private string _newEnglish = string.Empty;

        private static readonly string[] CategoryLabels =
        {
            "همه",
            "اسناد",
            "کلیدها",
            "سلاح",
            "مهمات",
            "شواهد",
            "مصرفی",
            "ابزار",
            "مأموریت",
            "بقیه"
        };

        private static readonly ItemCategory[] CategoryValues =
        {
            ItemCategory.Miscellaneous, // جای «همه»؛ فیلتر جداگانه بررسی می‌شود
            ItemCategory.Documents,
            ItemCategory.Keys,
            ItemCategory.Weapons,
            ItemCategory.Ammunition,
            ItemCategory.Evidence,
            ItemCategory.Consumables,
            ItemCategory.Tools,
            ItemCategory.QuestItems,
            ItemCategory.Miscellaneous
        };

        [MenuItem("Tehran Noir/آیتم‌ها")]
        public static void Open()
        {
            ItemBrowserWindow window = GetWindow<ItemBrowserWindow>("آیتم‌ها");
            window.minSize = new Vector2(860f, 540f);
            window.Refresh();
        }

        private void OnEnable()
        {
            ItemCatalog.Reload();
            Refresh();
        }

        private void OnGUI()
        {
            DrawToolbar();

            GUILayout.BeginHorizontal();
            DrawList();
            DrawDetails();
            GUILayout.EndHorizontal();

            DrawFooter();
        }

        /* ============================================================
           نوار بالا
           ============================================================ */

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("جست‌وجو", GUILayout.Width(50f));

            string search = GUILayout.TextField(_search ?? string.Empty, GUILayout.Width(180f));

            if (search != _search)
            {
                _search = search;
                Refresh();
            }

            int filter = EditorGUILayout.Popup(_categoryFilter, CategoryLabels, GUILayout.Width(110f));

            if (filter != _categoryFilter)
            {
                _categoryFilter = filter;
                Refresh();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("بازخوانی", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                ItemCatalog.Reload();
                Refresh();
            }

            if (GUILayout.Button("بررسی داده", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                Validate();

            GUILayout.EndHorizontal();
        }

        /* ============================================================
           فهرست
           ============================================================ */

        private void DrawList()
        {
            GUILayout.BeginVertical(GUILayout.Width(300f));

            GUILayout.Label(_shown.Count + " آیتم", EditorStyles.boldLabel);

            _listScroll = GUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));

            for (int i = 0; i < _shown.Count; i++)
            {
                ItemDefinition definition = _shown[i];

                bool active = definition == _selected;

                string label = definition.Id + "   ·   " + definition.Persian;

                if (GUILayout.Toggle(active, label, GUI.skin.button))
                {
                    _selected = definition;
                    GUI.FocusControl(null);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /* ============================================================
           جزئیات
           ============================================================ */

        private void DrawDetails()
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));

            if (_selected == null)
            {
                EditorGUILayout.HelpBox("یک آیتم از فهرست انتخاب کن.", MessageType.Info);
            }
            else
            {
                DrawDefinition(_selected);
            }

            GUILayout.Space(12f);
            DrawCreate();

            if (_issues.Count > 0)
            {
                GUILayout.Space(12f);
                GUILayout.Label("بررسی داده (" + _issues.Count + ")", EditorStyles.boldLabel);

                for (int i = 0; i < _issues.Count; i++)
                    EditorGUILayout.HelpBox(_issues[i], MessageType.Warning);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawDefinition(ItemDefinition definition)
        {
            EditorGUILayout.LabelField(definition.Persian, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(definition.English, EditorStyles.miniLabel);

            GUILayout.Space(6f);

            EditorGUILayout.LabelField("شناسه", definition.Id);
            EditorGUILayout.LabelField("دسته", definition.Category.ToString());
            EditorGUILayout.LabelField("زیردسته", definition.Subcategory);
            EditorGUILayout.LabelField("بیشترین تعداد", definition.MaxStack.ToString());
            EditorGUILayout.LabelField("وزن", definition.Weight.ToString("0.###"));
            EditorGUILayout.LabelField("ارزش", definition.Value.ToString());
            EditorGUILayout.LabelField("پرچم‌ها", ItemCategories.ToText(definition.Flags));

            if (!string.IsNullOrEmpty(definition.DocumentPath))
                EditorGUILayout.LabelField("سند", definition.DocumentPath);

            if (!string.IsNullOrEmpty(definition.DescriptionPersian))
                EditorGUILayout.LabelField("شرح", definition.DescriptionPersian, EditorStyles.wordWrappedLabel);

            GUILayout.Space(8f);
            GUILayout.Label("وضعیت اولیه", EditorStyles.boldLabel);

            if (definition.InitialState == null || definition.InitialState.Count == 0)
            {
                EditorGUILayout.LabelField("—", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < definition.InitialState.Count; i++)
                {
                    ItemStatePair pair = definition.InitialState[i];
                    EditorGUILayout.LabelField(pair.Key, pair.Value);
                }
            }

            GUILayout.Space(8f);
            GUILayout.Label("رفتار (قاعده‌ها)", EditorStyles.boldLabel);

            if (definition.Rules == null || definition.Rules.Count == 0)
            {
                EditorGUILayout.LabelField("—", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < definition.Rules.Count; i++)
                {
                    ItemRule rule = definition.Rules.Rules[i];

                    string title = ItemVerbs.Display(rule.Verb);

                    if (rule.HasCondition) title += "   if   " + rule.WhenSource;
                    if (rule.RequiresTarget) title += "   →   " + rule.TargetId;

                    EditorGUILayout.LabelField(title);

                    if (rule.Effects.Length > 0)
                        EditorGUILayout.LabelField("do   " + string.Join(" ; ", rule.Effects), EditorStyles.wordWrappedLabel);

                    if (rule.Consume)
                        EditorGUILayout.LabelField("مصرف یک عدد", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawCreate()
        {
            GUILayout.Label("آیتم تازه", EditorStyles.boldLabel);

            _newId = EditorGUILayout.TextField("شناسه", _newId);
            _newPersian = EditorGUILayout.TextField("فارسی", _newPersian);
            _newEnglish = EditorGUILayout.TextField("انگلیسی", _newEnglish);

            if (GUILayout.Button("افزودن به items.txt", GUILayout.Height(24f)))
                AddItem();
        }

        private void DrawFooter()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("فایل: " + ItemsPath, EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            GUILayout.Label("تعداد کل: " + ItemCatalog.Count, EditorStyles.miniLabel);

            GUILayout.EndHorizontal();
        }

        /* ============================================================
           داده
           ============================================================ */

        private void Refresh()
        {
            _shown.Clear();

            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;
            string search = (_search ?? string.Empty).Trim().ToLowerInvariant();

            ItemCategory wanted = CategoryValues[Mathf.Clamp(_categoryFilter, 0, CategoryValues.Length - 1)];
            bool filterByCategory = _categoryFilter > 0;

            for (int i = 0; i < all.Count; i++)
            {
                ItemDefinition definition = all[i];

                if (filterByCategory && definition.Category != wanted) continue;

                if (search.Length > 0 &&
                    definition.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    definition.Persian.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    definition.English.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                _shown.Add(definition);
            }

            if (_selected != null && !_shown.Contains(_selected)) _selected = null;
            if (_selected == null && _shown.Count > 0) _selected = _shown[0];

            Repaint();
        }

        /// <summary>بررسی سبک داده: تکرار شناسه، دسته و فعل ناشناس، مسیرهای گم‌شده.</summary>
        private void Validate()
        {
            _issues.Clear();

            string full = Path.GetFullPath(ItemsPath);

            if (!File.Exists(full))
            {
                _issues.Add("فایل پیدا نشد: " + ItemsPath);
                return;
            }

            string[] lines = File.ReadAllLines(full);
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                string text = lines[i].Trim();

                if (text.Length == 0 || text.StartsWith("#")) continue;

                string[] parts = text.Split('|');

                if (parts.Length < 4)
                {
                    _issues.Add("خط " + (i + 1) + ": کمتر از ۴ فیلد");
                    continue;
                }

                string id = parts[0].Trim().ToLowerInvariant();

                if (id.Length == 0)
                    _issues.Add("خط " + (i + 1) + ": شناسه خالی");

                if (!ids.Add(id))
                    _issues.Add("خط " + (i + 1) + ": شناسه‌ی تکراری «" + id + "»");

                if (!ItemCategories.IsKnown(parts[3]))
                    _issues.Add("خط " + (i + 1) + ": دسته‌ی ناشناس «" + parts[3].Trim() + "»");

                for (int p = 5; p < parts.Length; p++)
                {
                    string option = parts[p].Trim();

                    if (option.Length == 0) continue;

                    int split = option.IndexOf('=');

                    if (split <= 0)
                    {
                        _issues.Add("خط " + (i + 1) + ": گزینه‌ی بی‌شکل «" + option + "»");
                        continue;
                    }

                    string key = option.Substring(0, split).Trim().ToLowerInvariant();

                    if (key.StartsWith("state.") || key == "desc.fa" || key == "desc.en" ||
                        key == "ontake.do" || key == "ondrop.do")
                        continue;

                    if (key == "weight" || key == "value" || key == "flags" || key == "icon" ||
                        key == "prefab" || key == "sub" || key == "doc")
                        continue;

                    int dot = key.IndexOf('.');

                    if (dot <= 0)
                    {
                        _issues.Add("خط " + (i + 1) + ": کلید ناشناس «" + key + "»");
                        continue;
                    }

                    string verb = key.Substring(0, dot);

                    if (!ItemVerbs.IsKnown(verb))
                        _issues.Add("خط " + (i + 1) + ": فعل ناشناس «" + verb + "»");
                }
            }

            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                ItemDefinition definition = all[i];

                if (!string.IsNullOrEmpty(definition.IconPath) &&
                    Resources.Load<Sprite>(definition.IconPath) == null)
                    _issues.Add("آیکون پیدا نشد: " + definition.IconPath);

                if (!string.IsNullOrEmpty(definition.PrefabPath) &&
                    Resources.Load<GameObject>(definition.PrefabPath) == null)
                    _issues.Add("Prefab پیدا نشد: " + definition.PrefabPath);

                if (!string.IsNullOrEmpty(definition.DocumentPath) &&
                    Resources.Load<TextAsset>(definition.DocumentPath) == null)
                    _issues.Add("سند پیدا نشد: " + definition.DocumentPath);
            }

            if (_issues.Count == 0) _issues.Add("چیزی پیدا نشد؛ داده سالم است.");

            Repaint();
        }

        private void AddItem()
        {
            _issues.Clear();

            string id = (_newId ?? string.Empty).Trim().ToLowerInvariant();

            if (id.Length == 0)
            {
                _issues.Add("شناسه خالی است.");
                return;
            }

            if (ItemCatalog.Exists(id))
            {
                _issues.Add("شناسه‌ی «" + id + "» از قبل هست.");
                return;
            }

            string persian = string.IsNullOrEmpty(_newPersian) ? id : _newPersian.Trim();
            string english = string.IsNullOrEmpty(_newEnglish) ? id : _newEnglish.Trim();

            string line = id + " | " + persian + " | " + english +
                          " | item | 1 | weight=0.1 | state.new=1 | use.if=state.new = 1 | use.do=state.new = 0";

            File.AppendAllText(Path.GetFullPath(ItemsPath), "\n" + line + "\n");

            AssetDatabase.Refresh();
            ItemCatalog.Reload();

            _newId = "item.new_01";
            _newPersian = string.Empty;
            _newEnglish = string.Empty;

            Refresh();

            Debug.Log("[Items] آیتم تازه اضافه شد: " + line);
        }
    }
}
