using UnityEngine;
using TehranNoir.Core;
using TehranNoir.Rendering;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// پایه‌ی همه‌ی آبجکت‌های قابل تعامل.
    /// نوع تعامل (Action) داده است؛ متنش از Localization می‌آید.
    /// نام و توضیح دوزبانه‌اند: «فارسی|English».
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Interaction")]
        [Tooltip("نوع تعامل؛ متنش خودکار از Localization می‌آید")]
        [SerializeField] private InteractionAction action = InteractionAction.Use;

        [Tooltip("نقطه‌ای که بازیکن باید نزدیکش باشد. خالی = خودِ این آبجکت")]
        [SerializeField] private Transform interactionPoint;

        [Tooltip("حداکثر فاصله از نقطه، بر حسب متر")]
        [SerializeField] private float maxDistance = 2.5f;

        [Tooltip("حداکثر انحراف از جهت جلوی نقطه. 360 = از هر طرف")]
        [SerializeField, Range(10f, 360f)] private float maxAngle = 360f;

        [Header("Identity")]
        [Tooltip("نام نمایشی در کارت تعامل. دوزبانه: «رضا|Reza». خالی = نام آبجکت")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("خط سوم کارت: نقش، نوع، توضیح. دوزبانه")]
        [SerializeField] private string subtype = string.Empty;

        [Header("Visual")]
        [Tooltip("خالی بماند تا خودکار روی همین آبجکت پیدا شود")]
        [SerializeField] private OutlineEffect outline;

        public Transform Point => interactionPoint != null ? interactionPoint : transform;
        public float MaxDistance => maxDistance;

        /// <summary>نوع تعامل. کلاس‌های فرزند می‌توانند ثابتش کنند.</summary>
        public virtual InteractionAction Action => action;

        /// <summary>متن دکمه‌ی تعامل، به زبان فعلی بازی («برداشتن»، «صحبت»، …).</summary>
        public string Prompt => Localization.Action(Action);

        /// <summary>خط اول کارت: نام.</summary>
        public virtual string Title =>
            string.IsNullOrWhiteSpace(displayName) ? name : Localization.Pick(displayName);

        /// <summary>خط سوم کارت: نقش یا نوع. خالی یعنی نمایش داده نشود.</summary>
        public virtual string Subtitle => Localization.Pick(subtype);

        /// <summary>آیا بازیکن از این موقعیت می‌تواند تعامل کند؟</summary>
        public bool IsAvailableFrom(Vector3 interactorPosition)
        {
            Vector3 pointPosition = Point.position;

            if (Vector3.Distance(interactorPosition, pointPosition) > maxDistance)
                return false;

            if (maxAngle >= 360f)
                return true;

            Vector3 direction = interactorPosition - pointPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return true;

            return Vector3.Angle(Point.forward, direction) <= maxAngle;
        }

        /// <summary>فقط InteractionSensor صدا می‌زند.</summary>
        public void Focus()
        {
            if (outline == null) outline = GetComponent<OutlineEffect>();
            if (outline != null) outline.Show();
            OnFocusEnter();
        }

        /// <summary>فقط InteractionSensor صدا می‌زند.</summary>
        public void Unfocus()
        {
            if (outline == null) outline = GetComponent<OutlineEffect>();
            if (outline != null) outline.Hide();
            OnFocusExit();
        }

        protected virtual void OnFocusEnter() { }
        protected virtual void OnFocusExit() { }

        public abstract void Interact(GameObject interactor);

        private void OnDrawGizmosSelected()
        {
            Transform point = Point;
            if (point == null) return;

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(point.position, maxDistance);

            if (maxAngle >= 360f) return;

            Vector3 forward = point.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            Gizmos.color = new Color(1f, 0.55f, 0f, 0.9f);

            const int segments = 24;
            Vector3 previous = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(-maxAngle, maxAngle, t);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 current = point.position + direction * maxDistance;

                if (i > 0) Gizmos.DrawLine(previous, current);
                if (i == 0 || i == segments) Gizmos.DrawLine(point.position, current);
                previous = current;
            }
        }
    }
}
