# مرحله ۶ — منوی اصلی به سبک Mafia

همان منویی که در Godot ساخته بودی، این بار در Unity. ظاهر از `main_menu.tscn`
گرفته شده و الگو همان Mafia: Definitive Edition است.

## چه چیزی ساخته می‌شود

| بخش | مقدار (از فایل Godot) |
| --- | --- |
| پس‌زمینه | `bgmenu.png` تمام‌صفحه و **آینه‌شده** (پرچم سمت چپ می‌آید، مثل `flip_h` در Godot) |
| جای ستون منو | `x = 270`، `y = -286` از گوشه‌ی بالا-چپ |
| عنوان | «HOME» طلایی `#A88030`، سایز `90`، وزن Black |
| آیتم‌ها | سایز `56`، وزن Bold، خاکستری `#969189` |
| آیتم انتخاب‌شده | نوار تیره‌ی نیمه‌شفاف + متن `#EEEFEC` |
| فاصله‌ی آیتم‌ها | `8` |
| لوزی + خط | `menu end.png`، **ثابت** زیر لیست |

سه آیتم: **SANDBOX TEST** / **OPTIONS** / **QUIT**
(«شروع بازی» و «ادامه» بعداً که سیستم مراحل و سیو آمد اضافه می‌شوند.)

زبان با `F1` عوض می‌شود: انگلیسی با Roboto، فارسی با Vazirmatn. این کلید موقتی است
تا منوی تنظیمات ساخته شود؛ بعداً همان‌جا می‌رود.

## فایل‌های تازه

| مسیر | چیست |
| --- | --- |
| `Art/Menu/bgmenu.png` | پس‌زمینه (1672×941) |
| `Art/Menu/menu_end.png` | لوزی + خط (1695×130) |
| `Art/Fonts/Source/Roboto-Black.ttf` | وزن عنوان |
| `Art/Fonts/Source/Roboto-Bold.ttf` | وزن آیتم‌ها |
| `Art/Fonts/Source/Vazirmatn-Black.ttf` | وزن عنوان فارسی |
| `Art/Fonts/Source/Vazirmatn-Bold.ttf` | وزن آیتم‌های فارسی |
| `Scripts/UI/MainMenuUI.cs` | منو |

هر دو فونت مجوز آزاد دارند (Roboto: Apache 2.0، Vazirmatn: OFL).

## کار ۱ — نوع تصویرها

۱. `bgmenu.png` را در Project انتخاب کن → Inspector → `Texture Type` = **Sprite (2D and UI)** → `Apply`
۲. همین کار را برای `menu_end.png` بکن

## کار ۲ — ساخت فونت اَسِت‌ها (۴ عدد)

`Window > TextMeshPro > Font Asset Creator` و برای هر فونت:

| Source Font File | Atlas | Character Set | نام فایل خروجی |
| --- | --- | --- | --- |
| `Roboto-Black.ttf` | 1024×1024 | `ASCII` | `Roboto-Black SDF` |
| `Roboto-Bold.ttf` | 1024×1024 | `ASCII` | `Roboto-Bold SDF` |
| `Vazirmatn-Black.ttf` | 1024×1024 | `Unicode Range (Hex)` = `0600-06FF, 200C` بعد `ASCII` هم اضافه کن | `Vazirmatn-Black SDF` |
| `Vazirmatn-Bold.ttf` | 1024×1024 | همان بالا | `Vazirmatn-Bold SDF` |

برای فونت فارسی، در Font Asset Creator:

1. `Character Set` = **Unicode Range (Hex)**
2. در کادر بنویس: `0600-06FF,200C,0020-007E`
3. `Render Mode` = `SDFAA`
4. `Generate Font Atlas` → `Save` → همه را در `Assets/_Project/Art/Fonts/` ذخیره کن

اگر حروف فارسی در آتلاس جا نشدند، `Atlas Resolution` را `2048×2048` بگذار.

## کار ۳ — صحنه

۱. `File > New Scene` → **Empty**
۲. `GameObject > Create Empty` → نام: `MainMenu`
۳. `Add Component` → `Main Menu UI`
۴. فیلدها:

| فیلد | مقدار |
| --- | --- |
| `Background Sprite` | `bgmenu` |
| `Marker Sprite` | `menu_end` |
| `English Fonts > Title` | `Roboto-Black SDF` |
| `English Fonts > Label` | `Roboto-Bold SDF` |
| `Persian Fonts > Title` | `Vazirmatn-Black SDF` |
| `Persian Fonts > Label` | `Vazirmatn-Bold SDF` |
| `Persian` | تیک خورده یا نه، سلیقه‌ای |
| `Game Scene Name` | `Sandbox` |

۵. `Ctrl + S` → `Assets/_Project/Scenes/MainMenu`
۶. `File > Build Profiles` → `Scene List`:
   - `MainMenu` **index 0**
   - `Sandbox` **index 1**

## ورودی‌ها

| کلید | کار |
| --- | --- |
| `↑` `↓` یا `W` `S` | جابه‌جایی بین آیتم‌ها |
| `Enter` / `Space` | انتخاب |
| موس | هاور و کلیک |
| `F1` | عوض کردن زبان (موقت) |
| `Esc` | خروج |

## آزمایش

۱. صحنه‌ی `MainMenu` → `Play` → تصویر دفتر باید تمام صفحه باشد، پرچم سمت چپ.
۲. `HOME` طلایی، سه آیتم خاکستری، لوزی + خط زیر لیست.
۳. با `↓` بین آیتم‌ها حرکت کن → نوار تیره پشت آیتم انتخاب‌شده.
۴. `F1` → متن‌ها فارسی شوند و **همان استایل** بماند.
۵. `SANDBOX TEST` → صحنه‌ی `Sandbox` باز شود.

## نکته‌ها

- اگر پیام `صحنه‌ی Sandbox در Build Profiles نیست` آمد، کار ۳ بند ۶ را انجام بده.
- اگر متن‌ها با فونت پیش‌فرض دیده شدند، یعنی فونت اَسِت‌ها در Inspector وصل نشده‌اند.
- `OPTIONS` فعلاً فقط یک پیام کوتاه نشان می‌دهد تا صفحه‌ی تنظیمات ساخته شود.
