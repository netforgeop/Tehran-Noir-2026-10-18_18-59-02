# HANDOVER — Tehran Noir

> این فایل برای ایجنت بعدی نوشته شده. هدفش این است که بدون خواندن تاریخچه‌ی چت،
> بتواند پروژه را باز کند، بفهمد چه چیزی ساخته شده، و قدم بعدی را درست بردارد.
> فایل‌ها منبع حقیقت‌اند؛ اگر این نوشته با کد تفاوت داشت، **کد** حرف آخر را می‌زند.

---

## ۰. پرامپت آماده برای ایجنت

```text
پروژه‌ی من در ریپوی GitHub زیر است:
https://github.com/netforgeop/Tehran-Noir-2026-10-18_18-59-02

شاخه‌ی کاری: arena/4796527a-tehran-noir-2026-10-18-18-59-0

قبل از هر کاری:
1. فایل HANDOVER.md در ریشه‌ی ریپو را کامل بخوان.
2. بعد docs/19-file-mission-design.md و docs/21-file-verbs.md را بخوان.
3. سپس کد زیر را بخوان: Assets/_Project/Scripts/Interaction/*.cs
   و Assets/_Project/Scripts/Settings/Loc.cs

تو دسترسی مستقیم به فایل‌ها داری. قواعد کار:
- هر بازی که Unity Editor روی این ماشین دارد استفاده کن؛ اگر نداری، فقط کد بنویس و
  بگو کاربر چه چیزی را در Inspector تنظیم کند.
- همیشه فایل کامل بده، نه تکه‌کد (patch).
- هیچ سیستم موجودی را بازنویسی نکن مگر دلیلش را نوشته باشی.
- جواب‌ها فارسی، با اصطلاحات فنی انگلیسی.
- یک قدم در یک نوبت؛ قبل از رفتن به قدم بعد، تست را از من بگیر.
```

---

## ۱. پروژه در یک نگاه

