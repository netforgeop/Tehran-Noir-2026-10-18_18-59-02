using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TehranNoir.EditorTools
{
    /// <summary>
    /// گذاشتن مدل سه‌بعدی روی جعبه‌های خاکستری صحنه.
    ///
    /// چرا این شکل؟ صحنه‌ها با اسکریپت ساخته می‌شوند و هر وسیله یک مکعب است.
    /// عوض‌کردن مستقیم فایل صحنه با مدل یعنی شناسه‌ی دارایی در YAML، و هر بار
    /// ساخت دوباره‌ی صحنه آن را پاک می‌کند. این ابزار بعد از ساخت صحنه
    /// اجرا می‌شود: مدل را فرزند همان جعبه می‌کند، جعبه را نامرئی می‌کند، و
    /// کولایدر سرِ جایش می‌ماند.
    ///
    /// قاعده: اندازه‌ی مدل از جعبه گرفته می‌شود، نه از فایل. پس چیدمان صحنه
    /// دست نمی‌خورد، حتی اگر مدل هزار برابر بزرگ‌تر یا کوچک‌تر باشد.
    /// </summary>
    public static class ModelSwapper
    {
        private const string Root = "Assets/_Project/Art/Models";
        private const string ManifestPath = Root + "/model-slots.txt";
        private const string MaterialFolder = Root + "/Materials";
        private const string InstanceName = "Model";

        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Hilton.unity",
            "Assets/_Project/Scenes/Hilton814.unity",
            "Assets/_Project/Scenes/Teahouse.unity",
            "Assets/_Project/Scenes/Sandbox.unity"
        };

        /* ============================================================
           خواندن فهرست
           ============================================================ */

        public sealed class Slot
        {
            public string ObjectName;
            public string ModelPath;
            public float RotY;
            public bool Stretch;
            public float Lift;
            public int Line;
        }

        public static List<Slot> ReadManifest(out List<string> errors)
        {
            List<Slot> slots = new List<Slot>();
            errors = new List<string>();

            if (!System.IO.File.Exists(ManifestPath))
            {
                errors.Add("فهرست پیدا نشد: " + ManifestPath);
                return slots;
            }

            string[] lines = System.IO.File.ReadAllLines(ManifestPath);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Length == 0 || line[0] == '#') continue;

                int equals = line.IndexOf('=');

                if (equals <= 0)
                {
                    errors.Add("خط " + (i + 1) + ": علامت = ندارد");
                    continue;
                }

                Slot slot = new Slot();
                slot.ObjectName = line.Substring(0, equals).Trim();
                slot.Line = i + 1;

                string[] parts = line.Substring(equals + 1).Split('|');

                string path = parts[0].Trim();
                slot.ModelPath = path.StartsWith("Assets/") ? path : Root + "/" + path;

                for (int p = 1; p < parts.Length; p++)
                {
                    string[] pair = parts[p].Split('=');

                    if (pair.Length != 2)
                    {
                        errors.Add("خط " + (i + 1) + ": تغییردهنده‌ی بدشکل «" + parts[p].Trim() + "»");
                        continue;
                    }

                    string key = pair[0].Trim().ToLowerInvariant();
                    float value;

                    if (!float.TryParse(pair[1].Trim(), NumberStyles.Float,
                                        CultureInfo.InvariantCulture, out value))
                    {
                        errors.Add("خط " + (i + 1) + ": عدد نامعتبر «" + pair[1].Trim() + "»");
                        continue;
                    }

                    if (key == "roty") slot.RotY = value;
                    else if (key == "stretch") slot.Stretch = value >= 1f;
                    else if (key == "lift") slot.Lift = value;
                    else errors.Add("خط " + (i + 1) + ": تغییردهنده‌ی ناشناخته «" + key + "»");
                }

                slots.Add(slot);
            }

            return slots;
        }

        /* ============================================================
           منو
           ============================================================ */

        [MenuItem("Tehran Noir/مدل‌ها/جایگزینی در صحنه‌ی جاری", false, 300)]
        public static void SwapInCurrentScene()
        {
            List<string> errors;
            List<Slot> slots = ReadManifest(out errors);

            Report(errors, slots);

            int applied = Apply(SceneManager.GetActiveScene(), slots);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[Models] " + applied + " مدل در صحنه‌ی جاری گذاشته شد.");
        }

        [MenuItem("Tehran Noir/مدل‌ها/جایگزینی در همه‌ی صحنه‌ها", false, 301)]
        public static void SwapInAllScenes()
        {
            List<string> errors;
            List<Slot> slots = ReadManifest(out errors);

            Report(errors, slots);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string current = SceneManager.GetActiveScene().path;
            int total = 0;

            for (int i = 0; i < Scenes.Length; i++)
            {
                if (!System.IO.File.Exists(Scenes[i])) continue;

                Scene scene = EditorSceneManager.OpenScene(Scenes[i], OpenSceneMode.Single);
                int applied = Apply(scene, slots);

                if (applied > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                total += applied;
            }

            if (!string.IsNullOrEmpty(current))
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);

            Debug.Log("[Models] " + total + " مدل در " + Scenes.Length + " صحنه گذاشته شد.");
        }

        [MenuItem("Tehran Noir/مدل‌ها/درست‌کردن متریال‌ها (URP)", false, 302)]
        public static void FixMaterials()
        {
            int built = BuildMaterials();
            AssetDatabase.Refresh();

            Debug.Log("[Models] " + built + " متریال URP ساخته شد در " + MaterialFolder);
        }

        /* ============================================================
           گذاشتن
           ============================================================ */

        private static int Apply(Scene scene, List<Slot> slots)
        {
            int applied = 0;

            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];

                if (!System.IO.File.Exists(slot.ModelPath)) continue;

                GameObject target = FindByName(roots, slot.ObjectName);

                if (target == null) continue;

                if (Swap(target, slot)) applied++;
            }

            return applied;
        }

        private static GameObject FindByName(GameObject[] roots, string name)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);

                for (int t = 0; t < all.Length; t++)
                    if (all[t].name == name) return all[t].gameObject;
            }

            return null;
        }

        private static bool Swap(GameObject target, Slot slot)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(slot.ModelPath);

            if (model == null)
            {
                Debug.LogWarning("[Models] «" + slot.ModelPath + "» به‌عنوان مدل خوانده نشد.");
                return false;
            }

            // از قبل یکی هست؟ برداشته می‌شود تا دو نسخه روی هم ننشینند.
            for (int i = target.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = target.transform.GetChild(i);

                if (child.name == InstanceName)
                    Object.DestroyImmediate(child.gameObject);
            }

            Renderer[] original = target.GetComponents<Renderer>();

            // اندازه از رندرر گرفته می‌شود. اگر اجرای قبلی آن را خاموش کرده
            // باشد، کولایدر همان اندازه را دارد و دوباره اجرا شدنی است.
            Bounds box;
            bool hadRenderer = original.Length > 0 && original[0].enabled;

            if (hadRenderer)
            {
                box = original[0].bounds;
            }
            else
            {
                Collider collider = target.GetComponent<Collider>();

                if (collider == null)
                {
                    Debug.LogWarning("[Models] «" + target.name + "» نه رندرر روشن دارد نه کولایدر.");
                    return false;
                }

                box = collider.bounds;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, target.transform);
            instance.name = InstanceName;

            // کولایدر و دوربین و نورِ مدل برداشته می‌شود: جعبه کولایدر خودش را دارد.
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) Object.DestroyImmediate(colliders[i]);

            Camera[] cameras = instance.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++) Object.DestroyImmediate(cameras[i]);

            Light[] lights = instance.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) Object.DestroyImmediate(lights[i]);

            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, slot.RotY, 0f);
            instance.transform.localScale = Vector3.one;

            // ۱) اندازه‌ی مدل، در فضای جهانی، پیش از هر بزرگ‌کردنی
            Bounds mesh = Measure(instance);

            if (mesh.size.sqrMagnitude < 0.000001f)
            {
                Debug.LogWarning("[Models] «" + target.name + "»: مدل اندازه ندارد.");
                return false;
            }

            // ۲) ضریب
            Vector3 factor = new Vector3(box.size.x / Mathf.Max(mesh.size.x, 0.0001f),
                                         box.size.y / Mathf.Max(mesh.size.y, 0.0001f),
                                         box.size.z / Mathf.Max(mesh.size.z, 0.0001f));

            float uniform = Mathf.Min(factor.x, Mathf.Min(factor.y, factor.z));

            instance.transform.localScale = slot.Stretch
                ? factor
                : new Vector3(uniform, uniform, uniform);

            // ۳) مرکزِ مدل را روی مرکزِ جعبه بنشان
            mesh = Measure(instance);

            Vector3 offset = box.center - mesh.center;

            instance.transform.position += offset + Vector3.up * slot.Lift;

            // ۴) مدل روشن، جعبه خاموش
            Renderer[] made = instance.GetComponentsInChildren<Renderer>(true);

            AssignMaterials(made, slot.ModelPath);

            for (int i = 0; i < original.Length; i++)
                original[i].enabled = false;

            return true;
        }

        private static Bounds Measure(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            Bounds result = new Bounds();

            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled) continue;

                if (!any)
                {
                    result = renderers[i].bounds;
                    any = true;
                }
                else
                {
                    result.Encapsulate(renderers[i].bounds);
                }
            }

            return any ? result : new Bounds(go.transform.position, Vector3.zero);
        }

        /* ============================================================
           متریال
           ============================================================ */

        private static void AssignMaterials(Renderer[] renderers, string modelPath)
        {
            string baseName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/" + baseName + ".mat");

            if (material == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] shared = renderers[i].sharedMaterials;

                for (int m = 0; m < shared.Length; m++)
                    if (shared[m] != null) shared[m] = material;

                renderers[i].sharedMaterials = shared;
            }
        }

        private static int BuildMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { Root });
            int built = 0;

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder(Root, "Materials");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

                string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
                string folder = System.IO.Path.GetDirectoryName(path);

                Texture2D diffuse = FindTexture(folder, baseName, "_diff");
                if (diffuse == null) diffuse = FindTexture(folder, baseName, "_albedo");

                Texture2D normal = FindTexture(folder, baseName, "_nor_gl");
                if (normal == null) normal = FindTexture(folder, baseName, "_nor_dx");

                Texture2D rough = FindTexture(folder, baseName, "_rough");

                if (diffuse == null && normal == null)
                {
                    Debug.LogWarning("[Models] «" + baseName + "» بافت پیدا نشد. " +
                                     "پوشه‌ی textures را کنار fbx بگذار.");
                    continue;
                }

                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    MaterialFolder + "/" + baseName + ".mat");

                if (material == null)
                {
                    material = new Material(FindLitShader());
                    AssetDatabase.CreateAsset(material, MaterialFolder + "/" + baseName + ".mat");
                }
                else
                {
                    material.shader = FindLitShader();
                }

                if (diffuse != null) material.SetTexture("_BaseMap", diffuse);
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }

                if (rough != null)
                {
                    material.SetFloat("_Smoothness", 0.35f);
                    material.SetTexture("_MetallicGlossMap", null);
                }

                material.SetFloat("_Metallic", 0f);

                EditorUtility.SetDirty(material);
                built++;
            }

            return built;
        }

        private static Shader FindLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                Debug.LogWarning("[Models] شیدر URP پیدا نشد؛ Standard استفاده می‌شود.");
                shader = Shader.Find("Standard");
            }

            return shader;
        }

        /// <summary>بافت را کنار fbx و در پوشه‌ی textures جست‌وجو می‌کند.</summary>
        private static Texture2D FindTexture(string folder, string baseName, string suffix)
        {
            string[] candidates = { folder, folder + "/textures", folder + "/Textures" };
            string[] extensions = { ".jpg", ".png", ".jpeg", ".tga" };

            for (int c = 0; c < candidates.Length; c++)
            {
                if (!System.IO.Directory.Exists(candidates[c])) continue;

                string[] files = System.IO.Directory.GetFiles(candidates[c]);

                for (int f = 0; f < files.Length; f++)
                {
                    string name = System.IO.Path.GetFileName(files[f]);
                    string lower = name.ToLowerInvariant();

                    if (!lower.Contains(suffix)) continue;

                    bool matches = lower.StartsWith(baseName.ToLowerInvariant());

                    // نام بافت گاهی حرف اول را کوچک دارد (Armchair_01)
                    if (!matches && lower.Length > 1)
                        matches = lower.Substring(1).StartsWith(baseName.ToLowerInvariant().Substring(1));

                    if (!matches) continue;

                    bool known = false;
                    for (int e = 0; e < extensions.Length; e++)
                        if (lower.EndsWith(extensions[e])) known = true;

                    if (!known) continue;

                    string assetPath = files[f].Replace('\\', '/');
                    int index = assetPath.IndexOf("Assets/");

                    if (index < 0) continue;

                    return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath.Substring(index));
                }
            }

            return null;
        }

        private static void Report(List<string> errors, List<Slot> slots)
        {
            for (int i = 0; i < errors.Count; i++) Debug.LogError("[Models] " + errors[i]);

            int missing = 0;

            for (int i = 0; i < slots.Count; i++)
                if (!System.IO.File.Exists(slots[i].ModelPath)) missing++;

            if (slots.Count == 0)
                Debug.Log("[Models] فهرست خالی است؛ همه‌ی صحنه‌ها همان جعبه‌ها را دارند.");

            if (missing > 0)
                Debug.Log("[Models] " + missing + " جای خالی است (فایل دانلود نشده).");
        }
    }
}
