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
