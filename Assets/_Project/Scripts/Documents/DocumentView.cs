using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TehranNoir.Cameras;
using TehranNoir.Core;
using TehranNoir.Dialogue;
using TehranNoir.Inputs;
using TehranNoir.Interaction;
using TehranNoir.Player;
using TehranNoir.UI;

namespace TehranNoir.Documents
{
    /// <summary>
    /// نمایش یک سند کاغذی روی صفحه: برگه، سرصفحه، متن و مهر.
    ///
    /// الگو یکی است و از تگ‌های فایل سند پر می‌شود؛ برای سند بعدی فقط یک فایل متنی می‌نویسیم.
    /// دو قالب دارد: «نامه»ی رسمی با نشان و شماره و تاریخ، و «یادداشت» کوچک و بی‌سرصفحه.
    ///
    /// مثل دیالوگ باز می‌شود (Esc، E، Space یا B می‌بندد) ولی قالبش فرق دارد:
    /// یک برگه‌ی واقعی وسط صفحه، نه نوار پایین. همه‌چیز در زمان اجرا ساخته می‌شود؛
    /// در صحنه هیچ چیدمانی لازم نیست.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DocumentView : MonoBehaviour
    {
        public static DocumentView Current { get; private set; }

        public static bool IsOpen { get { return Current != null && Current._open; } }

        /* ============================================================
           تنظیمات
           ============================================================ */

        [Header("فونت")]
        [Tooltip("خالی بماند تا از Dialogue UI صحنه گرفته شود")]
        [SerializeField] private TMP_FontAsset font;

        [Tooltip("برای متن انگلیسی؛ خالی = همان فونت بالا")]
        [SerializeField] private TMP_FontAsset latinFont;

        [Header("توقف بازی")]
        [Tooltip("وقتی سند باز است خاموش می‌شوند. خالی = خودکار پیدا می‌شود")]
        [SerializeField] private MonoBehaviour[] freezeWhileOpen = new MonoBehaviour[0];

        [Header("ورودی")]
        [SerializeField] private bool releaseCursorWhileOpen = true;

        [Header("اندازه")]
        [SerializeField] private float fadeSeconds = 0.16f;
        [SerializeField] private float letterWidth = 760f;
        [SerializeField] private float noteWidth = 640f;

        /* ---------- پالتِ کاغذ ---------- */

        private static readonly Color BackdropColor = new Color(0.035f, 0.033f, 0.030f, 0.82f);
        private static readonly Color InkColor = new Color(0.165f, 0.137f, 0.118f, 1f);
        private static readonly Color InkMuted = new Color(0.380f, 0.333f, 0.286f, 1f);
        private static readonly Color StampColor = new Color(0.560f, 0.145f, 0.110f, 0.78f);
        private static readonly Color HintColor = new Color(0.470f, 0.420f, 0.360f, 1f);
        private static readonly Color RuleColor = new Color(0.35f, 0.30f, 0.25f, 0.35f);

        /* ---------- وضعیت ---------- */

        private CanvasGroup _group;
        private RectTransform _root;
        private RawImage _paper;
        private RectTransform _paperRect;
        private RectTransform _shadowRect;
        private RectTransform _content;
        private TMP_Text _body;
        private TMP_Text _stampText;
        private RectTransform _stampRect;

        private bool _built;
        private bool _open;
        private int _openedFrame;
        private bool _closing;
        private Action _onClosed;

        private bool[] _freezeSnapshot;
        private CursorLockMode _savedLockMode;
        private bool _savedCursorVisible;
        private bool _cursorReleased;

        private static Texture2D _ringTexture;
        private static Texture2D _paperCache;
        private static Texture2D _emblemCache;

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            if (Current == null) Current = this;

            Build();
            _root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (!_open || _closing) return;
            if (Time.frameCount == _openedFrame) return;

            if (ClosePressed()) Close();
        }

        /* ============================================================
           API
           ============================================================ */

