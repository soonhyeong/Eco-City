using EcoCity.Core;

namespace EcoCity
{
    public sealed class DisposalBin : Interactable
    {
        public WasteCategory category;
        public override string GetPrompt(EcoGame game)
        {
            if (game.Session != null && game.Session.Held == null) return FeedbackText.Category(category) + " 통 · 먼저 쓰레기를 주워요";
            return "E  " + FeedbackText.Category(category) + " 통에 넣기";
        }
        public override void Interact(EcoGame game) { game.Dispose(category); }
    }
}
