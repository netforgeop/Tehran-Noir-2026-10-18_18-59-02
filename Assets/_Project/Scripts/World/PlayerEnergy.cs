using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.World
{
    /// خستگی بازیکن. دویدن انرژی می‌برد، ایستادن/راه رفتن برمی‌گرداند.
    /// مقدار را در WorldState می‌نویسد تا دیالوگ با آن شرط بگذارد: energy < 30
    public sealed class PlayerEnergy : MonoBehaviour
    {
        [Header("انرژی")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float startEnergy = 100f;

        [Header("مصرف و بازیابی")]
        [Tooltip("واحد در ثانیه، وقتی می‌دوی")]
        [SerializeField] private float sprintDrain = 1.5f;

        [Tooltip("واحد در ثانیه، وقتی نمی‌دوی")]
        [SerializeField] private float restRegen = 0.8f;

        [Tooltip("بالاتر از این سرعت (متر بر ثانیه) دویدن حساب می‌شود")]
        [SerializeField] private float sprintSpeedThreshold = 4.5f;

        [Header("رفتار")]
        [Tooltip("در طول دیالوگ انرژی عوض نمی‌شود")]
        [SerializeField] private bool freezeDuringDialogue = true;

        private CharacterController _controller;
        private float _energy;
        private float _lastPushed = -1f;

        public float Current { get { return _energy; } }

        public float Normalized { get { return maxEnergy > 0f ? _energy / maxEnergy : 0f; } }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _energy = Mathf.Clamp(startEnergy, 0f, maxEnergy);
        }

        private void Start()
        {
            Push(true);
        }

        private void Update()
        {
            if (freezeDuringDialogue && DialogueUI.Current != null) return;

            bool sprinting = IsSprinting();

            if (sprinting) _energy -= sprintDrain * Time.deltaTime;
            else _energy += restRegen * Time.deltaTime;

            _energy = Mathf.Clamp(_energy, 0f, maxEnergy);

            Push(false);
        }

        private bool IsSprinting()
        {
            if (_controller == null) return false;

            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;

            return velocity.magnitude > sprintSpeedThreshold;
        }

        /// فقط وقتی مقدار محسوسی عوض شد می‌نویسد؛ WorldState ارزان است ولی بیهوده هم نیست.
        private void Push(bool force)
        {
            if (!force && Mathf.Abs(_energy - _lastPushed) < 0.25f) return;

            WorldStateProvider provider = WorldStateProvider.Instance;
            if (provider == null) return;

            _lastPushed = _energy;
            provider.Set("energy", (double)_energy);
        }
    }
}
