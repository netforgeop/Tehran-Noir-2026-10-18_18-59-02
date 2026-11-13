using UnityEngine;
using UnityEngine.SceneManagement;

namespace TehranNoir.World
{
    // A reversible runtime trial: the original scene and its colliders are untouched.
    public static class HotelModelTrial
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Hilton") return;
            Replace(scene, "Register", "ModelTrials/Register/CashRegister_01_2k",
                "ModelTrials/Register/textures/CashRegister_01_diff_2k");
            Replace(scene, "Lobby_Desk", "ModelTrials/Desk/metal_office_desk_2k",
                "ModelTrials/Desk/textures/metal_office_desk_diff_2k");
        }

        private static void Replace(Scene scene, string name, string modelPath, string texturePath)
        {
            Transform target = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == name) { target = item; break; }
                if (target != null) break;
            }
            GameObject prefab = Resources.Load<GameObject>(modelPath);
            if (target == null || prefab == null)
            {
                Debug.LogWarning("[HotelModelTrial] Missing target or model: " + name);
                return;
            }
            Renderer original = target.GetComponent<Renderer>();
            if (original == null || !original.enabled) return;
            Bounds box = original.bounds;
            Vector3 parentScale = target.lossyScale;
            if (Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f || Mathf.Abs(parentScale.z) < 0.0001f) return;

            GameObject instance = Object.Instantiate(prefab, target);
            instance.name = "ModelTrial";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            // Scene primitives have nonuniform scale; cancel it before measuring.
            instance.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Object.Destroy(instance);
                Debug.LogWarning("[HotelModelTrial] Model has no renderer: " + name);
                return;
            }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.x < 0.0001f || bounds.size.y < 0.0001f || bounds.size.z < 0.0001f)
            {
                Object.Destroy(instance);
                Debug.LogWarning("[HotelModelTrial] Model has invalid bounds: " + name);
                return;
            }
            float scale = Mathf.Min(box.size.x / bounds.size.x, Mathf.Min(box.size.y / bounds.size.y, box.size.z / bounds.size.z));
            instance.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            instance.transform.position += box.center - bounds.center;

            Texture2D diffuse = Resources.Load<Texture2D>(texturePath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (diffuse != null && shader != null)
            {
                Material material = new Material(shader);
                material.mainTexture = diffuse;
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                }
            }
            original.enabled = false;
            Debug.Log("[HotelModelTrial] Replaced " + name);
        }
    }
}
