using EcoCity.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoCity
{
    // Game-scene side of the title scene: starts the requested game straight away and returns to the title scene
    // when the player chooses "처음 화면으로". Starting the game scene directly (Play with this scene open) goes to the
    // title scene first, so every start is title → character choice → game and the built-in title screen never shows.
    [DefaultExecutionOrder(150)]
    public sealed class TitleSceneBridge : MonoBehaviour
    {
        public AlphaFlow flow;
        private bool active;
        private void Start()
        {
            var request = GameStartRequest.Pending;
            GameStartRequest.Pending = GameStartRequest.Mode.None;
            if (flow == null) return;
            if (request == GameStartRequest.Mode.None)
            {
                if (Application.CanStreamedLevelBeLoaded(GameStartRequest.TitleScene)) KidLoading.Load(GameStartRequest.TitleScene);
                return;
            }
            active = true;
            if (request == GameStartRequest.Mode.Continue && flow.CanContinue) flow.Continue();
            else
            {
                flow.NewGame();
                if (flow.Screens.Current == AlphaScreen.ConfirmNewGame) flow.ConfirmNewGame(true);
            }
            flow.ScreenChanged += OnScreenChanged;
        }
        private void OnDestroy() { if (flow != null) flow.ScreenChanged -= OnScreenChanged; }
        private void OnScreenChanged()
        {
            if (!active || flow.Screens.Current != AlphaScreen.Title) return;
            active = false;
            KidLoading.Load(GameStartRequest.TitleScene);
        }
    }
}
