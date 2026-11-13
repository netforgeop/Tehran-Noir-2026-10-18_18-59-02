using System;
using UnityEngine;

namespace TehranNoir.Documents
{
    /// <summary>
    /// نقطه‌ی ورود به لایه‌ی اسناد. هر کسی می‌خواهد کاغذ نشان دهد فقط همین را صدا می‌زند؛
    /// لازم نیست در صحنه آبجکتی از قبل باشد — اگر نبود، ساخته می‌شود.
    /// </summary>
    public static class DocumentLayer
    {
        /// <summary>لایه‌ی اسناد صحنه؛ اگر نبود می‌سازد.</summary>
        public static DocumentView Ensure()
        {
            if (DocumentView.Current != null) return DocumentView.Current;

            DocumentView found = UnityEngine.Object.FindAnyObjectByType<DocumentView>(FindObjectsInactive.Include);
            if (found != null) return found;

            GameObject host = new GameObject("DocumentLayer");
            return host.AddComponent<DocumentView>();
        }

        /// <summary>نمایش یک سند. با بسته شدنش، onClosed صدا زده می‌شود.</summary>
        public static void Show(DocumentText document, Action onClosed = null)
        {
            Ensure().Show(document, onClosed);
        }

        /// <summary>خواندن از Resources و نمایش. مسیر بدون پسوند: «Documents/file_sealed».</summary>
        public static bool ShowResource(string resourcePath, Action onClosed = null)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                Debug.LogError("[Document] مسیر سند خالی است.");
                if (onClosed != null) onClosed();
                return false;
            }

            string path = resourcePath.Trim();
            if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 4);

            TextAsset asset = Resources.Load<TextAsset>(path);

            if (asset == null)
            {
                Debug.LogError("[Document] سند پیدا نشد: Resources/" + path);
                if (onClosed != null) onClosed();
                return false;
            }

            DocumentText document = DocumentText.Parse(asset.text, asset.name);

            for (int i = 0; i < document.Issues.Count; i++)
                Debug.LogWarning("[Document] " + asset.name + " → " + document.Issues[i]);

            Show(document, onClosed);
            return true;
        }

        public static void Close()
        {
            if (DocumentView.Current != null) DocumentView.Current.Close();
        }
    }
}
