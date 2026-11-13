using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TehranNoir.EditorTools
{
    /// <summary>
    /// صحنه‌های بازی را در Build Settings می‌گذارد.
    ///
    /// چرا لازم است؟ `SceneManager.LoadScene("Hilton814")` فقط وقتی کار می‌کند
    /// که صحنه در فهرست بیلد باشد. اگر نباشد، هیچ خطای واضحی نمی‌دهد؛ فقط
    /// کنسول یک خط قرمز می‌نویسد و بازیکن روی همان لابی گیر می‌کند. و چون
    /// `ProjectSettings` در گیت نیست، این فهرست روی هر کلون تازه خالی است.
    ///
    /// هر وقت پروژه باز شود یک بار چک می‌کند؛ اگر صحنه‌ای نبود، اضافه می‌کند.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneListBootstrap
    {
        private const string Folder = "Assets/_Project/Scenes/";

        // ترتیب مهم است: اولین صحنه، صحنه‌ای است که بازی با آن بالا می‌آید.
        private static readonly string[] Scenes =
        {
            "MainMenu",
            "Hilton",
            "Hilton814",
            "Sandbox",
            "Teahouse"
        };

        static SceneListBootstrap()
        {
            EditorApplication.delayCall += Ensure;
        }

        [MenuItem("Tehran Noir/صحنه‌ها/افزودن به Build Settings", false, 200)]
        public static void Ensure()
        {
            List<EditorBuildSettingsScene> result = new List<EditorBuildSettingsScene>();
            List<string> missing = new List<string>();
            bool changed = false;

            for (int i = 0; i < Scenes.Length; i++)
            {
                string path = Folder + Scenes[i] + ".unity";
                SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                if (asset == null)
                {
                    missing.Add(Scenes[i]);
                    continue;
                }

                result.Add(new EditorBuildSettingsScene(path, true));
            }

            // صحنه‌هایی که از قبل در فهرست بودند و ما نمی‌شناسیم، حذف نمی‌شوند.
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;

            for (int i = 0; i < existing.Length; i++)
            {
                bool known = false;

                for (int j = 0; j < result.Count; j++)
                    if (result[j].path == existing[i].path) known = true;

                if (!known && !string.IsNullOrEmpty(existing[i].path))
                {
                    result.Add(existing[i]);
                    changed = true;
                }
            }

            if (result.Count != existing.Length && existing.Length > 0) changed = true;
            if (existing.Length == 0) changed = true;

            if (!changed) return;

            EditorBuildSettings.scenes = result.ToArray();

            Debug.Log("[Scenes] فهرست بیلد به‌روز شد: " + result.Count + " صحنه" +
                      (missing.Count > 0 ? " (پیدا نشد: " + string.Join(", ", missing.ToArray()) + ")" : ""),
                      null);
        }
    }
}
