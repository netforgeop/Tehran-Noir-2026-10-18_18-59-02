#!/usr/bin/env bash
# همه‌ی بررسی‌های آفلاین قبل از تحویل.
#   bash tools/preflight.sh
set -u
root="${1:-Assets/_Project/Scripts}"
status=0

echo "۱) تداخل نام عضو (CS0102)"
python3 tools/cscheck.py "$root" || status=1

echo
echo "۲) نام‌های ناشناس (CS0103)"
python3 tools/symcheck.py "$root" || status=1

echo
echo "۳) تعداد آرگومان (CS1501/CS7036)"
python3 tools/argcheck.py "$root" || status=1

echo
echo "۴) using جاافتاده (CS0103 روی نام نوع)"
python3 tools/usingcheck.py "$root" || status=1

echo
echo "۵) ارجاع داستان به آیتم‌ها (شناسه‌ی ناشناس / پروجکشن دستی)"
python3 tools/storycheck.py || status=1

exit $status
