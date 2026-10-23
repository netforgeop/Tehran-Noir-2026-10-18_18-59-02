using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace TehranNoir.UI
{
    /// <summary>
    /// ابزارهای مشترک ساخت رابط در زمان اجرا: گره، متن راست‌به‌چپ، شکل‌های آماده.
    /// همه‌ی اندازه‌ها در واحد کانواس ۱۹۲۰×۱۰۸۰ هستند.
    /// </summary>
    internal static class UiKit
    {
        /* ---------- پالت ---------- */

        public static readonly Color Gold = new Color(0.941f, 0.847f, 0.349f);
        public static readonly Color Idle = new Color(0.741f, 0.741f, 0.741f);
        public static readonly Color RowIdle = new Color(0.894f, 0.894f, 0.894f);
        public static readonly Color Rule = new Color(0.227f, 0.227f, 0.227f);
        public static readonly Color Dim = new Color(0.55f, 0.55f, 0.55f);

        private static Type _rtlType;
        private static bool _rtlSearched;
        private static Sprite _rounded;

        /* ---------- گره ---------- */

        public static RectTransform Node(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>جعبه با مبدأ بالا-چپِ والد؛ y به سمت پایین اندازه‌گیری می‌شود.</summary>
        public static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = Node(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /* ---------- متن ---------- */

        public static TMP_Text Text(string name, Transform parent, float size, Color color, TMP_FontAsset font, bool farsi)
        {
            RectTransform rect = Node(name, parent);
            Stretch(rect);
            return MakeText(rect.gameObject, size, color, font, farsi);
        }

        public static TMP_Text MakeText(GameObject gameObject, float size, Color color, TMP_FontAsset font, bool farsi)
        {
            TMP_Text text = AddTextComponent(gameObject);

            if (font != null) text.font = font;

            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            SetRtl(text, farsi);
            return text;
        }

        private static TMP_Text AddTextComponent(GameObject gameObject)
        {
            Type rtl = FindRtlTextType();

            if (rtl != null)
            {
                Component component = gameObject.AddComponent(rtl);
                TMP_Text text = component as TMP_Text;

                if (text != null) return text;
            }

            return gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static void SetRtl(TMP_Text text, bool farsi)
        {
            if (text == null) return;

            Component rtl = FindRtlComponent(text.gameObject);
            if (rtl == null) return;

            SetBoolMember(rtl, "Farsi", farsi);
            SetBoolMember(rtl, "FixTags", true);
        }

        private static Type FindRtlTextType()
        {
            if (_rtlSearched) return _rtlType;

            _rtlSearched = true;

            _rtlType = typeof(UiKit).Assembly.GetType("RTLTMPro.RTLTextMeshPro", false);
            if (_rtlType != null) return _rtlType;

            _rtlType = Type.GetType("RTLTMPro.RTLTextMeshPro, RTLTMPro", false);
            return _rtlType;
        }

        private static Component FindRtlComponent(GameObject gameObject)
        {
            Type rtl = FindRtlTextType();
            if (rtl == null) return null;

            return gameObject.GetComponent(rtl);
        }

        private static void SetBoolMember(Component component, string name, bool value)
        {
            if (component == null) return;

            Type type = component.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
            {
                property.SetValue(component, value);
                return;
            }

            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(component, value);
        }

        /* ---------- شکل ---------- */

        /// <summary>مربع گوشه‌گرد سفید و نُه‌تکه؛ با Image.type = Sliced هر اندازه‌ای درست است.</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_rounded != null) return _rounded;
                _rounded = BuildRoundedRect();
                return _rounded;
            }
        }

        private static Sprite BuildRoundedRect()
        {
            const int size = 64;
            const int radius = 16;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.name = "UiRounded";

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0f);
                    float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = "UiRounded";
            return sprite;
        }
    }
}
