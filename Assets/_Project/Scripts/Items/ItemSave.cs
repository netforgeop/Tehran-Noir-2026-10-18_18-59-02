using System;
using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.Items
{
    /// <summary>وضعیت یک نمونه، دقیقاً همان چیزی که در فایل ذخیره می‌شود.</summary>
    [Serializable]
    public sealed class ItemInstanceRecord
    {
        public string InstanceId;
        public string DefinitionId;
        public int Quantity;
        public float Durability;
        public string[] StateKeys;
        public string[] StateValues;
    }

    /// <summary>یک ظرف و محتویاتش.</summary>
    [Serializable]
    public sealed class ItemContainerRecord
    {
        public string Id;
        public string LabelKey;
        public int SlotLimit;
        public float WeightLimit;
        public ItemInstanceRecord[] Items;
    }

    /// <summary>ریشه‌ی فایل ذخیره. `Version` برای مهاجرت است.</summary>
    [Serializable]
    public sealed class ItemSaveData
    {
        public int Version = ItemSave.CurrentVersion;
        public ItemContainerRecord[] Containers;
    }

    /// <summary>
    /// ذخیره و بازگرداندن آیتم‌ها.
    ///
    /// چه چیزی ذخیره می‌شود: شناسه‌ی ظرف، شناسه‌ی یکتای نمونه، شناسه‌ی تعریف،
    /// تعداد، دوام و همه‌ی کلیدهای وضعیت (مهر، خوانده‌شدن، فشنگ).
    ///
    /// چه چیزی ذخیره **نمی‌شود**: آبجکت صحنه، ScriptableObject، اشاره‌گر.
    /// فقط شناسه‌ی پایدار. پس ذخیره بعد از جابه‌جایی صحنه هم سالم می‌ماند.
    ///
    /// نسخه: هر بار ساختار عوض شد، `CurrentVersion` بالا می‌رود و در `Migrate`
    /// یک قدم برای نسخه‌ی قدیمی نوشته می‌شود. نسخه‌ی ناشناسِ آینده رد می‌شود،
    /// نه اینکه خراب بار شود.
    /// </summary>
    public static class ItemSave
    {
        public const int CurrentVersion = 1;

        private static readonly List<string> KeyBuffer = new List<string>(8);

        /// <summary>گرفتن عکس کامل از همه‌ی ظرف‌ها، به‌شکل JSON.</summary>
        public static string Capture()
        {
            ItemSaveData data = new ItemSaveData();
            data.Version = CurrentVersion;

            List<ItemContainerRecord> containers = new List<ItemContainerRecord>(6);

            foreach (ItemContainer container in InventoryManager.All)
            {
                if (container == null) continue;

                ItemContainerRecord record = new ItemContainerRecord();
                record.Id = container.Id;
                record.LabelKey = container.LabelKey;
                record.SlotLimit = container.SlotLimit;
                record.WeightLimit = container.WeightLimit;

                List<ItemInstanceRecord> items = new List<ItemInstanceRecord>(container.Items.Count);

                for (int i = 0; i < container.Items.Count; i++)
                {
                    ItemInstance instance = container.Items[i];

                    if (instance == null) continue;

                    ItemInstanceRecord itemRecord = new ItemInstanceRecord();
                    itemRecord.InstanceId = instance.InstanceId;
                    itemRecord.DefinitionId = instance.DefinitionId;
                    itemRecord.Quantity = instance.Quantity;
                    itemRecord.Durability = instance.Durability;

                    KeyBuffer.Clear();
                    instance.CopyStateKeys(KeyBuffer);

                    string[] keys = new string[KeyBuffer.Count];
                    string[] values = new string[KeyBuffer.Count];

                    for (int k = 0; k < KeyBuffer.Count; k++)
                    {
                        keys[k] = KeyBuffer[k];
                        values[k] = instance.GetState(KeyBuffer[k]);
                    }

                    itemRecord.StateKeys = keys;
                    itemRecord.StateValues = values;

                    items.Add(itemRecord);
                }

                record.Items = items.ToArray();
                containers.Add(record);
            }

            data.Containers = containers.ToArray();

            return JsonUtility.ToJson(data, true);
        }

        /// <summary>بازگرداندن. اگر فایل ناقص یا نسخه‌اش ناشناس بود، false.</summary>
        public static bool Restore(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            ItemSaveData data;

            try
            {
                data = JsonUtility.FromJson<ItemSaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Items] فایل ذخیره خوانده نشد: " + exception.Message);
                return false;
            }

            if (data == null || data.Containers == null)
            {
                Debug.LogWarning("[Items] فایل ذخیره خالی است.");
                return false;
            }

            if (data.Version > CurrentVersion)
            {
                Debug.LogWarning("[Items] ذخیره از نسخه‌ی جدیدتر (" + data.Version + ") است؛ بارگذاری نشد.");
                return false;
            }

            Migrate(data);

            InventoryManager.ResetAll();

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            int restored = 0;
            int skipped = 0;

            for (int c = 0; c < data.Containers.Length; c++)
            {
                ItemContainerRecord record = data.Containers[c];

                if (record == null || string.IsNullOrEmpty(record.Id)) continue;

                InventoryManager.Ensure(record.Id, record.LabelKey, record.SlotLimit, record.WeightLimit);

                if (record.Items == null) continue;

                for (int i = 0; i < record.Items.Length; i++)
                {
                    ItemInstanceRecord itemRecord = record.Items[i];

                    if (itemRecord == null) continue;

                    if (!seen.Add(itemRecord.InstanceId ?? string.Empty))
                    {
                        Debug.LogWarning("[Items] شناسه‌ی تکراری در ذخیره: " + itemRecord.InstanceId + "؛ رد شد.");
                        skipped++;
                        continue;
                    }

                    ItemInstance instance = ItemInstance.Restore(
                        itemRecord.InstanceId,
                        itemRecord.DefinitionId,
                        itemRecord.Quantity,
                        itemRecord.Durability,
                        record.Id);

                    if (instance == null)
                    {
                        skipped++;
                        continue;
                    }

                    if (!instance.IsKnown)
                        Debug.LogWarning("[Items] آیتم «" + instance.DefinitionId + "» دیگر تعریف نشده؛ با تعریف جایگزین بار شد.");

                    ApplyState(instance, itemRecord);

                    if (InventoryManager.AddRestored(instance, record.Id)) restored++;
                    else skipped++;
                }
            }

            InventoryManager.SyncProjection();
            ItemEvents.RaiseRestored();

            Debug.Log("[Items] بارگذاری: " + restored + " آیتم، " + skipped + " رد شد.");

            return true;
        }

        /// <summary>مهاجرت‌ها. هر نسخه یک قدم؛ نسخه‌ی فعلی آخرین است.</summary>
        private static void Migrate(ItemSaveData data)
        {
            if (data.Version == CurrentVersion) return;

            // نسخه ۱ اولین نسخه است؛ اگر روزی فیلدی اضافه شد، اینجا مقدار پیش‌فرض بگیرد.
            if (data.Version < 1) data.Version = 1;

            if (data.Version != CurrentVersion)
                Debug.LogWarning("[Items] مهاجرت از نسخه‌ی " + data.Version + " نوشته نشده؛ با احتیاط ادامه می‌دهیم.");
        }

        private static void ApplyState(ItemInstance instance, ItemInstanceRecord record)
        {
            if (record.StateKeys == null || record.StateValues == null) return;

            int count = Math.Min(record.StateKeys.Length, record.StateValues.Length);

            for (int i = 0; i < count; i++)
            {
                string key = record.StateKeys[i];

                if (string.IsNullOrEmpty(key)) continue;

                instance.SetState(key, record.StateValues[i]);
            }
        }
    }
}
