# RTLTMPro (runtime فقط)

کپی از [pnarimani/RTLTMPro](https://github.com/pnarimani/RTLTMPro) — مجوز MIT (`LICENSE.txt`).

فقط اسکریپت‌های زمان اجرا این‌جاست تا متن فارسی داخل Unity درست شکل بگیرد
(حروف به هم بچسبند) و راست‌به‌چپ چیده شود. پوشه‌های `Fonts`، `Scenes`، `Tests`،
`Ranges`، `Resources` و `Editor` عمداً نیامده‌اند: کاری با رابط بازی نمی‌کنند.

بدون `.asmdef` گذاشته شده تا مستقیم داخل `Assembly-CSharp` کامپایل شود؛ برای همین
`UiKit` و `DialogueUI` با همان کدی که از قبل داشتند (`Assembly.GetType`) پیدایش می‌کنند.

**این پوشه را دستی ویرایش نکن.** به‌روزرسانی یعنی جایگزینی همین فایل‌ها با نسخه‌ی تازه.
