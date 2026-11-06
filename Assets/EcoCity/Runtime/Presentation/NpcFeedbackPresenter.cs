using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    public sealed class NpcFeedbackPresenter : MonoBehaviour
    {
        public EcoGame game;
        public string Message { get; private set; }
        private void OnEnable()
        {
            Message = "반짝이는 쓰레기를 찾아 E로 주워요!";
            if (game == null) return;
            game.ResultPublished += OnResult;
            game.MessagePublished += OnMessage;
        }
        private void OnDisable()
        {
            if (game == null) return;
            game.ResultPublished -= OnResult;
            game.MessagePublished -= OnMessage;
        }
        private void Start() { if (game != null && game.Session != null) game.AskGuide(); }
        private void OnResult(ActionResult result)
        {
            // Name the bin only after success or repeated mistakes with the item in hand.
            Message = FeedbackText.Build(result, game.NeedsBinHint || result.Code == ResultCode.Accepted);
            if (result.Code == ResultCode.PickedUp || result.Code == ResultCode.Prepared)
            {
                Preparation next = WasteRules.NextStep(game.Session.Held.Required & ~game.Session.CompletedPreparation);
                Message += next == Preparation.None
                    // The bin is not named here: players choose it themselves, and hints come only after a mistake.
                    ? "\n이제 맞는 통을 찾아봐요. 통 위 그림을 잘 봐요."
                    : "\n이제 '" + FeedbackText.Step(next) + "' 자리로 가요.";
            }
        }
        private void OnMessage(string message) { Message = message; }
    }
}
