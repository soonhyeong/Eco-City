namespace EcoCity
{
    // Carries the title-scene choice into the game scene (static: survives the scene load, not the app).
    public static class GameStartRequest
    {
        public enum Mode { None, NewGame, Continue }
        public const string TitleScene = "EcoCityTitle";
        public const string CharacterScene = "EcoCityCharacter";
        public const string GameScene = "EcoCitySubmission";
        public static Mode Pending = Mode.None;
        public static bool LaunchedFromTitle;
    }
}
