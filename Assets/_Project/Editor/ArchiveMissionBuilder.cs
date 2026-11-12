using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TehranNoir.Dialogue;
using TehranNoir.Interaction;

namespace TehranNoir.EditorTools
{
    /// <summary>
    /// ساخت مأموریت بایگانی داخل صحنه‌ی Sandbox: راهرو، اتاق بایگانی، قفسه‌ها،
    /// دو NPC تازه، دفتر ثبت، یادداشت و جعبه‌ی ۱۳۵۲.
    ///
    /// چرا اسکریپت Editor؟ چون فایل صحنه در گیت نیست و این‌طوری همه‌ی آبجکت‌ها
    /// با یک کلیک، با همان کامپوننت‌ها و همان مقدارهای Inspector ساخته می‌شوند.
    /// بعد از ساخت، همه چیز آبجکت واقعی صحنه است و دستی هم قابل تغییر.
    /// </summary>
    public static class ArchiveMissionBuilder
    {
        private const string RootName = "ArchiveWing";

        /// <summary>
        /// شرطی که کار «دادن» را روی آدم‌ها نشان می‌دهد. از خود آیتم می‌پرسد
        /// («حداقل یکی همراهت هست؟»)، نه از پرچم کپی‌شده؛ پس با انداختن یا
        /// تحویل، از واقعیت جدا نمی‌شود.
        /// </summary>
        public const string HandoverCondition = "item.file.sealed >= 1";
        private const string DialogueFolder = "Assets/_Project/Dialogue/";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Generated";

        /* ============================================================
           منو
           ============================================================ */

        [MenuItem("Tehran Noir/ساخت بایگانی و مأموریت", false, 100)]
        public static void BuildArchive()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Archive] در حالت Play نمی‌شود ساخت. اول Play را قطع کن.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();

            if (!scene.IsValid() || scene.name != "Sandbox")
            {
                Debug.LogError("[Archive] صحنه‌ی Sandbox را باز کن، بعد این منو را بزن. صحنه‌ی فعلی: " + scene.name);
                return;
            }

            DialogueUI ui = Object.FindAnyObjectByType<DialogueUI>();

            if (ui == null)
            {
                Debug.LogError("[Archive] DialogueUI در صحنه پیدا نشد. آبجکت DialogueSystem را چک کن.");
                return;
            }

            RemoveExisting();

            Material wall = EnsureMaterial("ArchiveWall", new Color(0.40f, 0.36f, 0.30f));
            Material floor = EnsureMaterial("ArchiveFloor", new Color(0.20f, 0.18f, 0.15f));
            Material wood = EnsureMaterial("ArchiveWood", new Color(0.33f, 0.24f, 0.16f));
            Material paper = EnsureMaterial("ArchivePaper", new Color(0.80f, 0.77f, 0.68f));
            Material cardboard = EnsureMaterial("ArchiveCardboard", new Color(0.55f, 0.44f, 0.29f));
            Material coat = EnsureMaterial("ArchiveCoat", new Color(0.24f, 0.25f, 0.23f));

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build archive mission");

            BuildCorridor(root.transform, wall, floor);
            BuildRoom(root.transform, wall, floor);
            BuildShelves(root.transform, wood, paper, cardboard);
            BuildCounter(root.transform, wood, paper);
            BuildLights(root.transform);

            Person(root.transform, "Farhadi", new Vector3(0f, 0.9f, -11.7f), coat,
                "خانم فرهادی|Mrs. Farhadi", "بایگان|Archivist",
                "farhadi_archive", "handover", ui);

            Person(root.transform, "Mahmoud", new Vector3(0.55f, 0.9f, -6.2f), coat,
                "محمود|Mahmoud", "خدمه‌ی ساختمان|Building porter",
                "mahmoud_corridor", string.Empty, ui);

            Readable(root.transform, "Logbook", new Vector3(1f, 1.10f, -10.5f),
                new Vector3(0.36f, 0.07f, 0.26f), paper,
                "دفتر ثبت|Logbook", "۱۳۵۲ تا ۱۳۵۳|1352 to 1353",
                "archive_log", "read", ui);

            Readable(root.transform, "Note", new Vector3(-1f, 1.095f, -10.5f),
                new Vector3(0.22f, 0.012f, 0.30f), paper,
                "یادداشت|Note", "دست‌نویس|Handwritten",
                "note_yazdani", "read", ui);

            Box1352(root.transform, cardboard);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;

