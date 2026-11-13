using UnityEngine;
using UnityEngine.InputSystem;

namespace TehranNoir.Inputs
{
    /// <summary>
    /// تنها نقطه‌ی خواندن ورودی بازیکن. بقیه‌ی اسکریپت‌ها فقط از این کلاس می‌خوانند.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerControls controls;

        private bool sprintToggled;

        public Vector2 Move => controls.Player.Move.ReadValue<Vector2>();
        public Vector2 Look => controls.Player.Look.ReadValue<Vector2>();
        public bool JumpPressedThisFrame => controls.Player.Jump.WasPressedThisFrame();
        public bool InteractPressedThisFrame => controls.Player.Interact.WasPressedThisFrame();

        /// <summary>وضعیت دویدن: با هر تپ Sprint عوض می‌شود (toggle).</summary>
        public bool SprintToggled => sprintToggled;

        /// <summary>از بیرون خاموشش کن، مثلاً وقتی بازیکن کامل ایستاد.</summary>
        public void ClearSprint() => sprintToggled = false;

        // کدام دستگاه در این لحظه Look را می‌چرخاند؟
        public bool LookFromGamepad =>
            controls.Player.Look.activeControl != null &&
            controls.Player.Look.activeControl.device is Gamepad;

        private void Awake()
        {
            controls = new PlayerControls();
        }

        private void OnEnable() => controls.Player.Enable();
        private void OnDisable() => controls.Player.Disable();
        private bool usingGamepad;

        /// <summary>آخرین ورودی از دسته بوده یا کیبورد؟</summary>
        public bool UsingGamepad => usingGamepad;
        private void Update()
        {

            // WasPressedThisFrame = فقط لحظه‌ی فشردن، نه نگه داشتن
            if (controls.Player.Sprint.WasPressedThisFrame())
                sprintToggled = !sprintToggled;
            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
                usingGamepad = true;

            if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
                usingGamepad = false;

        }
    }
}