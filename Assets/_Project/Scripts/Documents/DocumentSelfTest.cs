using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Documents
{
    /// <summary>
    /// تست خودکار سند، بدون وابستگی به پکیج Test Framework.
    ///
    /// چه چیزی را ثابت می‌کند؟ مهم‌ترین قرارداد این جلسه: **ردیف نادر راد تا
    /// وقتی پرونده تحویل نشده در دفتر نیست**، و بعد از تحویل، وضعیت مهر همان
    /// چیزی است که او گفته — نه یک جمله‌ی ثابت.
    ///
    /// چرا این تست لازم شد؟ چون آن خطا هیچ خطای کامپایلی نداشت: متن دفتر درست
    /// چاپ می‌شد، فقط از اول می‌گفت «مهر سالم». تست باید ببیند چه چیزی **دیده
    /// نمی‌شود**، نه فقط چه چیزی دیده می‌شود.
    ///
    /// استفاده: یک GameObject خالی بساز، این کامپوننت را اضافه کن، Play.
    /// در صحنه‌ی بازی نگذارش؛ فقط در صحنه‌ی خالی.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DocumentSelfTest : MonoBehaviour
    {
        [Header("اجرا")]
        [Tooltip("با شروع بازی خودکار اجرا شود")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("مسیر سند در Resources، بدون پسوند")]
        [SerializeField] private string documentPath = "Documents/archive_log";

        [Tooltip("مسیر یادداشت دست‌نویس، بدون پسوند")]
        [SerializeField] private string notePath = "Documents/note_yazdani";

        private readonly List<string> _report = new List<string>(16);

        private int _passed;
        private int _failed;

        private void Start()
        {
            if (runOnStart) Run();
        }

        [ContextMenu("اجرای تست سند")]
        public void Run()
        {
            _report.Clear();
            _passed = 0;
            _failed = 0;

            TextAsset asset = Resources.Load<TextAsset>(documentPath);

            Check("سند بار شد", asset != null, documentPath);

            if (asset == null)
            {
                Report();
                return;
            }

            DocumentText document = DocumentText.Parse(asset.text, asset.name);

            for (int i = 0; i < document.Issues.Count; i++)
                _report.Add("      تذکر: " + document.Issues[i]);

            Check("قالب دفتر است", document.Template == DocumentTemplate.Ledger, document.Template.ToString());
            Check("ستون‌ها پارس شدند", CountCells(document.Columns) == 5, "ستون: " + CountCells(document.Columns));
            Check("ردیف‌ها پارس شدند", document.Rows.Count == 9, "ردیف: " + document.Rows.Count);

            bool fiveCells = true;

            for (int i = 0; i < document.Rows.Count; i++)
                if (document.Rows[i].Cells.Count != 5) fiveCells = false;

            Check("هر ردیف پنج خانه دارد", fiveCells);

            // ۱) دنیای تازه: هیچ اسمی از بازیکن نباید در دفتر باشد.
            WorldState fresh = new WorldState();
            List<DocumentRow> rows = Visible(document, fresh);

            Check("بدون تحویل، ردیف بازیکن نیست",
                rows.Count == 4 && !HasNader(rows), "ردیف: " + rows.Count);

            // ۲) پرونده دست بازیکن است، ولی به بایگانی نرسیده.
            WorldState carrying = new WorldState();
            carrying.Set("flag.quest.file", 2d);
            carrying.Set("flag.file.opened", true);

            rows = Visible(document, carrying);

            Check("دست رضا: هنوز ردیف بازیکن نیست",
                rows.Count == 4 && !HasNader(rows), "ردیف: " + rows.Count);

            // ۳) تحویل با مهر سالم.
            WorldState sealedFile = new WorldState();
            sealedFile.Set("flag.quest.file", 3d);

            rows = Visible(document, sealedFile);

            Check("تحویل سالم: ردیف بازیکن آمد",
                rows.Count == 5 && HasNader(rows), "ردیف: " + rows.Count);

            Check("تحویل سالم: وضعیت «مهر سالم» است",
                NaderStatus(rows).Contains("مهر سالم"), NaderStatus(rows));

            // ۴) مهر شکسته و اقرار.
            WorldState confessed = new WorldState();
            confessed.Set("flag.quest.file", 3d);
            confessed.Set("flag.file.opened", true);
            confessed.Set("flag.claim.seal", "opened");

            rows = Visible(document, confessed);

            Check("مهر شکسته با اقرار",
                rows.Count == 5 && NaderStatus(rows).Contains("با اقرار"), NaderStatus(rows));

            // ۵) مهر شکسته و انکار.
            WorldState denied = new WorldState();
            denied.Set("flag.quest.file", 3d);
            denied.Set("flag.file.opened", true);
            denied.Set("flag.claim.seal", "none");

            rows = Visible(document, denied);

            Check("مهر شکسته با انکار",
                rows.Count == 5 && NaderStatus(rows).Contains("منکر"), NaderStatus(rows));

            // ۶) شرط بدخط: ردیف پنهان می‌شود و گزارش هم می‌دهد.
            DocumentText broken = DocumentText.Parse(
                "[template] ledger\n[columns] الف | A؛ ب | B\n[when] item.nope >= 1\n[row] ۱ | 1؛ ۲ | 2\n");

            List<DocumentRow> brokenRows = new List<DocumentRow>(2);
            broken.Resolve(new WorldState(), null, brokenRows);

            Check("شرط بدخط، ردیف را پنهان می‌کند",
                brokenRows.Count == 0 && broken.Issues.Count > 0);

            // یادداشت هم کاغذ است، نه دیالوگ: همین فایل باید قالب note بدهد.
            TextAsset noteAsset = Resources.Load<TextAsset>(NotePath);

            Check("یادداشت بار شد", noteAsset != null, NotePath);

            if (noteAsset != null)
            {
                DocumentText note = DocumentText.Parse(noteAsset.text, noteAsset.name);

                Check("یادداشت قالب note دارد", note.Template == DocumentTemplate.Note,
                    note.Template.ToString());

                int noteLines = 0;

                for (int i = 0; i < note.Body.Count; i++)
                    if (!note.Body[i].IsBlank) noteLines++;

                Check("یادداشت سه خط دارد", noteLines == 3, "خط: " + noteLines);
                Check("یادداشت دست‌نویس است", note.Handwritten);
            }

            Report();
        }

        /* ============================================================
           ابزار
           ============================================================ */

        private static List<DocumentRow> Visible(DocumentText document, WorldState world)
        {
            List<DocumentRow> rows = new List<DocumentRow>(document.Rows.Count);
            document.Resolve(world, null, rows);

            return rows;
        }

        private static int CountCells(string source)
        {
            DocumentText probe = DocumentText.Parse("[row] " + source);

            return probe.Rows.Count > 0 ? probe.Rows[0].Cells.Count : 0;
        }

        private static bool HasNader(List<DocumentRow> rows)
        {
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Cell(3, true).Contains("نادر")) return true;

            return false;
        }

        private static string NaderStatus(List<DocumentRow> rows)
        {
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Cell(3, true).Contains("نادر")) return rows[i].Cell(4, true);

            return string.Empty;
        }

        private void Check(string name, bool ok, string detail = "")
        {
            if (ok) _passed++;
            else _failed++;

            _report.Add((ok ? "PASS  " : "FAIL  ") + name +
                        (string.IsNullOrEmpty(detail) ? string.Empty : "  → " + detail));
        }

        private void Report()
        {
            string header = "[Documents] تست: " + _passed + " قبول، " + _failed + " رد";
            Debug.Log(header + "\n" + string.Join("\n", _report));
        }
    }
}
