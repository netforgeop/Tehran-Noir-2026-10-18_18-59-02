using System.Collections.Generic;
using UnityEngine;

namespace TehranNoir.Items
{
    /// <summary>
    /// آیتمی که بازیکن می‌اندازد را روی زمین می‌گذارد.
    ///
    /// به رویداد `Dropped` گوش می‌دهد؛ هر جای بازی که چیزی انداخته شود،
    /// اینجا یک آبجکت ساخته می‌شود که همان نمونه را نگه می‌دارد. برداشتنش
    /// دوباره از همان راه برمی‌گردد (WorldItemPickup → InventoryManager.Move).
    ///
    /// اگر آیتم Prefab داشته باشد (`prefab=` در items.txt) همان ساخته می‌شود؛
    /// وگرنه یک مکعب کوچک. پس تا وقتی مدل‌ها آماده نیستند هم بازی کار می‌کند.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemDropSpawner : MonoBehaviour
    {
        [Header("جای افتادن")]
        [Tooltip("از این نقطه به جلو انداخته می‌شود. خالی = همین آبجکت")]
        [SerializeField] private Transform dropOrigin;

        [Tooltip("چند متر جلوتر از بازیکن")]
        [SerializeField] private float forwardOffset = 0.9f;

        [Tooltip("بلندی از زمین؛ تا نصفه داخل زمین نرود")]
        [SerializeField] private float groundOffset = 0.12f;

        [Header("ظاهر پیش‌فرض")]
        [Tooltip("اندازه‌ی مکعب جایگزین، وقتی آیتم Prefab ندارد")]
        [SerializeField] private float fallbackSize = 0.18f;

        [Tooltip("رنگ مکعب‌های جایگزین")]
        [SerializeField] private Color fallbackColor = new Color(0.62f, 0.53f, 0.31f, 1f);

        [Header("لاگ")]
        [SerializeField] private bool logDrops = true;

        private readonly Dictionary<string, GameObject> _prefabs =
            new Dictionary<string, GameObject>(8);

        private MaterialPropertyBlock _tint;

        /// <summary>نمونه‌ی فعال صحنه؛ اگر نبود ساخته می‌شود.</summary>
        public static ItemDropSpawner Runtime { get; private set; }

        /// <summary>تست خودکار این را خاموش می‌کند تا در صحنه مکعب نریزد.</summary>
        public static bool Spawning = true;

        /// <summary>
        /// تضمین وجود شنونده‌ی انداختن.
        ///
        /// چرا خودکار؟ چون اگر این کامپوننت را یادت برود روی Player بگذاری،
        /// انداختن بی‌صدا هیچ کاری نمی‌کند و پیدا کردن علتش سخت است. حالا
        /// اولین انداختن، خودش میزبان را می‌سازد.
        /// </summary>
        public static ItemDropSpawner Ensure()
        {
            if (Runtime != null) return Runtime;

            ItemDropSpawner found = FindAnyObjectByType<ItemDropSpawner>(FindObjectsInactive.Include);

            if (found != null)
            {
                Runtime = found;
                return found;
            }

            GameObject host = new GameObject("Item Drops");
            ItemDropSpawner spawner = host.AddComponent<ItemDropSpawner>();

            Runtime = spawner;

            return spawner;
        }

        private void Awake()
        {
            if (Runtime != null && Runtime != this)
            {
                // دو نسخه یعنی هر انداختن دو شیء می‌سازد؛ دومی خاموش می‌شود.
                Debug.LogWarning("[Items] بیش از یک ItemDropSpawner در صحنه است؛ دومی غیرفعال شد.", this);
                enabled = false;
                return;
            }

            Runtime = this;
        }

        private void OnDestroy()
        {
            if (Runtime == this) Runtime = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Runtime = null;
        }

        private void OnEnable()
        {
            ItemEvents.Dropped += HandleDropped;
        }

        private void OnDisable()
        {
            ItemEvents.Dropped -= HandleDropped;
        }

        private void HandleDropped(ItemEvent itemEvent)
        {
            if (!Spawning) return;
            if (itemEvent == null || itemEvent.Instance == null) return;

            // فقط انداختن روی زمین؛ انتقال بین ظرف‌ها رویداد جدا دارد.
            if (itemEvent.ContainerId != InventoryManager.WorldId) return;

            Transform origin = dropOrigin != null ? dropOrigin : ResolveOrigin();

            if (origin == null)
            {
                Debug.LogWarning("[Items] نقطه‌ی انداختن پیدا نشد؛ بازیکن در صحنه نیست.", this);
                return;
            }

            Vector3 position = origin.position + origin.forward * forwardOffset;

            // زمین را پیدا کن تا شیء نصفه داخل زمین یا معلق در هوا نماند.
            Ray ray = new Ray(position + Vector3.up * 2f, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 6f))
                position.y = hit.point.y + groundOffset;
            else
                position.y = origin.position.y + groundOffset;

            SpawnAt(itemEvent.Instance, position);
        }

        /// <summary>اگر Drop Origin خالی بود: خود بازیکن، وگرنه همین آبجکت.</summary>
        private Transform ResolveOrigin()
        {
            TehranNoir.Player.PlayerController player =
                FindAnyObjectByType<TehranNoir.Player.PlayerController>();

            if (player != null) return player.transform;

            return transform;
        }

        /// <summary>
        /// ساختن آبجکت زمین برای یک نمونه، در نقطه‌ی داده‌شده.
        /// عمومی است تا مأموریت بتواند آیتم از پیش افتاده بسازد.
        /// </summary>
        public WorldItemPickup SpawnAt(ItemInstance instance, Vector3 position)
        {
            if (instance == null) return null;

            ItemDefinition definition = instance.Definition;

            GameObject host = CreateHost(definition);

            if (host == null) return null;

            host.name = "Item_" + definition.Id;
            host.transform.SetPositionAndRotation(position, Quaternion.identity);
            host.transform.localScale = Vector3.one;

            if (host.GetComponent<Collider>() == null)
                host.AddComponent<BoxCollider>();

            WorldItemPickup pickup = host.GetComponent<WorldItemPickup>();

            if (pickup == null) pickup = host.AddComponent<WorldItemPickup>();

            pickup.Bind(instance);

            if (logDrops)
                Debug.Log("[Items] روی زمین افتاد: " + definition.Id, host);

            return pickup;
        }

        private GameObject CreateHost(ItemDefinition definition)
        {
            GameObject prefab = LoadPrefab(definition);

            if (prefab != null)
            {
                GameObject made = Instantiate(prefab);

                made.transform.localScale = Vector3.one;

                return made;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            cube.transform.localScale = Vector3.one * Mathf.Max(0.05f, fallbackSize);

            Renderer renderer = cube.GetComponent<Renderer>();

            if (renderer != null)
            {
                if (_tint == null) _tint = new MaterialPropertyBlock();

                renderer.GetPropertyBlock(_tint);
                _tint.SetColor("_BaseColor", fallbackColor);
                _tint.SetColor("_Color", fallbackColor);
                renderer.SetPropertyBlock(_tint);
            }

            return cube;
        }

        private GameObject LoadPrefab(ItemDefinition definition)
        {
            string path = definition.PrefabPath;

            if (string.IsNullOrEmpty(path)) return null;

            GameObject prefab;

            if (_prefabs.TryGetValue(path, out prefab)) return prefab;

            prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
                Debug.LogWarning("[Items] Prefab پیدا نشد: Resources/" + path + "؛ مکعب جایگزین ساخته شد.");

            _prefabs[path] = prefab;

            return prefab;
        }
    }
}
