using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TehranNoir.Dialogue;
using TehranNoir.Inputs;
using TehranNoir.UI;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// پیدا کردن آبجکت‌های قابل تعامل اطراف بازیکن و انتخاب یکی از آن‌ها.
    /// همه‌ی اهداف نزدیک جمع می‌شوند (نه فقط نزدیک‌ترین) تا بتوان با اسکرول بینشان
    /// چرخید؛ مثل Project Zomboid که چند آیتم روی یک میز، یک در، یک آدم را با هم می‌بیند.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class InteractionSensor : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("شعاع جستجو؛ باید از maxDistance آبجکت‌ها بزرگ‌تر باشد")]
        [SerializeField] private float searchRadius = 4f;

        [Header("Selection")]
        [Tooltip("چرخ موس بین اهداف نزدیک جابه‌جا می‌کند")]
        [SerializeField] private bool scrollSelects = true;

        [Tooltip("کمینه‌ی حرکت چرخ موس که یک پله حساب شود")]
        [SerializeField] private float scrollThreshold = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool logSelection = true;
        [SerializeField] private bool drawDebugLine = true;

        private readonly Collider[] hits = new Collider[32];
        private readonly List<Interactable> candidates = new List<Interactable>(32);

        private PlayerInputReader input;
        private Interactable current;
        private int index;

        /// <summary>هدف انتخاب‌شده؛ اگر چیزی نباشد null.</summary>
        public Interactable Current => current;

        /// <summary>همه‌ی اهداف نزدیک، از نزدیک به دور.</summary>
        public IReadOnlyList<Interactable> Candidates => candidates;

        /// <summary>جای هدف انتخاب‌شده در فهرست؛ برای نوار انتخاب در HUD.</summary>
        public int Index => index;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (Blocked())
            {
                candidates.Clear();
                index = 0;
                SetCurrent(null);
                return;
            }

            Collect();

            if (candidates.Count == 0)
            {
                index = 0;
                SetCurrent(null);
                return;
            }

            KeepSelectionValid();
            CycleByInput();

            SetCurrent(candidates[index]);

            if (current != null && drawDebugLine)
                Debug.DrawLine(transform.position, current.Point.position, Color.yellow);

            if (current != null && input.InteractPressedThisFrame)
                current.Interact(gameObject);
        }

        /// <summary>دیالوگ باز است یا منوی توقف؛ در هر دو حالت تعامل خاموش است.</summary>
        private static bool Blocked()
        {
            return DialogueUI.Current != null || PauseMenuUI.IsOpen;
        }

        /// <summary>همه‌ی اهداف معتبر را جمع و از نزدیک به دور مرتب می‌کند.</summary>
        private void Collect()
        {
            candidates.Clear();

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, searchRadius, hits,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            Vector3 origin = transform.position;

            for (int i = 0; i < count; i++)
            {
                // قانون این سیستم: Collider و Interactable روی همان GameObject
                Interactable candidate = hits[i].GetComponent<Interactable>();
                if (candidate == null || !candidate.enabled) continue;
                if (!candidate.IsAvailableFrom(origin)) continue;
                if (candidates.Contains(candidate)) continue;

                Insert(candidate, Vector3.Distance(origin, candidate.Point.position));
            }
        }

        /// <summary>مرتب‌سازی درجی؛ فهرست همیشه کوچک است و این‌جا هیچ حافظه‌ای ساخته نمی‌شود.</summary>
        private void Insert(Interactable candidate, float distance)
        {
            int position = candidates.Count;

            while (position > 0 &&
                   Vector3.Distance(transform.position, candidates[position - 1].Point.position) > distance)
                position--;

            candidates.Insert(position, candidate);
        }

        /// <summary>
        /// انتخاب قبلی را نگه می‌دارد تا با جابه‌جا شدن بازیکن، هدف جلوی چشم نپرد.
        /// فقط وقتی آن آبجکت از دسترس خارج شد، به نزدیک‌ترین می‌رود.
        /// </summary>
        private void KeepSelectionValid()
        {
            int found = current != null ? candidates.IndexOf(current) : -1;

            index = found >= 0 ? found : 0;
        }

        private void CycleByInput()
        {
            if (candidates.Count < 2) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
                if (keyboard.digit2Key.wasPressedThisFrame) Select(1);
                if (keyboard.digit3Key.wasPressedThisFrame) Select(2);
                if (keyboard.digit4Key.wasPressedThisFrame) Select(3);
            }

            if (scrollSelects)
            {
                Mouse mouse = Mouse.current;
                if (mouse != null)
                {
                    float scroll = mouse.scroll.ReadValue().y;

                    if (Mathf.Abs(scroll) > scrollThreshold)
                        Cycle(scroll > 0f ? 1 : -1);
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame)
                Cycle(1);
        }

        /// <summary>هدف بعدی/قبلی در فهرست.</summary>
        public void Cycle(int delta)
        {
            if (candidates.Count == 0 || delta == 0) return;

            Select(index + delta);
        }

        private void Select(int wanted)
        {
            if (candidates.Count == 0) return;

            index = (wanted % candidates.Count + candidates.Count) % candidates.Count;

            SetCurrent(candidates[index]);
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
            if (logSelection) Debug.Log("[Interaction] selected: " + interactable.name, interactable);
        }

        private void Unfocus(Interactable interactable)
        {
            interactable.Unfocus();
            if (logSelection) Debug.Log("[Interaction] released: " + interactable.name, interactable);
        }
    }
}
