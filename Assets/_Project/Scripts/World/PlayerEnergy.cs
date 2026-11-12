using UnityEngine;
using TehranNoir.Characters;
using TehranNoir.Dialogue;
using TehranNoir.Inputs;

namespace TehranNoir.World
{
    /// <summary>
    /// استقامت بازیکن — همان نوار زردِ کمان.
    /// دویدن، پریدن و (بعداً) نشانه‌گیری از آن کم می‌کند؛ آرام‌شدن برمی‌گرداندش.
    ///
    /// قاعده‌ی اصلی این کلاس: **مصرف بیش از حد ممکن است.** صفر سقف نیست، کفِ آن ‎-۱۵۰‎ است.
    /// هر چه منفی‌تر، بدتر: دویدن کندتر، پرش کوتاه‌تر، نشانه‌گیری پراکنده‌تر، ترس بیشتر.
    /// اگر روی ‎-۱۵۰‎ بمانی، بعد از یک دقیقه جان هم شروع می‌کند به کم شدن و کاراکتر سنگین می‌شود.
    ///
    /// مقدار در WorldState نگه داشته می‌شود (`stat.stamina`)، پس پنل تست هم می‌تواند عوضش کند
    /// و دیالوگ‌ها با `[if] stat.stamina < 0` از آن بخوانند.
    ///
    /// نام کلاس از روزهای اول مانده و عوض نشده تا کامپوننتِ داخل صحنه نشکند.
    /// </summary>
    public sealed class PlayerEnergy : MonoBehaviour
    {
        [Header("استقامت")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float minStamina = -150f;
        [SerializeField] private float startStamina = 100f;

        [Header("مصرف")]
        [Tooltip("واحد در ثانیه، وقتی می‌دوی")]
        [SerializeField] private float sprintDrainPerSecond = 7f;

        [Tooltip("هزینه‌ی هر پرش")]
        [SerializeField] private float jumpCost = 12f;

        [Tooltip("واحد در ثانیه، وقتی نشانه گرفته‌ای (وقتی سلاح اضافه شد)")]
        [SerializeField] private float aimDrainPerSecond = 8f;

        [Tooltip("هزینه‌ی هر شلیک (وقتی سلاح اضافه شد)")]
        [SerializeField] private float shotCost = 6f;

        [Header("بازیابی")]
        [Tooltip("چند ثانیه بعد از آخرین مصرف، بازیابی شروع شود")]
        [SerializeField] private float restDelay = 0.9f;

        [Tooltip("واحد در ثانیه، وقتی استقامت مثبت است")]
        [SerializeField] private float regenPositive = 10f;

        [Tooltip("واحد در ثانیه، وقتی استقامت منفی است — اول بدهی، بعد شارژ")]
        [SerializeField] private float regenNegative = 5f;

        [Header("فرسودگی")]
        [Tooltip("چند ثانیه ماندن روی کف، جان را شروع کند")]
        [SerializeField] private float floorSecondsBeforeDamage = 60f;

        [Tooltip("واحد جان در ثانیه، وقتی روی کف مانده‌ای")]
        [SerializeField] private float healthDrainPerSecond = 1.2f;

        [Tooltip("هر چند ثانیه یک بار سنگینی به بازیکن دست می‌دهد")]
        [SerializeField] private float staggerEverySeconds = 2.5f;

        [Tooltip("هر سنگینی چند ثانیه طول می‌کشد")]
        [SerializeField] private float staggerDuration = 0.45f;

        [Header("اثرهای منفی")]
        [Tooltip("سرعت راه رفتن/دویدن در بدترین حالت — ضرب در مقدار پایه")]
        [SerializeField] private float worstMoveScale = 0.55f;

        [Tooltip("ارتفاع پرش در بدترین حالت")]
        [SerializeField] private float worstJumpScale = 0.6f;

        [Tooltip("پراکندگی نشانه‌گیری در بدترین حالت — بیشتر یعنی بدتر")]
        [SerializeField] private float worstAimSpread = 2.2f;

        [Tooltip("سرعت بازیابی جان در بدترین حالت — وقتی بازیابی جان اضافه شود")]
        [SerializeField] private float worstHealthRegen = 0.25f;

        [Tooltip("واحد ترس در ثانیه، روی کف استقامت")]
        [SerializeField] private float worstFearRise = 6f;

        [Header("رفتار")]
        [Tooltip("در طول دیالوگ استقامت عوض نمی‌شود")]
        [SerializeField] private bool freezeDuringDialogue = true;

        [Tooltip("بالای این سرعت، دویدن حساب می‌شود")]
        [SerializeField] private float sprintSpeedThreshold = 4.5f;

        private CharacterController _controller;
        private PlayerInputReader _input;
        private float _restTimer;
        private float _floorTimer;
        private float _staggerTimer;
        private float _staggerEnd;

        /// <summary>وقتی روی کف مانده‌ای و سنگینی شروع شده.</summary>
        public bool Staggering { get { return Time.time < _staggerEnd; } }

        /// <summary>آیا روی کف (‎-۱۵۰‎) گیر کرده‌ای.</summary>
        public bool AtFloor { get { return Current <= minStamina + 0.5f; } }

        public float Value { get { return Current; } }

        private float Current
        {
            get
            {
                WorldState world = World;
                if (world == null) return startStamina;

                DialogueValue value = world.Get(CharacterStats.StaminaKey);
                if (value.IsUndefined) return maxStamina;

                double number = value.AsNumber();
                if (double.IsNaN(number)) return maxStamina;

                return Mathf.Clamp((float)number, minStamina, maxStamina);
            }
            set
            {
                float clamped = Mathf.Clamp(value, minStamina, maxStamina);

                WorldState world = World;
                if (world == null) return;

                world.Set(CharacterStats.StaminaKey, (double)clamped);

                // کلید قدیمی energy برای دیالوگ‌های قبلی: همیشه ۰ تا ۱۰۰
                world.Set("energy", (double)Mathf.Clamp(clamped, 0f, maxStamina));
            }
        }

        private static WorldState World
        {
            get
            {
                return WorldStateProvider.Instance != null
                    ? WorldStateProvider.Instance.State
                    : null;
            }
        }

        /// <summary>۰ یعنی خسته، ۱ یعنی سرحال. برای هر ضریبی که از خستگی حساب می‌شود.</summary>
        private float Fatigue
        {
            get
            {
                float value = Current;
                if (value >= 0f) return 0f;

                return Mathf.Clamp01(-value / Mathf.Abs(minStamina));
            }
        }

        /* ---------- ضریب‌هایی که بازیکن و سلاح می‌خوانند ---------- */

        public float MoveMultiplier
        {
            get { return Mathf.Lerp(1f, worstMoveScale, Fatigue); }
        }

        public float JumpMultiplier
        {
            get { return Mathf.Lerp(1f, worstJumpScale, Fatigue); }
        }

        public float AimSpreadMultiplier
        {
            get { return Mathf.Lerp(1f, worstAimSpread, Fatigue); }
        }

        public float HealthRegenMultiplier
        {
            get { return Mathf.Lerp(1f, worstHealthRegen, Fatigue); }
        }

        /* ============================================================
           چرخه
           ============================================================ */

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
        }

