using System;
using System.Collections.Generic;

namespace EcoCity.Core
{
    // Single authority for pickup, preparation, scoring, missions and persistence.
    // Failed disposal never removes the held item or adds rewards.
    public sealed class EcoSession
    {
        private readonly Dictionary<string, WasteSpec> catalog = new Dictionary<string, WasteSpec>();
        private readonly HashSet<string> disposed = new HashSet<string>();
        private readonly int[] goals = new int[3];
        private readonly List<AttemptRecord> recent = new List<AttemptRecord>();
        public const int RecentLimit = 200;
        public WasteSpec Held { get; private set; }
        public Preparation CompletedPreparation { get; private set; }
        public int TotalAttempts { get; private set; }
        public int IncorrectAttempts { get; private set; }
        public TutorialAction TutorialActions { get; private set; }
        public bool TutorialSkipped { get; private set; }
        public bool TutorialComplete { get { return TutorialSkipped || TutorialActions == TutorialAction.All; } }
        public bool ObserveTutorial(TutorialAction action)
        {
            if (TutorialComplete || action == TutorialAction.None || (action & ~TutorialAction.All) != 0) return false;
            TutorialAction updated = TutorialActions | action;
            if (updated == TutorialActions) return false;
            TutorialActions = updated;
            return true;
        }
        public void SkipTutorial() { TutorialSkipped = true; }
        public int DisposedCount { get { return disposed.Count; } }
        public int TotalItems { get { return catalog.Count; } }
        public double Accuracy { get { return TotalAttempts == 0 ? 0 : (double)(TotalAttempts - IncorrectAttempts) / TotalAttempts; } }

        public EcoSession(IEnumerable<WasteSpec> specs)
        {
            if (specs == null) throw new ArgumentNullException("specs");
            foreach (WasteSpec spec in specs)
            {
                if (spec == null || catalog.ContainsKey(spec.Id))
                    throw new ArgumentException("Null item or duplicate scene-instance ID.");
                catalog.Add(spec.Id, spec);
                goals[(int)spec.Level]++;
            }
            if (catalog.Count == 0) throw new ArgumentException("At least one waste item is required.");
        }

        public int Cleared(LearningLevel level)
        {
            int count = 0;
            foreach (string id in disposed) if (catalog[id].Level == level) count++;
            return count;
        }
        public int Goal(LearningLevel level) { return goals[(int)level]; }
        public bool IsComplete { get { return disposed.Count == catalog.Count; } }
        public LearningLevel CurrentLevel
        {
            get
            {
                for (int i = 0; i < goals.Length; i++)
                    if (Cleared((LearningLevel)i) < goals[i]) return (LearningLevel)i;
                return LearningLevel.Composite;
            }
        }
        public int CompletedMissions
        {
            get
            {
                int count = 0;
                for (int i = 0; i < goals.Length; i++)
                    if (goals[i] > 0 && Cleared((LearningLevel)i) == goals[i]) count++;
                return count;
            }
        }
        public int Score { get { return Math.Max(0, disposed.Count * 20 + CompletedMissions * 30 - IncorrectAttempts * 2); } }
        // Educational simulation indicators, not real-world environmental statistics.
        public int Cleanliness { get { return (int)Math.Round(100.0 * disposed.Count / catalog.Count); } }
        public int Pollution { get { return Math.Max(0, Math.Min(100, 100 - Cleanliness + IncorrectAttempts * 3)); } }
        public bool IsDisposed(string id) { return id != null && disposed.Contains(id); }
        public bool IsUnavailableInWorld(string id) { return IsDisposed(id) || (Held != null && Held.Id == id); }
        public bool IsUnlocked(WasteSpec item) { return item != null && (int)item.Level <= (int)CurrentLevel; }

        public ActionResult PickUp(string id)
        {
            WasteSpec item;
            if (id == null || !catalog.TryGetValue(id, out item)) return new ActionResult(ResultCode.UnknownItem);
            if (IsComplete) return new ActionResult(ResultCode.SessionComplete, item);
            if (IsUnavailableInWorld(id)) return new ActionResult(ResultCode.AlreadyCollected, item);
            if (Held != null) return new ActionResult(ResultCode.HandsFull, item);
            if (!IsUnlocked(item)) return new ActionResult(ResultCode.Locked, item);
            Held = item;
            CompletedPreparation = Preparation.None;
            return new ActionResult(ResultCode.PickedUp, item);
        }

        public ActionResult Prepare(Preparation step)
        {
            if (Held == null) return new ActionResult(ResultCode.EmptyHands);
            if (step != Preparation.Empty && step != Preparation.Separate && step != Preparation.Wash)
                return new ActionResult(ResultCode.NotRequired, Held);
            if ((Held.Required & step) == 0) return new ActionResult(ResultCode.NotRequired, Held, step);
            if ((CompletedPreparation & step) != 0) return new ActionResult(ResultCode.AlreadyPrepared, Held, step);
            Preparation next = WasteRules.NextStep(Held.Required & ~CompletedPreparation);
            if (next != step) return new ActionResult(ResultCode.PrerequisiteMissing, Held, next);
            CompletedPreparation |= step;
            return new ActionResult(ResultCode.Prepared, Held, step);
        }

        public ActionResult Dispose(WasteCategory bin)
        {
            if (!Enum.IsDefined(typeof(WasteCategory), bin)) throw new ArgumentException("Invalid bin.");
            ActionResult result = WasteRules.Evaluate(Held, CompletedPreparation, bin);
            if (Held == null) return result; // Empty-bin interactions do not lower accuracy.
            TotalAttempts++;
            if (result.Code == ResultCode.Accepted)
            {
                disposed.Add(Held.Id);
                Held = null;
                CompletedPreparation = Preparation.None;
            }
            else IncorrectAttempts++;
            recent.Add(new AttemptRecord {
                itemId = result.Item.Id, selectedBin = bin, result = result.Code,
                utc = DateTime.UtcNow.ToString("o")
            });
            if (recent.Count > RecentLimit) recent.RemoveAt(0);
            return result;
        }