| مورد | مقدار |
| --- | --- |
| نام بازی | **Tehran Noir** |
| ژانر | کارآگاهی / جنایی / معمایی / جهان باز |
| زمان و مکان | تهران، ۱۳۵۲ تا ۱۳۵۷ شمسی (۱۹۷۴–۱۹۷۸) |
| لحن | نوآر تاریخی؛ الهام از L.A. Noire، GTA: Chinatown Wars، The Wolf Among Us — با هویت مستقل |
| نما | سه‌بعدی Retro-3D، دوربین از بالا با زاویه‌ی قابل چرخش |
| موتور | **Unity 6.6 — `6000.6.5f1`**، خروجی Universal 3D (URP) |
| پکیج‌ها | Cinemachine 3 (6.6.0، core package در ۶٫۶)، Input System 1.20 |
| Active Input Handling | `Input System Package (New)` |
| زبان‌ها | فارسی و انگلیسی، هر دو کامل و قابل تعویض در لحظه |
| مسیر پروژه روی کامپیوتر کاربر | `D:\PYWP\Game\Unity Games\Tehran Noir\` |
| ریپو | https://github.com/netforgeop/Tehran-Noir-2026-10-18_18-59-02 |
| شاخه | `arena/4796527a-tehran-noir-2026-10-18-18-59-0` |
| آخرین کامیت این نوشته | `37b380d` — feat(interaction): several verbs per object, and a file you can open |

بازی اول کاربر است؛ سطح مهارت برنامه‌نویسی: مبتدی. کد باید ساده، مدولار و قابل توضیح بماند.

---

## ۲. چه چیزی داخل ریپو هست و چه چیزی نیست

**هست:**

- تمام اسکریپت‌های C# در `Assets/_Project/Scripts/`
- فایل‌های دیالوگ در `Assets/_Project/Dialogue/*.txt`
- فایل‌های زبان در `Assets/_Project/Resources/Localization/{fa,en}.txt`
- آیکون کلیدها در `Assets/_Project/Resources/Icons/Keys/` (۱۶ عدد PNG)
- **هر دو صحنه** (`Assets/_Project/Scenes/{Sandbox,MainMenu}.unity`)
- `.meta` همه‌چیز، شیدر `Outline.shader`، متریال‌ها، `Settings/PlayerControls.*`
  و ابزار ادیتور `Editor/ArchiveMissionBuilder.cs` (از ممیزی جلسه‌ی ۱۰ — `docs/22`)
- مستندات مرحله‌به‌مرحله در `docs/01` تا `docs/22`

**نیست:**

- هفت فونت SDF (`Art/Fonts/* SDF.asset`، ۱۱.۳ مگابایت) و `Art/Audio/menu 1.mp3` (۴.۷ مگابایت)
  — عمداً بیرون از گیت، داخل `Backup/` روی `main`. `.meta`شان هست، پس برگرداندنشان GUID را حفظ می‌کند.
- `ProjectSettings/` و `Packages/` — این‌ها هیچ‌وقت در گیت نبوده‌اند.

نتیجه‌ی عملی: کلونِ ریپو به‌تنهایی بازی را بالا نمی‌آورد (نه `ProjectSettings` دارد نه فونت‌ها).
**منبع حقیقت برای اجرا، پروژه‌ی روی ماشین کاربر است.** هر تغییر صحنه باید هم در Inspector
انجام شود و هم دستورش در پیام/`docs/` بماند؛ و هر تغییری که در فایل‌های متنی می‌شود باید
همان لحظه کامیت شود (بکاپ ۲۰۲۶-۱۱-۰۷ نشان داد کار جلسه‌ی ۹ فقط روی ماشین کاربر مانده بود).

---

## ۳. وضعیت فعلی — چه چیزی کار می‌کند

| سیستم | وضعیت | فایل‌های اصلی |
| --- | --- | --- |
| حرکت بازیکن | ✅ کار می‌کند و تنظیم شده | `PlayerController.cs`, `PlayerInputReader.cs` |
| دوربین از بالا با چرخش ماوس | ✅ کار می‌کند | `CameraOrbitController.cs` (Cinemachine `CM_TopDown`) |
| تعامل (تشخیص، انتخاب چندگانه، هایلایت) | ✅ کار می‌کند | `Interaction/*.cs`, `Rendering/OutlineEffect.cs` |
| HUD تعامل (کارت راست-پایین + نوار اهداف) | ✅ کار می‌کند | `UI/InteractionHUD.cs` |
| موتور دیالوگ + پنل دیالوگ | ✅ کار می‌کند، فارسی و انگلیسی | `Dialogue/*.cs` |
| محتوای بازجویی نمونه | ✅ آماده | `Dialogue/javad_interrogation.txt` |
| مأموریت پرونده — مرحله ۱ تا ۴ | ✅ کد و دیالوگ کامل؛ ۵ اتصال صحنه باقی (`docs/22`) | `Dialogue/reza_office.txt`, `TalkInteractable`, `PlaceInteractable` |
| تقویم شمسی، خورشید واقعی، ساعت | ✅ کار می‌کند | `World/*.cs` |
| انرژی و هوا | ✅ پایه‌ی ساده | `World/PlayerEnergy.cs`, `World/WorldWeather.cs` |
| منوی اصلی | ✅ کار می‌کند | `UI/MainMenuUI.cs`, `UI/MenuMusic.cs` |
| منوی توقف با ۴ تب | ✅ پوسته کار می‌کند؛ ۳ تب هنوز Placeholder | `UI/PauseMenuUI.cs`, `UI/Pause/*.cs` |
| صفحه‌ی تنظیمات (صدا، تصویر، گرافیک، زبان) | ✅ کار می‌کند و از هر دو منو باز می‌شود | `UI/MenuSettings.cs`, `Settings/*.cs` |
| محو بین صحنه‌ها | ✅ کار می‌کند | `UI/ScreenFade.cs` |
| تحویل به بایگانی (فرهادی، محمود، دفتر ثبت) | ✅ ساخته شد، ولی بایگانی هنوز در صحنه ساخته نشده | `Editor/ArchiveMissionBuilder.cs` |

---

## ۴. معماری و قواعد

### Namespace‌ها

| Namespace | محتوا |
| --- | --- |
| `TehranNoir.Interaction` | `Interactable` و فرزندانش، `InteractionSensor`, `InteractionAction` |
| `TehranNoir.Dialogue` | پارسر، رانر، مدل، `WorldState`, `WorldStateProvider`, `DialogueUI`, `DialogueStarter` |
| `TehranNoir.Settings` | `Loc` (زبان)، `GameSettings` (صدا/تصویر/گرافیک)، `ChannelledAudio` |
| `TehranNoir.UI` | منوها، HUD، فید، `UiKit`, `PauseTab` و تب‌ها |
| `TehranNoir.World` | `GameDate`, `SunPosition`, `SunDriver`, `WorldClock`, `WorldWeather`, `PlayerEnergy` |
| `TehranNoir.Core` | `Localization` (پل کوچک به `Loc`) |
| `TehranNoir.Inputs` | `PlayerInputReader` |
| `TehranNoir.Rendering` | `OutlineEffect` |

همه در یک Assembly؛ Assembly Definition تعریف نشده.

### قواعدی که پروژه روی آن‌ها بنا شده

1. **رابط در زمان اجرا ساخته می‌شود.** هیچ پریفبی برای منو، HUD یا تنظیمات نیست. دلیل: چیدمان در کد
   قابل اندازه‌گیری و تغییر است و از دزدی فایل‌های صحنه جلوگیری می‌کند. `UiKit` ابزارهای مشترک است:
   `Node`, `Box` (مبدأ بالا-چپ، y به پایین), `Text`, `KeyIcon`, `RoundedRect`, رنگ‌ها.
2. **هیچ متن ثابتی در کد نیست.** همه از `Loc.Get(Loc.Channel.Menu, "key")` می‌آید.
   زبان جدید = یک فایل `.txt` تازه، بدون تغییر کد.
3. **متن‌های Inspector دوزبانه‌اند:** `«فارسی|English»`؛ `Localization.Pick()` جدا می‌کند.
   یک زبان یعنی متن ساده.
4. **`WorldState` تنها منبع حقیقت است.** دیالوگ (`[do]`)، آیتم‌ها (`PickupInteractable`) و
   HUD آینده همه از همان می‌خوانند و در همان می‌نویسند.
5. **افزودن آبجکت تازه = پر کردن فیلد، نه نوشتن کد.** تا وقتی رفتار تازه لازم نشده،
   از `SimpleInteractable` / `TalkInteractable` / `PickupInteractable` / `FileInteractable` استفاده کن.
6. **یک کلاس، یک مسئولیت.** `SystemPauseTab` فهرست را می‌داند، `MenuSettings` صفحه را؛
   `InteractionSensor` پیدا می‌کند، `InteractionHUD` نشان می‌دهد.

### دقیقاً به خاطر بسپار

- کلاس‌های ساده‌ی C# (`MenuSettings`, `PauseTab`) نمی‌توانند `Destroy` یا `StartCoroutine` را
  بدون پیشوند صدا بزنند؛ `UnityEngine.Object.Destroy` لازم است.
- `CanvasScaler` **فقط روی کانواس ریشه** اثر دارد. کانواس تودرتو مقیاس نمی‌گیرد و به اندازه‌ی
  والدش فشرده می‌شود. HUD دقیقاً به همین دلیل کانواس خودش را با `SetParent(null)` می‌سازد.
- فونت `Vazirmatn-Black SDF` گلیف‌های فارسی کامل ندارد و مربع می‌سازد. فقط
  **`Vazirmatn-Regular SDF`** (guid `6ca5052bc7dc65c4fb37dee0825528ca`) درست است.
- هر متن فارسی با `RTLTextMeshPro` (پلاگین RTLTMPro) و پرچم `Farsi` نمایش داده می‌شود.
  `UiKit.Text(...)` و `UiKit.MakeText(...)` این را خودشان ست می‌کنند.
- تغییر زبان کل رابط را بازسازی می‌کند (`Loc.Changed`). برای همین هر تب باید
  `SaveState()`/`LoadState()` داشته باشد تا کاربر از جای خودش نپرد.

---

## ۵. صحنه‌ی `Sandbox`

مسیر: `Assets/_Project/Scenes/Sandbox.unity`. سلسله‌مراتب فعلی:

```text
Sandbox
├── Main Camera
├── Directional Light
├── Ground
├── Player                    (Capsule، CharacterController، PlayerController،
│                              PlayerInputReader، InteractionSensor)
├── CM_TopDown                (CinemachineCamera)
├── HUD                       (InteractionHUD — بچه‌های قدیمی‌اش پاک شده‌اند)
├── EventSystem
├── DialogueSystem            (DialogueUI)
├── World                     (WorldClock، SunDriver، WorldWeather، PlayerEnergy، WorldStateProvider)
├── PauseMenu                 (PauseMenuUI، AudioSource)
├── Office
│   ├── Floor, Wall_N, Wall_S_Left, Wall_S_Right, Wall_E, Wall_W
│   ├── Desk, Chair, Cabinet
│   └── File                  (Cube + FileInteractable + DialogueStarter)
├── Reza                      (Capsule + TalkInteractable + DialogueStarter)
└── DontDestroyOnLoad         (فید، موزیک — خودکار)
```

اتاق ۱۰×۸ متر، دیوارها ۱٫۲ متر ارتفاع (برای دوربین از بالا؛ دیوار بلند دید را می‌بندد).
درِ ورودی عرض ۱٫۲ متر در دیوار جنوبی.

### مقدارهای Inspector که باید همین بمانند

| آبجکت | فیلد | مقدار |
| --- | --- | --- |
| `Player` | Position | `0, 0.1, -3.4` |
| `Player` → `Interaction Sensor` | Search Radius | `4` |
| `Reza` | Position | `3.02, 0.6, 3.05` (پشت میز) |
| `Reza` → `Identity` | Display Name | `رضا\|Reza` |
| `Reza` → `Identity` | Subtype | `همکار دفتر\|Office colleague` |
| `Reza` → `Dialogue Starter` | Dialogue File | `reza_office` |
| `Reza` → `Dialogue Starter` | Dialogue UI | `DialogueSystem` |
| `File` | Position / Scale | `2.2, 0.78, 2.6` / `0.4, 0.03, 0.3` |
| `File` → `File Interactable` | Require Variable / Value | `flag.quest.file` / `1` |
| `File` → `File Interactable` | Taken Variable | `flag.file.taken` |
| `File` → `Dialogue Starter` | Dialogue File | `file_sealed` |
| `HUD` → `Interaction HUD` | Font | `Vazirmatn-Regular SDF` |
| `HUD` → `Interaction HUD` | Latin Font | `RobotoCondensed-Bold SDF` |
| `PauseMenu` → `PauseMenuUI` | Font / Latin Font | Vazirmatn / RobotoCondensed |
| `PauseMenu` → `PauseMenuUI` | Main Menu Scene Name | `MainMenu` |
| `PauseMenu` → `AudioSource` → `Music Clip` | `Art/Audio/menu.mp3` (هنوز دستی پر نشده) |

### مقدارهای عددی سیستم‌ها

| سیستم | مقدار |
| --- | --- |
| حرکت | walk `3`، run `6`، accel `12`، decel `24`، rotation `540`، gravity `-19.62`، jump `1.2`، groundCheck `0.1`، Skin Width `0.02` |
| دوربین | Radius `15`، Vertical `50`، sens `0.15`، stick `120`، recenterDelay `1`، recenterSpeed `20` |
| تعامل | searchRadius `4`، maxDistance پیش‌فرض `2.5`، maxAngle `360` |
| ساعت بازی | شروع `1352/1/1 08:00`، هر ساعت بازی = `25` ثانیه‌ی واقعی، در دیالوگ متوقف |
| خورشید | عرض `35.6892`، طول `51.3890`، UTC `+3.5`، `SunDriver` شدت `1.6` |
| هوا | هر `240` ثانیه؛ انرژی `100`، `-1.5` در دقیقه، `+0.8` استراحت |
| فید | ورود `0.6` ثانیه، خروج منو `0.45`، خروج بازی `0.4` |

---

## ۶. مأموریت «پرونده‌ی مهر و موم‌شده» — دقیقاً کجاست

### مرحله ۱ — گرفتن کار (✅ ساخته شد)

`Assets/_Project/Dialogue/reza_office.txt`

```text
start → (اگر met.reza) again → offer
offer: 4 خط + 3 گزینه (چی توش هست؟ / می‌برمش / الان وقتش نیست)
accept: flag.quest.file = 1، rel.reza +5
refuse: rel.reza -5
again:  flag.file.taken == true → carrying
        flag.quest.file == 1     → go_get
        وگرنه → offer
```

### مرحله ۲ — برداشتن و باز کردن (✅ ساخته شد)

- پرونده تا وقتی `flag.quest.file == 1` نشده دیده نمی‌شود.
- دو کار دارد: **باز کردن** (`flag.file.opened = true` + پخش `file_sealed.txt`) و
  **برداشتن** (`flag.file.taken = true` + ناپدید شدن).
- محتوای پرونده: صورت‌جلسه‌ی بازجویی ۱۴ مرداد ۱۳۵۲، متهم کریم شریفی، مرگ نامشخص،
  امضای سرگرد یزدانی.

### مرحله ۳ — تحویل و رویارویی (❌ قدم بعد)

طراحی کامل در `docs/19-file-mission-design.md`. خلاصه:

| پرچم | معنی |
| --- | --- |
| `flag.delivery` | `personal` (دستی) یا `desk` (روی میز) |
| `flag.claim.seal` | `opened` · `already` · `none` · `unknown` |
| `flag.lie.seal` | ادعایی که با واقعیت نمی‌خواند |
| `known.reza.seal` | رضا مهر شکسته را دیده |
| `known.reza.lie` | رضا فهمیده دروغ گفتی |

قاعده: **مهر شکسته = مدرک.** دستی دادن = همان لحظه معلوم می‌شود؛ روی میز گذاشتن =
بعداً معلوم می‌شود؛ دروغ فقط وقتی رو می‌شود که مدرک + تناقض با چیزی که رضا خودش می‌داند
هم‌زمان باشند. «یادم نیست» تناقض نمی‌سازد، فقط `rel.reza` را کم می‌کند.

### مرحله ۴ — آبرو و تب پرونده‌ها (❌ بعد از مرحله ۳)

سه نوار از ۵۰ شروع می‌شوند و فقط از **باور**ها حرکت می‌کنند:
**شرافت**، **صداقت**، **وفاداری**. برچسب‌ها: وفادار، خائن، درستکار، بدنام، معمولی.
تب «پرونده‌ها» در منوی توقف فقط از `WorldState` می‌خواند.

---

## ۷. فرمت فایل دیالوگ

هر خط یک تگ در ابتدای خود دارد. جداکننده‌ی دو زبان `|` است.

```text
[person] reza                    شناسه‌ی گوینده
[name] رضا | Reza                نام نمایشی
[role] همکار دفتر | Colleague    نقش
[cast] reza                      فهرست گویندگانی که در این فایل هستند

[node] start                     شروع یک گره
[if] met.reza == true -> again   شرط با پرش
[if] rel.reza >= 50              شرط برای خطوط بعدی (guard)
[say] reza | متن فارسی | English یک خط دیالوگ
[opt] متن فارسی | English -> node   گزینه
[go] next                        پرش بی‌شرط
[do] rel.reza +5                 تغییر WorldState
[do] met.reza = true
[end]                            پایان
```

- عملگرها: `=`, `+`, `-`
- مقدارها: عدد، `true`، `false`، یا متن
- متغیرهای شناخته‌شده: `time`, `weather`, `energy`, `fear`, `sanity`, `trust`, `playerAge`,
  `season`, `year`, `month`, `day`, `hour` و پیشوندهای `rel.`, `met.`, `present.`, `flag.`
- برای متغیر تازه فقط کافی است در دیالوگ استفاده شود؛ `EnsureDefaults` خودش می‌سازد.
- پارسر خطا و هشدار می‌دهد؛ اگر `document.HasErrors` باشد دیالوگ پخش نمی‌شود.

پنل دیالوگ: ارتفاع هر خط `max(1.2s, words × 0.35s)`. جلوبردن با کلیک، `Space`، `Enter` یا `B` دسته.

---

## ۸. رابط‌ها — چیدمان و اندازه‌ها

همه در واحد کانواس **۱۹۲۰×۱۰۸۰** با `matchWidthOrHeight = 0.5`.

### HUD تعامل — راست-پایین

| مورد | مقدار |
| --- | --- |
| لنگر | `(1,0)`، pivot `(1,0)`، حاشیه `48 / 44` |
| عرض کارت | `470` |
| ارتفاع کارت | پویا: هر کار `24` پیکسل + عنوان + زیرعنوان + راهنما |
| نوار اهداف | بالای کارت، قرص‌ها `32` ارتفاع، فاصله `8` |
| بج کلید | `44×44` |
| رنگ‌ها | کارت `#171B20` ۹۳٪، سطح `#252A30`، متن `#F0EEE9`، دوم `#A7A99E`، تاکید `#8A9A5B` |
| رفتار | محو + لغزش `16` پیکسل؛ در فارسی آینه می‌شود (بج راست، متن راست‌چین) |

کنترل‌ها: چرخ موس بین اهداف، `1`..`3` انتخاب کار، `E` اجرا. کارت وقتی هدفی نیست کاملاً
ناپدید است و ورودی نمی‌گیرد.

### منوی اصلی

`MenuX 270`, `MenuY -286`, عرض ستون `640`، عنوان `118/90px`، آیتم `76/56px`، فاصله `8`,
پدینگ متن `54`، طلایی `#A88030`، متن عادی `#969189`، متن فعال `#EEEFEC`.
آیتم‌ها: تست سندباکس / تنظیمات / خروج. `Esc` در منو = خروج.
هیچ کلید زبان در منو نیست — زبان فقط داخل تنظیمات.

### منوی توقف

`RowX 130`, `RowWidth 800`, `RowHeight 52`, `FirstRowY 396`, `RowFont 28`.
نوار بالا `100`، نوار پایین `91`، فاصله تب `56`، فونت تب `30`.
چهار تب **از راست به چپ**: تنظیمات · نقشه بازی · کدکس · پرونده‌ها.
`Q` = تب قبلی (سمت چپ)، `E` = تب بعدی. `Esc` اول به تب می‌رسد (در تنظیمات = برگشت)،
بعد منو را می‌بندد. `Tab`/دکمه‌ی شمالی = بخش بعدی.

### صفحه‌ی تنظیمات (`MenuSettings`، مشترک بین دو منو)

`RailX 130`, `RailWidth 330`, `RailFirstY 150`, `RailHeight 46`, `RowX 520`,
`RowWidth 1300`, `RowHeight 52`, `FirstY 150`, `BackY 470`.
چهار بخش: صدا / تصویر / گرافیک / زبان. ستون مقدار `380` عرض، راست‌چین.

- **صدا** فوری اعمال می‌شود: مستر، موزیک، افکت، دیالوگ؛ پله‌ها ۵٪.
- **تصویر و گرافیک** فقط با ردیف «اعمال تغییرات» واقعاً اعمال می‌شوند و در `PlayerPrefs` ذخیره می‌شوند.
  تا وقتی اعمال نشود، «اعمال‌نشده» نشان می‌دهد؛ بعد از اعمال، «اعمال شد».
- **آنتی‌آلیاسینگ و سایه** عمداً وجود ندارند؛ در URP فقط از `UniversalRenderPipeline.asset`
  قابل تغییرند. کاربر گفته «فعلاً نیاز نیست» — بعداً و با تذکر، زنده شود.

---

## ۹. زبان

- فایل‌های زبان: `Assets/_Project/Resources/Localization/fa.txt` و `en.txt`.
- هر فایل با `@name` و `@rtl` شروع می‌شود، بعد کلید‌ها.
- سه کانال: `Master`, `Menu`, `Subtitles`. کانال تازه = یک عضو در enum `Loc.Channel`.
- `Loc.Get(Loc.Channel.Menu, "key")` → متن؛ اگر نبود در فایل فال‌بک (`fa`) می‌گردد؛
  اگر نبود `<key>` برمی‌گرداند (نشانه‌ی کلید گم‌شده — در تست دنبالش بگرد).
- `Loc.Name("fa")` نام زبان را به زبان منو می‌دهد (`lang.fa`).
- ذخیره در `PlayerPrefs` با پیشوند `tehran.loc.*`.

کلیدهای موجود: `menu.*`، `pause.*`، `settings.*`، `settings.lang.*`، `action.*`، `hud.scroll`، `hud.verbs`.

---

## ۱۰. کارهای باقی‌مانده (به ترتیب اولویت پیشنهادی)

| # | کار | چرا الان | اندازه |
| --- | --- | --- | --- |
| ۱ | **مرحله ۳ مأموریت**: تحویل پرونده و رویارویی رضا | اولین «دروغ» واقعی بازی و جایی که دو دفتر (واقعیت/باور) کار می‌کند | یک نشست |
| ۲ | **مرحله ۴**: نوارهای آبرو + تب «پرونده‌ها» در منوی توقف | بدون این، انتخاب‌های مرحله ۳ هیچ نتیجه‌ی دیدنی ندارند | یک تا دو نشست |
| ۳ | دایره‌ی وضعیت (سلامت/انرژی/زره) گوشه‌ی راست | بریف کاربر هست، ولی فقط با آمار واقعی؛ الان فقط `energy` واقعی است | یک نشست |
| ۴ | تب «نقشه بازی»، «کدکس»، «پرونده‌ها» از Placeholder به محتوا | پوسته آماده است | هر تب یک نشست |
| ۵ | میان‌پرده‌ی «۴ ماه بعد» | از مرحله ۵ باز مانده و هنوز جواب کاربر نگرفته | یک نشست |
| ۶ | آنتی‌آلیاسینگ و سایه (URP asset) | با تذکر، فقط اگر کاربر خواست | نیم نشست |
| ۷ | پاک‌کردن صحنه‌ی تکراری `Assets/Scenes/MainMenu.unity` | خطر لود صحنه‌ی اشتباه | دست کاربر |

### سؤال‌های باز که باید از کاربر پرسیده شود

- میان‌پرده‌ی «۴ ماه بعد» چه شکل و چه لحنی داشته باشد؟
- قاعده‌ی «رضا می‌فهمد» همان مهر و موم است یا «دروغ در دیالوگ» هم وارد شود؟
- سه کار روی آبجکت کافی است یا باید پشتیبانی از بیشتر باشد؟

---

## ۱۱. تله‌های شناخته‌شده (این‌ها را دوباره کشف نکن)

1. **`F1` روی منوی اصلی ممنوع است.** کاربر صریحاً حذفش کرد. زبان فقط داخل تنظیمات.
2. **فونت Black مربع می‌سازد.** فقط `Vazirmatn-Regular SDF` درست است.
3. **کانواس تودرتو مقیاس نمی‌گیرد.** HUD باید کانواس ریشه داشته باشد.
4. **`MarkPending()` نباید در `PlayerPrefs` ذخیره کند**، وگرنه تغییر اعمال‌نشده با ری‌استارت
   روی بازی می‌نشیند و دکمه‌ی اعمال بی‌معنی می‌شود.
5. **بعد از اجرای `Action` در ردیف‌های تنظیمات باید `Refresh()` صدا زده شود**، وگرنه متن
   عوض نمی‌شود و کاربر فکر می‌کند دکمه کار نکرده.
6. **`Screen.SetResolution` در ادیتور پنجره‌ی Game را عوض نمی‌کند.** مدرک اعمال، متن
   «اعمال شد» و خط `[Settings] applied ...` در Console است. اثر واقعی در Build دیده می‌شود.
7. **دو اسکریپت `PauseMenuUI` اشتباه است.** یکی در `Scripts/UI/PauseMenuUI.cs`، بقیه حذف شده‌اند.
8. **`CharacterController.isGrounded` قابل اعتماد نیست.** Raycast استفاده می‌شود.
9. **تغییر Inspector در حالت Play بعد از توقف برمی‌گردد.**
10. **`DialogueUI.Current` و `PauseMenuUI.IsOpen`** دو پرچم عمومی‌اند که سیستم‌های دیگر
    با آن‌ها می‌فهمند بازی مشغول است. قبل از کار روی ورودی، همیشه اول این‌ها را چک کن.
11. **`.unity` و `.meta` در گیت نیستند.** هیچ‌وقت `.meta` را دستی حذف نکن.

---

## ۱۲. قاعده‌های همکاری با کاربر

این‌ها خواسته‌ی صریح خود کاربر است، نه سلیقه‌ی ایجنت:

- پاسخ‌ها **فارسی**؛ اصطلاحات فنی، نام کلاس/تابع و متن‌های موتور **انگلیسی**.
- **هر فایل کامل** فرستاده شود؛ هیچ‌وقت «این خط را عوض کن» به‌تنهایی، مگر در فایل‌های بزرگ
  که بخش مشخصی تغییر کرده باشد (آن‌وقت هم محدوده با ذکر `private void X()` روشن شود).
- **یک قدم در یک نوبت.** هرگز مرحله‌ی بعد بدون اجازه شروع نشود.
- قالب شش‌گانه: ۱ معرفی / ۲ پیش‌نیاز / ۳ آموزش عملی / ۴ توضیح کد / ۵ آزمایش / ۶ توقف.
- هیچ سیستم موجودی بدون دلیل بازنویسی نشود؛ قبل از بازنویسی، کد فعلی خوانده شود.
- هر تنظیم دستی Unity باید علاوه بر پیام، در `docs/` هم نوشته شود.
- لحن: صمیمی، حرفه‌ای، دقیق، بدون تعارف و بدون ایموجی.
- **فارسی و انگلیسی قاطی نشود داخل یک جمله:** هر جا متن فارسی و انگلیسی کنار هم می‌آید
  (نام فیلد، نام کلاس، جمله‌ی انگلیسی، مسیر فایل) در **جدول** نوشته شود، با ستون جدا برای هر زبان.
  کاربر متن درهم را نمی‌تواند بخواند.
- تصمیم نهایی مال کاربر است؛ ایجنت گزینه‌ها را با دلیل می‌گذارد.

---

## ۱۳. بررسی کد و گیت

### بررسی سریع سینتکس (بدون Unity)

```bash
python3 -m venv /tmp/venv && /tmp/venv/bin/pip -q install tree-sitter tree-sitter-c-sharp
/tmp/venv/bin/python - <<'PY'
import glob
from tree_sitter import Language, Parser
import tree_sitter_c_sharp as tscs
p = Parser(Language(tscs.language()))
bad = 0
for f in sorted(glob.glob('Assets/_Project/Scripts/**/*.cs', recursive=True)):
    if p.parse(open(f,'rb').read()).root_node.has_error:
        bad += 1; print('ERROR', f)