        /// <summary>سند را باز می‌کند. اگر سندی باز است، درخواست تازه نادیده گرفته می‌شود.</summary>
        public void Show(DocumentText document, Action onClosed = null)
        {
            if (document == null)
            {
                Debug.LogError("[Document] سند خالی است.");
                if (onClosed != null) onClosed();
                return;
            }

            if (_open) return;

            if (!_built) Build();

            _onClosed = onClosed;
            _closing = false;
            _open = true;
            _openedFrame = Time.frameCount;

            BuildContent(document);

            _root.gameObject.SetActive(true);

            ApplyFreeze(true);
            ReleaseCursor(true);

            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }

        public void Close()
        {
            if (!_open || _closing) return;

            _open = false;
            _closing = true;

            StopAllCoroutines();
            StartCoroutine(FadeOutAndHide());
        }

        /* ============================================================
           ورودی
           ============================================================ */

        private static bool ClosePressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) return true;
                if (keyboard.eKey.wasPressedThisFrame) return true;
                if (keyboard.spaceKey.wasPressedThisFrame) return true;
                if (keyboard.enterKey.wasPressedThisFrame) return true;
            }

            if (gamepad != null)
            {
                if (gamepad.buttonEast.wasPressedThisFrame) return true;
                if (gamepad.buttonSouth.wasPressedThisFrame) return true;
            }

            return false;
        }

        /* ============================================================
           ساختِ پایه
           ============================================================ */

        private void Build()
        {
            if (_built) return;
            _built = true;

            GameObject canvasObject = new GameObject("DocumentCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _root = Node("Root", canvasObject.transform);
            Stretch(_root);

            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // پس‌زمینه‌ی تیره؛ Image بدون اسپرایت یک چهارضلعی رنگی می‌کشد
            Image backdrop = _root.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            // سایه: همان بافت کاغذ، سیاه و کمی جابه‌جا
            _shadowRect = Node("Shadow", _root);
            _shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _shadowRect.pivot = new Vector2(0.5f, 0.5f);

            RawImage shadow = _shadowRect.gameObject.AddComponent<RawImage>();
            shadow.texture = PaperTexture();
            shadow.color = new Color(0f, 0f, 0f, 0.40f);
            shadow.raycastTarget = false;

            _paperRect = Node("Sheet", _root);
            _paperRect.anchorMin = new Vector2(0.5f, 0.5f);
            _paperRect.anchorMax = new Vector2(0.5f, 0.5f);
            _paperRect.pivot = new Vector2(0.5f, 0.5f);

            _paper = NodeRaw("Paper", _paperRect);
            Stretch(_paper.rectTransform);
            _paper.texture = PaperTexture();
            _paper.color = Color.white;
            _paper.raycastTarget = false;

            _content = Node("Content", _paper.rectTransform);
            Stretch(_content);

            RectTransform hintRect = Node("Hint", _content);
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.offsetMin = new Vector2(0f, 26f);
            hintRect.offsetMax = new Vector2(0f, 50f);

            TMP_Text hintText = UiKit.MakeText(hintRect.gameObject, 14f, HintColor, Font, Localization.Persian);
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.text = Localization.Get("doc.close");

            _stampRect = Node("Stamp", _content);
            _stampRect.anchorMin = new Vector2(0f, 0f);
            _stampRect.anchorMax = new Vector2(0f, 0f);
            _stampRect.pivot = new Vector2(0.5f, 0.5f);

            RawImage ring = _stampRect.gameObject.AddComponent<RawImage>();
            ring.texture = RingTexture();
            ring.color = StampColor;
            ring.raycastTarget = false;

            // نشان کوچک داخل مهر؛ با نسبت لنگر می‌چسبد، پس با هر اندازه‌ای می‌ماند
            Texture2D emblemTexture = EmblemTexture();

            if (emblemTexture != null)
            {
                RawImage stampEmblem = NodeRaw("StampEmblem", _stampRect);
                stampEmblem.texture = emblemTexture;
                stampEmblem.color = new Color(0.30f, 0.24f, 0.19f, 0.55f);
                stampEmblem.raycastTarget = false;

                RectTransform stampEmblemRect = stampEmblem.rectTransform;
                stampEmblemRect.anchorMin = new Vector2(0.30f, 0.55f);
                stampEmblemRect.anchorMax = new Vector2(0.70f, 0.88f);
                stampEmblemRect.offsetMin = Vector2.zero;
                stampEmblemRect.offsetMax = Vector2.zero;
                stampEmblemRect.pivot = new Vector2(0.5f, 0.5f);
            }

            RectTransform stampLabelRect = Node("StampText", _stampRect);
            Stretch(stampLabelRect);
            stampLabelRect.offsetMin = new Vector2(20f, 18f);
            stampLabelRect.offsetMax = new Vector2(-20f, -70f);

            _stampText = UiKit.MakeText(stampLabelRect.gameObject, 15f, StampColor, Font, Localization.Persian);
            _stampText.alignment = TextAlignmentOptions.Center;
        }

        /* ============================================================
           پر کردن الگو
           ============================================================ */

        private void BuildContent(DocumentText document)
        {
            ClearContent();

            bool persian = Localization.Persian;
            TMP_FontAsset textFont = persian || latinFont == null ? Font : latinFont;

            bool isNote = document.Template == DocumentTemplate.Note;

            float width = isNote ? noteWidth : letterWidth;
            float height = width * 1200f / 880f;

            Vector2 sheetPosition = new Vector2(isNote ? -110f : 0f, 0f);
            Quaternion sheetRotation = Quaternion.Euler(0f, 0f, isNote ? -1.4f : 0f);

            _paperRect.sizeDelta = new Vector2(width, height);
            _paperRect.anchoredPosition = sheetPosition;
            _paperRect.localRotation = sheetRotation;

            if (_shadowRect != null)
            {
                _shadowRect.sizeDelta = new Vector2(width, height);
                _shadowRect.anchoredPosition = sheetPosition + new Vector2(9f, -11f);
                _shadowRect.localRotation = sheetRotation;
            }

            string meta = BuildMeta(document, persian);

            bool hasHeader = document.Agency.Length > 0 || document.Office.Length > 0
                || document.Title.Length > 0 || meta.Length > 0;

            float padX = isNote ? 46f : 62f;
            float bodyTop = isNote ? (hasHeader ? 208f : 68f) : 300f;
            float bodyBottom = isNote ? 96f : 112f;

            if (document.ShowEmblem)
            {
                Texture2D texture = EmblemTexture();

                if (texture != null)
                {
                    RawImage emblem = NodeRaw("Emblem", _content);
                    emblem.texture = texture;
                    emblem.color = new Color(0.30f, 0.24f, 0.19f, 0.92f);
                    emblem.raycastTarget = false;

                    float emblemWidth = 76f;
                    float emblemHeight = emblemWidth * texture.height / (float)texture.width;

                    Place(emblem.rectTransform, (width - emblemWidth) * 0.5f, 44f, emblemWidth, emblemHeight);
                }
            }

            if (document.Agency.Length > 0)
                CenteredLine("Agency", DocumentText.Pick(document.Agency, persian), 23f, InkColor,
                    isNote ? 66f : 138f, textFont);

            if (document.Office.Length > 0)
                CenteredLine("Office", DocumentText.Pick(document.Office, persian), 15f, InkMuted,
                    isNote ? 96f : 170f, textFont);

            if (!isNote)
            {
                Rule(26f, 202f, width);
                Rule(26f, 205f, width);
            }

            if (meta.Length > 0)
                CenteredLine("Meta", meta, 14f, InkMuted, isNote ? 118f : 216f, textFont);

            if (document.Title.Length > 0)
                CenteredLine("Title", DocumentText.Pick(document.Title, persian), isNote ? 24f : 28f,
                    InkColor, isNote ? 150f : 252f, textFont, FontStyles.Bold);

            RectTransform bodyRect = Node("Body", _content);
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(padX, bodyBottom);
            bodyRect.offsetMax = new Vector2(-padX, -bodyTop);

            _body = UiKit.MakeText(bodyRect.gameObject, 21f, InkColor, textFont, persian);
            _body.alignment = persian ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            _body.lineSpacing = 8f;
            _body.paragraphSpacing = 12f;
            _body.fontStyle = FontStyles.Normal;
            _body.text = document.BuildBody(persian);

            FitBody(width - padX * 2f, height - bodyTop - bodyBottom, isNote);

            if (document.ShowStamp)
            {
                float stampSize = isNote ? 132f : 150f;

                _stampRect.gameObject.SetActive(true);
                _stampRect.sizeDelta = new Vector2(stampSize, stampSize);
                // مهر پایین-چپ برگه می‌نشیند: جای امضا در نامه‌های آن دوره
                _stampRect.anchoredPosition = new Vector2(
                    stampSize * 0.5f + (isNote ? 58f : 74f),
                    isNote ? 132f : 150f);
                _stampRect.localRotation = Quaternion.Euler(0f, 0f, persian ? -9f : 9f);

                _stampText.text = DocumentText.Pick(
                    string.IsNullOrEmpty(document.Stamp) ? "محرمانه | Secret" : document.Stamp, persian);
            }
            else
            {
                _stampRect.gameObject.SetActive(false);
            }
        }

        private static string BuildMeta(DocumentText document, bool persian)
        {
            string number = DocumentText.Pick(document.Number, persian);
            string date = DocumentText.Pick(document.Date, persian);

            if (number.Length == 0 && date.Length == 0) return string.Empty;

            string numberLabel = persian ? "شماره" : "No.";
            string dateLabel = persian ? "تاریخ" : "Date";

            string left = number.Length > 0 ? numberLabel + " " + number : string.Empty;
            string right = date.Length > 0 ? dateLabel + " " + date : string.Empty;

            if (left.Length == 0) return right;
            if (right.Length == 0) return left;

            return left + "      " + right;
        }

        /// <summary>متن بلند یعنی فونت کوچک‌تر؛ آن‌قدر که در برگه جا شود.</summary>
        private void FitBody(float width, float height, bool isNote)
        {
            if (_body == null || width <= 1f || height <= 1f) return;

            float size = isNote ? 20f : 21f;
            const float smallest = 13f;

            while (size > smallest)
            {
                _body.fontSize = size;
                _body.ForceMeshUpdate();

                if (_body.preferredHeight <= height) return;

                size -= 0.5f;
            }

            _body.fontSize = smallest;
            _body.ForceMeshUpdate();

            if (_body.preferredHeight > height)
                Debug.LogWarning("[Document] متن از برگه بلندتر است و بریده می‌شود: "
                    + _body.preferredHeight.ToString("0") + " > " + height.ToString("0"));
        }

        /* ============================================================
           تکه‌های تصویری
           ============================================================ */

        private void CenteredLine(string name, string text, float size, Color color, float top,
            TMP_FontAsset textFont, FontStyles style = FontStyles.Normal)
        {
            if (string.IsNullOrEmpty(text)) return;

            RectTransform rect = Node(name, _content);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(44f, 0f);
            rect.offsetMax = new Vector2(-44f, 0f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, size * 1.7f);

            TMP_Text label = UiKit.MakeText(rect.gameObject, size, color, textFont, Localization.Persian);
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = style;
            label.text = text;
        }

        private void Rule(float x, float top, float width)
        {
            RectTransform rect = Node("Rule", _content);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = new Vector2(width - x * 2f, 1f);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = RuleColor;
            image.raycastTarget = false;
        }

        private void ClearContent()
        {
            if (_content == null) return;

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Transform child = _content.GetChild(i);

                // مهر و راهنمای پایین دوباره استفاده می‌شوند
                if (child == _stampRect) continue;
                if (child.name == "Hint") continue;

                Destroy(child.gameObject);
            }
        }

        /* ============================================================
           محو شدن
           ============================================================ */

        private IEnumerator FadeIn()
        {
            float time = 0f;

            while (time < fadeSeconds)
            {
                time += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(time / fadeSeconds);
                yield return null;
            }

            _group.alpha = 1f;
        }

        private IEnumerator FadeOutAndHide()
        {
            float time = 0f;

            while (time < fadeSeconds)
            {
                time += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(time / fadeSeconds);
                yield return null;
            }

            _group.alpha = 0f;
            _root.gameObject.SetActive(false);

            ReleaseCursor(false);

            Action callback = _onClosed;
            _onClosed = null;
            _closing = false;

            // یک فریم صبر می‌کند تا کلیدِ بستن در همان فریم دوباره خوانده نشود
            yield return null;
            ApplyFreeze(false);

            if (callback != null) callback();
        }

        /* ============================================================
           توقف و نشانگر
           ============================================================ */

        private void ApplyFreeze(bool frozen)
        {
            if (freezeWhileOpen == null || freezeWhileOpen.Length == 0)
                freezeWhileOpen = FindDefaults();

            if (freezeWhileOpen == null || freezeWhileOpen.Length == 0) return;

            if (frozen)
            {
                _freezeSnapshot = new bool[freezeWhileOpen.Length];

                for (int i = 0; i < freezeWhileOpen.Length; i++)
                {
                    MonoBehaviour target = freezeWhileOpen[i];
                    if (target == null) continue;

                    _freezeSnapshot[i] = target.enabled;
                    target.enabled = false;
                }

                return;
            }

            if (_freezeSnapshot == null) return;

            for (int i = 0; i < freezeWhileOpen.Length && i < _freezeSnapshot.Length; i++)
            {
                if (freezeWhileOpen[i] == null) continue;
                freezeWhileOpen[i].enabled = _freezeSnapshot[i];
            }

            _freezeSnapshot = null;
        }

        private static MonoBehaviour[] FindDefaults()
        {
            return new MonoBehaviour[]
            {
                FindAnyObjectByType<PlayerController>(),
                FindAnyObjectByType<CameraOrbitController>(),
                FindAnyObjectByType<PlayerInputReader>(),
                FindAnyObjectByType<InteractionSensor>()
            };
        }

        private void ReleaseCursor(bool release)
        {
            if (!releaseCursorWhileOpen) return;

            if (release)
            {
                if (_cursorReleased) return;

                _savedLockMode = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                _cursorReleased = true;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (!_cursorReleased) return;

            Cursor.lockState = _savedLockMode;
            Cursor.visible = _savedCursorVisible;
            _cursorReleased = false;
        }

        /* ============================================================
           منابع
           ============================================================ */

        private TMP_FontAsset Font
        {
            get
            {
                if (font != null) return font;

                DialogueUI ui = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
                if (ui != null) font = ui.Font;

                if (font == null) font = TMP_Settings.defaultFontAsset;

                return font;
            }
        }

        private static Texture2D PaperTexture()
        {
            if (_paperCache == null) _paperCache = Resources.Load<Texture2D>("Documents/Images/paper");
            if (_paperCache == null) Debug.LogWarning("[Document] کاغذ پیدا نشد: Resources/Documents/Images/paper");
            return _paperCache;
        }

        private static Texture2D EmblemTexture()
        {
            if (_emblemCache == null) _emblemCache = Resources.Load<Texture2D>("Documents/Images/emblem");
            if (_emblemCache == null) Debug.LogWarning("[Document] نشان پیدا نشد: Resources/Documents/Images/emblem");
            return _emblemCache;
        }

        /// <summary>حلقه‌ی مهر: یک بار در کد ساخته و کش می‌شود، فایل تصویری لازم ندارد.</summary>
        private static Texture2D RingTexture()
        {
            if (_ringTexture != null) return _ringTexture;

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;

                    if (radius > 0.86f && radius < 0.965f) alpha = 1f;
                    else if (radius > 0.655f && radius < 0.690f) alpha = 0.9f;

                    if (alpha > 0f)
                    {
                        float edge = Mathf.Min(
                            Mathf.Abs(radius - 0.86f), Mathf.Abs(radius - 0.965f),
                            Mathf.Abs(radius - 0.655f), Mathf.Abs(radius - 0.690f));

                        alpha *= Mathf.Clamp01(edge / 0.010f);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 235f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            _ringTexture = texture;
            return texture;
        }

        /* ============================================================
           ابزار ساخت
           ============================================================ */

        private static RectTransform Node(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RawImage NodeRaw(string name, Transform parent)
        {
            RectTransform rect = Node(name, parent);
            return rect.gameObject.AddComponent<RawImage>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
