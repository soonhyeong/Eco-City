using System;

namespace EcoCity.Core
{
    public enum WasteCategory { Paper, Plastic, Metal, Glass, Vinyl, Food, General }
    public enum LearningLevel { Basic, State, Composite }
    [Flags] public enum Preparation { None = 0, Empty = 1, Separate = 2, Wash = 4 }
    public enum ResultCode
    {
        PickedUp, Prepared, Accepted, EmptyHands, HandsFull, Locked,
        AlreadyCollected, UnknownItem, MissingPreparation, WrongBin,
        NotRequired, AlreadyPrepared, PrerequisiteMissing, SessionComplete
    }

    // Immutable content + unique scene-instance ID. Unity types stay outside Core.
    public sealed class WasteSpec
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public WasteCategory Category { get; private set; }
        public Preparation Required { get; private set; }
        public LearningLevel Level { get; private set; }
        public string Reason { get; private set; }

        public WasteSpec(string id, string name, WasteCategory category,
            Preparation required, LearningLevel level, string reason)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Waste instance ID is required.");
            if (!Enum.IsDefined(typeof(WasteCategory), category)) throw new ArgumentException("Invalid category.");
            if (!Enum.IsDefined(typeof(LearningLevel), level)) throw new ArgumentException("Invalid level.");
            if ((required & ~(Preparation.Empty | Preparation.Separate | Preparation.Wash)) != 0)
                throw new ArgumentException("Invalid preparation flags.");
            Id = id; Name = name ?? id; Category = category;
            Required = required; Level = level; Reason = reason ?? "";
        }
    }

    public sealed class ActionResult
    {
        public ResultCode Code { get; private set; }
        public WasteSpec Item { get; private set; }
        public Preparation Step { get; private set; }
        public bool IsSuccess
        {
            get { return Code == ResultCode.PickedUp || Code == ResultCode.Prepared || Code == ResultCode.Accepted; }
        }
        public ActionResult(ResultCode code, WasteSpec item = null, Preparation step = Preparation.None)
        { Code = code; Item = item; Step = step; }
    }

    public static class WasteRules
    {
        public static Preparation NextStep(Preparation missing)
        {
            if ((missing & Preparation.Empty) != 0) return Preparation.Empty;
            if ((missing & Preparation.Separate) != 0) return Preparation.Separate;
            if ((missing & Preparation.Wash) != 0) return Preparation.Wash;
            return Preparation.None;
        }

        public static ActionResult Evaluate(WasteSpec item, Preparation completed, WasteCategory bin)
        {
            if (item == null) return new ActionResult(ResultCode.EmptyHands);
            Preparation missing = item.Required & ~completed;
            if (missing != Preparation.None)
                return new ActionResult(ResultCode.MissingPreparation, item, NextStep(missing));
            return new ActionResult(item.Category == bin ? ResultCode.Accepted : ResultCode.WrongBin, item);
        }
    }
}