print('errors:', bad)
PY
```

tree-sitter فقط **سینتکس** را ثابت می‌کند. خطاهای نوع، تکرار اعضا و ارجاع‌های گم‌شده
فقط در Unity دیده می‌شوند، پس بعد از هر تغییر باید کاربر Console را بفرستد.

### گیت

```bash
git fetch origin arena/4796527a-tehran-noir-2026-10-18-18-59-0 --depth=20
git reset --soft origin/arena/4796527a-tehran-noir-2026-10-18-18-59-0
git commit -m "..."
git push origin HEAD:arena/4796527a-tehran-noir-2026-10-18-18-59-0
```

کلون **shallow** است. هیچ‌وقت `git pull` یا `merge` نزن؛ الگوی بالا همیشه کار می‌کند.
فقط به شاخه‌ی `arena/4796527a-...` پوش شود؛ شاخه‌های دیگر (مثل `arena/92e4d4c4-…`) مال
جلسه‌های قبلی‌اند و نباید دست بخورند.

---

## ۱۴. فهرست فایل‌ها با یک خط توضیح

### Interaction

| فایل | کار |
| --- | --- |
| `Interactable.cs` | پایه: کار، نقطه، فاصله، زاویه، نام/زیرعنوان دوزبانه، `Verbs`، `Interact` |
| `InteractionAction.cs` | enum: Use, Open, Close, Talk, Take, Read, Enter, Drive |
| `InteractionSensor.cs` | جمع‌آوری اهداف نزدیک، انتخاب پایدار، چرخش با اسکرول/کلید/دسته |
| `SimpleInteractable.cs` | شیء بدون رفتار خاص (فقط لاگ) |
| `TalkInteractable.cs` | آدم؛ دیالوگ را از `DialogueStarter` روی خودش پخش می‌کند |
| `PickupInteractable.cs` | آیتم برداشتنی با شرط ظهور و اثر برداشتن، هر دو از داده |
| `FileInteractable.cs` | پرونده: باز کردن (مهر) + برداشتن، با پخش محتوا |

### Dialogue

| فایل | کار |
| --- | --- |
| `DialogueModel.cs` | ساختار داده: گره، خط، گزینه، لینک |
| `DialogueCondition.cs` | پارس و ارزیابی شرط‌ها، `DialogueValue` |
| `DialogueParser.cs` | خواندن فایل، خطا/هشدار، اعتبارسنجی گره‌ها |
| `DialogueRunner.cs` | اجرای گره‌ها، اثرها، `ApplyEffect` عمومی |
| `DialogueUI.cs` | پنل دیالوگ، گزینه‌ها، زمان‌بندی، ورودی |
| `DialogueStarter.cs` | پخش یک فایل از هر جای بازی |
| `WorldState.cs` / `WorldStateProvider.cs` | منبع حقیقت متغیرها |
| `DialogueConsoleTester.cs` / `DialogueTestTrigger.cs` | تست‌های قدیمی |

### Settings

| فایل | کار |
| --- | --- |
| `Loc.cs` | زبان: کانال‌ها، فایل‌ها، رویداد `Changed`، `Name`, `Next`, `CommonCode` |
| `GameSettings.cs` | صدا (فوری) و تصویر/گرافیک (با اعمال)، `PlayerPrefs` |
| `ChannelledAudio.cs` | پخش صدا با حجم کانال («موسیقی»، «افکت»، «دیالوگ») |

### UI

| فایل | کار |
| --- | --- |
| `UiKit.cs` | ابزار ساخت: `Node`, `Box`, `Text`, رنگ‌ها، `KeyIcon`, `RoundedRect`, RTL |
| `MainMenuUI.cs` | منوی اصلی کامل، زمان اجرا |
| `MenuMusic.cs` | موزیک صحنه با فید و احترام به کانال موسیقی |
| `MenuSettings.cs` | صفحه‌ی تنظیمات مشترک (صدا/تصویر/گرافیک/زبان) |
| `PauseMenuUI.cs` | پوسته‌ی منوی توقف: تب‌ها، محو، موزیک، ورودی |
| `Pause/PauseTab.cs` | پایه‌ی تب + `PauseRow` |
| `Pause/SystemPauseTab.cs` | تب تنظیمات: فهرست ۴ ردیفی + `MenuSettings` |
| `Pause/PlaceholderPauseTab.cs` | تب‌های ساخته‌نشده |
| `Pause/SettingsLayout.cs` | عددهای چیدمان صفحه‌ی تنظیمات |
| `InteractionHUD.cs` | کارت تعامل و نوار اهداف |
| `ScreenFade.cs` | پرده‌ی سیاه بین صحنه‌ها |

### بقیه

`PlayerController.cs`، `PlayerInputReader.cs`، `CameraOrbitController.cs`،
`Rendering/OutlineEffect.cs`، `Core/Localization.cs`، `World/*.cs`.

---

## ۱۵. مستندات (به ترتیب)

| فایل | موضوع |
| --- | --- |
| `docs/01`–`05` | موتور دیالوگ، پنل، زبان، پرامپت بازجویی، متغیرهای جهان |
| `docs/06`–`08` | دوربین در دیالوگ، ساعت جهان، خورشید و تقویم |
| `docs/09`–`11` | منوی اصلی، موزیک و فونت، صدا و کادر انتخاب |
| `docs/12`–`14` | منوی توقف، تنظیمات و زبان، اصلاحات منوی توقف و فید |
| `docs/15`–`18` | دکمه‌ی اعمال و نام زبان‌ها، تنظیمات مشترک، کلیدها و فید، ماندن در بخش زبان |
| `docs/19` | **طراحی مأموریت پرونده و سیستم آبرو** (مهم‌ترین سند طراحی) |
| `docs/20` | HUD تعامل و انتخاب با اسکرول |
| `docs/21` | چند کار روی یک آبجکت: پرونده |
| `docs/22` | ممیزی بکاپ ۲۰۲۶-۱۱-۰۷ و پنج اتصال باقی‌مانده |
| `docs/23` | **اسناد کاغذی**: الگو، تگ‌ها، سه راه باز کردن، RTLTMPro |
| `docs/24` | **کارها و ژورنال**: فرمت کار، تراکر گوشه، نقشه‌ی چهار بخش منو |

---

## ۱۶. کامیت‌های مهم

| کامیت | چه چیزی |
| --- | --- |
| `31b98c9` | منوی اصلی در Unity |
| `526d8aa` | پس‌زمینه‌ی تمام‌صفحه |
| `f924224` | پوسته‌ی منوی توقف |
| `4f5470b` | کلیک‌پذیری، Q/E، فید، موزیک منوی توقف |
| `a4cd59a` | نام زبان‌ها و حذف AA/سایه |
| `4be26b1` | `MenuSettings` مشترک، حذف `F1` |
| `c29306a` | فید خروج منو، کنترل کامل کلیدها، دکمه‌ی اعمال |
| `23c24b4` | ماندن در بخش زبان بعد از تغییر زبان |
| `4543035` | دیالوگ گرفتن مأموریت از رضا |
| `90fab2e` | HUD تازه + انتخاب چندگانه + آیتم برداشتنی |
| `15ead1b` | کارت در راست-پایین، کانواس ریشه |
| `37b380d` | چند کار روی یک آبجکت + پرونده‌ی بازشدنی |

---

## ۱۷. قدم بعدی — دقیق

**اتصال صحنه، نه کد تازه.** مرحله‌ی ۳ (تحویل و رویارویی) در بکاپ ۲۰۲۶-۱۱-۰۷ نوشته شده است؛
چیزی که مانده پنج اتصال در Inspector است و هر پنج‌تا در `docs/22` با مقدار دقیق فیلدها آمده‌اند:

۱. `OutlineEffect` روی `Reza` و `Cube` با متریال `M_Outline`.
۲. `Cube`: تعویض `PickupInteractable` با `FileInteractable` و پر کردن `Document Path`
   با `Documents/file_sealed`. (`Content Reader` دیگر لازم نیست.)
۳. `Reza`: `Handover Variable = flag.file.taken` (وگرنه کار «دادن» هرگز نمی‌آید).
۴. `DeskPoint` زیر میز با `PlaceInteractable` و سه اثر `flag.delivery = desk`،
   `flag.quest.file = 2`، `flag.file.taken = false`.
۵. `Tehran Noir/ساخت لایه‌ی اسناد و اتصال پرونده` — این خودش `DocumentLayer` را می‌سازد
   و `Document Path` پرونده را ست می‌کند.
۶. اجرای `Tehran Noir/ساخت بایگانی و مأموریت` و بعد `Ctrl+S`.

تا شماره ۲ انجام نشود `flag.file.opened` همیشه false است و سه گره
(`handover_broken`, `desk_open`, `broken`) دست‌نیافتنی می‌مانند.

بعد از اتصال، آزمایش این ترتیب است: گرفتن کار از رضا → برداشتن پرونده → `۱` (باز کردن)
→ دیدن برگه‌ی کاغذی → بستن با `Esc` → `۲` (برداشتن) → رفتن به رضا → کار «دادن»
→ دیدن «چرا درش بازه؟».
هیچ‌کدام از این‌ها بدون آن پنج اتصال قابل آزمایش نیست.

---

## ۱۸. اسناد کاغذی (جلسه‌ی ۱۰)

خواندن پرونده دیگر مثل دیالوگ نیست: یک برگه‌ی واقعی وسط صفحه می‌آید
(مثل Mafia: The Old Country). یک الگو، هر تعداد سند؛ متن در `.txt`.

| قطعه | نقش |
| --- | --- |
| `Scripts/Documents/DocumentText.cs` | خواندن فایل سند، دو زبان، تگ‌ها |
| `Scripts/Documents/DocumentView.cs` | ساخت برگه در زمان اجرا، دو قالب `letter` / `note` |
| `Scripts/Documents/DocumentLayer.cs` | `DocumentLayer.ShowResource("Documents/...")` |
| `Scripts/Documents/DocumentInteractable.cs` | کاغذ روی میز، کار «خواندن» |
| `Resources/Documents/*.txt` | سندها؛ نمونه: `file_sealed.txt` |
| `Editor/DocumentBuilder.cs` | `Tehran Noir/ساخت لایه‌ی اسناد و اتصال پرونده` |
| `Assets/RTLTMPro/` | شکل‌دهی فارسی (MIT، فقط runtime) |

- تگ‌ها: `[template] [agency] [office] [number] [date] [title] [stamp] [emblem] [seal]`
- خط بدنه: `فارسی | English`؛ خط خالی = پاراگراف تازه.
- سه راه باز شدن: `DocumentInteractable`، فیلد `Document Path` روی `FileInteractable`،
  یا تگ `[read] Documents/...` داخل دیالوگ (دیالوگ بعد از بسته شدن سند ادامه می‌دهد).
- جزئیات: `docs/23-documents.md`.

---

## ۱۹. ممیزی بکاپ ۲۰۲۶-۱۱-۰۷ (جلسه‌ی ۱۰)

خلاصه‌ی `docs/22`:

| مورد | نتیجه |
| --- | --- |
| اسکریپت‌ها | ۴۳ فایل؛ ۳۸ یکسان، ۵ جلوتر در زیپ، ۱ تازه (`PlaceInteractable`) |
| دیالوگ‌ها | ۴ فایل تازه (`farhadi_archive`, `mahmoud_corridor`, `archive_log`, `note_yazdani`) و `reza_office` از ۲.۵ به ۱۱.۶ کیلوبایت |
| صحنه‌ها | هر دو صحنه فقط در زیپ بودند؛ حالا در ریپو |
| ارجاع‌ها | همه‌ی GUIDهای هر دو صحنه resolve می‌شوند — هیچ ارجاع شکسته‌ای نیست |
| شکاف اصلی | صحنه از کد عقب است؛ پنج اتصال بخش ۱۷ انجام نشده |
| سطح زیپ | فقط `Assets/_Project` — بدون `ProjectSettings`/`Packages` |

نکته‌ی مهم برای ایجنت بعدی: **قبل از هر کار تازه، بخش ۵ `docs/22` را چک کن.** اگر کاربر
بگوید «اتصال‌ها را انجام دادم»، اولین کار تست مسیر مهر شکسته است، نه نوشتن محتوای جدید.

---

## ۲۰. کارها و ژورنال (جلسه‌ی ۱۰)

ساختار از Cyberpunk / Alan Wake 2 / AC Origins / Kingdom Come گرفته شده،
ظاهر از Mafia: The Old Country. منوی توقف چهار تب می‌ماند؛ تب آخر «کاراکتر» می‌شود
و چهار بخش دارد: کاراکتر، وسایل، کارها، پرونده‌ها. ترتیب ساخت: کارها → وسایل →
پرونده‌ها → کاراکتر.

این مرحله: داده‌ی کارها + تراکر گوشه.

| قطعه | نقش |
| --- | --- |
| `Scripts/Quests/QuestText.cs` | داده: کار، قدم، نوع |
| `Scripts/Quests/QuestParser.cs` | خواندن `.txt` با همان زبان شرط دیالوگ |
| `Scripts/Quests/QuestLog.cs` | وضعیت از `WorldState` + قفل «شروع شد» |
| `Scripts/Quests/QuestBootstrap.cs` | ساخت خودکار؛ صحنه دستی لازم ندارد |
| `Scripts/Quests/QuestTracker.cs` | فهرست بالا-گوشه، فارسی راست / انگلیسی چپ |
| `Resources/Quests/case_file.txt` | دو کار نمونه |

قاعده: `[start]` قفل است (یک بار برقرار شد، کار باز می‌ماند)، `[done]` کار را برمی‌دارد،
و ترتیب قدم‌ها تعیین می‌کند کدام هدف نشان داده شود.

## ۲۱. آمار و پنل تست (جلسه‌ی ۱۱)

تصمیم‌های کاربر: آمار **هر دو فهرست** (سؤال ۱ = ج) و پول **عدد واقعی** (سؤال ۲ = الف).

| قطعه | نقش |
| --- | --- |
| `Scripts/Characters/CharacterStats.cs` | هفت آمار روی `WorldState` با پیشوند `stat.` |
| `Scripts/Cheats/CheatPanel.cs` | پنل تست سه‌تبی، `F10` |
| `Scripts/Cheats/CheatBootstrap.cs` | ساخت خودکار پنل، فقط Editor و Development Build |

| کلید | فارسی | بازه |
| --- | --- | --- |
| `stat.money` | پول | ۰ تا ۹۹٬۹۹۹٬۹۹۹ |
| `stat.health` | جان | ۰ تا ۱۰۰ |
| `stat.honor` | شرافت | ۰ تا ۱۰۰ |
| `stat.charisma` | جذبه | ۰ تا ۱۰۰ |
| `stat.fame` | شهرت | ۰ تا ۱۰۰ |
| `stat.honesty` | صداقت | ۰ تا ۱۰۰ |
| `stat.loyalty` | وفاداری | ۰ تا ۱۰۰ |

شرافت در هر دو فهرست بود؛ **یک عدد** است. حلقه‌ی HUD سه‌تای اول را نشان می‌دهد،
تب کاراکتر پنج‌تای ویژگی را.

`WorldState` دو چیز تازه دارد: پیشوند `stat.` در `Prefixes` و `Names` / `Count` برای
پیمایش از بیرون (پنل تست). بقیه‌ی منطق دست‌نخورده است.

پنل: `Time.timeScale` را صفر نمی‌کند؛ فقط حرکت/دوربین/ورودی/سنسور را خاموش می‌کند
و `PauseMenuUI` را موقتاً غیرفعال نگه می‌دارد. مستند کامل: `docs/25-stats-and-cheat-panel.md`.

## ۲۲. کمانِ HUD و استقامت (جلسه‌ی ۱۲)

| کمان | رنگ | معنی |
| --- | --- | --- |
| بیرونی چپ | سرخ + آبی | جان + زره روی یک کمان ۲۰۰ واحدی |
| درونی چپ | زرد → سرخ | استقامت ۱۰۰ تا ‎-۱۵۰‎ |
| راست | رنگ ویژگی | یکی از پنج ویژگی، عادی شرافت، ۲۵ ثانیه برای تازه‌عوض‌شده |
| نبض | سرخ | استقامت زیر صفر |

| فایل | نقش |
| --- | --- |
| `Scripts/UI/ArcGraphic.cs` | مش کمان با زاویه و ضخامت دلخواه |
| `Scripts/UI/StatRingHud.cs` | چیدمان کمان، محو شدن، کمان راست |
| `Scripts/UI/RingBootstrap.cs` | ساخت خودکار HUD |
| `Scripts/Characters/CharacterStats.cs` | `stat.armor`، `stat.stamina`، `Damage`، `Heal` |
| `Scripts/World/PlayerEnergy.cs` | استقامت، اثرهای منفی، فرسودگی |

قاعده: استقامت زیر صفر **مجاز** است و کفش ‎-۱۵۰‎. روی کف، بعد از ۶۰ ثانیه جان کم می‌شود.
دویدن از قصد (تپ + ورودی) فهمیده می‌شود نه از سرعت — وگرنه حلقه‌ی مصرف/بازیابی می‌سازد.

مستند کامل: `docs/26-ring-hud.md`.

## ۲۳. حرکت پنل‌ها (جلسه‌ی ۱۳)

`Scripts/UI/PanelMotion.cs`: تنها منبع ورود و خروج پنل‌های HUD — آلفا + مقیاس + جابه‌جایی
از لبه، با `EaseOutBack` در هر دو جهت (کمی باد، بعد می‌نشیند / جمع می‌شود).

| پنل | باز | بسته | مقیاس بسته |
| --- | --- | --- | --- |
| کارت تعامل | ۰٫۲۰ | ۰٫۱۴ | ۰٫۹۴ |
| کمان | ۰٫۲۲ | ۰٫۱۶ | ۰٫۹۰ |

کمان دو `CanvasGroup` دارد: ریشه برای بودن/نبودن، بدنه برای کم‌رنگی بی‌حرکتی.
هنگام باز بودن کارت تعامل، کمان کنار می‌رود (`hideWhileCardShown`).

مستند: `docs/27-panel-motion.md`.

## ۲۴. ظاهر تازه‌ی HUD (جلسه‌ی ۱۴)

| فایل | نقش |
| --- | --- |
| `Scripts/UI/HudArt.cs` | پنل گرادیانی، دیسک، شش آیکون خطی — همه در زمان اجرا، کش‌شده |
| `Scripts/UI/InteractionHUD.cs` | کارت کرم: سرصفحه، ردیف‌ها، فوتر تیره |
| `Scripts/UI/StatRingHud.cs` | دیسک تیره + کمان‌های محوشونده + سه قطعه + برگه |
| `Scripts/UI/ArcGraphic.cs` | `SetFade(degrees)` با آلفای رأس |

`PanelMotion` از این پس محو نمی‌کند: آلفا تا ۴۰٪ آخرِ بسته شدن روی ۱ است.

پالت: کرم `#CCB89B` تا `#735E4A`، حاشیه `#D1BAA0`، فوتر `#12100F`، دیسک `#0B0908`.
مستند: `docs/28-hud-redesign.md`.

## ۲۵. سه حالت گیج (جلسه‌ی ۱۶)

| حالت | جای دیسک (نسبت به راست-وسط) |
| --- | --- |
| آرام | `(-215, -290)` — پایین-راست، فقط کمان جان/استقامت/شرافت |
| برگه باز | `(-360, 0)` — برگه از زیر دیسک بیرون می‌آید |
| کارت تعامل | `(-268, 60)` — گیج بالا می‌رود، کارت زیرش |

`_sheet` (۰..۱، ۰٫۲۴ ثانیه) هم‌زمان مکان، مقیاس و آلفای برگه و آلفای کمان‌ها را می‌چرخاند.
هر ویژگیِ پنج‌گانه که عوض شود، ۲۵ ثانیه باز می‌ماند؛ ردیف اول پس از بسته شدن کامل
به شرافت برمی‌گردد.

سه لایه: `Root` (آلفا) → `Place` (جابه‌جایی) → `Content` (مقیاس + سُر + کم‌رنگی).

سرصفحه‌ی کارت تیره شد: عنوان `#211C17`، نقش `#544537`.

## ۲۶. تب «کاراکتر» (جلسه‌ی ۱۷)

تب آخر منوی توقف = `CharacterPauseTab` (جایش `PlaceholderPauseTab("cases")` را گرفت).

| بخش | داده |
| --- | --- |
| خلاصه | نُه آمار از `CharacterStats` + تاریخ از `WorldClock.Now` |
| وسایل / کارها / پرونده‌ها | پیام «مرحله‌ی بعد» |

`UsesSections => true`؛ Q/E بخش را عوض می‌کند، `Tab` بخش بعدی، Esc اول → خلاصه.
ریل با کلیک هم کار می‌کند. کلیدهای زبان: `pause.tab.character`, `char.name`, `char.role`,
`char.section.*`, `char.date.unknown`.

مستند: `docs/29-character-tab.md`.

## ۲۷. ساعت‌های ویژگی و باگ رقم (جلسه‌ی ۱۸)

**باگ:** تاریخ «۲۵۳۱» می‌شد. ریشه: `UiKit.SetRtl` مقدار `PreserveNumbers = true` می‌گذاشت؛
RTLTMPro با این پرچم فقط رقم ASCII را عدد می‌شناسد و رقم فارسی ما را حرف RTL می‌دید و
برعکس می‌چید. **رفع:** `PreserveNumbers = !farsi`؛ عدد همیشه ASCII، تبدیل کار RTLTMPro.
`HudArt.Digits` حذف شد.

**تب کاراکتر:** فقط پنج ویژگی، هر کدام یک ساعت گرد (`ArcGraphic`) با آیکون؛
زیر ۳۰ سرخ، بالا سفید. نشان بزرگ شرافت در سرصفحه.
`PauseTab.TickValues()` هر فریم وقتی منو باز است صدا زده می‌شود.
آیکون‌ها: ستاره / تاج / آدم‌ها / سند / سپر (سپر تازه به `HudArt.Glyph` اضافه شد).

مستند: `docs/30-trait-gauges-and-digits.md`.

## ۲۸. ترتیب عدد (جلسه‌ی ۱۹)

**باگ:** ساعت «۰۸:۲۷» به‌صورت «۲۷:۰۸». ریشه: `RTLTMPro` رقم فارسی را حرف RTL
می‌داند، پس دونقطه‌ی بین رقم‌ها رشته را می‌شکست. سه وصله در کتابخانه:
علامت بین دو رقم داخل رشته‌ی LTR بماند، علامت آخر جمله در فارسی سمت چپ بنشیند،
و `IsRTLInput` با «هر حرف RTL» درست شود. عدد خالص هم `ForceFix` می‌خواهد
(`UiKit.ForceFixNumbers`). مستند: `docs/31-number-order.md`.

## ۲۹. جیب (جلسه‌ی ۲۰)

بخش «وسایل» تب کاراکتر واقعی شد. آیتم‌ها در `Resources/Items/items.txt`
تعریف می‌شوند (id | فارسی | انگلیسی | نوع | سقف) و تعدادشان فقط در
`WorldState` با کلید `item.<id>` است؛ پس دیالوگ و پنل F10 هم همان را می‌بینند.
`Inventory` (Count/Add/Remove/Take/Slots/Room/Signature) و `ItemCatalog`.
`PickupInteractable` فیلدهای `Item Key`/`Item Count` گرفت؛ اگر جیب پر باشد
شیء برداشته نمی‌شود. آیکون‌های تازه: `Glyph.Key` و `Glyph.Case`.
مستند: `docs/32-inventory.md`.

## ۳۰. شبکه‌ی وسایل (جلسه‌ی ۲۱)

بخش «وسایل» از ردیف متنی به شبکه‌ی ۴×۳ کارت تبدیل شد و به کلاس جدا رفت:
`UI/Pause/CharacterItemsSection.cs`. نوار دسته (همه/اسناد/کلیدها/بقیه) + دکمه‌ی
ترتیب + `صفحه n / m`. کارت: نوار رنگ دسته، آیکون، نام، نوع، تعداد؛ انتخاب با
حاشیه‌ی طلایی. ناوبری کیبورد و ماوس، عبور از صفحه خودکار.
تب فقط صدا می‌زند: `Move/Step/Activate/Pointer/Tick`.
مستند: `docs/33-items-grid.md`.

## ۳۱. خطای CS0102 در شبکه‌ی وسایل (جلسه‌ی ۲۲)

`CharacterItemsSection` یک ثابت رنگ به نام `Clear` داشت و یک متد `Clear(RectTransform)`؛
C# اجازه نمی‌دهد در یک نوع، فیلد و متد هم‌نام باشند. ثابت به `Transparent` تغییر کرد.
فیلد `ChipView.Image` هم به `Background` تغییر کرد تا نامش با نوعش یکی نباشد.

**ابزار تازه:** `tools/cscheck.py` — هر فیلد و متدِ هم‌نام در یک نوع را پیدا می‌کند
(با اسکن آکولادی، پس اعضای نوع تودرتو و متغیرهای محلی خطا نمی‌سازند):

```
python3 tools/cscheck.py Assets/_Project/Scripts
```

## ۳۲. خطای CS0103 و پیش‌بررسی اجباری (جلسه‌ی ۲۳)

موقع بیرون‌بردن فهرست وسایل از تب، کمکی‌های `ShowNote`/`HideNote` هم حذف شدند
ولی `BuildSection` هنوز صداشان می‌زد → دو خطای CS0103. برگشتند.

**درس:** جابه‌جایی کد یعنی هم مرجع‌ها را ببر، هم تعریف‌ها. ابزار تازه:
`tools/preflight.sh` که سه بررسی را پشت سر هم اجرا می‌کند:

| ابزار | چه چیزی | خطای واقعی که گرفت |
| --- | --- | --- |
| `tools/cscheck.py` | فیلد و متدِ هم‌نام در یک نوع | همان `CS0102` قبلی |
| `tools/symcheck.py` | فراخوانی/فیلدِ اعلام‌نشده | همین دو `CS0103` |
| `tools/argcheck.py` | تعداد آرگومان در فراخوانی متدهای پروژه | با تست دستی سنجیده شد |

اجرا: `bash tools/preflight.sh`

## ۳۳. آغاز چارچوب آیتم (جلسه‌ی ۲۴)

فاز ۰ و ۱ از چارچوب آیتم انجام شد: تعریف و نمونه جدا شدند تا دو سند با یک
تعریف بتوانند وضعیت متفاوت داشته باشند.

| فایل | تغییر |
| --- | --- |
| `Items/ItemCategory.cs` | تازه: `ItemCategory`، `ItemKind`، `ItemCategories` (پارس دسته و پرچم) |
| `Items/ItemDefinition.cs` | تازه: `ItemFlags` و تعریف کامل (وزن، ارزش، آیکون، Prefab، شرح دوزبانه) |
| `Items/ItemInstance.cs` | تازه: شناسه‌ی یکتا، تعداد، دوام، وضعیت آزاد، `Split/Clone/CanStackWith` |
| `Items/ItemCatalog.cs` | پارس نسخه‌ی ۲ با گزینه‌های `key=value`؛ API بیرونی عوض نشد |
| `Resources/Items/items.txt` | ستون ششم و گزینه‌ها |

**قاعده‌ی تازه:** `item.<id>` در `WorldState` تنها شمارشگر است و از این به بعد فقط
`InventoryManager` آن را می‌نویسد؛ جزئیات در `ItemInstance` است.

**ابزار تازه:** `tools/itemcheck.py` (اعتبارسنجی داده) و گسترش `tools/argcheck.py`
به سازنده‌ها (`new Type(...)`). ساختار `struct` از قاعده مستثناست.

سند کامل: `docs/34-item-framework.md`

## ۳۴. آیتم داده‌محور: قاعده به‌جای کامپوننت (جلسه‌ی ۲۵)

سند ۳۴ برای هر دسته کامپوننت جدا پیش‌بینی کرده بود. طبق خواسته‌ی صریح
(«برای هر آیتم کامپوننت نمی‌خوام»)، رفتار آیتم به داده منتقل شد:

| قبل | بعد |
| --- | --- |
| `DocumentComponent`، `WeaponComponent`، `KeyComponent` | یک گرامر قاعده برای همه |
| دسته‌ی تازه = کلاس تازه | دسته‌ی تازه = یک مقدار در `enum ItemCategory` |
| شرط داخل کد | `read.if=state.sealed = 0` |
| اثر داخل کد | `read.do=state.read = 1; flag.letter.read = true` |

**قاعده = فعل + شرط + اثر.** شرط و اثر همان گرامر `[if]`/`[do]` دیالوگ‌اند،
پس `item.x >= 1`، `rel.reza`، `known.reza.*` و `state.*` همه کار می‌کنند.

### فایل‌های تازه

| فایل | کار |
| --- | --- |
| `Items/ItemVerb.cs` | نام فعل‌ها |
| `Items/ItemRule.cs` | قاعده، مجموعه‌ی قاعده، جفت وضعیت |
| `Items/ItemState.cs` | `state.*` + دنیای «شده» |
| `Items/ItemResult.cs` | نتیجه و دلیل شکست (`item.reason.*`) |
| `Items/ItemEvents.cs` | شش رویداد + پاک‌کردن در شروع بازی |
| `Items/ItemContainer.cs` | ظرف |
| `Items/InventoryManager.cs` | تراکنش‌ها + پروجکشن `item.*` |
| `Items/ItemRuntime.cs` | اجرای فعل |
| `Items/ItemSave.cs` | ذخیره‌ی نسخه‌دار |
| `Items/ItemSelfTest.cs` | ۱۴ گروه تست داخل بازی |
| `Editor/ItemBrowserWindow.cs` | پنجره‌ی `Tehran Noir/آیتم‌ها` |

### قواعد ثابت

| قاعده | چرا |
| --- | --- |
| `item.<id>` فقط با `InventoryManager` نوشته می‌شود | یک منبع شمارش؛ دیالوگ و F10 همان را می‌خوانند |
| اثر `item.x - 1` در قاعده نوشته نشود | پروجکشن با نمونه‌ها ناهماهنگ می‌شود |
| وضعیت فقط روی `ItemInstance` | دو سند با یک تعریف باید دو حالت داشته باشند |
| شماره‌ی شناسه‌ی نمونه یکتاست؛ ظرف تکراری را رد می‌کند | بارگذاری نباید آیتم را دو بار بسازد |

### ابزارها

`python3 tools/itemcheck.py` حالا فعل، `state.*`، `target`، `consume` و
`doc` را هم بررسی می‌کند. `bash tools/preflight.sh` بدون تغییر.

### تست

`ItemSelfTest` را روی یک GameObject بگذار و Play بزن؛ جدول PASS/FAIL در Console.
گروه‌ها: تعریف، ساخت، اضافه/کم، پشته، جا، انداختن، انتقال، سند، کلید،
سلاح، رویداد، ذخیره، تعریف ناشناس، شناسه‌ی تکراری.

سند کامل: `docs/35-item-rules.md`

## ۳۵. حذف آیتم، هم‌ترازی و پیام خطای گویا (جلسه‌ی ۲۶)

| تغییر | فایل | چرا |
| --- | --- | --- |
| دکمه‌ی «حذف این آیتم» با تأیید | `Editor/ItemBrowserWindow.cs` | ساختن بود، حذف نبود |
| `InventoryManager.Reconcile()` | `Items/InventoryManager.cs` | عدد از F10/دیالوگ عوض می‌شد ولی نمونه نبود؛ Enter روی کارت کاری نمی‌کرد |
| پاک‌کردن کاراکتر نامرئی در توکن‌ها | `Items/ItemCatalog.cs` | کپی از صفحه‌ی وب `‏` و `＝` می‌آورد و «گزینه‌ی بی‌شکل» می‌داد |
| پیام دقیق‌تر خطا | `Items/ItemCatalog.cs` | حالا خود توکن داخل «» چاپ می‌شود و علت حدس زده می‌شود |
| مرتب‌کردن فایل بعد از افزودن/حذف | `Editor/ItemBrowserWindow.cs` | خط خالی اضافه تلنبار نشود |

`item.<id>` و نمونه‌ها حالا با `Reconcile` هم‌تراز می‌شوند؛ **پروجکشن** همچنان
فقط دست `InventoryManager` است.

## ۳۶. شرط با «=» تنها — علت ۷ تست رد (جلسه‌ی ۲۷)

`ConditionParser` فقط `==` را به‌عنوان برابر می‌پذیرفت. قاعده‌های `items.txt`
با `=` نوشته شده بودند (`read.if=state.sealed = 0`)، پس هر شرط با
«بخش اضافه در شرط: «=»» رد می‌شد و **قاعده بی‌صدا حذف** می‌شد.
آیتم به رفتار پیش‌فرض برمی‌گشت: سند بدون قاعده «همیشه خواندنی» است.

| اصلاح | فایل |
| --- | --- |
| `=` تنها به `==` نگاشت می‌شود | `Dialogue/DialogueCondition.cs` |
| اخطار شرط خراب → Error با شناسه‌ی آیتم | `Items/ItemCatalog.cs` |
| `tools/itemcheck.py` گرامر شرط را هم می‌خواند | `tools/itemcheck.py` |
| تست: خواندن خشاب قبل از شلیک | `Items/ItemSelfTest.cs` |

**درس:** هر چیزی که «بی‌صدا رد می‌شود» بدتر از خطای سخت است. قاعده‌ای که
خوانده نشود، آیتم را با رفتاری دیگر نشان می‌دهد و کسی نمی‌فهمد.

### وضعیت پایانی سیستم آیتم

`ItemSelfTest` در بازی: **۳۶ قبول، ۰ رد**. تنها نویسنده‌ی `item.<id>`
همان `InventoryManager` است (بازرسی: هیچ `Inventory.Add/Remove/Take` بیرون از
آن نمانده). محدودیت‌های باز در `docs/35-item-rules.md` §۱۴ ثبت شده‌اند:
انتخاب فعل غیراصلی در رابط، ساخت آبجکت سه‌بعدی هنگام انداختن، و وصل‌نشدن
رویدادها به یک کار واقعی.

## ۳۷. فعل‌های آیتم در منو و انداختن روی زمین (جلسه‌ی ۲۸)

| چه چیزی | فایل |
| --- | --- |
| فهرست فعل‌های ممکن با شرط | `Items/ItemDefinition.cs` → `Verbs(...)` |
| نوار ۱..۴ زیر خط شرح | `UI/Pause/CharacterItemsSection.cs` |
| مسیر کلید عددی | `UI/PauseMenuUI.cs` → `PauseTab.Digit` → `CharacterPauseTab.Digit` |
| انداختن روی زمین | `Items/ItemDropSpawner.cs`، `Items/WorldItemPickup.cs` |
| پرچم‌های پیش‌فرض | `ItemFlags.Common` جای `All` |

**قواعد ثابت:**

| قاعده | چرا |
| --- | --- |
| برداشتن از زمین همان `ItemInstance` را برمی‌گرداند | مهر و حالت سند نباید با یک رفت‌وبرگشت عوض شود |
| فهرست فعل از `CanDo` می‌آید، نه از یک لیست ثابت | فعل غیرممکن نباید دیده شود |
| `use` و `equip` صریح در `flags` | پوکه نباید دکمه‌ی استفاده داشته باشد |

`ItemSelfTest`: **۴۴ تست** (۳۶ قبلی + ۸ تازه).

سند: `docs/36-item-verbs-and-drops.md`

## ۳۸. انداختن کار نمی‌کرد — علت و اصلاح (جلسه‌ی ۲۹)

گزارش: «باز کردن و خواندن کار کرد، انداختن نه؛ چیزی روی زمین نمی‌آمد.»

**علت اثبات‌شده (از کد، نه حدس):** `ItemSelfTest.Run()` در `finally` این خط را
داشت: `ItemEvents.ClearAll();`. چون تست با `Run On Start` اجرا می‌شد، **همه‌ی**
مشترک‌های رویداد — از جمله `ItemDropSpawner` — پاک می‌شدند و انداختن تا آخر آن
Play هیچ شنونده‌ای نداشت.

| اصلاح | فایل |
| --- | --- |
| تست فقط مشترکِ خودش را برمی‌دارد | `Items/ItemSelfTest.cs` |
| تست تازه: «مشترکِ بیرونی بعد از تست زنده می‌ماند» | `Items/ItemSelfTest.cs` |
| `Ensure()` خودکار میزبان انداختن را می‌سازد | `Items/ItemDropSpawner.cs` |
| نقطه‌ی افتادن از بازیکن + Raycast به زمین | `Items/ItemDropSpawner.cs` |
| سوئیچ `Spawning` تا تست در صحنه مکعب نریزد | `Items/ItemDropSpawner.cs` |

**دو اصلاح جانبی از خروجی تست خودت:**

| نشانه | معنی | اصلاح |
| --- | --- | --- |
| `examine, equip, unequip, consume, drop` برای کلت | «مصرف» و «جدا کردن» پیش‌فرض الکی داشتند | هر دو فعل بدون قاعده نمایش داده نمی‌شوند |
| کلت پر: ۵ فعل | چهار خانه کم بود | نوار به ۵ خانه و کلید `۵` رسید |

**قاعده‌ی ثابت:** هیچ تست خودکار اجازه ندارد وضعیت مشترک بازی را پاک کند؛
فقط چیزی را که خودش ساخته برمی‌دارد.

## ۳۹. نام آیتم باید از وضعیت بیاید (جلسه‌ی ۳۰)

گزارش: «پرونده را باز کردم، باز هم نوشته مهرشده.»

نام کارت از `ItemDefinition.Name` (تعریف ثابت) می‌آمد و باز شدن مهر فقط
`state.sealed` را عوض می‌کرد؛ پس رابط چیزی نمی‌فهمید.

| تغییر | فایل |
| --- | --- |
| `title.if` / `title.fa` / `title.en` (+ شماره) | `Items/ItemCatalog.cs`، `Resources/Items/items.txt` |
| `ItemTitleVariant` | `Items/ItemRule.cs` |
| `ItemDefinition.Title(view, persian)` | `Items/ItemDefinition.cs` |
| `ItemRuntime.Title(instance, persian)` | `Items/ItemRuntime.cs` |
| کارت، خط شرح و کارت تعامل زمین | `UI/Pause/CharacterItemsSection.cs`، `Items/WorldItemPickup.cs` |
| تست نام در سه حالت | `Items/ItemSelfTest.cs` (۴۹ تست) |

**قاعده:** هیچ متنی در رابط نباید مستقیماً از تعریف ثابت خوانده شود اگر آن
آیتم وضعیت دارد؛ از `ItemRuntime.Title` بخوان.

## ۴۰. پرونده‌ی داستان، یک واقعیت (جلسه‌ی ۳۱)

دو جدایی رفع شد:

| جدایی | اصلاح |
| --- | --- |
| آبجکت روی میز یک `_opened` جدا داشت و آیتم جیب از `state.sealed` بی‌خبر بود | `SceneItemBinding`: هر آبجکت یک نمونه می‌گیرد؛ برداشتن = `Move` همان نمونه |
| آیتم `flag.letter.opened` می‌نوشت، داستان `flag.file.opened` می‌خواند | `items.txt` با `ontake.do` / `ondrop.do` / `open.do` همان نام‌های داستان را می‌نویسد |

| فایل | تغییر |
| --- | --- |
| `Items/SceneItemBinding.cs` | تازه: پیوند آبجکت صحنه به نمونه |
| `Interaction/PickupInteractable.cs` | `BoundInstance`، برداشتن با `Move`، نام کارت از `ItemRuntime.Title` |
| `Interaction/FileInteractable.cs` | باز کردن/خواندن از طریق `ItemRuntime`؛ مسیر سند از تعریف |
| `Resources/Items/items.txt` | `ontake.do`، `ondrop.do`، `flag.file.*` |
| `tools/itemcheck.py` | `ontake.do` / `ondrop.do` بررسی می‌شوند |

`ItemSelfTest`: **۵۳ تست**.

**قاعده‌ی ثابت:** هیچ آبجکت صحنه نباید وضعیت آیتم را در فیلد خودش نگه دارد؛
وضعیت در `ItemInstance` است و آبجکت فقط به آن وصل می‌شود.

سند: `docs/37-story-item-link.md`