        // Adaptation is limited to hint detail. Mission difficulty is a fixed progression.
        public bool NeedsDetailedHint
        {
            get
            {
                int mistakes = 0;
                for (int i = Math.Max(0, recent.Count - 5); i < recent.Count; i++)
                    if (recent[i].result != ResultCode.Accepted) mistakes++;
                return mistakes >= 2;
            }
        }

        public ProgressData Capture()
        {
            ProgressData data = new ProgressData {
                disposedIds = new List<string>(disposed), heldId = Held == null ? "" : Held.Id,
                heldPreparation = CompletedPreparation, totalAttempts = TotalAttempts,
                incorrectAttempts = IncorrectAttempts, tutorialRevision = 1,
                tutorialActions = TutorialActions, tutorialSkipped = TutorialSkipped
            };
            data.disposedIds.Sort(StringComparer.Ordinal);
            foreach (AttemptRecord a in recent)
                data.recentAttempts.Add(new AttemptRecord { itemId = a.itemId, selectedBin = a.selectedBin, result = a.result, utc = a.utc });
            return data;
        }

        // Transactional restore: validate into a separate session, then copy it.
        // Incompatible/corrupt saves cannot partially change the running session.
        public bool Restore(ProgressData data, out string error)
        {
            error = "";
            if (data == null || data.schemaVersion != 1) { error = "Unsupported save format."; return false; }
            if (data.tutorialRevision < 0 || data.tutorialRevision > 1 || (data.tutorialActions & ~TutorialAction.All) != 0)
            { error = "Unsupported tutorial state."; return false; }
            if (data.disposedIds == null || data.recentAttempts == null || data.recentAttempts.Count > RecentLimit ||
                data.totalAttempts < 0 || data.totalAttempts > 1000000 || data.incorrectAttempts < 0 || data.incorrectAttempts > data.totalAttempts)
            { error = "Invalid save counters or lists."; return false; }
            EcoSession candidate = new EcoSession(catalog.Values);
            foreach (string id in data.disposedIds)
            {
                if (id == null || !catalog.ContainsKey(id) || !candidate.disposed.Add(id))
                { error = "Unknown or duplicate disposed item."; return false; }
            }
            if (data.totalAttempts - data.incorrectAttempts != candidate.disposed.Count)
            { error = "Success count does not match disposed items."; return false; }
            // No later tier can contain progress while an earlier tier is unfinished.
            for (int i = 1; i < goals.Length; i++)
                if (candidate.Cleared((LearningLevel)i) > 0)
                    for (int earlier = 0; earlier < i; earlier++)
                        if (candidate.Cleared((LearningLevel)earlier) != goals[earlier])
                        { error = "Invalid mission order."; return false; }
            if (!string.IsNullOrEmpty(data.heldId))
            {
                WasteSpec item;
                if (!catalog.TryGetValue(data.heldId, out item) || candidate.disposed.Contains(data.heldId) || !candidate.IsUnlocked(item))
                { error = "Invalid held item."; return false; }
                if ((data.heldPreparation & ~item.Required) != 0)
                { error = "Invalid preparation flags."; return false; }
                // Only reachable preparation prefixes are accepted.
                Preparation reached = Preparation.None;
                while (reached != data.heldPreparation)
                {
                    Preparation next = WasteRules.NextStep(item.Required & ~reached);
                    if (next == Preparation.None || (data.heldPreparation & next) == 0)
                    { error = "Invalid preparation order."; return false; }
                    reached |= next;
                }
                candidate.Held = item;
                candidate.CompletedPreparation = data.heldPreparation;
            }
            else if (data.heldPreparation != Preparation.None)
            { error = "Preparation without a held item."; return false; }
            foreach (AttemptRecord a in data.recentAttempts)
            {
                if (a == null || a.itemId == null || !catalog.ContainsKey(a.itemId) ||
                    !Enum.IsDefined(typeof(WasteCategory), a.selectedBin) ||
                    (a.result != ResultCode.Accepted && a.result != ResultCode.MissingPreparation && a.result != ResultCode.WrongBin))
                { error = "Invalid attempt log."; return false; }
                candidate.recent.Add(new AttemptRecord { itemId = a.itemId, selectedBin = a.selectedBin, result = a.result, utc = a.utc });
            }
            if (candidate.recent.Count > data.totalAttempts)
            { error = "Attempt log exceeds total attempts."; return false; }
            disposed.Clear();
            foreach (string id in candidate.disposed) disposed.Add(id);
            Held = candidate.Held; CompletedPreparation = candidate.CompletedPreparation;
            TotalAttempts = data.totalAttempts; IncorrectAttempts = data.incorrectAttempts;
            recent.Clear(); recent.AddRange(candidate.recent);
            TutorialActions = data.tutorialActions;
            TutorialSkipped = data.tutorialSkipped;
            if (data.tutorialRevision == 0 && (Held != null || DisposedCount > 0))
            {
                TutorialActions |= TutorialAction.Move | TutorialAction.Look | TutorialAction.Discover | TutorialAction.PickUp;
                if (DisposedCount > 0) TutorialActions |= TutorialAction.Dispose;
                if (CompletedPreparation != Preparation.None || Cleared(LearningLevel.State) > 0 || Cleared(LearningLevel.Composite) > 0)
                    TutorialActions |= TutorialAction.Prepare;
            }
            return true;
        }
    }
}
