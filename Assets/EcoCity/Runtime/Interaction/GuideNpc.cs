namespace EcoCity
{
    public sealed class GuideNpc : Interactable
    {
        public override string GetPrompt(EcoGame game) { return "E  도우미에게 물어보기"; }
        public override void Interact(EcoGame game) { game.AskGuide(true); }
    }
}
