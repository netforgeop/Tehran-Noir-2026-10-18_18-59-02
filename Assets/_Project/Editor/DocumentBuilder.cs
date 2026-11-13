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
    /// لایه‌ی اسناد را می‌سازد (با فونت و فهرست توقف درست)، پرونده‌ی روی میز را
    /// به نسخه‌ی «بازشدنی» تبدیل می‌کند و مسیر سند را رویش می‌گذارد.
    ///
    /// چرا Editor؟ چون فایل صحنه در گیت نیست و هر تغییر باید دستور دقیق داشته باشد؛
    /// این‌طور یک کلیک همان کار را می‌کند و بعدش همه‌چیز آبجکت واقعی صحنه است.
    /// </summary>
    public static class DocumentBuilder
    {
        private const string RootName = "DocumentLayer";
        private const string DocumentPath = "Documents/file_sealed";
        private const string OpenedVariable = "flag.file.opened";

        /// <summary>پرونده‌ی داستان با همین شرط روی میز ظاهر می‌شود؛ نشانه‌ی قطعی خودِ پرونده است.</summary>
        private const string FileRequireVariable = "flag.quest.file";

        /// <summary>همان فیلدهایی که هنگام تعویض کامپوننت باید منتقل شوند؛ نامشان در هر دو کلاس یکی است.</summary>
        private static readonly string[] CopyFields =
        {
            "action",
            "interactionPoint",
            "maxDistance",
            "maxAngle",
            "displayName",
            "subtype",
            "outline",
            "requireVariable",
            "requireValue",
            "takenVariable",
            "setOnTakeVariable",
            "setOnTakeValue"
        };

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

            FileInteractable file = FindOrConvertFile();

            if (file != null)
            {
                SetString(file, "documentPath", DocumentPath);
                SetString(file, "openedVariable", OpenedVariable);
                EditorUtility.SetDirty(file);
            }
            else
            {
                Debug.LogWarning("[Document] پرونده‌ی روی میز پیدا نشد؛ فقط لایه ساخته شد. "
                    + "روی آبجکت پرونده یک PickupInteractable با Require Variable = "
                    + FileRequireVariable + " لازم است تا تبدیل شود.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Document] آماده است. پرونده: " + DocumentPath
                + " — تست: Play، گرفتن کار، نزدیک میز؛ ۱ باز کردن، ۲ برداشتن، بعد از باز شدن ۱ خواندن.");
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
           پرونده: پیدا کردن یا تبدیل کردن
           ============================================================ */

        private static FileInteractable FindOrConvertFile()
        {
            FileInteractable existing = UnityEngine.Object.FindAnyObjectByType<FileInteractable>(FindObjectsInactive.Include);

            if (existing != null) return existing;

            PickupInteractable[] pickups = UnityEngine.Object.FindObjectsByType<PickupInteractable>(
                FindObjectsInactive.Include);

            for (int i = 0; i < pickups.Length; i++)
            {
                PickupInteractable pickup = pickups[i];

                if (pickup == null || pickup is FileInteractable) continue;
                if (ReadString(pickup, "requireVariable") != FileRequireVariable) continue;

                return Convert(pickup);
            }

            return null;
        }

        /// <summary>
        /// کامپوننت را عوض می‌کند ولی تنظیمات را نگه می‌دارد:
        /// FileInteractable فرزند PickupInteractable است، پس همه‌ی فیلدهای قبلی معنی خودشان را دارند
        /// و فقط دو کار به آن اضافه می‌شود: باز کردن و خواندن.
        /// </summary>
        private static FileInteractable Convert(PickupInteractable pickup)
        {
            GameObject go = pickup.gameObject;

            FileInteractable file = Undo.AddComponent<FileInteractable>(go);

            SerializedObject source = new SerializedObject(pickup);
            SerializedObject target = new SerializedObject(file);

            for (int i = 0; i < CopyFields.Length; i++)
            {
                string field = CopyFields[i];
                Copy(source.FindProperty(field), target.FindProperty(field));
            }

            target.ApplyModifiedPropertiesWithoutUndo();

            Undo.DestroyObjectImmediate(pickup);
            EditorUtility.SetDirty(file);

            Debug.Log("[Document] «" + go.name + "» از PickupInteractable به FileInteractable تبدیل شد: "
                + "دو کار شد، باز کردن و برداشتن.", go);

            return file;
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

        private static string ReadString(UnityEngine.Object target, string field)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            return property != null ? property.stringValue : string.Empty;
        }

        /// <summary>کپی مقدار بر اساس نوع فیلد؛ فیلد ناموجود یا نوع ناشناس بی‌صدا رد می‌شود.</summary>
        private static void Copy(SerializedProperty from, SerializedProperty to)
        {
            if (from == null || to == null) return;

            switch (from.propertyType)
            {
                case SerializedPropertyType.String:
                    to.stringValue = from.stringValue;
                    break;

                case SerializedPropertyType.ObjectReference:
                    to.objectReferenceValue = from.objectReferenceValue;
                    break;

                case SerializedPropertyType.Float:
                    to.floatValue = from.floatValue;
                    break;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    to.intValue = from.intValue;
                    break;

                case SerializedPropertyType.Boolean:
                    to.boolValue = from.boolValue;
                    break;
            }
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
