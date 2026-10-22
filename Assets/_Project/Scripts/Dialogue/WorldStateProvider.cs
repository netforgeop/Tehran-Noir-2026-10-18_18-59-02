using UnityEngine;

namespace TehranNoir.Dialogue
{
    /// تنها منبع وضعیت جهان. یک نمونه در صحنه؛ همه‌ی دیالوگ‌ها و سیستم‌ها از همین می‌خوانند.
    [DisallowMultipleComponent]
    public sealed class WorldStateProvider : MonoBehaviour
    {
        [Header("مقدارهای اولیه")]
        [Tooltip("هر خانه یک خط مثل  met.reza = true  یا  rel.reza = 20")]
        [SerializeField] private string[] initialVariables = new string[0];

        [Header("ماندن بین صحنه‌ها")]
        [SerializeField] private bool persistBetweenScenes;

        public static WorldStateProvider Instance { get; private set; }

        public WorldState State { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Dialogue] بیش از یک WorldStateProvider در صحنه است؛ دومی غیرفعال شد.");
                enabled = false;
                return;
            }

            Instance = this;

            if (persistBetweenScenes) DontDestroyOnLoad(gameObject);

            ResetWorld();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [ContextMenu("ریست جهان")]
        public void ResetWorld()
        {
            State = new WorldState();

            for (int i = 0; i < initialVariables.Length; i++)
                DialogueRunner.ApplyEffect(initialVariables[i], State);

            Debug.Log("[Dialogue] جهان آماده شد: " + initialVariables.Length + " مقدار اولیه.");
        }

        /// خواندن یک مقدار برای سیستم‌های دیگر (HUD، تعامل، …).
        public DialogueValue Get(string variableName)
        {
            return State != null ? State.Get(variableName) : DialogueValue.Undefined;
        }

        public void Set(string variableName, double value)
        {
            if (State != null) State.Set(variableName, value);
        }

        public void Set(string variableName, bool value)
        {
            if (State != null) State.Set(variableName, value);
        }

        public void Set(string variableName, string value)
        {
            if (State != null) State.Set(variableName, value);
        }
    }
}
