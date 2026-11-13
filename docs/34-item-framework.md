# ۳۴. چارچوب آیتم — نقشه و گام یک

> **به‌روزرسانی (جلسه ۲۵):** بخش «مؤلفه‌ها»ی این سند کنار گذاشته شد.
> رفتار آیتم حالا با قاعده‌ی داده‌ای است، نه کامپوننت به‌ازای دسته.
> گرامر کامل: `docs/35-item-rules.md`.

تاریخ: ۲۰۲۶-۱۱-۱۲ — جلسه‌ی بیست‌وچهارم

## ۱. بازرسی پروژه (فاز ۰)

| چه چیزی هست | کجا | تصمیم |
| --- | --- | --- |
| فهرست آیتم‌ها | `Assets/_Project/Resources/Items/items.txt` | می‌ماند؛ ستون‌های تازه اختیاری اضافه شد |
| تعریف آیتم | `Scripts/Items/ItemCatalog.cs` (کلاس داخلی) | به فایل جدا رفت و بزرگ شد |
| تعدادها | `WorldState["item.<id>"]` | می‌ماند؛ تنها منبع شمارش برای دیالوگ و کار |
| رابط وسایل | `UI/Pause/CharacterItemsSection.cs` | دست‌نخورده؛ فقط `Definition.Kind` را می‌خواند |
| برداشتن | `Interaction/PickupInteractable.cs` | بعداً به `InventoryManager` وصل می‌شود |
| سند خواندنی | `Documents/DocumentInteractable.cs` + `DocumentView` | وضعیت مهر/خوانده‌شدن روی `ItemInstance` می‌نشیند |
| کارها | `Quests/QuestTracker.cs` + `QuestLog` | با رویداد آیتم جلو می‌رود (فاز ۵) |
| ذخیره‌سازی | ندارد | فاز ۶ از صفر |
| بسته‌ها | `Packages/` در مخزن نیست؛ پروژه‌ی خام Unity | چیزی اضافه نمی‌شود |
| asmdef | ندارد | همه در `Assembly-CSharp` می‌مانند |
| نام‌گذاری | `namespace TehranNoir.<حوزه>`، `sealed class`، `_camelCase`، XML فارسی | رعایت شد |

## ۲. تصمیم اصلی: دو لایه، نه دو کتابخانه

| لایه | چه چیزی نگه می‌دارد | تنها نویسنده |
| --- | --- | --- |
| `ItemInstance` | مهر، خوانده‌شدن، دوام، مالک، شناسه‌ی یکتا | `InventoryManager` (فاز ۳) |
| `item.<id>` در `WorldState` | تعداد کل هر آیتم در جیب بازیکن | همان `InventoryManager`، بعد از هر تراکنش |

دلیل: دیالوگ‌ها و کارها از قبل با `[if] item.x >= 1` و `[do] item.x - 1` کار می‌کنند
و پنل تست (`F10`) همان متغیرها را نشان می‌دهد. اگر شمارش جدا شود، دو نسخه از حقیقت
ساخته می‌شود. پس تعدادها **پروجکشن** مانده‌اند، نه منبع دوم.

## ۳. منبع تعریف: فایل یا ScriptableObject؟

فعلاً **فایل** (`items.txt`)، چون:

| معیار | فایل متنی | ScriptableObject |
| --- | --- | --- |
| آیتم تازه | یک خط | یک asset + فهرست + GUID |
| مرور تغییر در گیت | خوانا | فایل YAML |
| پارس/اعتبارسنجی | یک ابزار (`tools/itemcheck.py`) | باید خودت بنویسی |
| انتخاب آیکون با ماوس | بعداً در پنجره‌ی ادیتور | همین حالا در Inspector |

مرز عوض‌کردن، `ItemCatalog` است: هر کد بیرونی فقط `ItemCatalog.Get/All/Exists` را
می‌شناسد. اگر بعداً ScriptableObject خواستی، فقط همین یک فایل و یک پنجره‌ی ادیتور
دست می‌خورد. **این تصمیم با توست.**

## ۴. قالب تازه‌ی `items.txt`

```
id | فارسی | انگلیسی | دسته | بیشترین تعداد | گزینه‌ها
```

