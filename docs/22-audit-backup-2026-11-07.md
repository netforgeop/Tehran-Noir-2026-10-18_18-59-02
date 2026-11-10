# ۲۲. ممیزی بکاپ واقعی پروژه — ۲۰۲۶-۱۱-۰۷

> این سند نتیجه‌ی باز کردن و مقایسه‌ی `Backup/_Project_V.1.zip` با همین ریپو است.
> تاریخ ممیزی: جلسه‌ی ۱۰. اگر بعد از این تاریخ صحنه عوض شده، بخش ۵ را دوباره چک کن.

## ۰. منبع و روش

| مورد | مقدار |
| --- | --- |
| فایل | `Backup/_Project_V.1.zip` روی `origin/main` |
| حجم | ۱۴.۹ مگابایت فشرده، ۲۷.۱ مگابایت باز‌شده |
| محتوا | ۲۵۷ فایل، ریشه‌اش `_Project/` |
| دامنه | فقط `Assets/_Project` — `ProjectSettings/`، `Packages/` و `Library/` داخلش نیست |
| روش | مقایسه‌ی مسیر‌به‌مسیر (نام، اندازه، بایت)، خواندن YAML صحنه‌ها، و resolve کردن همه‌ی GUIDها |

این زیپ **بکاپ پوشه‌ی پروژه** است، نه یک پروژه‌ی کامل یونیتی. باز کردنش روی ماشین دیگر،
بدون `ProjectSettings` و `Packages`، صحنه را با متریال صورتی و بدون تنظیمات URP بالا می‌آورد.
برای همین ریپو هم هرگز «کلون کن و بازی کن» نبوده و نیست.

نکته‌ی فنی: هنگام باز کردن زیپ، `unzip` روی یک فایل اخطار می‌دهد
(`Art/Materials/Test_ّFloor.mat` — کاراکتر «ّ» در نام فایل). این اخطار بی‌خطر است؛
فایل سالم باز می‌شود. در ریپو نام درست (`Test_ّFloor.mat`) ثبت شده است.

## ۱. نتیجه در یک خط

پروژه سالم است و همه‌ی ارجاع‌های دو صحنه resolve می‌شوند؛ مشکل این است که
**کد زیپ چند مرحله از صحنه جلوتر است**: دیالوگ تحویل و رویارویی نوشته شده،
ولی پنج اتصال در صحنه انجام نشده و سه گره از دیالوگ تازه اصلاً دست‌نیافتنی‌اند.

## ۲. تطابق فایل‌ها

| گروه | در زیپ | یکسان با ریپو | متفاوت | تازه در زیپ |
| --- | --- | --- | --- | --- |
| اسکریپت `.cs` | ۴۳ | ۳۸ | ۵ | ۱ |
| دیالوگ `.txt` | ۸ | ۳ | ۱ | ۴ |
| زبان (`fa`, `en`) | ۲ | ۰ | ۲ | ۰ |
| صحنه `.unity` | ۲ | — | — | ۲ |
| ورودی (`PlayerControls`) | ۲ | — | — | ۲ |
| ادیتور | ۱ | — | — | ۱ |
| هنر (شیدر + متریال) | ۱۰ | — | — | ۱۰ |

**پنج اسکریپت متفاوت:** `DialogueStarter`, `WorldState`, `InteractionAction`,
`SimpleInteractable`, `TalkInteractable` — همه مربوط به تحویل و «دادن».
**یک اسکریپت تازه:** `PlaceInteractable`.
**یک دیالوگ متفاوت:** `reza_office.txt` از ۲.۵ کیلوبایت به ۱۱.۶ کیلوبایت.
**دو خط تازه در زبان‌ها:** `action.give` و `action.place`.

## ۳. دیالوگ‌های تازه

