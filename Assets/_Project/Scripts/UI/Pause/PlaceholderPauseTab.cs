using TehranNoir.Settings;
using TMPro;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// تبی که محتوایش هنوز ساخته نشده. به‌جای پنل خالی، یک پیام صادقانه نشان می‌دهد.
    /// </summary>
    public sealed class PlaceholderPauseTab : PauseTab
    {
        private readonly string _id;
        private readonly string _labelKey;

        public PlaceholderPauseTab(string id, string labelKey)
        {
            _id = id;
            _labelKey = labelKey;
        }

        public override string Id => _id;

        public override string Label => Loc.Get(Loc.Channel.Menu, _labelKey);

        protected override void OnBuild(RectTransform panel)
        {
            TMP_Text text = UiKit.Text("Soon", panel, 26f, UiKit.Dim, Menu.Font, Menu.Persian);
            text.alignment = TextAlignmentOptions.Center;
            text.text = Loc.Get(Loc.Channel.Menu, "pause.soon");
        }
    }
}
