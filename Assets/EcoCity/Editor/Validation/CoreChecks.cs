using System;
using System.Collections.Generic;
using EcoCity.Core;

namespace EcoCity.Validation
{
    // Pure C# checks: run in Edit mode from EcoCity/Run Core Checks.
    // Also usable by Validation/run_core_checks.sh with a local C# compiler.
    public static class CoreChecks
    {
        private static readonly List<string> failures = new List<string>();
        private static int checks;
        public static int CheckCount { get { return checks; } }

        private static WasteSpec Basic(string id = "basic")
        { return new WasteSpec(id, "Paper", WasteCategory.Paper, Preparation.None, LearningLevel.Basic, ""); }
        private static WasteSpec Dirty()
        { return new WasteSpec("dirty", "Container", WasteCategory.Plastic, Preparation.Empty | Preparation.Wash, LearningLevel.State, ""); }
        private static WasteSpec Composite()
        { return new WasteSpec("composite", "Composite", WasteCategory.General, Preparation.Separate, LearningLevel.Composite, ""); }
        private static EcoSession FullSession() { return new EcoSession(new[] { Basic(), Dirty(), Composite() }); }
        private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void Equal<T>(T expected, T actual)
        { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Check(string name, Action action)
        { checks++; try { action(); } catch (Exception e) { failures.Add(name + ": " + e.Message); } }
        private static void Throws(Action action)
        { bool threw = false; try { action(); } catch (ArgumentException) { threw = true; } Assert(threw, "Expected ArgumentException"); }
        private static void ClearBasic(EcoSession s) { s.PickUp("basic"); s.Dispose(WasteCategory.Paper); }

        public static string[] RunAll()
        {
            checks = 0; failures.Clear();
            Check("Reject duplicate IDs", () => Throws(() => new EcoSession(new[] { Basic(), Basic() })));
            Check("Reject empty catalog", () => Throws(() => new EcoSession(new WasteSpec[0])));
            Check("Reject undefined category", () => Throws(() => new WasteSpec("x", "x", (WasteCategory)99, Preparation.None, LearningLevel.Basic, "")));
            Check("Reject undefined preparation", () => Throws(() => new WasteSpec("x", "x", WasteCategory.Paper, (Preparation)128, LearningLevel.Basic, "")));
            Check("Pickup records held item", () => { var s = FullSession(); Equal(ResultCode.PickedUp, s.PickUp("basic").Code); Equal("basic", s.Held.Id); });
            Check("Unknown pickup rejected", () => Equal(ResultCode.UnknownItem, FullSession().PickUp("missing").Code));
            Check("Higher tier locked", () => Equal(ResultCode.Locked, FullSession().PickUp("dirty").Code));
            Check("One held item only", () => { var s = new EcoSession(new[] { Basic("a"), Basic("b") }); s.PickUp("a"); Equal(ResultCode.HandsFull, s.PickUp("b").Code); });
            Check("Repeat pickup rejected", () => { var s = FullSession(); s.PickUp("basic"); Equal(ResultCode.AlreadyCollected, s.PickUp("basic").Code); });
            Check("Empty bin use not counted", () => { var s = FullSession(); Equal(ResultCode.EmptyHands, s.Dispose(WasteCategory.Paper).Code); Equal(0, s.TotalAttempts); });
            Check("Wrong bin keeps item and score", () => { var s = FullSession(); s.PickUp("basic"); Equal(ResultCode.WrongBin, s.Dispose(WasteCategory.Metal).Code); Equal("basic", s.Held.Id); Equal(0, s.Score); Equal(1, s.IncorrectAttempts); });
            Check("Success clears held item", () => { var s = FullSession(); ClearBasic(s); Assert(s.Held == null, "Held item not cleared"); Equal(1, s.DisposedCount); });
            Check("Success unlocks next tier", () => { var s = FullSession(); ClearBasic(s); Equal(LearningLevel.State, s.CurrentLevel); Equal(ResultCode.PickedUp, s.PickUp("dirty").Code); });
            Check("Duplicate success has no reward", () => { var s = FullSession(); ClearBasic(s); int score = s.Score; s.PickUp("basic"); s.Dispose(WasteCategory.Paper); Equal(score, s.Score); Equal(1, s.TotalAttempts); });
            Check("Missing preparation blocks success", () => { var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); Equal(ResultCode.MissingPreparation, s.Dispose(WasteCategory.Plastic).Code); Equal("dirty", s.Held.Id); });
            Check("Wash requires empty first", () => { var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); Equal(ResultCode.PrerequisiteMissing, s.Prepare(Preparation.Wash).Code); Equal(Preparation.None, s.CompletedPreparation); });
            Check("Preparation can finish correctly", () => { var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); s.Prepare(Preparation.Empty); s.Prepare(Preparation.Wash); Equal(ResultCode.Accepted, s.Dispose(WasteCategory.Plastic).Code); });
            Check("Repeated processing has no effect", () => { var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); s.Prepare(Preparation.Empty); Equal(ResultCode.AlreadyPrepared, s.Prepare(Preparation.Empty).Code); Equal(Preparation.Empty, s.CompletedPreparation); });
            Check("Unneeded processing rejected", () => { var s = FullSession(); s.PickUp("basic"); Equal(ResultCode.NotRequired, s.Prepare(Preparation.Wash).Code); });
            Check("Multiple steps rejected as one operation", () => { var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); Equal(ResultCode.NotRequired, s.Prepare(Preparation.Empty | Preparation.Wash).Code); });
            Check("Adaptive hint after two errors", () => { var s = FullSession(); s.PickUp("basic"); s.Dispose(WasteCategory.Metal); Assert(!s.NeedsDetailedHint, "Hint expanded too early"); s.Dispose(WasteCategory.Metal); Assert(s.NeedsDetailedHint, "Hint not expanded"); });
            Check("Full scenario completes all missions", () => {
                var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); s.Prepare(Preparation.Empty); s.Prepare(Preparation.Wash);
                s.Dispose(WasteCategory.Plastic); s.PickUp("composite"); s.Prepare(Preparation.Separate); s.Dispose(WasteCategory.General);
                Assert(s.IsComplete, "Not complete"); Equal(3, s.CompletedMissions); Equal(150, s.Score); Equal(100, s.Cleanliness); Equal(0, s.Pollution);
            });
            Check("Accuracy includes corrected attempt", () => { var s = FullSession(); s.PickUp("basic"); s.Dispose(WasteCategory.Metal); s.Dispose(WasteCategory.Paper); Equal(0.5, s.Accuracy); });
            Check("Restore keeps held preparation", () => {
                var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); s.Prepare(Preparation.Empty); var restored = FullSession(); string e;
                Assert(restored.Restore(s.Capture(), out e), e); Equal("dirty", restored.Held.Id); Equal(Preparation.Empty, restored.CompletedPreparation); Equal(s.Score, restored.Score);
            });
            Check("Snapshot is a deep copy", () => { var s = FullSession(); s.PickUp("basic"); s.Dispose(WasteCategory.Metal); var d = s.Capture(); d.recentAttempts[0].itemId = "changed"; Equal("basic", s.Capture().recentAttempts[0].itemId); });
            Check("Bad version rejected transactionally", () => { var s = FullSession(); s.PickUp("basic"); var d = s.Capture(); d.schemaVersion = 99; string e; Assert(!s.Restore(d, out e), "Accepted bad version"); Equal("basic", s.Held.Id); });
            Check("Unknown disposed item rejected", () => { var d = new ProgressData(); d.disposedIds.Add("missing"); d.totalAttempts = 1; string e; Assert(!FullSession().Restore(d, out e), "Accepted missing item"); });
            Check("Duplicate disposed ID rejected", () => { var d = new ProgressData(); d.disposedIds.Add("basic"); d.disposedIds.Add("basic"); d.totalAttempts = 2; string e; Assert(!FullSession().Restore(d, out e), "Accepted duplicates"); });
            Check("Already disposed cannot be held", () => { var s = FullSession(); ClearBasic(s); var d = s.Capture(); d.heldId = "basic"; string e; Assert(!FullSession().Restore(d, out e), "Accepted held disposed item"); });
            Check("Invalid processing prefix rejected", () => { var s = FullSession(); ClearBasic(s); s.PickUp("dirty"); var d = s.Capture(); d.heldPreparation = Preparation.Wash; string e; Assert(!FullSession().Restore(d, out e), "Accepted wash before empty"); });
            Check("Invalid mission order rejected", () => { var d = new ProgressData(); d.disposedIds.Add("composite"); d.totalAttempts = 1; string e; Assert(!FullSession().Restore(d, out e), "Accepted skipped missions"); });
            Check("Invalid attempt totals rejected", () => { var d = new ProgressData(); d.totalAttempts = 2; string e; Assert(!FullSession().Restore(d, out e), "Accepted untracked successes"); });
            Check("Preparation without item rejected", () => { var d = new ProgressData(); d.heldPreparation = Preparation.Empty; string e; Assert(!FullSession().Restore(d, out e), "Accepted orphan preparation"); });
            Check("Empty tiers skipped", () => { var s = new EcoSession(new[] { Composite() }); Equal(LearningLevel.Composite, s.CurrentLevel); Equal(ResultCode.PickedUp, s.PickUp("composite").Code); });
            Check("Bounded log retains total counters", () => { var s = FullSession(); s.PickUp("basic"); for (int i = 0; i < 250; i++) s.Dispose(WasteCategory.Metal); Equal(250, s.TotalAttempts); Equal(200, s.Capture().recentAttempts.Count); string e; Assert(FullSession().Restore(s.Capture(), out e), e); });
            Check("Null progress rejected", () => { string e; Assert(!FullSession().Restore(null, out e), "Accepted null"); });
            Check("Invalid bin rejected", () => Throws(() => FullSession().Dispose((WasteCategory)99)));
            Check("Null pickup ID rejected", () => Equal(ResultCode.UnknownItem, FullSession().PickUp(null).Code));
            Check("Tier remains locked until every basic item is done", () => {
                var s = new EcoSession(new[] { Basic("a"), Basic("b"), Dirty() });
                s.PickUp("a"); s.Dispose(WasteCategory.Paper);
                Equal(LearningLevel.Basic, s.CurrentLevel); Equal(0, s.CompletedMissions);
                Equal(ResultCode.Locked, s.PickUp("dirty").Code);
                s.PickUp("b"); s.Dispose(WasteCategory.Paper);
                Equal(LearningLevel.State, s.CurrentLevel); Equal(1, s.CompletedMissions);
            });
            Check("All six sample items finish with expected reward", () => {
                var cup = new WasteSpec("cup", "Cup", WasteCategory.Plastic,
                    Preparation.Empty | Preparation.Separate | Preparation.Wash, LearningLevel.State, "");
                var s = new EcoSession(new[] { Basic("a"), Basic("b"), Basic("c"), Dirty(), cup, Composite() });
                foreach (string id in new[] { "a", "b", "c" }) { s.PickUp(id); s.Dispose(WasteCategory.Paper); }
                s.PickUp("dirty"); s.Prepare(Preparation.Empty); s.Prepare(Preparation.Wash); s.Dispose(WasteCategory.Plastic);
                s.PickUp("cup"); s.Prepare(Preparation.Empty); s.Prepare(Preparation.Separate); s.Prepare(Preparation.Wash); s.Dispose(WasteCategory.Plastic);
                s.PickUp("composite"); s.Prepare(Preparation.Separate); s.Dispose(WasteCategory.General);
                Assert(s.IsComplete, "Not complete"); Equal(210, s.Score); Equal(6, s.TotalAttempts);
                string e; var restored = new EcoSession(new[] { Basic("a"), Basic("b"), Basic("c"), Dirty(), cup, Composite() });
                Assert(restored.Restore(s.Capture(), out e), e); Assert(restored.IsComplete, "Completion lost on restore"); Equal(210, restored.Score);
            });
            Check("Random action sequences preserve restorable state", () => {
                for (int seed = 0; seed < 50; seed++)
                {
                    var random = new Random(seed); var s = FullSession();
                    for (int n = 0; n < 200; n++)
                    {
                        int op = random.Next(4);
                        if (op == 0) s.PickUp(new[] { "basic", "dirty", "composite" }[random.Next(3)]);
                        else if (op == 1) s.Prepare(new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash }[random.Next(3)]);
                        else s.Dispose((WasteCategory)random.Next(7));
                        Assert(s.Score >= 0, "Negative score");
                        Assert(s.DisposedCount <= s.TotalItems, "Disposed count overflow");
                        var restored = FullSession(); string e;
                        Assert(restored.Restore(s.Capture(), out e), "Seed " + seed + " action " + n + ": " + e);
                        Equal(s.Score, restored.Score); Equal(s.CurrentLevel, restored.CurrentLevel);
                        Equal(s.Held == null ? "" : s.Held.Id, restored.Held == null ? "" : restored.Held.Id);
                    }
                }
            });
            return failures.ToArray();
        }
    }
}
