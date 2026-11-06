using UnityEngine;
using TehranNoir.Core;

namespace TehranNoir.Interaction
{
    /// <summary>
    /// شیء قابل تعامل بدون رفتار خاص (فعلاً فقط نمایش).
    /// نوعش را در Inspector از منوی Action انتخاب کن: Open، Use، Read، ...
    /// </summary>
    public class SimpleInteractable : Interactable
    {
        [Tooltip("فعلاً برای تست؛ بعداً وقتی رفتار واقعی اضافه شد خاموشش کن")]
        [SerializeField] private bool logInteraction = true;

        public override void Interact(GameObject interactor)
        {
            if (logInteraction)
                Debug.Log(Title + ": " + Localization.Action(Action), this);
        }
    }
}