| گزینه | معنا | نمونه |
| --- | --- | --- |
| `weight` | وزن هر عدد | `weight=0.1` |
| `value` | ارزش پایه (تومان/ریال بازی) | `value=2` |
| `flags` | `use,drop,trade,store,examine,destroy` | `flags=examine,store` |
| `icon` | مسیر داخل `Resources` | `icon=Icons/Keys/archive` |
| `prefab` | مدل سه‌بعدی | `prefab=Prefabs/Items/key` |
| `sub` | زیردسته‌ی آزاد | `sub=police` |
| `desc.fa` / `desc.en` | شرح دوزبانه | `desc.fa=...` |

دسته‌ها: `document / key / weapon / ammo / evidence / consumable / tool / quest / item`.

قواعد سازگاری: ۴ ستون هم قبول است، ستون پنجم خالی = ۱، دسته‌ی ناشناس = `item`
با هشدار در Console، خط خراب رد می‌شود و بازی نمی‌خوابد.

## ۵. کلاس‌های گام یک

| فایل | کلاس | کار |
| --- | --- | --- |
| `Items/ItemCategory.cs` | `ItemCategory` | دسته‌ی دقیق، مستقل از رابط |
| | `ItemKind` | سبد نمایشی قدیمی برای رابط فعلی |
| | `ItemCategories` | پارس دسته/پرچم، نگاشت به `ItemKind`، کلید ترجمه |
| `Items/ItemDefinition.cs` | `ItemFlags` | `[Flags]` برای شش اجازه |
| | `ItemDefinition` | داده‌ی تغییرناپذیر: شناسه، نام و شرح دوزبانه، دسته، زیردسته، وزن، ارزش، آیکون، Prefab، سقف پشته |
| `Items/ItemInstance.cs` | `ItemInstance` | نمونه‌ی زمان اجرا: شناسه‌ی یکتا، تعداد، دوام، وضعیت آزاد، مالک؛ `Split/Clone/CanStackWith` |
| `Items/ItemCatalog.cs` | `ItemCatalog` | همان API قبلی؛ پارس نسخه‌ی ۲ |

قاعده: هیچ‌گاه وضعیت زمان اجرا در `ItemDefinition` نوشته نمی‌شود (همه‌ی فیلدها
`readonly`). دو سند با یک تعریف، دو وضعیت جدا دارند.

## ۶. سازگاری با کد فعلی

| مصرف‌کننده | چه می‌خواند | وضعیت |
| --- | --- | --- |
| `CharacterItemsSection` | `Definition.Kind`، `Definition.MaxStack`، `Name(bool)` | بدون تغییر کار می‌کند (`Kind` حالا property است) |
| `Inventory` | `ItemCatalog.All/Get`، `ItemDefinition` | بدون تغییر |
| `PickupInteractable` | `Inventory.Add/Room` | بدون تغییر |
| دیالوگ/کار | `item.<id>` | بدون تغییر |

## ۷. گام‌های بعدی

| گام | فایل | وضعیت |
| --- | --- | --- |
| ۰ بازرسی | همین سند | انجام شد |
| ۱ تعریف و نمونه | `ItemCategory/ItemDefinition/ItemInstance` | انجام شد |
| ۲ مؤلفه‌ها | `Components/*State.cs` | بعد از تأیید |
| ۳ مدیر جیب | `InventoryManager.cs` | بعد از تأیید |
| ۴ کارها | `Actions/*.cs` | بعد از تأیید |
| ۵ سند | اتصال `DocumentState` به `DocumentView` | بعد از تأیید |
| ۶ رویداد و کارها | `ItemEvents.cs` | بعد از تأیید |
| ۷ ذخیره | `Save/*` | بعد از تأیید |
| ۸ ادیتور | `Editor/ItemBrowserWindow.cs` | بعد از تأیید |
| ۹ تست | `ItemSelfTest.cs` (Play mode) | بعد از تأیید |

## ۸. ابزارها

```bash
bash tools/preflight.sh          # CS0102 / CS0103 / تعداد آرگومان و سازنده
python3 tools/itemcheck.py       # اعتبارسنجی items.txt
```

هر دو با تخریب عمدی آزموده شده‌اند: حذف `HasSpace` را `symcheck` گرفت،
و `new ItemDefinition(...)` با یک آرگومان کم را `argcheck` گرفت.
