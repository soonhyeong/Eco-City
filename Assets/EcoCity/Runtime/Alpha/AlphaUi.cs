using System.Collections.Generic;
using EcoCity.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoCity
{
    public sealed class AlphaUi : MonoBehaviour
    {
        public AlphaFlow flow;
        [System.NonSerialized] public bool externalOverlay;
        public NpcFeedbackPresenter feedback;
        public GameObject[] screens;
        [Tooltip("Legacy Text or TextMeshPro labels.")]
        public Graphic missionText, heldText, targetText, feedbackText, cityText, tutorialText, noticeText, resultText, celebrationText, menuGuideText;
        public Button newGame, continueGame, quit, titleHelp, pause, resume, pauseHelp, save, toTitle, askGuide;
        public Button practiceAgain, resultTitle, helpBack, skipTutorial, confirmYes, confirmNo;
        private readonly List<KeyValuePair<Button, UnityAction>> listeners = new List<KeyValuePair<Button, UnityAction>>();
        private EcoSession resultSession;
        private int resultAttempts = -1;

        private void OnEnable() { if (flow != null) Bind(); }
        private void OnDisable() { Unbind(); }
        public void Bind()
        {
            Unbind();
            Listen(newGame, flow.NewGame); Listen(continueGame, flow.Continue); Listen(quit, flow.Quit);
            Listen(titleHelp, flow.Help); Listen(pause, flow.Pause); Listen(resume, flow.Resume);
            Listen(pauseHelp, flow.Help); Listen(save, flow.Save); Listen(toTitle, flow.ToTitle);
            Listen(askGuide, flow.AskGuide); Listen(practiceAgain, flow.NewGame); Listen(resultTitle, flow.ToTitle);
            Listen(helpBack, flow.CloseHelp); Listen(skipTutorial, flow.SkipTutorial);
            Listen(confirmYes, () => flow.ConfirmNewGame(true)); Listen(confirmNo, () => flow.ConfirmNewGame(false));
        }
        private void Listen(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action); listeners.Add(new KeyValuePair<Button, UnityAction>(button, action));
        }
        private void Unbind()
        {
            foreach (var listener in listeners) if (listener.Key != null) listener.Key.onClick.RemoveListener(listener.Value);
            listeners.Clear();
        }
        private void Update() { Refresh(); }
        public void Refresh()
        {
            if (flow == null || flow.game == null) return;
            AlphaScreen current = flow.Screens.Current;
            for (int i = 0; i < screens.Length; i++)
                if (screens[i] != null && screens[i].activeSelf != (i == (int)current && !externalOverlay))
                {
                    screens[i].SetActive(i == (int)current && !externalOverlay);
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                }
            var game = flow.game;
            EcoSession session = game.Session;
            newGame.interactable = session != null;
            continueGame.gameObject.SetActive(flow.CanContinue);
            skipTutorial.interactable = flow.SessionStarted && session != null && !session.TutorialComplete;
            Set(noticeText, (game.StorageNotice ?? "") + (string.IsNullOrEmpty(PrototypeInput.DeviceNotice) ? "" : "  " + PrototypeInput.DeviceNotice));
            Set(celebrationText, flow.Celebration);
            if (session == null) return;
            Set(missionText, session.IsComplete ? "에코시티 청소 끝!" : FeedbackText.Level(session.CurrentLevel) + "  " +
                session.Cleared(session.CurrentLevel) + " / " + session.Goal(session.CurrentLevel));
            // "점수", not a star: stars are the per-region rating on the map.
            Set(cityText, "에코시티 깨끗함 " + session.Cleanliness + "% · 점수 " + session.Score);
            string held = "손이 비었어요\n반짝이는 ! 를 찾아요";
            if (session.Held != null)
            {
                Preparation left = session.Held.Required & ~session.CompletedPreparation;
                held = session.Held.Name + "\n" + (left == Preparation.None ? "준비 끝! 맞는 통을 찾아요" : "할 일: " + PreparationSummary(left));
            }
            Set(heldText, held);
            string prompt = flow.Screens.CanExplore && flow.interactor.Target != null ? flow.interactor.Target.GetPrompt(game) :
                !flow.interactor.proximity ? "가운데 표시로 대상을 바라보세요" :
                session != null && session.Held != null ? "준비 자리나 통 가까이 걸어가 보세요" : "물건 가까이 걸어가 보세요";
            // The kid HUD draws the E key as its own key cap, so the label drops the leading "E".
            if (flow.interactor.proximity && targetText is TMPro.TMP_Text && prompt.StartsWith("E  ")) prompt = prompt.Substring(3);
            Set(targetText, prompt);
            Set(feedbackText, feedback == null ? "" : feedback.Message);
            Set(menuGuideText, feedback == null ? "도우미에게 물어보면 다음 행동을 알려줘요." : feedback.Message);
            Set(tutorialText, TutorialText.Next(session));
            if (current == AlphaScreen.Results && (resultSession != session || resultAttempts != session.TotalAttempts))
            {
                resultSession = session; resultAttempts = session.TotalAttempts;
                string result = BuildResult(session);
                // TextMeshPro shows the star line large and golden; legacy Text keeps plain characters.
                if (resultText is TMPro.TMP_Text)
                {
                    int line = result.IndexOf('\n');
                    if (line > 0) result = "<size=170%><color=#FFB300>" + result.Substring(0, line) + "</color></size>" + result.Substring(line);
                }
                Set(resultText, result);
            }
        }
        private static WasteDefinition FindDefinition(EcoGame game, string id)
        { foreach (var item in game.wasteItems) if (item.instanceId == id) return item.definition; return null; }
        private static string PreparationSummary(Preparation steps)
        {
            if (steps == Preparation.None) return "없음";
            var values = new List<string>();
            foreach (Preparation step in new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash })
                if ((steps & step) != 0) values.Add(FeedbackText.Step(step));
            return string.Join(" → ", values);
        }
        /// <summary>Optional first lines of the results (the map's region stars and reward); replaces the accuracy stars.</summary>
        public static System.Func<string> StarSummary;
        public static string BuildResult(EcoSession session)
        {
            int missing = 0, wrongBin = 0;
            foreach (var attempt in session.Capture().recentAttempts)
            {
                if (attempt.result == ResultCode.MissingPreparation) missing++;
                if (attempt.result == ResultCode.WrongBin) wrongBin++;
            }
            string stars = StarSummary != null ? StarSummary() : session.Accuracy >= 0.9f ? "★★★" : session.Accuracy >= 0.6f ? "★★☆" : "★☆☆";
            return stars + "\n끝까지 해냈어요!\n\n" +
                "치운 쓰레기 " + session.DisposedCount + "개 · 점수 " + session.Score + "점\n" +
                (session.IncorrectAttempts == 0 ? "한 번도 안 틀렸어요! 최고예요." : "틀려도 다시 생각해서 끝까지 했어요.") +
                "\n\n다음에 기억해요\n" +
                (missing > 0 ? "· 더러우면 먼저 비우고 씻어요.\n" : "") +
                (wrongBin > 0 ? "· 통에 있는 그림을 잘 봐요.\n" : "") +
                "· 집에서는 어른과 함께 우리 동네 분리수거 방법을 확인해요.\n\n" +
                "에코시티 깨끗함 " + session.Cleanliness + "% (게임 속 점수예요)";
        }
        private static void Set(Graphic label, string value) { UiText.Set(label, value); }
    }
}
