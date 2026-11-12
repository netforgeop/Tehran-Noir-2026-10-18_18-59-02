using System;
using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Items
{
    /// <summary>
    /// مالک همه‌ی آیتم‌های زمان اجرا و همه‌ی ظرف‌ها.
    ///
    /// دو کار می‌کند و بس:
    ///   ۱) تراکنش‌های امن روی ظرف‌ها (اضافه، حذف، جابه‌جایی، تقسیم، ادغام، انداختن)
    ///   ۲) نوشتن «پروجکشن» در WorldState: برای هر آیتم، مجموع تعدادی که بازیکن
    ///      همراه دارد در `item.&lt;id&gt;` می‌نشیند.
    ///
    /// چرا پروجکشن؟ چون دیالوگ‌ها و کارها از قبل با `[if] item.x >= 1` کار می‌کنند
    /// و پنل تست همان متغیرها را نشان می‌دهد. پس این عدد «نمایش» است، نه منبع
    /// دوم؛ تنها این کلاس می‌نویسدش.
    ///
    /// هر تغییر اول بررسی می‌شود، بعد اعمال؛ اگر وسط کار چیزی رد شد، همان
    /// چیزی که کم شده برمی‌گردد. هیچ عملیاتی آیتم را دو بار نمی‌سازد و دو بار
    /// کم نمی‌کند.
    /// </summary>
    public static class InventoryManager
    {
        public const string PlayerId = "player";
        public const string PocketsId = "pockets";
        public const string EquippedId = "equipped";
        public const string WorldId = "world";
        public const string MissionId = "mission";

        /// <summary>ظرف‌هایی که «همراه بازیکن» حساب می‌شوند و در `item.*` جمع می‌زنند.</summary>
        private static readonly string[] CarriedIds = { PlayerId, PocketsId, EquippedId };

        private static readonly Dictionary<string, ItemContainer> Containers =
            new Dictionary<string, ItemContainer>(6, StringComparer.Ordinal);

        private static bool _ready;

        /* ============================================================
           ظرف‌ها
           ============================================================ */

        public static IEnumerable<ItemContainer> All
        {
            get
            {
                EnsureDefaults();
                return Containers.Values;
            }
        }

        public static ItemContainer Player
        {
            get { return Get(PlayerId); }
        }

        public static ItemContainer WorldBox
        {
            get { return Get(WorldId); }
        }

        public static void EnsureDefaults()
        {
            if (_ready) return;

            _ready = true;

            if (Containers.Count > 0) return;

            Register(new ItemContainer(PlayerId, "item.container.player"));
            Register(new ItemContainer(PocketsId, "item.container.pockets", 4));
            Register(new ItemContainer(EquippedId, "item.container.equipped", 4));
            Register(new ItemContainer(WorldId, "item.container.world"));
            Register(new ItemContainer(MissionId, "item.container.mission"));
        }

        /// <summary>ساخت یا برگرداندن ظرف با شناسه‌ی دلخواه: صندوق ماشین، کشوی میز.</summary>
        public static ItemContainer Ensure(string id, string labelKey = "", int slotLimit = 0, float weightLimit = 0f)
        {
            EnsureDefaults();

            if (string.IsNullOrEmpty(id)) return null;

            ItemContainer container;

            if (!Containers.TryGetValue(id, out container))
            {
                container = new ItemContainer(id, labelKey, slotLimit, weightLimit);
                Containers[id] = container;
                return container;
            }

            if (!string.IsNullOrEmpty(labelKey)) container.LabelKey = labelKey;
            if (slotLimit > 0) container.SlotLimit = slotLimit;
            if (weightLimit > 0f) container.WeightLimit = weightLimit;

            return container;
        }

        public static ItemContainer Get(string id)
        {
            EnsureDefaults();

            if (string.IsNullOrEmpty(id)) return null;

            ItemContainer container;

            return Containers.TryGetValue(id, out container) ? container : null;
        }

        public static void Register(ItemContainer container)
        {
            if (container == null || string.IsNullOrEmpty(container.Id)) return;

            Containers[container.Id] = container;

            if (container.Id == PlayerId) _ready = true;
        }

        /// <summary>همه‌چیز را خالی می‌کند؛ برای شروع بازی تازه و تست.</summary>
        public static void ResetAll()
        {
            Containers.Clear();
            _ready = false;
            EnsureDefaults();
            SyncProjection();
        }

        /* ============================================================
           خواندن
           ============================================================ */

        public static int CountOf(string definitionId, string containerId = PlayerId)
        {
            ItemContainer container = Get(containerId);

            return container != null ? container.CountOf(definitionId) : 0;
        }

        public static bool Has(string definitionId, int quantity = 1, string containerId = PlayerId)
        {
            if (quantity <= 0) return true;

            return CountOf(definitionId, containerId) >= quantity;
        }

        /// <summary>آیا این تعداد در این ظرف جا می‌شود؟ (محافظه‌کارانه: بدون فرض ادغام)</summary>
        public static bool CanAccept(string definitionId, int quantity, string containerId = PlayerId)
        {
            if (quantity <= 0) return false;

            ItemContainer container = Get(containerId);

            if (container == null) return false;

            ItemDefinition definition = ItemCatalog.Get(definitionId);
            int stacks = (quantity + definition.MaxStack - 1) / definition.MaxStack;

            if (container.SlotLimit > 0 && container.StackCount + stacks > container.SlotLimit) return false;

            if (container.WeightLimit > 0f &&
                container.TotalWeight + definition.Weight * quantity > container.WeightLimit + 0.0001f)
                return false;

            return true;
        }

        public static ItemContainer FindContainerOf(ItemInstance instance)
        {
            if (instance == null) return null;

            EnsureDefaults();

            foreach (KeyValuePair<string, ItemContainer> pair in Containers)
                if (pair.Value.Contains(instance)) return pair.Value;

            return null;
        }

        /* ============================================================
           گرفتن و حذف
           ============================================================ */

        /// <summary>ساختن آیتم و گذاشتن در ظرف. تعداد واقعی ساخته‌شده را نمی‌شکند.</summary>
        public static ItemResult Take(string definitionId, int quantity = 1, string containerId = PlayerId)
        {
            string id = ItemCatalog.Normalize(definitionId);

            if (id.Length == 0 || quantity <= 0)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

            ItemContainer container = Get(containerId);

            if (container == null)
                return ItemResult.Fail(ItemOutcome.NotFound, "item.reason.container");

            if (!CanAccept(id, quantity, containerId))
                return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");

            ItemDefinition definition = ItemCatalog.Get(id);
            int remaining = quantity;
            ItemInstance placed = null;
            int created = 0;

            while (remaining > 0)
            {
                int chunk = Mathf.Min(remaining, definition.MaxStack);
                ItemInstance instance = ItemInstance.Create(id, chunk);

                if (instance == null) break;

                ItemInstance target;

                if (!Place(instance, container, out target)) break;

                placed = target;
                created += chunk;
                remaining -= chunk;
            }

            if (created <= 0)
                return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");

            ItemRuntime.ApplyEffects(placed, definition.TakeEffects, ItemWorld.Current);
            SyncProjection();

            if (placed != null)
                ItemEvents.RaiseTaken(new ItemEvent(placed, ItemVerb.None, string.Empty, container.Id));
            else
                ItemEvents.RaiseContainerChanged(container.Id);

            return ItemResult.Success();
        }

        /// <summary>کم‌کردن از ظرف. اگر تعداد کمتر از موجود باشد، از همان پشته کم می‌شود.</summary>
        public static ItemResult Remove(ItemInstance instance, int quantity = 1)
        {
            if (instance == null || quantity <= 0)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

            ItemContainer container = FindContainerOf(instance);

            if (container == null)
                return ItemResult.Fail(ItemOutcome.NotFound, "item.reason.container");

            int amount = Mathf.Min(quantity, instance.Quantity);

            if (amount >= instance.Quantity)
            {
                container.Remove(instance);
            }
            else
            {
                instance.SetQuantity(instance.Quantity - amount);
            }

            SyncProjection();
            ItemEvents.RaiseRemoved(new ItemEvent(instance, ItemVerb.None, string.Empty, container.Id));

            return ItemResult.Success();
        }

        /* ============================================================
           جابه‌جایی، تقسیم، ادغام، انداختن
           ============================================================ */

        /// <summary>
        /// انتقال بین ظرف‌ها. `quantity` صفر یا منفی = همه.
        /// اول مقصد بررسی می‌شود؛ اگر نپذیرفت، چیزی جابه‌جا نمی‌شود.
        /// </summary>
        public static ItemResult Move(ItemInstance instance, string toContainerId, int quantity = 0)
        {
            if (instance == null)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

            ItemContainer from = FindContainerOf(instance);

            if (from == null)
                return ItemResult.Fail(ItemOutcome.NotFound, "item.reason.container");

            ItemContainer to = Get(toContainerId);

            if (to == null || to == from)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.container");

            int amount = quantity <= 0 ? instance.Quantity : Mathf.Clamp(quantity, 1, instance.Quantity);
            bool whole = amount >= instance.Quantity;

            if (whole)
            {
                if (!to.CanAccept(instance))
                    return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");

                if (!from.Remove(instance))
                    return ItemResult.Fail(ItemOutcome.NotFound, "item.reason.invalid");

                ItemInstance placed;

                if (!Place(instance, to, out placed))
                {
                    from.Add(instance);
                    return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");
                }
            }
            else
            {
                ItemInstance part = instance.Split(amount);

                if (part == null)
                    return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

                if (!to.CanAccept(part))
                {
                    instance.AddQuantity(part.Quantity);
                    return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");
                }

                ItemInstance placed;

                if (!Place(part, to, out placed))
                {
                    instance.AddQuantity(part.Quantity);
                    return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");
                }
            }

            SyncProjection();
            ItemEvents.RaiseRemoved(new ItemEvent(instance, ItemVerb.None, string.Empty, from.Id));
            ItemEvents.RaiseTaken(new ItemEvent(instance, ItemVerb.None, string.Empty, to.Id));

            return ItemResult.Success();
        }

        /// <summary>جدا کردن بخشی از یک پشته، در همان ظرف.</summary>
        public static ItemResult Split(ItemInstance instance, int quantity)
        {
            if (instance == null || quantity <= 0)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

            ItemContainer container = FindContainerOf(instance);

            if (container == null)
                return ItemResult.Fail(ItemOutcome.NotFound, "item.reason.container");

            ItemInstance part = instance.Split(quantity);

            if (part == null)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

            if (!container.CanAccept(part))
            {
                instance.AddQuantity(part.Quantity);
                return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");
            }

            // عمداً Place نه: Place با اولین پشته‌ی سازگار ادغام می‌کند و همان
            // پشته‌ای که از آن جدا شدیم اولین کاندید است؛ نتیجه «تقسیم» می‌شد
            // که هیچ چیزی را تقسیم نکرده باشد.
            if (!container.Add(part))
            {
                instance.AddQuantity(part.Quantity);
                return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");
            }

            SyncProjection();
            ItemEvents.RaiseTaken(new ItemEvent(part, ItemVerb.None, string.Empty, container.Id));

            return ItemResult.Success();
        }

        /// <summary>ریختن source داخل target تا جایی که جا دارد.</summary>
        public static ItemResult Merge(ItemInstance source, ItemInstance target)
        {
            if (source == null || target == null || source == target)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

            if (!source.CanStackWith(target))
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.no_merge");

            int room = target.FreeSpace;

            if (room <= 0)
                return ItemResult.Fail(ItemOutcome.ContainerFull, "item.reason.full");

            int amount = Mathf.Min(room, source.Quantity);

            target.AddQuantity(amount);

            if (amount >= source.Quantity)
            {
                ItemContainer container = FindContainerOf(source);

                if (container != null) container.Remove(source);
            }
            else
            {
                source.SetQuantity(source.Quantity - amount);
            }

            SyncProjection();
            ItemEvents.RaiseContainerChanged(string.Empty);

            return ItemResult.Success();
        }

        /// <summary>انداختن روی زمین. فقط اگر تعریف اجازه بدهد.</summary>
        public static ItemResult Drop(ItemInstance instance)
        {
            if (instance == null)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid");

            ItemDefinition definition = instance.Definition;

            // قاعده‌ی نوشته‌شده بر پرچم مقدم است: اگر طراحی drop.do نوشته،
            // خودش می‌داند چه می‌کند.
            if (!definition.Has(ItemFlags.Droppable) && !definition.HasRule(ItemVerb.Drop))
                return ItemResult.Fail(ItemOutcome.NotAllowed, "item.reason.not_droppable", ItemVerb.Drop);

            ItemResult moved = Move(instance, WorldId);

            if (!moved.Ok) return moved;

            ItemRuntime.ApplyEffects(instance, definition.DropEffects, ItemWorld.Current);
            ItemEvents.RaiseDropped(new ItemEvent(instance, ItemVerb.Drop, string.Empty, WorldId));

            return ItemResult.Success(ItemVerb.Drop);
        }

        /* ============================================================
           پروجکشن و بارگذاری
           ============================================================ */

        /// <summary>
        /// هم‌ترازکردن نمونه‌ها با پروجکشن.
        ///
        /// چرا لازم است؟ چون `item.&lt;id&gt;` از جاهای دیگر هم عوض می‌شود:
        /// پنل تست (F10)، دیالوگ `[do] item.x + 1`، و فهرست مقدارهای اولیه‌ی
        /// صحنه. اگر نمونه‌ای وجود نداشته باشد، کارت در منو دیده می‌شود ولی
        /// Enter روی آن کاری نمی‌کند. این متد موقع نیاز نمونه می‌سازد یا کم می‌کند.
        /// </summary>
        public static void Reconcile()
        {
            WorldState world = ItemWorld.Current;

            if (world == null) return;

            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                string id = all[i].Id;

                double raw = world.Get(Inventory.Key(id)).AsNumber();

                // متغیر تعریف‌نشده NaN می‌دهد و تبدیل NaN به int در C# تعریف‌نشده است.
                int projection = double.IsNaN(raw) || raw <= 0d ? 0 : Mathf.RoundToInt((float)raw);
                int actual = CarriedTotal(id);

                if (projection > actual) Take(id, projection - actual);
                else if (projection < actual) Trim(id, actual - projection);
            }
        }

        private static int CarriedTotal(string definitionId)
        {
            int total = 0;

            for (int c = 0; c < CarriedIds.Length; c++)
            {
                ItemContainer container = Get(CarriedIds[c]);

                if (container != null) total += container.CountOf(definitionId);
            }

            return total;
        }

        private static void Trim(string definitionId, int quantity)
        {
            int left = quantity;

            for (int c = 0; c < CarriedIds.Length && left > 0; c++)
            {
                ItemContainer container = Get(CarriedIds[c]);

                if (container == null) continue;

                for (int i = container.Items.Count - 1; i >= 0 && left > 0; i--)
                {
                    ItemInstance instance = container.Items[i];

                    if (instance.DefinitionId != definitionId) continue;

                    int cut = Mathf.Min(left, instance.Quantity);

                    if (cut >= instance.Quantity) container.Remove(instance);
                    else instance.SetQuantity(instance.Quantity - cut);

                    left -= cut;
                }
            }
        }

        /// <summary>نوشتن تعداد همراه‌ها در `item.&lt;id&gt;`؛ تنها نویسنده همین‌جاست.</summary>
        public static void SyncProjection()
        {
            WorldState world = ItemWorld.Current;

            if (world == null) return;

            IReadOnlyList<ItemDefinition> all = ItemCatalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                string id = all[i].Id;
                int total = 0;

                for (int c = 0; c < CarriedIds.Length; c++)
                {
                    ItemContainer container = Get(CarriedIds[c]);

                    if (container != null) total += container.CountOf(id);
                }

                world.Set(Inventory.Key(id), (double)total);
            }
        }

        /// <summary>افزودن آیتمی که شناسه‌اش از ذخیره آمده؛ بدون ادغام و بدون ساخت شناسه‌ی تازه.</summary>
        public static bool AddRestored(ItemInstance instance, string containerId)
        {
            if (instance == null) return false;

            ItemContainer container = Ensure(containerId);

            if (container == null) return false;

            if (container.Find(instance.InstanceId) != null) return false;

            return container.Add(instance);
        }

        /* ============================================================
           داخلی
           ============================================================ */

        private static bool Place(ItemInstance instance, ItemContainer container, out ItemInstance placed)
        {
            placed = instance;

            if (instance == null || container == null) return false;

            ItemInstance stack = container.FindStackFor(instance);

            if (stack != null && stack.FreeSpace >= instance.Quantity)
            {
                stack.AddQuantity(instance.Quantity);
                placed = stack;
                return true;
            }

            if (container.Add(instance)) return true;

            placed = null;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Containers.Clear();
            _ready = false;
        }
    }
}
