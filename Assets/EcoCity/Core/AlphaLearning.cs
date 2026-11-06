using System;

namespace EcoCity.Core
{
    [Flags]
    public enum TutorialAction
    {
        None = 0, Move = 1, Look = 2, Discover = 4, PickUp = 8,
        Prepare = 16, Dispose = 32, Guide = 64, Menu = 128, All = 255
    }

    public enum AlphaScreen { Title, Playing, Paused, Help, Results, ConfirmNewGame }

    // Navigation owns screens only. Rewards and tutorial progress remain in EcoSession.
    public sealed class AlphaScreenState
    {
        public AlphaScreen Current { get; private set; } = AlphaScreen.Title;
        private AlphaScreen previous;
        public bool CanExplore { get { return Current == AlphaScreen.Playing; } }
        public bool RequestNewGame(bool needsConfirmation)
        {
            if (Current != AlphaScreen.Title && Current != AlphaScreen.Results) return false;
            previous = Current;
            Current = needsConfirmation ? AlphaScreen.ConfirmNewGame : AlphaScreen.Playing;
            return !needsConfirmation;
        }
        public bool ConfirmNewGame(bool accept)
        {
            if (Current != AlphaScreen.ConfirmNewGame) return false;
            Current = accept ? AlphaScreen.Playing : previous;
            return accept;
        }
        public bool Continue(bool available, bool complete)
        {
            if (Current != AlphaScreen.Title || !available) return false;
            Current = complete ? AlphaScreen.Results : AlphaScreen.Playing;
            return true;
        }
        public void Pause() { if (CanExplore) Current = AlphaScreen.Paused; }
        public void Resume() { if (Current == AlphaScreen.Paused) Current = AlphaScreen.Playing; }
        public void OpenHelp()
        {
            if (Current == AlphaScreen.Help || Current == AlphaScreen.ConfirmNewGame) return;
            previous = Current; Current = AlphaScreen.Help;
        }
        public void CloseHelp() { if (Current == AlphaScreen.Help) Current = previous; }
        public void Finish() { if (Current == AlphaScreen.Playing || Current == AlphaScreen.Paused) Current = AlphaScreen.Results; }
        public void Title() { Current = AlphaScreen.Title; }
    }

    public static class TutorialText
    {
        /// <summary>Number of guided practice steps shown as "n / Steps".</summary>
        public const int Steps = 7;
        // Each line says what to do and why, in one or two short sentences young players can follow.
        public static string Next(EcoSession session)
        {
            if (session.TutorialComplete) return NowWhat(session);
            switch (Key(session))
            {
                case "tut_move": return "방향키를 눌러 공원을 걸어 봐요. 쓰레기를 치우러 출발!";
                case "tut_look": return "몸을 빙 돌려 공원을 둘러봐요. 쓰레기가 어디 있을까요?";
                case "tut_discover": return "반짝이는 ! 표시가 쓰레기예요. 가까이 가 봐요.";
                case "tut_pickup": return "쓰레기 앞에서 E를 누르면 주울 수 있어요.";
                case "tut_prepare": return "더러운 쓰레기는 바로 못 넣어요. 왼쪽 준비 구역에서 깨끗하게 해요.";
                case "tut_basic": return "깨끗한 쓰레기예요! 그림이 같은 통을 찾아가요.";
                case "tut_next": return "잘했어요! 다음 반짝이는 ! 쓰레기를 찾아 주워요.";
                case "tut_dispose": return "통 위 그림을 보고, 맞는 통 앞에서 E를 눌러 넣어요.";
                case "tut_guide": return "모를 땐 도우미 친구 앞에서 E를 눌러 봐요. 힌트를 알려 줘요.";
                default: return "Esc를 누르면 메뉴가 열려요. 계속하기를 눌러 돌아와요.";
            }
        }
        /// <summary>Voice clip key of the current line; the line text can change without breaking the voice.</summary>
        public static string Key(EcoSession session)
        {
            if (session.TutorialComplete) return "tut_done";
            TutorialAction done = session.TutorialActions;
            if ((done & TutorialAction.Move) == 0) return "tut_move";
            if ((done & TutorialAction.Look) == 0) return "tut_look";
            if ((done & TutorialAction.Discover) == 0) return "tut_discover";
            if ((done & TutorialAction.PickUp) == 0) return "tut_pickup";
            if ((done & TutorialAction.Prepare) == 0)
            {
                if (session.Held != null && session.Held.Required != Preparation.None) return "tut_prepare";
                return session.Held != null ? "tut_basic" : "tut_next";
            }
            if ((done & TutorialAction.Dispose) == 0) return "tut_dispose";
            if ((done & TutorialAction.Guide) == 0) return "tut_guide";
            return "tut_menu";
        }
        /// <summary>Practice step 1..Steps (move and look together are step 1), or 0 once the practice is over.</summary>
        public static int Step(EcoSession session)
        {
            switch (Key(session))
            {
                case "tut_move": case "tut_look": return 1;
                case "tut_discover": return 2;
                case "tut_pickup": return 3;
                case "tut_prepare": case "tut_basic": case "tut_next": return 4;
                case "tut_dispose": return 5;
                case "tut_guide": return 6;
                case "tut_menu": return 7;
                default: return 0;
            }
        }
        // After the guided practice the card keeps saying what to do next with the item in hand.
        private static string NowWhat(EcoSession session)
        {
            if (session.IsComplete) return "에코시티 청소 끝! 정말 멋져요.";
            if (session.Held == null)
            {
                int left = session.Goal(session.CurrentLevel) - session.Cleared(session.CurrentLevel);
                return "반짝이는 ! 쓰레기를 찾아 주워요. 이 단계에 " + left + "개 남았어요.";
            }
            Preparation next = WasteRules.NextStep(session.Held.Required & ~session.CompletedPreparation);
            if (next != Preparation.None) return "먼저 '" + StepName(next) + "'를 해요. 왼쪽 준비 구역으로 가요.";
            return "그림이 같은 통을 찾아 E를 눌러 넣어요.";
        }
        private static string StepName(Preparation step)
        {
            return step == Preparation.Empty ? "비우기" : step == Preparation.Separate ? "떼어내기" : "씻기";
        }
    }
}
