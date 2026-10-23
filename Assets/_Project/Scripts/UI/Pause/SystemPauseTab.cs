using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// تب تنظیمات: همان چیزی که در منوی توقف Mafia تب اول است —
    /// ادامه، تنظیمات، بازگشت به منوی اصلی، خروج.
    /// </summary>
    public sealed class SystemPauseTab : PauseTab
    {
        public override string Id => "system";

        public override string Label => "تنظیمات";

        protected override void OnBuild(RectTransform panel)
        {
            float y = PauseMenuUI.FirstRowY;

            AddRow(panel, "ادامه بازی", y, Menu.Resume);
            y += PauseMenuUI.RowHeight;

            AddRow(panel, "تنظیمات", y, ShowOptionsSoon);
            y += PauseMenuUI.RowHeight;

            AddRow(panel, "بازگشت به منوی اصلی", y, Menu.QuitToMainMenu);
            y += PauseMenuUI.RowHeight;

            AddRow(panel, "خروج از بازی", y, Menu.QuitGame);
        }

        private void ShowOptionsSoon()
        {
            Menu.ShowHint("صفحه‌ی تنظیمات در مرحله‌ی بعد ساخته می‌شود.");
        }
    }
}