| فایل | گره | خط | کار |
| --- | --- | --- | --- |
| `reza_office.txt` | ۳۰ | ۵۵ | گرفتن کار، تحویل دستی، سه ادعا، بازدید بعدی، مسیر میز، اعتراف دوم |
| `farhadi_archive.txt` | ۱۲ | ۲۱ | تحویل به خانم فرهادی، مهر شکسته، چهار جواب، ثبت در دفتر |
| `mahmoud_corridor.txt` | ۷ | ۸ | راهنمای راهرو + اشاره به یزدانی |
| `archive_log.txt` | ۱ | ۳ | دفتر ثبت؛ با خواندنش `flag.clue.log` و بعد جعبه‌ی ۱۳۵۲ |
| `note_yazdani.txt` | ۱ | ۳ | یادداشت دست‌نویس؛ `flag.clue.note` |

منطق هر سه مسیر درست نوشته شده و پارسر همه‌ی شرط‌هایش را می‌فهمد
(از جمله `and` در `flag.lie.seal == true and flag.clue.log == true` و
مقایسه‌ی متنی `flag.delivery == desk` — بررسی شد).

## ۴. صحنه‌ی `Sandbox` — چه چیزی داخلش هست

۴۷ آبجکت، ۲۵ کامپوننت اسکریپتی، همه‌ی GUIDها resolve می‌شوند.

| آبجکت | کامپوننت‌ها |
| --- | --- |
| `Office` → `Ground`, `Floor`, `Wall_N/E/S/W`, `Desk`, `Chair`, `Cabinet` | فقط کالایدر و رندرر — هیچ تعاملی ندارند |
| `Player` | `PlayerController`, `PlayerInputReader`, `InteractionSensor`, `PlayerEnergy`, `CharacterController` |
| `Reza` | `DialogueStarter` (`reza_office.txt`, گره `start`) + `TalkInteractable` (فقط Talk) |
| `Cube` | `PickupInteractable` — همان «پرونده محرمانه» روی میز |
| `HUD` | `InteractionHUD` + Canvas ریشه |
| `DialogueSystem` | `DialogueUI` + `WorldStateProvider` (متغیرهای اولیه: خالی) |
| `Main Camera` + `CM_TopDown` | دوربین سینم‌مشین + `CameraOrbitController` |
| `World` | `WorldClock` + `WorldWeather` + `SunDriver` |
| `PauseMenu`, `EventSystem`, `Directional Light` | پوسته‌ی توقف، ورودی UI، نور |

## ۵. پنج چیزی که در صحنه وصل نشده

| # | چه چیزی کار نمی‌کند | مدرک در فایل | اصلاح |
| --- | --- | --- | --- |
| ۱ | کار «دادن» روی رضا هرگز ظاهر نمی‌شود | `handoverVariable` روی `Reza` خالی است | فیلد را پر کن (پایین) |
| ۲ | پرونده باز نمی‌شود؛ پس `flag.file.opened` همیشه false است | `Cube` هنوز `PickupInteractable` است؛ `FileInteractable` صفر ارجاع در صحنه دارد | کامپوننت را عوض کن |
| ۳ | راهِ «گذاشتن روی میز» وجود ندارد | `PlaceInteractable` صفر ارجاع در صحنه دارد | یک نقطه‌ی خالی بساز |
| ۴ | بایگانی ساخته نشده؛ فرهادی و محمود در صحنه نیستند | هیچ آبجکتی با نام `ArchiveWing` نیست | منوی ادیتور را اجرا کن و **ذخیره کن** |
| ۵ | هایلایت تعامل هیچ‌وقت دیده نمی‌شود | `OutlineEffect` صفر ارجاع در صحنه دارد؛ فیلد `outline` همه‌جا خالی | کامپوننت را اضافه کن |

عاقبت شماره ۲: سه گره `handover_broken`، `desk_open` و `broken` (شاخه‌ی مهر شکسته در
هر دو فایل رضا و فرهادی) عملاً دست‌نیافتنی‌اند. کل ایده‌ی «مدرک + تناقض» تا این وصل نشود
روی کاغذ است.

