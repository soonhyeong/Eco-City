using EcoCity.Core;

namespace EcoCity
{
    public sealed class PreparationStation : Interactable
    {
        public Preparation step;
        public override string GetPrompt(EcoGame game)
        {
            if (game.Session != null && game.Session.Held == null) return FeedbackText.Step(step) + " 자리 · 먼저 쓰레기를 주워요";
            // Already done for the item in hand: no E, so the key cap hides and the player looks for the next place.
            if (game.Session != null && (game.Session.CompletedPreparation & step) != 0) return FeedbackText.Step(step) + " 끝! · 다음 자리로 가요";
            return "E  " + FeedbackText.Step(step);
        }
        public override void Interact(EcoGame game) { game.Prepare(step); }
    }
}
