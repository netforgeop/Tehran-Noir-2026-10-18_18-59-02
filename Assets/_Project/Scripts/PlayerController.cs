using TehranNoir.Inputs;
using UnityEngine;

namespace TehranNoir.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 24f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float gravity = -19.62f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float groundCheckDistance = 0.1f;

        [Header("Sprint Toggle")]
        [Tooltip("زیر این سرعت، بازیکن ایستاده حساب می‌شود و دویدن خاموش می‌شود")]
        [SerializeField] private float stopSpeed = 0.05f;

        private CharacterController characterController;
        private PlayerInputReader input;
        private Transform cameraTransform;
        private Vector3 velocity;        // فقط محور عمودی
        private Vector3 moveDirection;   // آخرین جهت افقی (برای اینرسی)
        private float currentSpeed;

        // آیا از آخرین تپِ Sprint، واقعاً دویده‌ایم؟
        private bool sprintWasUsed;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            bool grounded = CheckGrounded();

            Move(input.Move, input.SprintToggled, grounded);

            bool jumped = TryJump(grounded);
            ApplyGravity(grounded && !jumped);

            UpdateSprintToggle();
        }

        private Vector3 ToCameraRelative(Vector2 moveInput)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            return forward * moveInput.y + right * moveInput.x;
        }

        private void Move(Vector2 moveInput, bool running, bool grounded)
        {
            Vector3 inputDirection = ToCameraRelative(moveInput);
            bool hasInput = inputDirection.sqrMagnitude > 0.0001f;

            // فقط روی زمین: جهت و سرعت از ورودی گرفته می‌شود
            if (grounded)
            {
                if (hasInput)
                    moveDirection = inputDirection.normalized;

                float targetSpeed = hasInput ? (running ? runSpeed : walkSpeed) : 0f;
                float rate = targetSpeed > currentSpeed ? acceleration : deceleration;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

                if (hasInput)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            // در هوا moveDirection و currentSpeed دست‌نخورده می‌مانند (اینرسی)

            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        }

        // قاعده‌ی دویدن: تپ روشن می‌کند، ایستادن کامل خاموش
        private void UpdateSprintToggle()
        {
            if (currentSpeed > stopSpeed)
                sprintWasUsed = true;

            bool noInput = input.Move.sqrMagnitude <= 0.0001f;

            if (sprintWasUsed && noInput && currentSpeed <= stopSpeed)
            {
                input.ClearSprint();
                sprintWasUsed = false;
            }
        }

        // چک کردن زمین با Ray کوتاه از مرکز کپسول به پایین
        private bool CheckGrounded()
        {
            Vector3 origin = transform.position + characterController.center;
            float distance = (characterController.height * 0.5f) + groundCheckDistance;

            return Physics.Raycast(origin, Vector3.down, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private bool TryJump(bool grounded)
        {
            if (!grounded) return false;
            if (!input.JumpPressedThisFrame) return false;

            velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            return true;
        }

        private void ApplyGravity(bool grounded)
        {
            if (grounded)
            {
                if (velocity.y < 0f) velocity.y = -2f;   // چسبیدن به زمین
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }

            characterController.Move(velocity * Time.deltaTime);
        }
    }
}