        private void Start()
        {
            // اگر کلید هنوز در دنیا نیست، همین شروع اولین مقدار را می‌نویسد
            WorldState world = World;
            if (world != null && world.Has(CharacterStats.StaminaKey)) return;

            Current = startStamina;
        }

        private void Update()
        {
            if (freezeDuringDialogue && DialogueUI.Current != null) return;

            float delta = Time.deltaTime;
            float value = Current;

            // دویدن: از قصد بازیکن فهمیده می‌شود (تپ دویدن + ورودی حرکت)
            if (IsSprinting())
            {
                value -= sprintDrainPerSecond * delta;
                _restTimer = 0f;
            }
            else
            {
                _restTimer += delta;

                if (_restTimer >= restDelay)
                    value += (value < 0f ? regenNegative : regenPositive) * delta;
            }

            value = Mathf.Clamp(value, minStamina, maxStamina);
            Current = value;

            UpdateFloor(value, delta);
            UpdateFear(value, delta);
        }

        /// <summary>
        /// دویدن از «قصد» فهمیده می‌شود، نه از سرعت: تپ دویدن روشن + ورودی حرکت.
        /// اگر از سرعت استفاده کنیم، وقتی خستگی سرعت را زیر آستانه می‌برد، مصرف قطع می‌شود،
        /// استقامت برمی‌گردد، سرعت بالا می‌رود و همین حلقه تا آخر بازی تکرار می‌شود.
        /// </summary>
        private bool IsSprinting()
        {
            if (_input != null)
            {
                if (!_input.SprintToggled) return false;
                if (_input.Move.sqrMagnitude <= 0.0001f) return false;

                return true;
            }

            if (_controller == null) return false;

            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;

            return velocity.magnitude > sprintSpeedThreshold;
        }

        /* ============================================================
           مصرف — بازیکن و سلاح صدایش می‌زنند
           ============================================================ */

        public void Spend(float amount)
        {
            if (amount <= 0f) return;

            Current = Current - amount;
            _restTimer = 0f;
        }

        /// <summary>هزینه‌ی پرش؛ اگر استقامت کافی نباشد هم انجام می‌شود — مصرف بیش از حد مجاز است.</summary>
        public void SpendJump()
        {
            Spend(jumpCost);
        }

        /// <summary>وقتی سلاح اضافه شد: هر فریم که نشانه گرفته‌ای.</summary>
        public void SpendAim(float delta)
        {
            Spend(aimDrainPerSecond * delta);
        }

        /// <summary>وقتی سلاح اضافه شد: هر شلیک.</summary>
        public void SpendShot()
        {
            Spend(shotCost);
        }

        /* ============================================================
           فرسودگی و ترس
           ============================================================ */

        private void UpdateFloor(float value, float delta)
        {
            if (value > minStamina + 0.5f)
            {
                _floorTimer = 0f;
                _staggerTimer = 0f;
                _staggerEnd = 0f;
                return;
            }

            _floorTimer += delta;

            if (_floorTimer < floorSecondsBeforeDamage) return;

            CharacterStats.SetFloat(StatId.Health, CharacterStats.GetFloat(StatId.Health) - healthDrainPerSecond * delta);

            _staggerTimer += delta;

            if (_staggerTimer < staggerEverySeconds) return;

            _staggerTimer = 0f;
            _staggerEnd = Time.time + staggerDuration;
        }

        private void UpdateFear(float value, float delta)
        {
            WorldState world = World;
            if (world == null) return;

            DialogueValue current = world.Get("fear");
            float fear = current.IsUndefined ? 0f : (float)current.AsNumber();

            if (float.IsNaN(fear)) fear = 0f;

            if (value < 0f)
                fear += worstFearRise * Fatigue * delta;
            else
                fear -= 2f * delta;

            world.Set("fear", (double)Mathf.Clamp(fear, 0f, 100f));
        }

        /// <summary>بعد از مرگ یا شروع صحنه‌ی تازه.</summary>
        public void ResetStamina()
        {
            Current = startStamina;
            _restTimer = 0f;
            _floorTimer = 0f;
            _staggerTimer = 0f;
            _staggerEnd = 0f;
        }
    }
}
