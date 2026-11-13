using UnityEngine;

namespace TehranNoir.World
{
    /// <summary>
    /// نقشِ یک تکه از ساختمان در برش.
    ///
    /// `Floor` یعنی «خودِ این طبقه»؛ از کف تا زیرِ سقف.
    /// `Ceiling` یعنی «سقفِ بالای این طبقه»؛ همان دالِ طبقه‌ی بعد.
    ///
    /// تفاوتشان یک مقایسه است و همان، تفاوتِ «طبقه را ببین» و «سقف را بردار» را
    /// می‌سازد. نگاه کن به `IsVisibleAt`.
    /// </summary>
    public enum CutawayRole
    {
        Floor = 0,
        Ceiling = 1
    }

    /// <summary>
    /// یک تکه از ساختمان که با برش خاموش و روشن می‌شود.
    ///
    /// چرا روی ریشه‌ی گروه است و نه روی هر جعبه؟ چون یک طبقه چهل جعبه است و
    /// چهل کامپوننت یعنی چهل `GetComponentsInChildren` در هر تغییر. یک
    /// کامپوننت روی ریشه، همه‌ی رندررهای زیرش را یک‌جا برمی‌دارد.
    ///
    /// قاعده: تکه‌ها تودرتو نباشند. هر تکه فرزندِ مستقیمِ ریشه‌ی ساختمان است.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutawayPiece : MonoBehaviour
    {
        [Tooltip("Floor = خودِ طبقه · Ceiling = سقفِ بالای همین طبقه")]
        [SerializeField] private CutawayRole role = CutawayRole.Floor;

        [Tooltip("شماره‌ی طبقه؛ لابی صفر است و طبقه‌ی اول یکی")]
        [SerializeField] private int level;

        private Renderer[] renderers;

        public CutawayRole Role { get { return role; } }

        public int Level { get { return level; } }

        /// <summary>
        /// رندررها یک بار جمع می‌شوند. فراخوانی دوباره ضرری ندارد ولی لازم هم
        /// نیست؛ فقط مدیر ساختمان در `Awake` صدایش می‌زند.
        /// </summary>
        public void Cache()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// آیا این تکه وقتی بازیکن در طبقه‌ی `currentLevel` است دیده شود؟
        ///
        /// سقف: فقط اگر بالاتر از بازیکن باشد. طبقه‌ی هم‌سطح، سقفِ بالای سرِ
        /// خودش است و باید برود، وگرنه دوربینِ بالاسری فقط یک تخته‌ی قهوه‌ای
        /// می‌بیند.
        ///
        /// طبقه: اگر هم‌سطح یا پایین‌تر باشد. طبقه‌های بالاتر می‌روند.
        /// </summary>
        public bool IsVisibleAt(int currentLevel)
        {
            if (role == CutawayRole.Ceiling)
                return level > currentLevel;

            return level <= currentLevel;
        }

        /// <summary>
        /// قاعده‌ی کامل، با «داخل/بیرون» هم حساب شده. بیرون از ساختمان هیچ
        /// تکه‌ای پنهان نمی‌شود؛ ساختمان باید از خیابان کامل دیده شود.
        /// </summary>
        public bool IsVisible(bool inside, int currentLevel)
        {
            return !inside || IsVisibleAt(currentLevel);
        }

        /// <summary>
        /// مقدار دادن به role و level از کد — برای وقتی که تکه در زمان اجرا
        /// ساخته می‌شود (تست، یا ساختمانی که با اسکریپت ساخته شود).
        /// در صحنه، همین دو فیلد از Inspector خوانده می‌شوند.
        /// </summary>
        public void Configure(CutawayRole pieceRole, int pieceLevel)
        {
            role = pieceRole;
            level = pieceLevel;
            renderers = null;
        }

        /// <summary>
        /// فقط رندرر خاموش می‌شود، نه کولایدر. اگر کولایدر هم خاموش شود،
        /// بازیکن از طبقه‌ای که سقفش برداشته شده می‌افتد بیرون؛ و کولایدرِ
        /// نامرئی هیچ ضرری به تصویر نمی‌زند.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (renderers == null) Cache();

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = visible;
            }
        }
    }
}
