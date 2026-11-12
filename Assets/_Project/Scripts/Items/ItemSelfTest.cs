using System;
using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// تست خودکار سیستم آیتم، بدون وابستگی به پکیج Test Framework.
    ///
    /// چرا این‌طور؟ چون مخزن پروژه `Packages/` را ندارد و نمی‌دانیم
    /// `com.unity.test-framework` نصب است یا نه. این کامپوننت خودش اجرا
    /// می‌شود، جدول PASS/FAIL در Console می‌گذارد و هیچ چیزی در صحنه لازم ندارد.
    ///
    /// استفاده: یک GameObject خالی بساز، همین کامپوننت را اضافه کن، Play.
    /// یا از منوی سه‌نقطه‌ی کامپوننت «اجرای تست آیتم» را بزن.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemSelfTest : MonoBehaviour
    {
        [Header("اجرا")]
        [Tooltip("با شروع بازی خودکار اجرا شود")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("جدول نتیجه در Console چاپ شود")]
        [SerializeField] private bool logReport = true;

        private readonly List<string> _report = new List<string>(24);

        private int _passed;
        private int _failed;

        private void Start()
        {
            if (runOnStart) Run();
        }

        [ContextMenu("اجرای تست آیتم")]
        public void Run()
        {
            _report.Clear();
            _passed = 0;
            _failed = 0;

            bool previousDocumentView = ItemRuntime.ShowDocumentOnRead;
            bool previousSpawning = ItemDropSpawner.Spawning;

            ItemRuntime.ShowDocumentOnRead = false;
            ItemDropSpawner.Spawning = false;

            try
            {
                TestDefinitions();
                TestCreate();
                TestAddRemove();
                TestStacking();
                TestCapacity();
                TestDrop();
                TestTransfer();
                TestDocument();
                TestKey();
                TestWeapon();
                TestEvents();
                TestSave();
                TestMissingDefinition();
                TestDuplicates();
                TestTitles();
                TestStoryFlags();
                TestVerbList();
                TestSubscriptionsSurvive();
                TestWorldRoundTrip();
            }
            catch (Exception exception)
            {
                _failed++;
                _report.Add("FAIL  استثنا: " + exception.GetType().Name + " — " + exception.Message);
            }
            finally
            {
                ItemRuntime.ShowDocumentOnRead = previousDocumentView;
                ItemDropSpawner.Spawning = previousSpawning;
                ItemWorld.ClearOverride();
                InventoryManager.ResetAll();

                // عمداً ItemEvents.ClearAll صدا زده نمی‌شود: تست فقط باید مشترکِ
                // خودش را بردارد. قبلاً این خط همه‌ی مشترک‌ها را پاک می‌کرد
                // (از جمله ItemDropSpawner) و انداختن تا آخر بازی بی‌اثر می‌شد.
            }

            string header = "[Items] تست: " + _passed + " قبول، " + _failed + " رد";

            if (logReport) Debug.Log(header + "\n" + string.Join("\n", _report));
        }

        /* ============================================================
           گروه‌های تست
           ============================================================ */

        private void TestDefinitions()
        {
            Fresh();

            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;
            bool sane = all.Count > 0;

            for (int i = 0; i < all.Count; i++)
            {
                if (string.IsNullOrEmpty(all[i].Id)) sane = false;
                if (all[i].Rules == null) sane = false;
            }

            Check("تعریف‌ها بار شدند", sane, "تعداد: " + all.Count);

            ItemDefinition document = ItemCatalog.Get("file.sealed");

            Check("سند بودن از doc می‌آید", document.IsDocument);
            // file.sealed دو قاعده دارد: open و read (هر کدام if و do)
            Check("قاعده‌ها پارس شدند", document.Rules.Count == 2, "قاعده‌ها: " + document.Rules.Count);

            // یک شرط با «=» تنها باید خوانده شود، وگرنه قاعده بی‌صدا حذف می‌شود.
            Condition probe = ConditionParser.Parse("state.sealed = 1");
            WorldState probeWorld = new WorldState();

            probeWorld.Set("state.sealed", 1d);

            Check("شرط با = تنها", probe.IsTrue(probeWorld));
        }

        private void TestCreate()
        {
            Fresh();

            ItemInstance first = ItemInstance.Create("file.sealed");
            ItemInstance second = ItemInstance.Create("file.sealed");

            Check("ساخت نمونه", first != null && first.InstanceId.Length > 0);
            Check("شناسه‌ی یکتا", first != null && second != null && first.InstanceId != second.InstanceId);
            Check("وضعیت اولیه اعمال شد", first != null && first.GetState("sealed") == "1");
        }

        private void TestAddRemove()
        {
            WorldState world = Fresh();

            ItemResult take = InventoryManager.Take("cigarettes", 5);

            Check("اضافه‌کردن", take.Ok && InventoryManager.CountOf("cigarettes") == 5);
            Check("پروجکشن در WorldState",
                Math.Abs(world.Get(Inventory.Key("cigarettes")).AsNumber() - 5d) < 0.001d);

            ItemInstance instance = FirstOf("cigarettes");
            ItemResult remove = InventoryManager.Remove(instance, 2);

            Check("کم‌کردن", remove.Ok && InventoryManager.CountOf("cigarettes") == 3);
        }

        private void TestStacking()
        {
            Fresh();

            InventoryManager.Take("cigarettes", 5);

            ItemInstance stack = FirstOf("cigarettes");
            ItemResult split = InventoryManager.Split(stack, 2);

            Check("تقسیم پشته",
                split.Ok && InventoryManager.Player.StackCount == 2 && InventoryManager.CountOf("cigarettes") == 5);

            ItemInstance part = FirstOf("cigarettes", 2);
            ItemInstance main = FirstOf("cigarettes", 3);

            ItemResult merge = InventoryManager.Merge(part, main);

            Check("ادغام پشته",
                merge.Ok && InventoryManager.Player.StackCount == 1 && InventoryManager.CountOf("cigarettes") == 5);
        }

        private void TestCapacity()
        {
            Fresh();

            ItemContainer box = InventoryManager.Ensure("test.box", "", 1);

            InventoryManager.Take("file.sealed", 1, "test.box");
            ItemResult second = InventoryManager.Take("weapon.pistol_01", 1, "test.box");

            Check("سقف جا رعایت شد", !second.Ok && second.Outcome == ItemOutcome.ContainerFull);
            Check("ظرف همان یک آیتم را دارد", box != null && box.StackCount == 1);
        }

        private void TestDrop()
        {
            Fresh();

            InventoryManager.Take("cigarettes", 1);

            ItemInstance item = FirstOf("cigarettes");
            ItemResult drop = ItemRuntime.Perform(item, ItemVerb.Drop);

            Check("انداختن", drop.Ok && InventoryManager.CountOf("cigarettes") == 0);
            Check("انداختن تکرار نمی‌کند",
                InventoryManager.CountOf("cigarettes", InventoryManager.WorldId) == 1);

            ItemResult again = ItemRuntime.Perform(item, ItemVerb.Drop);

            Check("انداختن دوباره رد می‌شود",
                !again.Ok && InventoryManager.CountOf("cigarettes", InventoryManager.WorldId) == 1);
        }

        private void TestTransfer()
        {
            Fresh();

            InventoryManager.Take("ammo.9mm", 10);

            ItemInstance ammo = FirstOf("ammo.9mm");
            ItemResult move = InventoryManager.Move(ammo, InventoryManager.MissionId, 4);

            Check("انتقال بین ظرف‌ها",
                move.Ok &&
                InventoryManager.CountOf("ammo.9mm") == 6 &&
                InventoryManager.CountOf("ammo.9mm", InventoryManager.MissionId) == 4);
        }

        private void TestDocument()
        {
            WorldState world = Fresh();

            InventoryManager.Take("file.sealed", 1);

            ItemInstance document = FirstOf("file.sealed");

            ItemResult before = ItemRuntime.Perform(document, ItemVerb.Read);

            Check("مهر بسته: خواندن رد می‌شود",
                !before.Ok && before.Outcome == ItemOutcome.ConditionFailed);

            ItemResult open = ItemRuntime.Perform(document, ItemVerb.Open);

            Check("بازکردن مهر", open.Ok && document.GetState("sealed") == "0");
            Check("اثر مهر در دنیا", world.Get("flag.letter.opened").Truthy());

            ItemResult read = ItemRuntime.Perform(document, ItemVerb.Read);

            Check("خواندن بعد از بازکردن", read.Ok && document.GetState("read") == "1");
            Check("نشانه‌ی داستانی خواندن",
                world.Get(ItemRuntime.StoryFlag("file.sealed", ItemVerb.Read)).Truthy());
        }

        private void TestKey()
        {
            WorldState world = Fresh();

            InventoryManager.Take("key.archive", 1);

            ItemInstance key = FirstOf("key.archive");

            ItemResult wrong = ItemRuntime.Perform(key, ItemVerb.Unlock, "door.other");
            ItemResult right = ItemRuntime.Perform(key, ItemVerb.Unlock, "door.archive");

            Check("کلید به قفل دیگر نمی‌خورد", !wrong.Ok);
            Check("کلید به قفل خودش می‌خورد", right.Ok && world.Get("flag.door.archive").Truthy());
        }

        private void TestWeapon()
        {
            Fresh();

            InventoryManager.Take("weapon.pistol_01", 1);

            ItemInstance pistol = FirstOf("weapon.pistol_01");

            ItemResult empty = ItemRuntime.Perform(pistol, ItemVerb.Use);
            ItemResult reloadEmpty = ItemRuntime.Perform(pistol, ItemVerb.Reload);

            Check("بدون فشنگ شلیک نمی‌شود", !empty.Ok);
            Check("بدون فشنگ پر نمی‌شود", !reloadEmpty.Ok);

            InventoryManager.Take("ammo.9mm", 10);

            ItemResult reload = ItemRuntime.Perform(pistol, ItemVerb.Reload);

            // ترتیب مهم است: اگر شلیک قبل از خواندن بیاید، عدد ۸ را ۷ می‌بینیم.
            string afterReload = pistol.GetState("ammo");

            Check("پرکردن", reload.Ok && afterReload == "8", "خشاب: " + afterReload);

            ItemResult fire = ItemRuntime.Perform(pistol, ItemVerb.Use);

            Check("شلیک یکی کم می‌کند", fire.Ok && pistol.GetState("ammo") == "7",
                "خشاب: " + pistol.GetState("ammo"));
        }

        private void TestEvents()
        {
            Fresh();

            int performed = 0;
            Action<ItemEvent> handler = itemEvent => performed++;

            ItemEvents.Performed += handler;

            InventoryManager.Take("evidence.casing_01", 1);

            ItemInstance casing = FirstOf("evidence.casing_01");

            ItemRuntime.Perform(casing, ItemVerb.Examine);
            ItemEvents.Performed -= handler;
            ItemRuntime.Perform(casing, ItemVerb.Examine);

            Check("رویداد یک بار می‌رسد و لغو می‌شود", performed == 1, "دریافت: " + performed);
        }

        private void TestSave()
        {
            Fresh();

            InventoryManager.Take("file.sealed", 1);

            ItemInstance document = FirstOf("file.sealed");

            ItemRuntime.Perform(document, ItemVerb.Open);
            InventoryManager.Take("cigarettes", 3);

            string json = ItemSave.Capture();

            InventoryManager.ResetAll();

            bool cleared = InventoryManager.CountOf("cigarettes") == 0 &&
                           InventoryManager.CountOf("file.sealed") == 0;

            bool restored = ItemSave.Restore(json);

            bool counts = InventoryManager.CountOf("cigarettes") == 3 &&
                          InventoryManager.CountOf("file.sealed") == 1;

            ItemInstance loaded = FirstOf("file.sealed");

            Check("ذخیره", !string.IsNullOrEmpty(json) && cleared);
            Check("بارگذاری", restored && counts);
            Check("وضعیت سند در ذخیره", loaded != null && loaded.GetState("sealed") == "0");
        }

        private void TestMissingDefinition()
        {
            Fresh();

            ItemDefinition missing = ItemCatalog.Get("no.such.item");
            ItemInstance ghost = ItemInstance.Create("no.such.item");

            Check("تعریف ناشناس امن است", missing != null && !ItemCatalog.Exists("no.such.item"));
            Check("نمونه‌ی ناشناس ساخته می‌شود", ghost != null && !ghost.IsKnown);
        }

        private void TestDuplicates()
        {
            Fresh();

            ItemContainer container = new ItemContainer("dup.test");
            ItemInstance instance = ItemInstance.Create("cigarettes");

            bool first = container.Add(instance);
            bool second = container.Add(instance);

            Check("شناسه‌ی تکراری در یک ظرف رد می‌شود",
                first && !second && container.StackCount == 1);
        }

        /// <summary>
        /// نام آیتم باید از وضعیت بیاید، نه از تعریف ثابت.
        /// اگر پرونده بعد از باز شدن هنوز «مهرشده» باشد، بازیکن فکر می‌کند کار نکرد.
        /// </summary>
        private void TestTitles()
        {
            Fresh();

            InventoryManager.Take("file.sealed", 1);

            ItemInstance document = FirstOf("file.sealed");

            string sealedName = ItemRuntime.Title(document, true);

            Check("نام پایه‌ی سند", sealedName == "پرونده‌ی مهرشده", sealedName);

            ItemRuntime.Perform(document, ItemVerb.Open);

            string openedName = ItemRuntime.Title(document, true);

            Check("نام بعد از باز کردن مهر", openedName == "پرونده‌ی بازشده", openedName);

            ItemRuntime.Perform(document, ItemVerb.Read);

            string readName = ItemRuntime.Title(document, true);

            Check("نام بعد از خواندن", readName == "پرونده‌ی خوانده‌شده", readName);
        }

        /// <summary>
        /// قرارداد با داستان: نام پرچم‌هایی که دیالوگ‌ها و کارها می‌خوانند
        /// باید همان باشد که آیتم می‌نویسد. اگر جدا شوند، مأموریت بی‌صدا می‌شکند.
        /// </summary>
        private void TestStoryFlags()
        {
            WorldState world = Fresh();

            InventoryManager.Take("file.sealed", 1);

            ItemInstance document = FirstOf("file.sealed");

            Check("برداشتن پرونده: flag.file.taken",
                world.Get("flag.file.taken").Truthy());

            ItemRuntime.Perform(document, ItemVerb.Open);

            Check("باز کردن: flag.file.opened",
                world.Get("flag.file.opened").Truthy());

            ItemRuntime.Perform(document, ItemVerb.Read);

            Check("خواندن: flag.file.read",
                world.Get("flag.file.read").Truthy());

            // همان شرطی که در reza_office.txt و case_file.txt نوشته شده
            Condition storyCondition = ConditionParser.Parse("flag.file.taken == true");

            Check("شرط داستان با پرچم آیتم جواب می‌دهد", storyCondition.IsTrue(world));
        }

        /// <summary>فهرست فعل‌ها همان چیزی است که نوار ۱..۴ نشان می‌دهد.</summary>
        private void TestVerbList()
        {
            Fresh();

            InventoryManager.Take("file.sealed", 1);

            ItemInstance document = FirstOf("file.sealed");

            List<ItemVerb> verbs = new List<ItemVerb>(8);

            document.Definition.Verbs(ItemState.View(ItemWorld.Current, document), string.Empty, verbs);

            Check("سند مهرشده: «باز کردن» هست", verbs.Contains(ItemVerb.Open), Names(verbs));
            Check("سند مهرشده: «خواندن» نیست", !verbs.Contains(ItemVerb.Read), Names(verbs));

            ItemRuntime.Perform(document, ItemVerb.Open);

            document.Definition.Verbs(ItemState.View(ItemWorld.Current, document), string.Empty, verbs);

            Check("بعد از بازکردن: «خواندن» اضافه شد", verbs.Contains(ItemVerb.Read), Names(verbs));

            InventoryManager.Take("weapon.pistol_01", 1);

            ItemInstance pistol = FirstOf("weapon.pistol_01");

            pistol.Definition.Verbs(ItemState.View(ItemWorld.Current, pistol), string.Empty, verbs);

            Check("کلت خالی: «استفاده» نیست، «بررسی» هست",
                !verbs.Contains(ItemVerb.Use) && verbs.Contains(ItemVerb.Examine), Names(verbs));

            Check("کلت خالی: «مصرف» و «جدا کردن» بی‌دلیل نیستند",
                !verbs.Contains(ItemVerb.Consume) && !verbs.Contains(ItemVerb.Unequip), Names(verbs));
        }

        /// <summary>
        /// تست نباید مشترک‌های بیرونی را پاک کند؛ همان باگی که انداختن را
        /// در بازی بی‌اثر کرده بود.
        /// </summary>
        private void TestSubscriptionsSurvive()
        {
            Fresh();

            int seen = 0;
            Action<ItemEvent> handler = itemEvent => seen++;

            ItemEvents.Dropped += handler;

            InventoryManager.Take("cigarettes", 1);

            ItemInstance item = FirstOf("cigarettes");

            ItemRuntime.Perform(item, ItemVerb.Drop);

            ItemEvents.Dropped -= handler;

            Check("مشترکِ بیرونی بعد از تست زنده می‌ماند", seen == 1, "دریافت: " + seen);
        }

        /// <summary>
        /// انداختن و برداشتن باید همان نمونه را برگرداند، با همان حالت.
        /// این همان کاری است که WorldItemPickup روی زمین انجام می‌دهد.
        /// </summary>
        private void TestWorldRoundTrip()
        {
            Fresh();

            InventoryManager.Take("file.sealed", 1);

            ItemInstance document = FirstOf("file.sealed");

            ItemRuntime.Perform(document, ItemVerb.Open);

            string instanceId = document.InstanceId;

            ItemRuntime.Perform(document, ItemVerb.Drop);

            bool onGround = document.ContainerId == InventoryManager.WorldId;

            ItemResult back = InventoryManager.Move(document, InventoryManager.PlayerId);

            ItemInstance returned = FirstOf("file.sealed");

            Check("افتادن در ظرف زمین", onGround);
            Check("برداشتن دوباره", back.Ok && returned != null);
            Check("همان نمونه برگشت، نه نسخه‌ی تازه",
                returned != null && returned.InstanceId == instanceId);
            Check("حالت سند بعد از رفت‌وبرگشت",
                returned != null && returned.GetState("sealed") == "0");
        }

        private static string Names(List<ItemVerb> verbs)
        {
            string text = string.Empty;

            for (int i = 0; i < verbs.Count; i++)
                text += (i > 0 ? ", " : string.Empty) + ItemVerbs.Name(verbs[i]);

            return text;
        }

        /* ============================================================
           ابزار
           ============================================================ */

        private static WorldState Fresh()
        {
            WorldState world = new WorldState();

            ItemWorld.Override = world;
            InventoryManager.ResetAll();

            return world;
        }

        private static ItemInstance FirstOf(string definitionId, int quantity = 0)
        {
            ItemContainer player = InventoryManager.Player;

            if (player == null) return null;

            for (int i = 0; i < player.Items.Count; i++)
            {
                ItemInstance instance = player.Items[i];

                if (instance.DefinitionId != definitionId) continue;
                if (quantity > 0 && instance.Quantity != quantity) continue;

                return instance;
            }

            return null;
        }

        private void Check(string name, bool ok, string detail = "")
        {
            if (ok) _passed++;
            else _failed++;

            _report.Add((ok ? "PASS  " : "FAIL  ") + name +
                        (string.IsNullOrEmpty(detail) ? string.Empty : "  → " + detail));
        }
    }
}