## ۶. دستور اتصال در Unity

قبل از شروع: `File > Build Profiles > Scene List` — هر دو صحنه‌ی `MainMenu` و `Sandbox`
باید در لیست باشند، وگرنه `MainMenuUI` نمی‌تواند `Sandbox` را با نام لود کند.

**۱) هایلایت — روی `Reza` و `Cube`**

`Add Component` → `Outline Effect` → فیلد `Outline Material` = `Assets/_Project/Art/Materials/M_Outline.mat`.
(شیدر `Art/Shaders/Outline.shader` تازه در ریپو ثبت شده؛ مطمئن شو در پروژه هست.)

**۲) پرونده‌ی بازشدنی — روی `Cube`**

کامپوننت `Pickup Interactable` را `Remove Component` کن (مقادیرش پاک می‌شود، باید دوباره وارد شوند)
و `File Interactable` را اضافه کن، بعد این‌ها را پر کن:

| فیلد | مقدار |
| --- | --- |
| Max Distance / Max Angle | `2.5` / `360` |
| Display Name | `پرونده محرمانه\|Confidential file` |
| Subtype | `مهر و موم‌شده\|Sealed` |
| Require Variable / Require Value | `flag.quest.file` / `1` |
| Taken Variable | `flag.file.taken` |
| Opened Variable | `flag.file.opened` (پیش‌فرض همین است) |
| Content Reader | یک `DialogueStarter` با فایل `file_sealed.txt` |

بعد از این، روی پرونده دو کار می‌آید: «باز کردن» و «برداشتن» — با کلید `۱` و `۲`.

**۳) دادن به رضا — روی `Reza`**

در `Talk Interactable`، بخش `Handover`: `Handover Variable` = `flag.file.taken`،
`Handover Node` = `handover` (پیش‌فرض همین است). حالا وقتی پرونده دستت باشد،
کار دوم «دادن» اضافه می‌شود.

**۴) نقطه‌ی میز — یک آبجکت خالی**

زیر `Desk` یک `Create Empty` بساز، اسمش `DeskPoint`، روی سطح میز بگذار
و `Add Component` → `Place Interactable`:

| فیلد | مقدار |
| --- | --- |
| Require Variable / Require Value | `flag.file.taken` / `true` |
| Effects (سه خط) | `flag.delivery = desk` · `flag.quest.file = 2` · `flag.file.taken = false` |

کالایدر خودش با `RequireComponent` اضافه می‌شود؛ اندازه‌اش را کوچک بگیر (مثلاً `0.4`).

**۵) بایگانی**

منوی `Tehran Noir/ساخت بایگانی و مأموریت` را اجرا کن (صحنه‌ی فعال باید `Sandbox` باشد و
یک `DialogueUI` در صحنه وجود داشته باشد) و بعد **`Ctrl+S`** — وگرنه با بستن ادیتور،
`ArchiveWing` از بین می‌رود. ابزار خودش روی فرهادی `handoverVariable = flag.file.taken` را ست می‌کند.

## ۷. نکته‌های ریز که ارزش اصلاح دارند

| مورد | توضیح |
| --- | --- |
| `rel.reza` پایه ندارد | `[do] rel.reza -5` از صفر شروع می‌شود و منفی می‌شود. قبل از ساختن نوار آبرو، در `WorldStateProvider` یک خط `rel.reza = 50` بگذار. |
| موزیک منوی توقف | `PauseMenuUI.musicClip` خالی است؛ اگر باید موسیقی داشته باشد، فایل را بده. |
| فونت منوی اصلی | صحنه‌ی `MainMenu` برای فارسی `Vazirmatn-Black` و `Vazirmatn-Bold` را گرفته. اگر در منو مربع دیدی، `Vazirmatn-Regular` بگذار. |
| فونت لاتین HUD | `InteractionHUD.latinFont` روی `RobotoCondensed-Black` است، ولی منوی توقف و دیالوگ `Bold` هستند؛ یکی‌شان کن. |
| `SunDriver.clock` خالی | اشکال نیست؛ خودش `WorldClock.Instance` را می‌گیرد. |
| `Reza.maxAngle = 273` | از کشیدن اسلایدر مانده؛ معنی‌اش «تقریباً از هر طرف». اگر می‌خواهی فقط از روبه‌رو باشد، `90` بگذار. |

