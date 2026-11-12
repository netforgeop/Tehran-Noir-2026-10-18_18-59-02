using System;
using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.Documents
{
    /// <summary>
    /// همه‌ی کاغذهایی که در `Resources/Documents` هست، یک بار خوانده و نگه داشته
    /// می‌شود.
    ///
    /// چرا کش؟ تب پرونده‌ها هر بار باز شدن منو فهرست را می‌خواهد؛ اگر هر بار
    /// `Resources.LoadAll` و پارس کنیم، باز کردن منو به تعداد اسناد سنگین می‌شود.
    /// سندها در زمان اجرا عوض نمی‌شوند، پس یک بار کافی است.
    ///
    /// خطاهای پارس هر سند در Console می‌آید ولی جلوی بقیه را نمی‌گیرد؛ همان
    /// قاعده‌ی `DocumentText`: یک فایل بد، کل بایگانی را نمی‌خواباند.
    /// </summary>
    public static class DocumentCatalog
    {
        private static List<DocumentText> _documents;

        /// <summary>فهرست مرتب‌شده بر اساس نام فایل؛ فقط خواندنی.</summary>
        public static IReadOnlyList<DocumentText> All
        {
            get
            {
                Ensure();

                return _documents;
            }
        }

        private static void Ensure()
        {
            if (_documents != null) return;

            _documents = new List<DocumentText>(8);

            TextAsset[] files = Resources.LoadAll<TextAsset>("Documents");

            if (files == null || files.Length == 0)
            {
                Debug.LogWarning("[Document] هیچ فایلی در Resources/Documents پیدا نشد.");
                return;
            }

            Array.Sort(files, CompareByName);

            for (int i = 0; i < files.Length; i++)
            {
                DocumentText document = DocumentText.Parse(files[i].text, files[i].name);

                for (int j = 0; j < document.Issues.Count; j++)
                    Debug.LogWarning("[Document] " + files[i].name + " → " + document.Issues[j]);

                _documents.Add(document);
            }
        }

        private static int CompareByName(TextAsset left, TextAsset right)
        {
            return string.CompareOrdinal(left.name, right.name);
        }
    }
}
