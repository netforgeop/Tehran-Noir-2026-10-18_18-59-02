using UnityEngine;
using TehranNoir.Dialogue;
using TehranNoir.Inputs;

namespace TehranNoir.Interaction
{
    [RequireComponent(typeof(PlayerInputReader))]
    public class InteractionSensor : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("شعاع جستجو؛ باید از maxDistance آبجکت‌ها بزرگ‌تر باشد")]
        [SerializeField] private float searchRadius = 4f;

        [Header("Debug")]
        [SerializeField] private bool logSelection = true;
        [SerializeField] private bool drawDebugLine = true;

        private readonly Collider[] hits = new Collider[16];
        private PlayerInputReader input;
        private Interactable current;

        public Interactable Current => current;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            // تا وقتی پنل دیالوگ باز است، تعامل و هایلایت تعطیل است
            if (DialogueUI.Current != null)
            {
                SetCurrent(null);
                return;
            }

            SetCurrent(FindBest());

            if (current != null && drawDebugLine)
                Debug.DrawLine(transform.position, current.Point.position, Color.yellow);

            if (current != null && input.InteractPressedThisFrame)
                current.Interact(gameObject);
        }

        private void SetCurrent(Interactable next)
        {
            if (next == current) return;

            if (current != null) Unfocus(current);

            current = next;

            if (current != null) Focus(current);
        }

        private void Focus(Interactable interactable)
        {
            interactable.Focus();
            if (logSelection) Debug.Log($"Selected: {interactable.name}", interactable);
        }

        private void Unfocus(Interactable interactable)
        {
            interactable.Unfocus();
            if (logSelection) Debug.Log($"Unselected: {interactable.name}", interactable);
        }

        private Interactable FindBest()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, searchRadius, hits,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            Interactable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                // قانون این سیستم: Collider و Interactable روی همان GameObject
                Interactable candidate = hits[i].GetComponent<Interactable>();
                if (candidate == null || !candidate.enabled) continue;
                if (!candidate.IsAvailableFrom(transform.position)) continue;

                float distance = Vector3.Distance(transform.position, candidate.Point.position);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }
    }
}