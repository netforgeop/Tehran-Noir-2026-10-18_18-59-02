using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.World
{
    /// <summary>
    /// جای پیاده‌شدن در صحنه، بسته به این‌که از کدام در آمده‌ای.
    ///
    /// چرا لازم است؟ هر صحنه یک مکان شروع دارد. لابی از خیابان شروع می‌شود،
    /// ولی کسی که با آسانسور از طبقه‌ی هشتم پایین می‌آید باید جلوی آسانسور
    /// سبز شود، نه وسط خیابان. یک نقطه به ازای هر درِ ورودی.
    ///
    /// این کامپوننت باید روی خودِ Player باشد، نه روی نقطه. نقطه را
    /// `entryPoint` می‌دهد.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneEntryPoint : MonoBehaviour
    {
        [Tooltip("شناسه‌ای که در صحنه‌ی قبلی، در SceneDoor نوشته شده")]
        [SerializeField] private string entryId = "hotel_lift";

        [Tooltip("کجا پیاده شود. خالی = همین آبجکت")]
        [SerializeField] private Transform entryPoint;

        [Tooltip("رو به کدام سمت. خالی = چرخش فعلی دست نمی‌خورد")]
        [SerializeField] private Transform lookAt;

        [SerializeField] private bool logArrival = true;

        private void Start()
        {
            if (WorldStateProvider.Instance == null) return;

            string from = WorldStateProvider.Instance.Get("entry.from").AsText();

            if (string.IsNullOrEmpty(from) || from != entryId) return;

            Transform target = entryPoint != null ? entryPoint : transform;
            Move(target.position, target.rotation);

            if (logArrival)
                Debug.Log("[World] ورود از " + entryId + " → " + target.position, this);
        }

        /// <summary>
        /// جا‌به‌جایی امن با CharacterController.
        ///
        /// اگر کنترلر روشن بماند، `transform.position` را در همان فریم برمی‌گرداند
        /// یا بازیکن را از میان دیوار رد می‌کند. یک فریم خاموش، بعد روشن.
        /// </summary>
        private void Move(Vector3 position, Quaternion rotation)
        {
            CharacterController controller = GetComponent<CharacterController>();

            if (controller != null) controller.enabled = false;

            transform.position = position;
            transform.rotation = rotation;

            if (controller != null) controller.enabled = true;
        }
    }
}
