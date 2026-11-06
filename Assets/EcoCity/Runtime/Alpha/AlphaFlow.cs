using System;
using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    [DefaultExecutionOrder(100)]
    public sealed class AlphaFlow : MonoBehaviour
    {
        public EcoGame game;
        public PrototypePlayer player;
        public PlayerInteractor interactor;
        public AlphaUi ui;
        public Transform spawn;
        public AlphaScreenState Screens { get; private set; } = new AlphaScreenState();
        public string Celebration { get; private set; } = "";
        /// <summary>Hides the level-up banner early (another card takes its place).</summary>
        public void ClearCelebration() { Celebration = ""; if (ui != null) ui.Refresh(); }
        public bool CanContinue { get { return game != null && game.Session != null && (started || game.HasSavedProgress); } }
        public bool SessionStarted { get { return started; } }
        private bool started;
        private int knownMissions;
        private float celebrationUntil;
        private Vector3 moveOrigin;
        private Quaternion lookOrigin;
        private Quaternion bodyOrigin;
        public event Action ScreenChanged;
        [Tooltip("Seconds between clearing the last item and the results screen (0 = straight away). An ending scene can play meanwhile.")]
        public float finishDelay;
        /// <summary>Raised once when the last item is cleared and the results wait for <see cref="finishDelay"/>.</summary>
        public event Action Completing;
        public bool Finishing { get { return finishing; } }
        private bool finishing;
        private float finishAt;

        private void OnEnable()
        {
            if (game != null) game.StateChanged += RefreshProgress;
        }
        private void OnDisable()
        {
            if (game != null) game.StateChanged -= RefreshProgress;
            if (player != null) { player.inputBlocked = false; player.SetMouseCaptured(false); }
            if (interactor != null) interactor.inputBlocked = false;
            Time.timeScale = 1;
        }
        private void Start() { Initialize(); }
        public void Initialize()
        {
            game.StateChanged -= RefreshProgress;
            game.StateChanged += RefreshProgress;
            Screens = new AlphaScreenState(); started = false; finishing = false;
            knownMissions = game.Session == null ? 0 : game.Session.CompletedMissions;
            if (ui != null) ui.Bind();
            ApplyScreen();
        }
        private void Update()
        {
            if (finishing)
            {
                // The ending scene owns the screen; no pause or help until the results appear.
                if (Time.unscaledTime >= finishAt) { finishing = false; if (Screens.CanExplore) { Screens.Finish(); ApplyScreen(); } }
                return;
            }
            if (Screens.CanExplore)
            {
                if (PrototypeInput.EscapePressed || Cursor.lockState != CursorLockMode.Locked) { Pause(); return; }
                if (PrototypeInput.HelpPressed) { Help(); return; }
                ObserveExploration();
            }
            else if (PrototypeInput.EscapePressed)
            {
                if (Screens.Current == AlphaScreen.Paused) Resume();
                else if (Screens.Current == AlphaScreen.Help) CloseHelp();
                else if (Screens.Current == AlphaScreen.ConfirmNewGame) ConfirmNewGame(false);
            }
            if (!string.IsNullOrEmpty(Celebration) && Time.unscaledTime >= celebrationUntil) Celebration = "";
        }
        private void ObserveExploration()
        {
            if (!Screens.CanExplore || player.inputBlocked) return;
            if (Vector3.Distance(player.transform.position, moveOrigin) >= 0.35f) game.ObserveTutorial(TutorialAction.Move);
            // Third person: turning the character counts as looking around, even with a fixed camera.
            if (Quaternion.Angle(player.viewCamera.transform.rotation, lookOrigin) >= 5f ||
                (player.thirdPerson && Quaternion.Angle(player.transform.rotation, bodyOrigin) >= 30f)) game.ObserveTutorial(TutorialAction.Look);
            if (interactor.Target is WasteItem) game.ObserveTutorial(TutorialAction.Discover);
        }
        public void NewGame()
        {
            if (game.Session == null) return;
            if (Screens.RequestNewGame(CanContinue)) StartFresh();
            ApplyScreen();
        }
        public void ConfirmNewGame(bool accept)
        {
            if (Screens.ConfirmNewGame(accept)) StartFresh();
            ApplyScreen();
        }
        private void StartFresh()
        {
            started = true; knownMissions = 0; Celebration = "";
            game.Restart();
            ResetPlayer();
            game.AskGuide();
        }
        public void Continue()
        {
            if (!Screens.Continue(CanContinue, game.Session != null && game.Session.IsComplete)) return;
            started = true; game.EnableSaving(); game.AskGuide(); ApplyScreen();
        }
        public void Pause()
        {
            if (!Screens.CanExplore) return;
            Screens.Pause(); game.ObserveTutorial(TutorialAction.Menu); game.Save(); ApplyScreen();
        }
        public void Resume() { Screens.Resume(); ApplyScreen(); }
        public void Help()
        {
            if (Screens.CanExplore) game.ObserveTutorial(TutorialAction.Menu);
            Screens.OpenHelp(); ApplyScreen();
        }
        public void CloseHelp() { Screens.CloseHelp(); ApplyScreen(); }
        public void SkipTutorial() { if (started) game.SkipTutorial(); }
        public void AskGuide() { if (started) game.AskGuide(true); }
        public void Save() { if (started) game.Save(); }
        public void ToTitle()
        {
            if (started) game.Save();
            Screens.Title(); ApplyScreen();
        }
        public void Quit()
        {
            if (started) game.Save();
#if UNITY_EDITOR
            Celebration = "에디터에서는 Play 버튼으로 종료해 주세요.";
            celebrationUntil = Time.unscaledTime + 6;
#else
            Application.Quit();
#endif
        }
        private void ResetPlayer()
        {
            if (spawn != null) player.ResetPose(spawn.position, spawn.rotation);
        }
        private void RefreshProgress()
        {
            if (game.Session == null || !started) return;
            if (game.Session.CompletedMissions > knownMissions)
            {
                knownMissions = game.Session.CompletedMissions;
                Celebration = game.Session.IsComplete ? "★ 에코시티 청소 끝! 끝까지 해냈어요 ★" : "★ 잘했어요! 다음 단계가 열렸어요 ★\n" + FeedbackText.Level(game.Session.CurrentLevel);
                celebrationUntil = Time.unscaledTime + 6;
            }
            if (game.Session.IsComplete && Screens.CanExplore && !finishing)
            {
                if (finishDelay <= 0) { Screens.Finish(); ApplyScreen(); return; }
                finishing = true; finishAt = Time.unscaledTime + finishDelay;
                player.inputBlocked = true; interactor.inputBlocked = true;
                if (Completing != null) Completing();
            }
        }
        private void ApplyScreen()
        {
            bool exploring = Screens.CanExplore && game.Session != null;
            player.inputBlocked = !exploring;
            interactor.inputBlocked = !exploring;
            player.SetMouseCaptured(exploring);
            // UI pauses input, not the world clock. Gravity and UI stay responsive.
            Time.timeScale = 1;
            if (exploring)
            {
                moveOrigin = player.transform.position;
                lookOrigin = player.viewCamera.transform.rotation;
                bodyOrigin = player.transform.rotation;
            }
            if (ui != null) ui.Refresh();
            if (ScreenChanged != null) ScreenChanged();
        }
        private void OnApplicationFocus(bool focus) { if (!focus && Screens.CanExplore && !finishing) Pause(); }
    }
}
