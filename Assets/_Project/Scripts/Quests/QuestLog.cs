using System.Collections.Generic;
using UnityEngine;
using TehranNoir.Dialogue;

namespace TehranNoir.Quests
{
    /// <summary>یک کار در جریان، همراه قدمی که همین حالا باید انجام شود.</summary>
    public sealed class QuestStatus
    {
        public readonly QuestDefinition Quest;
        public readonly QuestStep Step;

        public QuestStatus(QuestDefinition quest, QuestStep step)
        {
            Quest = quest;
            Step = step;
        }
    }

    /// <summary>
    /// خواندن کارها از `Resources/Quests` و نگه‌داشتن فهرست کارهای در جریان.
    /// هیچ چیزی در صحنه لازم نیست: خودش ساخته می‌شود (QuestBootstrap).
    /// وضعیت هر کار از WorldState می‌آید؛ تنها چیزی که این کلاس نگه می‌دارد
    /// «این کار قبلاً شروع شده» است، چون شرط شروع ممکن است موقتی باشد.
    /// </summary>
    public sealed class QuestLog : MonoBehaviour
    {
        public static QuestLog Instance { get; private set; }

        [Header("رفتار")]
        [Tooltip("در Console بنویس چه فایلی خوانده شد و چه ایرادی داشت")]
        [SerializeField] private bool logLoading = true;

        private readonly QuestBook _book = new QuestBook();
        private readonly List<QuestStatus> _active = new List<QuestStatus>();

        // کارها یک بار که شروع شوند، شروع‌شده می‌مانند؛ شرط شروع ممکن است
        // موقتی باشد (مثل «پرونده دست نادر است») ولی کار تا آخر باز می‌ماند.
        private readonly HashSet<string> _started = new HashSet<string>();

        /// <summary>کارهایی که شروع شده‌اند و تمام نشده‌اند، به ترتیب: اصلی، بعد جانبی.</summary>
        public IReadOnlyList<QuestStatus> Active { get { return _active; } }

        public QuestBook Book { get { return _book; } }

        /// <summary>اثر انگشت فهرست؛ با هر تغییر کار یا قدم عوض می‌شود.</summary>
        public int Signature { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;

            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            Recompute();
        }

        /* ============================================================
           خواندن
           ============================================================ */

        public void Load()
        {
            _book.Quests.Clear();
            _book.Issues.Clear();
            _started.Clear();

            TextAsset[] files = Resources.LoadAll<TextAsset>("Quests");

            if (files == null || files.Length == 0)
            {
                if (logLoading) Debug.LogWarning("[Quest] هیچ فایلی در Resources/Quests پیدا نشد.", this);
                return;
            }

            System.Array.Sort(files, CompareByName);

            for (int i = 0; i < files.Length; i++)
                QuestParser.Parse(files[i].text, files[i].name, _book);

            _book.Quests.Sort(CompareQuests);

            for (int i = 0; i < _book.Quests.Count; i++)
                _book.Quests[i].Order = i;

            if (!logLoading) return;

            for (int i = 0; i < _book.Issues.Count; i++)
                Debug.LogWarning("[Quest] " + _book.Issues[i], this);

            Debug.Log("[Quest] " + _book.Quests.Count + " کار خوانده شد از " + files.Length + " فایل.", this);
        }

        private static int CompareByName(TextAsset left, TextAsset right)
        {
            return string.CompareOrdinal(left.name, right.name);
        }

        private static int CompareQuests(QuestDefinition left, QuestDefinition right)
        {
            if (left.Kind != right.Kind) return left.Kind == QuestKind.Main ? -1 : 1;
            return left.Order.CompareTo(right.Order);
        }

        /* ============================================================
           وضعیت
           ============================================================ */

        private void Recompute()
        {
            _active.Clear();

            int hash = 17;

            WorldState world = WorldStateProvider.Instance != null
                ? WorldStateProvider.Instance.State
                : null;

            if (world == null)
            {
                Signature = hash;
                return;
            }

            for (int i = 0; i < _book.Quests.Count; i++)
            {
                QuestDefinition quest = _book.Quests[i];

                bool done = quest.IsDone(world);

                if (!done && quest.IsStarted(world))
                    _started.Add(quest.Id);

                if (done)
                {
                    _started.Remove(quest.Id);
                    continue;
                }

                if (!quest.Tracked) continue;
                if (!_started.Contains(quest.Id)) continue;

                QuestStep step = quest.CurrentStep(world);

                _active.Add(new QuestStatus(quest, step));

                hash = hash * 31 + quest.Id.GetHashCode();
                if (step != null) hash = hash * 31 + step.Id.GetHashCode();
            }

            Signature = hash;
        }
    }
}
