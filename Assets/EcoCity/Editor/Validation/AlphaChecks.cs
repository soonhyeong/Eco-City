#if UNITY_EDITOR
using System;
using System.IO;
using EcoCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EcoCity.Validation
{
    public static class AlphaChecks
    {
        private static int checks;
        [MenuItem("EcoCity/Run Alpha Checks (No Play Mode)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
            IntegrationChecks.Run(); // Existing checks are retained and executed first.
            checks = 0;
            CheckPureState();
            EcoCity.Editor.AlphaSceneBuilder.Create();
            var flow = UnityEngine.Object.FindFirstObjectByType<AlphaFlow>();
            Require(flow != null && flow.ui != null && flow.spawn != null, "Alpha references.");
            var game = flow.game; var ui = flow.ui;
            Require(UnityEngine.Object.FindObjectsByType<PrototypeHud>(FindObjectsSortMode.None).Length == 0, "No IMGUI HUD in alpha.");
            Require(UnityEngine.Object.FindObjectsByType<EcoGame>(FindObjectsSortMode.None).Length == 1, "One game.");
            Require(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length == 1, "One camera.");
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "One UI event system.");
            Require(UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>() != null, "New Input System UI module.");
            var module = UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>();
            Require(module.actionsAsset != null && module.point != null && module.leftClick != null && module.submit != null && module.move != null,
                "Serialized UI input actions.");
            var canvas = ui.GetComponent<Canvas>();
            Require(canvas != null && canvas.GetComponent<GraphicRaycaster>() != null && canvas.GetComponent<CanvasScaler>() != null, "Canvas configuration.");
            Require(ui.screens.Length == 6, "All screens.");
            foreach (var text in ui.GetComponentsInChildren<Text>(true)) Require(text.font != null && text.font.HasCharacter('가'), "Bundled Korean font: " + text.name);
            foreach (var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                Require(text.font != null && text.font.HasCharacter('가'), "Korean world sign font.");
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing alpha script.");
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        var property = new SerializedObject(component).GetIterator();
                        while (property.Next(true)) if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Require(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0, "Broken alpha reference: " + property.propertyPath);
                    }
                }
            foreach (var item in game.wasteItems)
            {
                Require(item.definition.fictionalPractice && !string.IsNullOrEmpty(item.definition.stateDescription), "Explicit content status.");
                Require(!string.IsNullOrEmpty(item.definition.sourceNote) && !string.IsNullOrEmpty(item.definition.applicableRegion), "Content conditions.");
            }
            Require(flow.player.viewCamera.transform.parent == flow.player.transform && game.waitForStart, "Player/camera/title setup.");
            IntegrationChecks.CheckPlayerSetupAndMovement(flow.player);
            Physics.SyncTransforms();
            foreach (var target in UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                var bounds = target.GetComponent<Collider>().bounds;
                Vector3 origin = new Vector3(bounds.center.x, 1.6f, bounds.center.z - 2);
                RaycastHit hit;
                Require(Physics.Raycast(origin, (bounds.center - origin).normalized, out hit, flow.interactor.distance,
                    flow.interactor.mask, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Interactable>() == target,
                    "Clear interaction approach: " + target.name);
            }
            var city = UnityEngine.Object.FindFirstObjectByType<AlphaCityView>();
            Require(city != null && city.restoredGardens.Length == 3, "Three garden restoration stages.");
            string folder = Path.Combine(Path.GetTempPath(), "EcoCityAlpha-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "alpha.json");
            try
            {
                game.Initialize(path); flow.Initialize(); ui.Refresh();
                Require(flow.Screens.Current == AlphaScreen.Title && !flow.CanContinue && !File.Exists(path), "First launch does not write save.");
                Require(flow.player.inputBlocked && flow.interactor.inputBlocked, "Title blocks all exploration.");
                Require(!ui.skipTutorial.interactable, "Title help cannot mutate an unstarted tutorial.");
                ui.newGame.onClick.Invoke();
                Require(flow.Screens.CanExplore && !flow.player.inputBlocked && !flow.interactor.inputBlocked, "New-game button connected.");
                Require(game.Session.TutorialActions == TutorialAction.None && !game.Session.TutorialComplete, "New tutorial begins empty.");
                int score = game.Session.Score;
                var observe = typeof(AlphaFlow).GetMethod("ObserveExploration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                observe.Invoke(flow, null);
                Require(game.Session.TutorialActions == TutorialAction.None, "No elapsed-time tutorial progress.");
                flow.player.ResetPose(flow.player.transform.position + Vector3.forward * 0.5f, Quaternion.Euler(0,15,0));
                observe.Invoke(flow, null);
                Require((game.Session.TutorialActions & (TutorialAction.Move | TutorialAction.Look)) == (TutorialAction.Move | TutorialAction.Look), "Actual transform observations.");
                game.ObserveTutorial(TutorialAction.Discover);
                Require(game.Session.Score == score && game.Session.TotalAttempts == 0, "Tutorial does not award or count attempts.");
                ui.pause.onClick.Invoke();
                Require(flow.Screens.Current == AlphaScreen.Paused && flow.player.inputBlocked && flow.interactor.inputBlocked && Time.timeScale == 1, "Pause input/time.");
                ui.pauseHelp.onClick.Invoke(); Require(flow.Screens.Current == AlphaScreen.Help, "Help opens.");
                ui.helpBack.onClick.Invoke(); Require(flow.Screens.Current == AlphaScreen.Paused, "Help returns to pause.");
                ui.askGuide.onClick.Invoke(); Require((game.Session.TutorialActions & TutorialAction.Guide) != 0, "Explicit guide event.");
                ui.resume.onClick.Invoke();
                game.PickUp(game.wasteItems[0]); game.Dispose(WasteCategory.General);
                Require(game.Session.Held != null && (game.Session.TutorialActions & TutorialAction.Dispose) == 0, "Failure keeps tutorial/disposal state.");
                game.Dispose(game.wasteItems[0].definition.targetBin);
                for (int i = 1; i < 3; i++) { game.PickUp(game.wasteItems[i]); game.Dispose(game.wasteItems[i].definition.targetBin); }
                Require(game.Session.CompletedMissions == 1 && !string.IsNullOrEmpty(flow.Celebration), "Mission celebration.");
                game.PickUp(game.wasteItems[3]); game.Prepare(Preparation.Empty);
                Require((game.Session.TutorialActions & TutorialAction.Prepare) == 0 && !game.Session.TutorialComplete,
                    "Partial preparation does not prematurely finish tutorial.");
                game.Save();
                var saved = game.Session.Capture();
                flow.ToTitle(); game.Initialize(path); flow.Initialize();
                Require(flow.CanContinue && game.Session.Held.Id == saved.heldId && game.Session.CompletedPreparation == Preparation.Empty,
                    "Continue restores held item and partial preparation.");
                Require(!game.Session.TutorialComplete && game.Session.TutorialActions == saved.tutorialActions, "Partial tutorial survives reload.");
                ui.continueGame.onClick.Invoke(); Require(flow.Screens.CanExplore, "Continue button connected.");
                game.Prepare(Preparation.Wash);
                Require(game.Session.TutorialComplete && (game.Session.TutorialActions & TutorialAction.Prepare) != 0, "Complete required preparation finishes tutorial.");
                flow.ToTitle(); ui.newGame.onClick.Invoke();
                Require(flow.Screens.Current == AlphaScreen.ConfirmNewGame, "Existing progress confirmation.");
                ui.confirmNo.onClick.Invoke(); Require(game.Session.Held != null && flow.Screens.Current == AlphaScreen.Title, "Cancel preserves progress.");
                ui.newGame.onClick.Invoke(); ui.confirmYes.onClick.Invoke();
                Require(game.Session.TotalAttempts == 0 && game.Session.Held == null && game.Session.TutorialActions == TutorialAction.None, "New-game reset.");
                Require(Vector3.Distance(flow.player.transform.position, flow.spawn.position) < 0.001f, "New-game spawn reset.");
                flow.Help(); ui.skipTutorial.onClick.Invoke();
                Require(game.Session.TutorialSkipped && game.Session.Score == 0, "Skip is persisted without rewards.");
                var skipRestored = new EcoSession(Array.ConvertAll(game.wasteItems, item => item.definition.ToSpec(item.instanceId)));
                string skipNotice;
                Require(new ProgressStore(path).LoadInto(skipRestored, out skipNotice) && skipRestored.TutorialSkipped, "Skipped tutorial JSON round trip.");
                flow.CloseHelp();
                for (int round = 0; round < 2; round++)
                {
                    foreach (var item in game.wasteItems)
                    {
                        game.PickUp(item);
                        while ((game.Session.Held.Required & ~game.Session.CompletedPreparation) != Preparation.None)
                            game.Prepare(WasteRules.NextStep(game.Session.Held.Required & ~game.Session.CompletedPreparation));
                        game.Dispose(item.definition.targetBin);
                    }
                    Require(flow.Screens.Current == AlphaScreen.Results && flow.player.inputBlocked && game.Session.Score == 210, "Full round/results " + round);
                    city.Refresh();
                    foreach (var garden in city.restoredGardens) Require(garden.activeSelf, "Gardens restored after completion.");
                    Require(AlphaUi.BuildResult(game.Session).Contains("210") && game.Session.IsComplete, "Result values.");
                    ui.practiceAgain.onClick.Invoke(); ui.confirmYes.onClick.Invoke();
                    Require(flow.Screens.CanExplore && game.Session.DisposedCount == 0 && !game.Session.TutorialSkipped, "Practice again resets everything.");
                    city.Refresh();
                    foreach (var garden in city.restoredGardens) Require(!garden.activeSelf, "Gardens reset on second round.");
                }
                ui.Bind(); ui.Bind();
                int transitions = 0; flow.ScreenChanged += () => transitions++;
                ui.toTitle.onClick.Invoke(); Require(transitions == 1, "Rebinding does not duplicate listeners.");
                ui.titleHelp.onClick.Invoke(); ui.helpBack.onClick.Invoke();
                Require(flow.Screens.Current == AlphaScreen.Title, "Help returns to title.");
                // Loading bad progress must keep the title usable and preserve the original on next write.
                File.WriteAllText(path, "{bad"); if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
                game.Initialize(path); flow.Initialize(); Require(!flow.CanContinue, "Invalid save hides Continue.");
                flow.NewGame(); Require(Directory.GetFiles(folder, "*.rejected-*").Length == 1, "Invalid alpha save preserved.");
            }
            finally { Directory.Delete(folder, true); }
            // Restore serialized scene state without saving temporary test state or player positions.
            string guid = AssetDatabase.AssetPathToGUID(EcoCity.Editor.AlphaSceneBuilder.ScenePath);
            EditorSceneManager.OpenScene(EcoCity.Editor.AlphaSceneBuilder.ScenePath);
            EcoCity.Editor.AlphaSceneBuilder.Create();
            Require(guid == AssetDatabase.AssetPathToGUID(EcoCity.Editor.AlphaSceneBuilder.ScenePath), "Repeated alpha generation keeps GUID/user edits.");
            Debug.Log("EcoCity alpha PASSED: " + checks + " assertions; screens, Korean fonts, serialized references, tutorial, saves, two rounds. No Play Mode.");
        }
        private static void CheckPureState()
        {
            var screens = new AlphaScreenState();
            Require(!screens.Continue(false, false) && screens.Current == AlphaScreen.Title, "No unavailable continue.");
            Require(!screens.RequestNewGame(true) && screens.Current == AlphaScreen.ConfirmNewGame, "Confirmation state.");
            Require(!screens.ConfirmNewGame(false) && screens.Current == AlphaScreen.Title, "Cancel transition.");
            Require(screens.RequestNewGame(false) && screens.CanExplore, "Fresh state.");
            screens.Pause(); screens.OpenHelp(); screens.CloseHelp(); Require(screens.Current == AlphaScreen.Paused, "Nested help.");
            screens.Resume(); screens.Finish(); screens.Title();
            Require(screens.Continue(true, true) && screens.Current == AlphaScreen.Results, "Completed continue.");
            var spec = new WasteSpec("one", "연습품", WasteCategory.Plastic, Preparation.Empty, LearningLevel.Basic, "");
            var session = new EcoSession(new[] { spec });
            Require(!session.ObserveTutorial((TutorialAction)1024), "Invalid tutorial bits.");
            var data = session.Capture(); data.tutorialRevision = 2; string error;
            Require(!session.Restore(data, out error), "Future tutorial revision rejected.");
            data = new ProgressData { heldId = "one" };
            Require(session.Restore(data, out error) && (session.TutorialActions & TutorialAction.PickUp) != 0, "Legacy held progress migration.");
            session.SkipTutorial(); var restored = new EcoSession(new[] { spec });
            Require(restored.Restore(session.Capture(), out error) && restored.TutorialSkipped, "Skip capture/restore.");
            Require(restored.Score == 0 && restored.TotalAttempts == 0, "Migration/skip no rewards.");
        }
        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
        private static void Require(bool condition, string message)
        { checks++; if (!condition) throw new Exception("Alpha check: " + message); }
    }
}
#endif
