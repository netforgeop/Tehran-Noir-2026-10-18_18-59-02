# مرحله ۳ — جهان مشترک و پخش دیالوگ از هر جا

تا اینجا هر تست جهل خودش را داشت. از این به بعد یک جهان واحد در صحنه هست،
و هر NPC می‌تواند دیالوگ خودش را پخش کند.

## فایل‌ها

| فایل | کار |
| --- | --- |
| `Assets/_Project/Scripts/Dialogue/WorldStateProvider.cs` | تنها منبع وضعیت جهان |
| `Assets/_Project/Scripts/Dialogue/DialogueStarter.cs` | پخش یک فایل دیالوگ |
| `Assets/_Project/Scripts/Dialogue/DialogueTestTrigger.cs` | تغییر: اگر Provider باشد، از جهان او استفاده می‌کند |

## تنظیم در Unity

1. روی `DialogueSystem` کامپوننت `World State Provider` را اضافه کن.
2. `Initial Variables` را اینجا بگذار (قبلاً روی تست‌تریگر بود). مثلاً:
   - `met.reza = false`
   - `rel.reza = 0`
   - `time = night`
3. روی هر NPC که دیالوگ دارد، `DialogueStarter` بگذار:
   - `Dialogue File` → فایل همان شخص
   - `Dialogue UI` → `DialogueSystem`
   - `Start Node Id` را خالی بگذار تا از اولین گره شروع شود
4. برای تست بدون NPC: راست‌کلیک روی `DialogueStarter` → **شروع دیالوگ**.

`WorldStateProvider` فقط یک نمونه می‌تواند در صحنه باشد؛ دومی خودش را غیرفعال می‌کند.

## چرا Provider

- `met.reza` یک جا ذخیره می‌شود؛ دیالوگ رضا و بازجویی جواد و هر سیستم دیگر یک عدد را می‌بینند.
- دیالوگ فقط `WorldState` را می‌خواند و می‌نویسد؛ Provider مالک آن است.
- بعداً برای ذخیره‌ی بازی (Save) فقط همین یک کلاس باید سریال شود.
- برای پاک کردن: راست‌کلیک روی کامپوننت → **ریست جهان**.

## اتصال به سیستم تعامل (قدم بعد)

`TalkInteractable` در `Interact()` فقط باید یک خط صدا بزند:

```csharp
dialogueStarter.StartDialogue();
```

برای این کار به فایل فعلی `TalkInteractable.cs` نیاز دارم تا نسخه‌ی کامل و درست را بدهم.

## تست پذیرش

1. روی رضا و روی جواد دو `DialogueStarter` بگذار، هر کدام با فایل خودش.
2. با رضا حرف بزن، تمامش کن.
3. با جواد حرف بزن.
4. در Console بنویس `met.reza` را چطور ببینی — یا کافی است دوباره با رضا حرف بزنی:
   باید مسیر «آشنای قدیمی» بیاید، نه «اولین باره می‌بینمت».
5. **ریست جهان** → دوباره با رضا حرف بزن: باید برگردد به اولین دیدار.
