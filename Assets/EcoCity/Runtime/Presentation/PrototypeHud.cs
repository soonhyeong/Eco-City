using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    // Functional debug UI. Replace with the team's final UI after loop validation.
    public sealed class PrototypeHud : MonoBehaviour
    {
        public EcoGame game;
        public PrototypePlayer player;
        public PlayerInteractor interactor;
        public NpcFeedbackPresenter feedback;
        public Font koreanFont;
        private GUIStyle textStyle;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private bool confirmRestart;
        private Font createdFont;

        private void BuildStyles()
        {
            if (textStyle != null) return;
            if (koreanFont == null)
            {
                createdFont = Font.CreateDynamicFontFromOSFont(new[] {
                    "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial"
                }, 18);
                koreanFont = createdFont;
            }
            textStyle = new GUIStyle(GUI.skin.label) { font = koreanFont, fontSize = 18, wordWrap = true };
            textStyle.normal.textColor = Color.white;
            titleStyle = new GUIStyle(textStyle) { fontSize = 23, fontStyle = FontStyle.Bold };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = koreanFont, fontSize = 18, wordWrap = true };
        }
        private void OnGUI()
        {
            if (game == null) return;
            BuildStyles();
            Matrix4x4 originalMatrix = GUI.matrix;
            float scale = Mathf.Max(0.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2, 0),
                Quaternion.identity, Vector3.one * scale);
            EcoSession session = game.Session;
            if (session == null)
            {
                Panel(new Rect(20, 20, 900, 100));
                GUI.Label(new Rect(35, 35, 870, 70), game.StorageNotice ?? "초기화 중...", textStyle);
                GUI.matrix = originalMatrix; return;
            }

            Panel(new Rect(20, 20, 400, 168));
            GUI.Label(new Rect(35, 28, 370, 32), "에코시티 구조대 | PROTOTYPE", titleStyle);
            string mission = session.IsComplete ? "모든 미션 완료" : FeedbackText.Level(session.CurrentLevel) + "  " +
                session.Cleared(session.CurrentLevel) + "/" + session.Goal(session.CurrentLevel);
            GUI.Label(new Rect(35, 67, 370, 110), mission + "\n점수 " + session.Score + " | 배출 " + session.DisposedCount + "/" + session.TotalItems +
                "\n모의 깨끗함 " + session.Cleanliness + " | 오염도 " + session.Pollution + "\n정답률 " + (session.Accuracy * 100).ToString("0") + "% | 오답 " + session.IncorrectAttempts, textStyle);

            Panel(new Rect(870, 20, 390, 218));
            string held = "손에 든 쓰레기 없음";
            if (session.Held != null)
            {
                held = session.Held.Name + "\n완료한 처리: " + PreparationSummary(session.CompletedPreparation);
                Preparation remaining = session.Held.Required & ~session.CompletedPreparation;
                held += "\n남은 처리: " + PreparationSummary(remaining);
            }
            GUI.Label(new Rect(888, 35, 352, 183), "쓰레기 상태\n" + held, textStyle);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                GUI.Label(new Rect(631, 342, 24, 30), "+", titleStyle);
                if (interactor != null && interactor.Target != null)
                {
                    Panel(new Rect(310, 440, 660, 55));
                    GUI.Label(new Rect(325, 451, 630, 40), interactor.Target.GetPrompt(game), textStyle);
                }
            }
            Panel(new Rect(20, 515, 1240, 133));
            GUI.Label(new Rect(38, 525, 1204, 112), "환경 도우미\n" + (feedback == null ? "" : feedback.Message), textStyle);
            GUI.Label(new Rect(25, 655, 1220, 27), "WASD 이동 | 마우스 시점 | E 상호작용 | Esc 마우스 해제 | 처리대: EMPTY / SEPARATE / WASH", textStyle);
            GUI.Label(new Rect(25, 686, 980, 25), "※ 연습용 가상 분리배출 기준·도시 지표 | " + game.StorageNotice, textStyle);

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Panel(new Rect(415, 245, 450, 180));
                GUI.Label(new Rect(435, 258, 410, 35), session.IsComplete ? "모든 연습을 마쳤어요!" : "에코시티 연습 · 시작 / 계속", titleStyle);
                if (GUI.Button(new Rect(435, 303, 190, 43), "탐색 계속하기", buttonStyle))
                { confirmRestart = false; if (player != null) player.SetMouseCaptured(true); }
                if (GUI.Button(new Rect(640, 303, 200, 43), "진행 저장", buttonStyle)) game.Save();
                if (GUI.Button(new Rect(435, 360, 405, 43), confirmRestart ? "진행을 지우고 처음부터 시작" : "처음부터 연습하기", buttonStyle))
                {
                    if (confirmRestart) { game.Restart(); confirmRestart = false; }
                    else confirmRestart = true;
                }
            }
            if (!string.IsNullOrEmpty(PrototypeInput.DeviceNotice))
                GUI.Label(new Rect(435, 465, 800, 40), PrototypeInput.DeviceNotice, textStyle);
            if (session.IsComplete && Cursor.lockState == CursorLockMode.Locked)
            {
                Panel(new Rect(400, 245, 480, 130));
                GUI.Label(new Rect(422, 260, 440, 100), "모든 미션 완료!\n점수 " + session.Score + " | 정답률 " + (session.Accuracy * 100).ToString("0") +
                    "%\nEsc를 눌러 처음부터 다시 연습할 수 있어요.", titleStyle);
            }
            GUI.matrix = originalMatrix;
        }
        private static string PreparationSummary(Preparation flags)
        {
            if (flags == Preparation.None) return "없음";
            string value = "";
            foreach (Preparation step in new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash })
                if ((flags & step) != 0) value += (value.Length == 0 ? "" : ", ") + FeedbackText.Step(step);
            return value;
        }
        private static void Panel(Rect rect) { GUI.Box(rect, GUIContent.none); }
        private void OnDestroy() { if (createdFont != null) Destroy(createdFont); }
    }
}