## ۸. چه چیزی در گیت نماند و چرا

این ممیزی همه‌ی فایل‌های متنی زیپ را به ریپو آورد (اسکریپت‌ها، دیالوگ‌ها، زبان‌ها،
صحنه‌ها، `.meta`ها، شیدر، متریال‌ها، `PlayerControls`، ابزار ادیتور).
فقط ۸ فایل سنگین وارد نشد:

| فایل | حجم | چرا |
| --- | --- | --- |
| هفت فونت SDF (`Art/Fonts/* SDF.asset`) | ۱۱.۳ مگابایت | با هر بازسازی فونت عوض می‌شوند؛ در `Backup/` هستند |
| `Art/Audio/menu 1.mp3` | ۴.۷ مگابایت | باینری صوتی؛ در `Backup/` است |

`.meta` این فایل‌ها ثبت شده، پس اگر خود فایل‌ها را از زیپ برگردانی، همان GUID قبلی را می‌گیرند
و ارجاع صحنه‌ها نمی‌شکند.

## ۹. پیگیری اتصال‌ها (به‌روزرسانی جلسه‌ی ۱۰)

| اتصال | وضعیت |
| --- | --- |
| `FileInteractable` روی `Cube` | ✅ کامپوننت عوض شد (اسکرین‌شات ۲۰۲۶-۱۰-۱۸) |
| فیلدهای هویت و شرط روی `Cube` | ⬜ خالی — کارت «Cube» نشان می‌دهد و پرونده از فریم اول پیداست |
| `DialogueStarter` مستقل روی `Cube` | ⬜ `Content Reader` اشتباهاً به استارتر رضا وصل است |
| `Handover Variable` روی `Reza` | ⬜ خالی |
| `DeskPoint` | ⬜ ساخته نشده |
| `OutlineEffect` | ⬜ روی هیچ آبجکتی نیست |
| ابزار بایگانی | ⬜ اجرا و ذخیره نشده |

**تله‌ی `Content Reader`:** این فیلد باید به استارتری وصل شود که `Dialogue File` آن
`file_sealed.txt` است. اگر به استارتر رضا وصل بماند، «باز کردن پرونده» دیالوگ
همکاتبی رضا را پخش می‌کند — بدون هیچ خطایی در Console. استارتر تازه را روی خودِ
`Cube` بساز؛ `FileInteractable` اگر فیلد خالی باشد خودش `GetComponent` می‌کند.

## ۱۰. شکاف طراحی که در تست دیده خواهد شد

با ترتیب فعلی کارها، اگر بازیکن اول «برداشتن» (`۲`) را بزند، پرونده ناپدید می‌شود
و دیگر هرگز باز نمی‌شود؛ چون `PickupInteractable` رندرر و کالایدر را خاموش می‌کند
و موجودی (inventory) هم وجود ندارد. عملاً `flag.file.opened` همیشه false می‌ماند.

دو راه، هر دو کوچک:

۱. فهرست کارها را عوض کن: تا وقتی پرونده باز نشده، فقط «باز کردن» بیاید و «برداشتن»
   بعد از آن. یک خط در `FileInteractable`.
۲. یا فعلاً همین را بپذیر و در آموزش بازی، پرونده را فقط با باز کردن قابل برداشتن کن
   (نتیجه یکی است، فقط از سمت محتوا).

این تصمیم با کاربر است؛ تا آن موقع، در تست همیشه اول `۱` بزن.
