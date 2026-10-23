using TMPro;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// تبی که محتوایش هنوز ساخته نشده. به‌جای پنل خالی، یک پیام صادقانه نشان می‌دهد
    /// تا شکل منو از همین حالا درست باشد.
    /// </summary>
    public sealed class PlaceholderPauseTab : PauseTab
    {
        private readonly string _id;
        private readonly string _label;

        public PlaceholderPauseTab(string id, string label)
        {
            _id = id;
            _label = label;
        }

        public override string Id => _id;

        public override string Label => _label;

        protected override void OnBuild(RectTransform panel)
        {
            TMP_Text text = UiKit.Text("Soon", panel, 26f, UiKit.Dim, Menu.Font, true);
            text.alignment = TextAlignmentOptions.Center;
            text.text = "این بخش در مرحله‌ی بعد ساخته می‌شود.";
        }
    }
}
