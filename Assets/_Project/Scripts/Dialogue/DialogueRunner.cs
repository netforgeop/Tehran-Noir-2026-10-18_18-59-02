using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TehranNoir.Dialogue
{
    /// موتور اجرای دیالوگ: گره به گره جلو می‌رود، شرط‌ها را می‌سنجد و خط یا گزینه‌ها را بیرون می‌دهد.
    public sealed class DialogueRunner
    {
        private const int MaxStepsPerAdvance = 4000;

        private static readonly Regex EffectPattern =
            new Regex(@"^\s*([A-Za-z0-9_.]+)\s*([+\-=])\s*(.+?)\s*$");

        private readonly List<OptionItem> _options = new List<OptionItem>();
        private int _index;

        public DialogueDocument Document { get; private set; }
        public WorldState World { get; private set; }
        public DialogueNode Node { get; private set; }
        public SayItem CurrentLine { get; private set; }
        public bool IsRunning { get; private set; }

        public IReadOnlyList<OptionItem> CurrentOptions { get { return _options; } }
        public bool IsWaitingForChoice { get { return _options.Count > 0; } }

        public event Action<SayItem> LineShown;
        public event Action<IReadOnlyList<OptionItem>> OptionsShown;
        public event Action Finished;

        /// <summary>یک سند کاغذی باید باز شود؛ مسیرش را می‌دهد و منتظر می‌ماند تا Continue صدا زده شود.</summary>
        public event Action<string> DocumentRequested;

        public void Begin(DialogueDocument document, WorldState world, string startNodeId = null)
        {
            Document = document;
            World = world ?? new WorldState();
            CurrentLine = null;
            _options.Clear();
            _index = 0;
            IsRunning = false;

            if (Document == null || Document.Nodes.Count == 0) return;

            string nodeId = string.IsNullOrEmpty(startNodeId) ? Document.Nodes[0].Id : startNodeId;
            Node = Document.FindNode(nodeId);
            if (Node == null) Node = Document.Nodes[0];

            IsRunning = true;
            Step();
        }

        /// رفتن به خط بعدی (کلیک روی جعبه‌ی دیالوگ یا دکمه‌ی B).
        public void Advance()
        {
            if (!IsRunning || IsWaitingForChoice) return;
            if (CurrentLine == null) return;

            CurrentLine = null;
            Step();
        }

        /// بعد از بسته شدن سند، دیالوگ از همان‌جا ادامه می‌دهد.
        public void Continue()
        {
            if (!IsRunning) return;
            if (IsWaitingForChoice) return;

            CurrentLine = null;
            Step();
        }

        /// انتخاب گزینه‌ی شماره‌ی index.
        public void Choose(int index)
        {
            if (!IsRunning || !IsWaitingForChoice) return;
            if (index < 0 || index >= _options.Count) return;

            OptionItem chosen = _options[index];
            _options.Clear();
            CurrentLine = null;

            if (!Jump(chosen.Target)) return;
            Step();
        }

        public void Stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            CurrentLine = null;
            _options.Clear();
            Node = null;

            if (Finished != null) Finished();
        }

        private void Step()
        {
            int guardSteps = 0;

            while (true)
            {
                guardSteps++;
                if (guardSteps > MaxStepsPerAdvance)
                {
                    Stop();
                    return;
                }

                if (Node == null)
                {
                    Stop();
                    return;
                }

                if (_index >= Node.Items.Count)
                {
                    Stop();
                    return;
                }

                DialogueItem item = Node.Items[_index];
                _index++;

                bool available = IsItemAvailable(item, World);

                if (item is IfItem || item is ElseItem) continue;

                RedirectItem redirect = item as RedirectItem;
                if (redirect != null)
                {
                    if (available && !string.IsNullOrEmpty(redirect.Target))
                    {
                        if (!Jump(redirect.Target)) return;
                    }
                    continue;
                }

                ReadItem read = item as ReadItem;
                if (read != null)
                {
                    if (available)
                    {
                        if (DocumentRequested != null)
                        {
                            CurrentLine = null;
                            DocumentRequested(read.Path);
                            return;
                        }

                        // کسی گوش نمی‌دهد؛ دیالوگ نباید قفل شود
                        Debug.LogWarning("[Dialogue] تگ [read] آمد ولی DocumentView در صحنه نیست: " + read.Path);
                    }
                    continue;
                }

                EffectItem effect = item as EffectItem;
                if (effect != null)
                {
                    if (available) ApplyEffect(effect.Source, World);
                    continue;
                }

                SayItem say = item as SayItem;
                if (say != null)
                {
                    if (!available) continue;

                    CurrentLine = say;
                    if (LineShown != null) LineShown(say);
                    return;
                }

                OptionItem option = item as OptionItem;
                if (option != null)
                {
                    _options.Clear();
                    if (available) _options.Add(option);

                    while (_index < Node.Items.Count && Node.Items[_index] is OptionItem)
                    {
                        OptionItem next = (OptionItem)Node.Items[_index];
                        _index++;
                        if (IsItemAvailable(next, World)) _options.Add(next);
                    }

                    if (_options.Count == 0) continue;

                    CurrentLine = null;
                    if (OptionsShown != null) OptionsShown(_options);
                    return;
                }

                GoItem go = item as GoItem;
                if (go != null)
                {
                    if (available && !string.IsNullOrEmpty(go.Target))
                    {
                        if (!Jump(go.Target)) return;
                    }
                    continue;
                }

                if (item is EndItem)
                {
                    if (available)
                    {
                        Stop();
                        return;
                    }
                    continue;
                }
            }
        }

        private bool Jump(string target)
        {
            DialogueNode node = Document.FindNode(target);
            if (node == null)
            {
                Stop();
                return false;
            }

            Node = node;
            _index = 0;
            CurrentLine = null;
            return true;
        }

        /// شرط چسبان + شرط خودِ پرش شرطی؛ هر دو باید برقرار باشند.
        public static bool IsItemAvailable(DialogueItem item, WorldState world)
        {
            if (item == null) return false;
            if (!IsGuardSatisfied(item.Guard, world)) return false;

            RedirectItem redirect = item as RedirectItem;
            if (redirect != null && redirect.When != null)
                return redirect.When.IsTrue(world);

            return true;
        }

        public static bool IsGuardSatisfied(List<Condition> guard, WorldState world)
        {
            if (guard == null || guard.Count == 0) return true;

            for (int i = 0; i < guard.Count; i++)
                if (!guard[i].IsTrue(world)) return false;

            return true;
        }

        /// [do] rel.reza +5   /   [do] met.reza = true
        public static void ApplyEffect(string source, WorldState world)
        {
            if (world == null || string.IsNullOrEmpty(source)) return;

            Match match = EffectPattern.Match(source);
            if (!match.Success) return;

            string name = match.Groups[1].Value;
            string op = match.Groups[2].Value;
            string raw = match.Groups[3].Value.Trim();

            if (op == "=")
            {
                if (raw == "true")
                {
                    world.Set(name, true);
                    return;
                }

                if (raw == "false")
                {
                    world.Set(name, false);
                    return;
                }

                double parsed;
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    world.Set(name, parsed);
                else
                    world.Set(name, raw);
                return;
            }

            double current = world.Get(name).AsNumber();
            if (double.IsNaN(current)) current = 0d;

            double delta;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out delta)) return;

            world.Set(name, op == "+" ? current + delta : current - delta);
        }
    }
}
