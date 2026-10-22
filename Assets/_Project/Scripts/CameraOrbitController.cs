using TehranNoir.Inputs;
using Unity.Cinemachine;
using UnityEngine;

namespace TehranNoir.Cameras
{
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public class CameraOrbitController : MonoBehaviour
    {
        [Header("Look")]
        [Tooltip("درجه چرخش به ازای هر پیکسل حرکت موس")]
        [SerializeField] private float mouseSensitivity = 0.15f;
        [Tooltip("درجه بر ثانیه برای استیک گیم‌پد")]
        [SerializeField] private float stickSensitivity = 120f;
        [SerializeField] private bool lockCursor = true;

        [Header("Follow Behind")]
        [Tooltip("آبجکت Player (بعداً ماشین)")]
        [SerializeField] private Transform followTarget;
        [Tooltip("درجه بر ثانیه؛ 0 = غیرفعال")]
        [SerializeField] private float recenterSpeed = 20f;
        [Tooltip("چند ثانیه بعد از آخرین ورودی چرخش شروع کند")]
        [SerializeField] private float recenterDelay = 1f;
        [Tooltip("زیر این سرعت (متر بر ثانیه) برنمی‌گردد")]
        [SerializeField] private float moveThreshold = 0.5f;

        private CinemachineOrbitalFollow orbitalFollow;
        private PlayerInputReader input;
        private Vector3 lastTargetPosition;
        private float timeSinceLookInput;

        private void Awake()
        {
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

            if (followTarget != null)
                input = followTarget.GetComponent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            if (!lockCursor) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Start()
        {
            if (followTarget != null)
                lastTargetPosition = followTarget.position;
        }

        private void Update()
        {
            bool looked = ApplyLook();

            if (looked) timeSinceLookInput = 0f;
            else timeSinceLookInput += Time.deltaTime;

            FollowBehind(MeasureSpeed());
        }

        private bool ApplyLook()
        {
            if (input == null) return false;

            Vector2 look = input.Look;
            if (look.sqrMagnitude < 0.0001f) return false;

            float horizontal;
            if (input.LookFromGamepad)
            {
                // استیک: سرعت ثابت، پس در زمان ضرب می‌شود
                horizontal = look.x * stickSensitivity * Time.deltaTime;
            }
            else
            {
                // موس: خودش جابه‌جایی همین فریم است، پس در زمان ضرب نمی‌شود
                horizontal = look.x * mouseSensitivity;
            }

            orbitalFollow.HorizontalAxis.Value += horizontal;

            float vertical = orbitalFollow.VerticalAxis.Value - look.y *
                (input.LookFromGamepad ? stickSensitivity * Time.deltaTime : mouseSensitivity);

            orbitalFollow.VerticalAxis.Value = Mathf.Clamp(vertical,
                orbitalFollow.VerticalAxis.Range.x,
                orbitalFollow.VerticalAxis.Range.y);

            return true;
        }

        private float MeasureSpeed()
        {
            if (followTarget == null) return 0f;

            Vector3 movement = followTarget.position - lastTargetPosition;
            lastTargetPosition = followTarget.position;
            movement.y = 0f;

            return movement.magnitude / Time.deltaTime;
        }

        private void FollowBehind(float speed)
        {
            if (followTarget == null || recenterSpeed <= 0f) return;
            if (timeSinceLookInput < recenterDelay) return;
            if (speed < moveThreshold) return;

            orbitalFollow.HorizontalAxis.Value = Mathf.MoveTowardsAngle(
                orbitalFollow.HorizontalAxis.Value,
                followTarget.eulerAngles.y,
                recenterSpeed * Time.deltaTime);
        }
    }
}