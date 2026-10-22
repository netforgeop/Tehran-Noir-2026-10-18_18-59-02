# مرحله ۴ — جهان زنده: ساعت، هوا، خستگی

سه مقدار که تا حالا ثابت بودند حالا خودشان عوض می‌شوند و در `WorldState` نوشته می‌شوند:

| متغیر | نویسنده | مقدارها |
| --- | --- | --- |
| `time` | `World Clock` | `morning`، `day`، `evening`، `night` |
| `weather` | `World Weather` | `clear`، `cloudy`، `rain` |
| `energy` | `Player Energy` | عدد ۰ تا ۱۰۰ |

مرز فازها: ۵ تا ۱۱ صبح = `morning`، ۱۱ تا ۱۷ = `day`، ۱۷ تا ۲۰ = `evening`، بقیه = `night`.

## تنظیم در Unity

۱. در Hierarchy یک آبجکت خالی بساز: `World`
   - `Add Component` → `World Clock`
   - `Add Component` → `World Weather`

۲. روی `Player`:
   - `Add Component` → `Player Energy`

۳. روی `DialogueSystem` → `World State Provider`:
   - اگر در `Initial Variables` خطی برای `time` یا `weather` یا `energy` هست، **پاکش کن**؛
     از این به بعد این سه را خودِ سیستم‌های بالا می‌نویسند.

## مقدارهای پیش‌فرض

| کامپوننت | فیلد | پیش‌فرض | معنی |
| --- | --- | --- | --- |
| World Clock | `Day Length Seconds` | ۶۰۰ | هر روز کامل ۱۰ دقیقه |
| World Clock | `Start Hour` | ۸ | شروع از صبح |
| World Weather | `Change Interval Seconds` | ۲۴۰ | هر ۴ دقیقه هوا عوض شود (۰ = هرگز) |
| Player Energy | `Sprint Drain` | ۱.۵ | هر ثانیه دویدن |
| Player Energy | `Rest Regen` | ۰.۸ | هر ثانیه استراحت |

## تست

۱. `Play` → در Console باید `[World] morning — ساعت 08:00` بیاید.
۲. روی `World Clock` → منوی ⋮ کامپوننت → **پرش به ساعت آزمایشی** (`debugHour` = ۲۲)
   → Console: `[World] night — ساعت 22:00`.
۳. برو سراغ رضا و `E` بزن → باید دیالوگ شب بیاید، نه دیالوگ صبح.
۴. برای دیدن گذر زمان: `Day Length Seconds` را موقتاً `60` کن → هر ۱۵ ثانیه یک فاز عوض می‌شود.

برای تست خستگی، یک NPC یا یک دیالوگ بساز که شرط داشته باشد `[if] energy < 30` و چند ثانیه بدو.

## نکته

- هر سه کامپوننت در طول دیالوگ متوقف می‌شوند (`Freeze During Dialogue`)، پس وسط حرف، هوا و ساعت عوض نمی‌شود.
- اگر `WorldStateProvider` در صحنه نباشد، این کامپوننت‌ها فقط یک هشدار در Console می‌دهند و کار دیگری نمی‌کنند.