            Debug.Log("[Archive] ساخته شد: راهرو، اتاق بایگانی، ۶ قفسه، ۲ NPC تازه، " +
                      "دفتر ثبت، یادداشت و جعبه‌ی ۱۳۵۲. صحنه ذخیره شد.");
        }

        [MenuItem("Tehran Noir/حذف بایگانی", false, 101)]
        public static void RemoveArchive()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Archive] در حالت Play نمی‌شود حذف کرد.");
                return;
            }

            bool removed = RemoveExisting();
            Scene scene = SceneManager.GetActiveScene();

            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(removed ? "[Archive] بایگانی حذف شد." : "[Archive] بایگانی‌ای در صحنه نبود.");
        }

        private static bool RemoveExisting()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing == null) return false;

            Undo.DestroyObjectImmediate(existing);
            return true;
        }

        /* ============================================================
           اتاق و راهرو
           ============================================================ */

        /// <summary>ورودی بایگانی از در جنوبی دفتر است: راهروی ۲٫۴ متری تا اتاق ۱۲×۷.</summary>
        private static void BuildCorridor(Transform parent, Material wall, Material floor)
        {
            Box(parent, "Corridor_Floor", new Vector3(0f, -0.05f, -6.5f), new Vector3(2.4f, 0.1f, 5.2f), floor);

            Box(parent, "Corridor_Wall_W", new Vector3(-1.2f, 0.6f, -6.5f), new Vector3(0.2f, 1.2f, 5.2f), wall);
            Box(parent, "Corridor_Wall_E", new Vector3(1.2f, 0.6f, -6.5f), new Vector3(0.2f, 1.2f, 5.2f), wall);

            // چراغ سقفی ساده‌ی راهرو
            Box(parent, "Corridor_Lamp", new Vector3(0f, 2.2f, -6.5f), new Vector3(0.5f, 0.06f, 0.2f), wall);
        }

        private static void BuildRoom(Transform parent, Material wall, Material floor)
        {
            Box(parent, "Archive_Floor", new Vector3(0f, -0.05f, -12.5f), new Vector3(12f, 0.1f, 7.2f), floor);

            // دیوار شمالی، دو طرف درِ ورودی (در: x بین ۱- و ۱)
            Box(parent, "Archive_Wall_N_W", new Vector3(-3.5f, 0.6f, -9f), new Vector3(5f, 1.2f, 0.2f), wall);
            Box(parent, "Archive_Wall_N_E", new Vector3(3.5f, 0.6f, -9f), new Vector3(5f, 1.2f, 0.2f), wall);

            Box(parent, "Archive_Wall_S", new Vector3(0f, 0.6f, -16f), new Vector3(12.4f, 1.2f, 0.2f), wall);
            Box(parent, "Archive_Wall_W", new Vector3(-6f, 0.6f, -12.5f), new Vector3(0.2f, 1.2f, 7.2f), wall);
            Box(parent, "Archive_Wall_E", new Vector3(6f, 0.6f, -12.5f), new Vector3(0.2f, 1.2f, 7.2f), wall);

            Box(parent, "Archive_Lamp", new Vector3(0f, 2.2f, -12.5f), new Vector3(1.2f, 0.06f, 0.3f), wall);
        }

        private static void BuildShelves(Transform parent, Material wood, Material paper, Material cardboard)
        {
            float[] rows = { -10.6f, -12.4f, -14.2f };

            for (int i = 0; i < rows.Length; i++)
            {
                Shelf(parent, "Shelf_E" + (i + 1), new Vector3(5.6f, 0f, rows[i]), wood, paper, cardboard);
                Shelf(parent, "Shelf_W" + (i + 1), new Vector3(-5.6f, 0f, rows[i]), wood, paper, cardboard);
            }

            // دو صندوق روی زمین، گوشه‌ی جنوب غربی
            Box(parent, "Crate_1", new Vector3(-4.2f, 0.3f, -14.8f), new Vector3(0.9f, 0.6f, 0.9f), cardboard);
            Box(parent, "Crate_2", new Vector3(-3.1f, 0.25f, -15.2f), new Vector3(0.7f, 0.5f, 0.7f), cardboard);
        }

        /// <summary>یک قفسه‌ی ساده: دو دیوارک + سه تخته + چند جعبه‌ی کاغذی.</summary>
        private static void Shelf(Transform parent, string name, Vector3 center, Material wood, Material paper, Material cardboard)
        {
            Box(parent, name + "_Panel_A", center + new Vector3(0f, 1.0f, -0.7f), new Vector3(0.6f, 2.0f, 0.08f), wood);
            Box(parent, name + "_Panel_B", center + new Vector3(0f, 1.0f, 0.7f), new Vector3(0.6f, 2.0f, 0.08f), wood);
            Box(parent, name + "_Back", center + new Vector3(0.26f, 1.0f, 0f), new Vector3(0.08f, 2.0f, 1.4f), wood);

            float[] levels = { 0.55f, 1.15f, 1.75f };

            for (int i = 0; i < levels.Length; i++)
            {
                Box(parent, name + "_Board_" + i, center + new Vector3(0f, levels[i], 0f), new Vector3(0.62f, 0.06f, 1.42f), wood);

                Box(parent, name + "_Box_" + i + "a", center + new Vector3(-0.05f, levels[i] + 0.20f, -0.42f),
                    new Vector3(0.4f, 0.24f, 0.34f), i % 2 == 0 ? paper : cardboard);

                Box(parent, name + "_Box_" + i + "b", center + new Vector3(-0.05f, levels[i] + 0.20f, 0.30f),
                    new Vector3(0.38f, 0.22f, 0.5f), cardboard);
            }
        }

        private static void BuildCounter(Transform parent, Material wood, Material paper)
        {
            Box(parent, "Counter", new Vector3(0f, 0.5f, -10.5f), new Vector3(3.2f, 1.0f, 0.8f), wood);
            Box(parent, "Counter_Top", new Vector3(0f, 1.04f, -10.5f), new Vector3(3.4f, 0.08f, 0.95f), wood);

            // یک پوشه‌ی باز روی پیشخوان، فقط برای حس مکان
            Box(parent, "Papers", new Vector3(1.35f, 1.10f, -10.35f), new Vector3(0.4f, 0.02f, 0.3f), paper);
        }

        private static void BuildLights(Transform parent)
        {
            PointLight(parent, "Light_Archive", new Vector3(0f, 2.1f, -12.3f), 1.7f, 9f);
            PointLight(parent, "Light_Corridor", new Vector3(0f, 2.1f, -6.5f), 1.2f, 6f);
        }

        private static void PointLight(Transform parent, string name, Vector3 position, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.87f, 0.68f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        /* ============================================================
           آدم‌ها، خواندنی‌ها، جعبه
           ============================================================ */

        private static void Person(Transform parent, string name, Vector3 position, Material material,
            string displayName, string subtype, string dialogueFile, string handoverNode, DialogueUI ui)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            TalkInteractable talk = go.AddComponent<TalkInteractable>();
            SetString(talk, "displayName", displayName);
            SetString(talk, "subtype", subtype);

            DialogueStarter starter = go.AddComponent<DialogueStarter>();
            SetObject(starter, "dialogueFile", LoadDialogue(dialogueFile));
            SetObject(starter, "dialogueUI", ui);

            SetObject(talk, "dialogueStarter", starter);

            if (!string.IsNullOrEmpty(handoverNode))
            {
                SetString(talk, "handoverVariable", HandoverCondition);
                SetString(talk, "handoverNode", handoverNode);
            }
        }

        /// <summary>هر چیز خواندنی: مش + SimpleInteractable با کار Read + DialogueStarter.</summary>
        private static void Readable(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            string displayName, string subtype, string dialogueFile, string nodeId, DialogueUI ui)
        {
            GameObject go = Box(parent, name, position, size, material);

            SimpleInteractable read = go.AddComponent<SimpleInteractable>();
            SetEnum(read, "action", (int)InteractionAction.Read);
            SetString(read, "displayName", displayName);
            SetString(read, "subtype", subtype);
            SetString(read, "startNodeId", nodeId);

            DialogueStarter starter = go.AddComponent<DialogueStarter>();
            SetObject(starter, "dialogueFile", LoadDialogue(dialogueFile));
            SetObject(starter, "dialogueUI", ui);

            SetObject(read, "dialogueStarter", starter);
        }

        private static void Box1352(Transform parent, Material cardboard)
        {
            GameObject go = Box(parent, "Box_1352", new Vector3(5.6f, 1.39f, -12.4f), new Vector3(0.55f, 0.42f, 0.55f), cardboard);

            PickupInteractable pickup = go.AddComponent<PickupInteractable>();
            SetString(pickup, "displayName", "جعبه‌ی ۱۳۵۲|Box 1352");
            SetString(pickup, "subtype", "قفسه‌ی شرقی|East shelf");
            SetString(pickup, "requireVariable", "flag.clue.log");
            SetString(pickup, "requireValue", "true");
            SetString(pickup, "takenVariable", "flag.clue.box");
        }

        /* ============================================================
           ابزار
           ============================================================ */

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;

            return go;
        }

        private static TextAsset LoadDialogue(string fileName)
        {
            string path = DialogueFolder + fileName + ".txt";
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            if (asset == null)
                Debug.LogError("[Archive] فایل دیالوگ پیدا نشد: " + path + " — شاید Unity تازه‌سازی نشده.");

            return asset;
        }

        private static Material EnsureMaterial(string name, Color color)
        {
            EnsureFolder(MaterialFolder);

            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                material = new Material(shader);
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);

            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int slash = path.LastIndexOf('/');
            if (slash <= 0) return;

            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetString(Object target, string field, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError("[Archive] فیلد «" + field + "» روی " + target.GetType().Name + " پیدا نشد.");
                return;
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError("[Archive] فیلد «" + field + "» روی " + target.GetType().Name + " پیدا نشد.");
                return;
            }

            property.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError("[Archive] فیلد «" + field + "» روی " + target.GetType().Name + " پیدا نشد.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
