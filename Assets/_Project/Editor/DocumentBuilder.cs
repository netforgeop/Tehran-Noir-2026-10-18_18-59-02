using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TehranNoir.Dialogue;
using TehranNoir.Documents;
using TehranNoir.Interaction;

namespace TehranNoir.EditorTools
{
    /// <summary>
    /// اتصال سند کاغذی به صحنه، بدون کار دستی:
    /// لایه‌ی اسناد را می‌سازد (با فونت و فهرست توقف درست) و مسیر سند را روی پرونده می‌گذارد.
    ///
    /// چرا Editor؟ چون فایل صحنه در گیت نیست و هر تغییر باید دستور دقیق داشته باشد؛
    /// این‌طور یک کلیک همان کار را می‌کند و بعدش همه‌چیز آبجکت واقعی صحنه است.
    /// </summary>
    public static class DocumentBuilder
    {
        private const string RootName = "DocumentLayer";
        private const string DocumentPath = "Documents/file_sealed";

        [MenuItem("Tehran Noir/ساخت لایه‌ی اسناد و اتصال پرونده", false, 120)]
        public static void BuildDocuments()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Document] در حالت Play نمی‌شود ساخت. اول Play را قطع کن.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();

            if (!scene.IsValid())
            {
                Debug.LogError("[Document] صحنه‌ی فعال معتبر نیست.");
                return;
            }

            DialogueUI ui = UnityEngine.Object.FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);

            if (ui == null)
            {
                Debug.LogError("[Document] DialogueUI در صحنه نیست. اول سیستم دیالوگ را راه بینداز.");
                return;
            }

            DocumentView view = FindView();

            if (view == null)
            {
                GameObject host = new GameObject(RootName);
                view = host.AddComponent<DocumentView>();
                Undo.RegisterCreatedObjectUndo(host, "DocumentLayer");
            }

            SetObject(view, "font", ui.Font);
            SetFreezeList(view);
            EditorUtility.SetDirty(view);

            FileInteractable file = UnityEngine.Object.FindAnyObjectByType<FileInteractable>(FindObjectsInactive.Include);

            if (file != null)
            {
                SetString(file, "documentPath", DocumentPath);
                EditorUtility.SetDirty(file);
            }
            else
            {
                Debug.LogWarning("[Document] FileInteractable در صحنه نبود؛ فقط لایه ساخته شد. "
                    + "مسیر سند را دستی روی کامپوننت بگذار: " + DocumentPath);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Document] آماده است. پرونده: " + DocumentPath + " — تست: Play، گرفتن کار، برداشتن پرونده، کلید ۱.");
        }

        [MenuItem("Tehran Noir/حذف لایه‌ی اسناد", false, 121)]
        public static void RemoveDocuments()
        {
            DocumentView view = FindView();

            if (view == null)
            {
                Debug.LogWarning("[Document] لایه‌ی اسناد در صحنه نیست.");
                return;
            }

            Undo.DestroyObjectImmediate(view.gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[Document] لایه حذف شد.");
        }

        /* ============================================================
           ابزار
           ============================================================ */

        private static DocumentView FindView()
        {
            return UnityEngine.Object.FindAnyObjectByType<DocumentView>(FindObjectsInactive.Include);
        }

        /// <summary>همان چهار کامپوننتی که هنگام باز بودن سند باید خاموش شوند.</summary>
        private static void SetFreezeList(DocumentView view)
        {
            MonoBehaviour player = UnityEngine.Object.FindAnyObjectByType<TehranNoir.Player.PlayerController>(FindObjectsInactive.Include);
            MonoBehaviour camera = UnityEngine.Object.FindAnyObjectByType<TehranNoir.Cameras.CameraOrbitController>(FindObjectsInactive.Include);
            MonoBehaviour input = UnityEngine.Object.FindAnyObjectByType<TehranNoir.Inputs.PlayerInputReader>(FindObjectsInactive.Include);
            MonoBehaviour sensor = UnityEngine.Object.FindAnyObjectByType<InteractionSensor>(FindObjectsInactive.Include);

            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty property = serialized.FindProperty("freezeWhileOpen");

            if (property == null)
            {
                Debug.LogError("[Document] فیلد freezeWhileOpen پیدا نشد.");
                return;
            }

            property.arraySize = 0;

            AddToFreeze(property, player);
            AddToFreeze(property, camera);
            AddToFreeze(property, input);
            AddToFreeze(property, sensor);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (property.arraySize == 0)
                Debug.LogWarning("[Document] هیچ کامپوننتی برای توقف پیدا نشد؛ سند بازی را متوقف نمی‌کند.");
        }

        private static void AddToFreeze(SerializedProperty array, MonoBehaviour component)
        {
            if (component == null) return;

            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            array.GetArrayElementAtIndex(index).objectReferenceValue = component;
        }

        private static void SetString(UnityEngine.Object target, string field, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError("[Document] فیلد «" + field + "» روی " + target.GetType().Name + " پیدا نشد.");
                return;
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError("[Document] فیلد «" + field + "» روی " + target.GetType().Name + " پیدا نشد.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
