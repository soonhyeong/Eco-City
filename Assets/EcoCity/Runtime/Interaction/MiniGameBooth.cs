namespace EcoCity
{
    // Booth that opens its region's mini game (park: 번개 분리수거, beach: 먼저 뭐 할까?, market: 맞아요 틀려요!).
    // It does not touch the held item or the missions.
    public sealed class MiniGameBooth : Interactable
    {
        public KidMiniGame miniGame;
        public KidMiniGame.Kind kind;
        public override string GetPrompt(EcoGame game) { return "E  미니 게임 · " + (miniGame != null ? miniGame.Title(kind) : "하기"); }
        public override void Interact(EcoGame game) { if (miniGame != null) miniGame.Open(kind); }
    }
}
