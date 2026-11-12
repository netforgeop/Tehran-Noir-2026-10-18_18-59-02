using UnityEngine;
using TehranNoir.Dialogue;
using TehranNoir.Documents;

namespace TehranNoir.Items
{
    /// <summary>
    /// اجرای فعل‌ها روی یک آیتم.
    ///
    /// اینجا هیچ `switch`ی روی آیتم یا دسته نیست. جریان همیشه یکی است:
    ///   ۱) قاعده‌ی همان فعل را پیدا کن (شرطش با دنیا + state نمونه سنجیده می‌شود)
    ///   ۲) اثرهایش را بنویس (`state.*` روی نمونه، بقیه روی دنیا)
    ///   ۳) نشانه‌ی داستانی `flag.item.<verb>.<id> = true` را بگذار
    ///   ۴) اگر خواسته شده، یک عدد مصرف کن
    ///   ۵) رویداد بده
    ///
    /// پس «چه شرطی لازم است و چه چیزی را عوض می‌کند» فقط در `items.txt` نوشته
    /// می‌شود؛ هیچ کلاس C# به‌ازای آیتم یا دسته ساخته نمی‌شود.
    /// </summary>
    public static class ItemRuntime
    {
        /// <summary>
        /// بازکردن برگه هنگام «خواندن». تست خودکار این را خاموش می‌کند تا
        /// وسط تست، پنجره‌ی سند باز نشود.
        /// </summary>
        public static bool ShowDocumentOnRead = true;
        /// <summary>انجام فعل. `targetId` برای قفل/در/ماشین است و خالی مجاز است.</summary>
        public static ItemResult Perform(ItemInstance instance, ItemVerb verb, string targetId = "")
        {
            if (instance == null || verb == ItemVerb.None)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.invalid", verb);

            WorldState world = ItemWorld.Current;

            if (world == null)
                return ItemResult.Fail(ItemOutcome.Invalid, "item.reason.world", verb);

            ItemDefinition definition = instance.Definition;

            if (definition == null)
                return ItemResult.Fail(ItemOutcome.NotFound, "item.reason.definition", verb);

            string target = targetId ?? string.Empty;
            WorldState view = ItemState.View(world, instance);
            ItemRule rule = definition.FindRule(verb, view, target);

            if (rule == null)
            {
                // قاعده نوشته شده ولی شرطش الان برقرار نیست → «هنوز وقتش نیست»
                if (definition.HasRule(verb))
                    return ItemResult.Fail(ItemOutcome.ConditionFailed, "item.reason.condition", verb);

                // هیچ قاعده‌ای نیست → فقط رفتار پیش‌فرض پرچم‌ها/سند
                if (!definition.HasDefault(verb))
                    return ItemResult.Fail(ItemOutcome.NotAllowed, "item.reason.not_allowed", verb);
            }
            else if (rule.RequiresTarget && target.Length == 0)
            {
                return ItemResult.Fail(ItemOutcome.TargetMismatch, "item.reason.target", verb);
            }

            if (rule != null) ApplyEffects(instance, rule.Effects, world);

            // --- انداختن: جابه‌جایی ظرف کار مدیر است، ولی قاعده‌ی drop.do اول اجرا شد ---
            if (verb == ItemVerb.Drop)
            {
                ItemResult dropped = InventoryManager.Drop(instance);

                if (!dropped.Ok) return dropped;

                MarkStoryFlag(world, definition.Id, verb);
                ItemEvents.RaisePerformed(new ItemEvent(instance, verb, target, InventoryManager.WorldId));

                return dropped;
            }

            MarkStoryFlag(world, definition.Id, verb);

            ItemEvents.RaisePerformed(new ItemEvent(instance, verb, target, instance.ContainerId));

            if (verb == ItemVerb.Read && ShowDocumentOnRead && definition.IsDocument)
                DocumentLayer.ShowResource(definition.DocumentPath);

            bool consume = rule != null ? rule.Consume : verb == ItemVerb.Consume;

            if (consume) InventoryManager.Remove(instance, 1);
            else InventoryManager.SyncProjection();

            return ItemResult.Success(verb);
        }

        /// <summary>فقط بررسی: آیا این فعل الان جواب می‌دهد؟ برای ساختن دکمه‌ها.</summary>
        public static bool CanPerform(ItemInstance instance, ItemVerb verb, string targetId = "")
        {
            if (instance == null || verb == ItemVerb.None) return false;

            ItemDefinition definition = instance.Definition;

            if (definition == null) return false;

            if (verb == ItemVerb.Drop)
                return definition.Has(ItemFlags.Droppable) || definition.HasRule(ItemVerb.Drop);

            WorldState world = ItemWorld.Current;

            WorldState view = ItemState.View(world, instance);

            if (definition.HasRule(verb)) return definition.FindRule(verb, view, targetId ?? string.Empty) != null;

            return definition.HasDefault(verb);
        }

        /// <summary>
        /// یک فهرست اثر را اجرا می‌کند. `state.*` روی نمونه و بقیه با همان
        /// گرامر `[do]` دیالوگ روی دنیا.
        /// </summary>
        public static int ApplyEffects(ItemInstance instance, string[] effects, WorldState world)
        {
            if (effects == null || effects.Length == 0) return 0;

            int applied = 0;

            for (int i = 0; i < effects.Length; i++)
            {
                string effect = effects[i];

                if (string.IsNullOrEmpty(effect)) continue;

                if (ItemState.ApplyEffect(instance, effect))
                {
                    applied++;
                    continue;
                }

                if (world == null) continue;

                if (effect.StartsWith("state.", System.StringComparison.Ordinal))
                {
                    Debug.LogWarning("[Items] وضعیت «" + effect + "» بدون نمونه اجرا شد؛ رد شد.");
                    continue;
                }

                DialogueRunner.ApplyEffect(effect, world);
                applied++;
            }

            return applied;
        }

        /// <summary>
        /// نشانه‌ی داستانی. کارها و دیالوگ‌ها با همین یک خط شرط می‌نویسند:
        ///   [if] flag.item.read.file.sealed = true
        /// و لازم نیست هیچ کدی برای آن مأموریت نوشته شود.
        /// </summary>
        public static string StoryFlag(string definitionId, ItemVerb verb)
        {
            return "flag.item." + ItemVerbs.Name(verb) + "." + ItemCatalog.Normalize(definitionId);
        }

        private static void MarkStoryFlag(WorldState world, string definitionId, ItemVerb verb)
        {
            if (world == null) return;

            world.Set(StoryFlag(definitionId, verb), true);
        }

        /// <summary>متن «بررسی»: شرح آیتم و وضعیت‌های تازه‌اش.</summary>
        public static string Describe(ItemInstance instance, bool persian)
        {
            if (instance == null) return string.Empty;

            ItemDefinition definition = instance.Definition;
            string text = definition.Description(persian);

            if (text.Length > 0) return text;

            return definition.Name(persian);
        }
    }
}
