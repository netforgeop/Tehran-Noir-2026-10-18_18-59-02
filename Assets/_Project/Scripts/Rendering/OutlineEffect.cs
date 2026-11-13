using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TehranNoir.Rendering
{
    /// <summary>
    /// یک کپی بزرگ‌تر از مِش آبجکت با متریال Outline می‌سازد.
    /// متریال و مِش اصلی دست‌نخورده می‌ماند.
    /// </summary>
    public class OutlineEffect : MonoBehaviour
    {
        [Tooltip("متریال ساخته‌شده با شیدر TehranNoir/Outline")]
        [SerializeField] private Material outlineMaterial;

        private readonly List<GameObject> pieces = new List<GameObject>();
        private bool built;

        public void Show()
        {
            EnsureBuilt();
            SetVisible(true);
        }

        public void Hide()
        {
            if (!built) return;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            foreach (GameObject piece in pieces)
                if (piece != null) piece.SetActive(visible);
        }

        // فقط بار اول ساخته می‌شود؛ بعد از آن فقط روشن/خاموش می‌شود
        private void EnsureBuilt()
        {
            if (built) return;
            built = true;

            if (outlineMaterial == null)
            {
                Debug.LogWarning($"OutlineEffect on {name}: Outline Material is not assigned.", this);
                return;
            }

            MeshRenderer[] sources = GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer source in sources)
            {
                if (source.sharedMaterial == outlineMaterial) continue;

                MeshFilter filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                GameObject piece = new GameObject($"{source.name}_Outline");
                piece.transform.SetParent(source.transform, false);

                MeshFilter pieceFilter = piece.AddComponent<MeshFilter>();
                pieceFilter.sharedMesh = filter.sharedMesh;

                MeshRenderer pieceRenderer = piece.AddComponent<MeshRenderer>();
                pieceRenderer.sharedMaterial = outlineMaterial;
                pieceRenderer.shadowCastingMode = ShadowCastingMode.Off;
                pieceRenderer.receiveShadows = false;

                piece.SetActive(false);
                pieces.Add(piece);
            }
            Debug.Log($"OutlineEffect on {name}: {pieces.Count} piece(s) built.", this);
        }
    }
}