# مرحله ۷ — موسیقی منو + رفع مشکل فونت فارسی

## بخش ۱ — چرا مربع شد (تشخیص)

دو مشکل جدا بود، نه یکی.

### مشکل اول: `F1: فارسی` با فونت انگلیسی

در Console این خط را داشتی:

```
The character with Unicode value \u0641 was not found in the [Roboto-Bold SDF] font asset
```

`\u0641` = «ف». متن `F1: فارسی` داشت با **Roboto** رندر می‌شد، و Roboto هیچ حرف فارسی ندارد.
مشکل فونت فارسی نبود؛ خودِ آن یک متن، فونت اشتباه گرفته بود.

### مشکل دوم: فونت‌های Black/Bold، حروف Presentation Form ندارند

```
The character with Unicode value \uFE9D was not found in the [Vazirmatn-Bold SDF] font asset
```

`\uFE9D` یک «حرف فارسی در شکل نمایشی» است. RTLTMPro قبل از نمایش، متن فارسی را به
**Arabic Presentation Forms-B** تبدیل می‌کند و همان کدها را به TMP می‌دهد.

بنابراین فونت اَسِت باید این بلوک‌ها را داشته باشد:

- `FB50–FDFF`
- `FE70–FEFF` ← همان `FE9D` اینجاست

`Vazirmatn-Regular SDF` که قبلاً ساختیم این‌ها را دارد، برای همین کار می‌کرد.
در `Vazirmatn-Black SDF` و `Vazirmatn-Bold SDF` فقط `0600-06FF` را داده بودی؛ پس مربع شد.

خبر خوب: خودِ فایل `Vazirmatn-Bold.ttf` این گلیف‌ها را دارد. با `fontTools` شمردم:

| فایل | FE70–FEFF | FB50–FDFF |
| --- | --- | --- |
| `Vazirmatn-Bold.ttf` | ۱۴۱ گلیف | ۷۲ گلیف |
| `Vazirmatn-Black.ttf` | ۱۴۱ گلیف | ۷۲ گلیف |

پس فقط باید فونت اَسِت را با دامنه‌ی درست بسازیم.

## بخش ۲ — راه اول (پیشنهاد من): Dynamic

بدون هیچ دامنه‌ای. TMP هر گلیفی که لازم شد خودش موقع اجرا اضافه می‌کند — برای فارسی،
عربی، بعداً چینی و هر چیز دیگری.

برای **هر چهار** فونت اَسِت:

1. فایل اَسِت را در Project انتخاب کن (مثلاً `Vazirmatn-Bold SDF`)
2. Inspector → پایین، بخش `Atlas Population Mode` → از `Static` به **`Dynamic`**
3. تیک `Multi Atlas Textures` را بزن (تا آتلاس در صورت پر شدن بزرگ شود)
4. `Ctrl+S`

همین. اگر بعد از Play هنوز مربع دیدی، یک بار Unity را ببند و باز کن.

> لازم نیست چهار فونت را از نو بسازی. فقط حالت آتلاس عوض می‌شود.

## بخش ۳ — راه دوم (اگر Dynamic را نخواستی): دامنه‌ی کامل

`Window > TextMeshPro > Font Asset Creator` → `Character Set` = **Unicode Range (Hex)**

این رشته را عیناً کپی کن (از خودِ RTLTMPro، فایل `ArabicAll.txt`):

```
060C,061B,061F,0621-063A,0640-064A,064B-065F,0670,067E,0686,0698,06A9,06AF,06CC,06F0-06F9,0660-0669,FB56-FB59,FB7A-FB7D,FB8A-FB8D,FB8E-FB95,FBFC-FBFF,FC5E-FC63,FE70-FEFF
```

`Atlas Resolution` = `2048×2048` (چون تعداد گلیف زیاد است)
`Render Mode` = `SDFAA`
و برای اینکه انگلیسی هم کار کند، در انتها `,0020-007E` اضافه کن.

نام‌ها: `Vazirmatn-Black SDF` و `Vazirmatn-Bold SDF` — همان‌هایی که الان وصل کرده‌ای.

## بخش ۴ — موسیقی

فایل `menu.mp3` (۶.۵ مگابایت، همان آهنگ Godot) در پروژه هست:
`Assets/_Project/Art/Audio/menu.mp3`

کامپوننت `Menu Music` خودش `AudioSource` می‌سازد، لوپ می‌کند، با فید ۲.۵ ثانیه‌ای
شروع می‌شود و با رفتن به صحنه‌ی بعد قطع می‌شود.

### تنظیم

1. آبجکت `MainMenu` را انتخاب کن
2. `Add Component` → `Menu Music`
3. `Clip` → `menu`
4. `Volume` = `0.45`

هیچ `AudioSource` دستی لازم نیست.

## بخش ۵ — فایل‌های عوض‌شده

| فایل | تغییر |
| --- | --- |
| `Scripts/UI/MainMenuUI.cs` | هر متن فونت و جهت خودش را دارد؛ نام زبان با فونت خودش |
| `Scripts/UI/MenuMusic.cs` | تازه |
| `Art/Audio/menu.mp3` | تازه |

### چه چیزی در منو عوض شد

قبلاً یک متن بود: `F1: فارسی` — انگلیسی و فارسی در یک رشته، با فونت انگلیسی. سه ایراد داشت:
فونت اشتباه، ترتیب حروف در RTL، و حرف‌های جاافتاده.

حالا دو خط جدا در گوشه‌ی پایین-راست:

```
            F1
       ENGLISH        ← وقتی بازی فارسی است
```

```
            F1
         فارسی         ← وقتی بازی انگلیسی است
```

- `F1` همیشه با `Roboto-Bold` (لاتین خالص)
- نام زبان همیشه با فونتِ **زبان مقصد** و جهت خودش

قاعده‌ی کلی که از این به بعد رعایت می‌کنیم: **هیچ رشته‌ای دو زبان را با هم ندارد.**

## بخش ۶ — آزمایش

1. `Play` → آهنگ منو باید با فید شروع شود و لوپ شود.
2. `F1` → منو فارسی شود، بدون هیچ مربع.
3. عنوان `خانه` با `Vazirmatn-Black` و آیتم‌ها با `Vazirmatn-Bold`.
4. برگرد به انگلیسی → `HOME` با `Roboto-Black`.
5. در Console نباید هیچ هشدار `character with Unicode value ... was not found` بماند.
