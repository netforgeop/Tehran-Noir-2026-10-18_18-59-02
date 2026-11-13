using System;
using UnityEngine;

namespace TehranNoir.Items
{
    /// <summary>
    /// یک اتفاق آیتم، آماده‌ی پخش برای کارها، رابط کاربری و دیالوگ.
    /// </summary>
    public sealed class ItemEvent
    {
        public readonly ItemInstance Instance;

        public readonly ItemVerb Verb;

        public readonly string TargetId;

        public readonly string ContainerId;

        public ItemEvent(ItemInstance instance, ItemVerb verb, string targetId, string containerId)
        {
            Instance = instance;
            Verb = verb;
            TargetId = targetId ?? string.Empty;
            ContainerId = containerId ?? string.Empty;
        }

        public string DefinitionId
        {
            get { return Instance != null ? Instance.DefinitionId : string.Empty; }
        }
    }

    /// <summary>
    /// خبررسانی سیستم آیتم. کارها و رابط کاربری گوش می‌دهند؛ هیچ‌کس هر فریم
    /// چیزی را نمی‌پرسد.
    ///
    /// قاعده‌ی رویدادهای static در ادیتور: بعد از هر بار Play دوباره مقدار می‌گیرند.
    /// اگر پاک نشوند، بار دوم دو بار صدا زده می‌شوند. `ClearAll` همین کار را می‌کند
    /// و خودکار هم در شروع بازی صدا زده می‌شود.
    /// </summary>
    public static class ItemEvents
    {
        /// <summary>آیتم به یک ظرف اضافه شد (از جمله برداشتن از زمین).</summary>
        public static event Action<ItemEvent> Taken;

        /// <summary>آیتم از یک ظرف حذف شد (مصرف، دور انداختن، انتقال).</summary>
        public static event Action<ItemEvent> Removed;

        /// <summary>هر فعل موفقی: use / read / open / unlock / …</summary>
        public static event Action<ItemEvent> Performed;

        /// <summary>آیتم عمداً دور انداخته شد.</summary>
        public static event Action<ItemEvent> Dropped;

        /// <summary>محتویات یک ظرف عوض شد؛ برای تازه‌کردن رابط.</summary>
        public static event Action<string> ContainerChanged;

        /// <summary>بازی از ذخیره بارگذاری شد.</summary>
        public static event Action Restored;

        public static void RaiseTaken(ItemEvent itemEvent)
        {
            if (itemEvent == null) return;

            Action<ItemEvent> handler = Taken;
            if (handler != null) handler(itemEvent);

            RaiseContainerChanged(itemEvent.ContainerId);
        }

        public static void RaiseRemoved(ItemEvent itemEvent)
        {
            if (itemEvent == null) return;

            Action<ItemEvent> handler = Removed;
            if (handler != null) handler(itemEvent);

            RaiseContainerChanged(itemEvent.ContainerId);
        }

        public static void RaisePerformed(ItemEvent itemEvent)
        {
            if (itemEvent == null) return;

            Action<ItemEvent> handler = Performed;
            if (handler != null) handler(itemEvent);
        }

        public static void RaiseDropped(ItemEvent itemEvent)
        {
            if (itemEvent == null) return;

            Action<ItemEvent> handler = Dropped;
            if (handler != null) handler(itemEvent);
        }

        public static void RaiseContainerChanged(string containerId)
        {
            Action<string> handler = ContainerChanged;
            if (handler != null) handler(containerId ?? string.Empty);
        }

        public static void RaiseRestored()
        {
            Action handler = Restored;
            if (handler != null) handler();
        }

        /// <summary>همه‌ی مشترک‌ها را خالی می‌کند؛ تست و شروع صحنه.</summary>
        public static void ClearAll()
        {
            Taken = null;
            Removed = null;
            Performed = null;
            Dropped = null;
            ContainerChanged = null;
            Restored = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            ClearAll();
        }
    }
}
