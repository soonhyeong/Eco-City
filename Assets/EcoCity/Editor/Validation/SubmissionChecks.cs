#if UNITY_EDITOR
using System;
using System.IO;
using EcoCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoCity.Validation
{
    public static class SubmissionChecks
    {
        private static int assertions;
        [MenuItem("EcoCity/Submission/Run Submission Checks (No Play Mode)")]
        public static void Run()
        {
            AlphaChecks.Run();
            assertions = 0;
            var preferences = new SubmissionPreferences { sensitivity = float.NaN, volume = float.PositiveInfinity };
            preferences.Normalize(); Require(preferences.sensitivity == 2f && preferences.volume == SubmissionPreferences.DefaultVolume, "Invalid preferences default.");
            var older = new SubmissionPreferences { volume = 0.5f, music = 0.6f, mixRevision = 0 }; older.Normalize();
            Require(older.volume == SubmissionPreferences.DefaultVolume && older.music == SubmissionPreferences.DefaultMusic && older.mixRevision == 1, "Older quiet settings raised once.");
            preferences.sensitivity = 100; preferences.volume = -3; preferences.Normalize();
            Require(preferences.sensitivity == 4 && preferences.volume == 0, "Preferences clamp.");
            EcoCity.Editor.SubmissionSceneBuilder.Create();
            var flow = UnityEngine.Object.FindFirstObjectByType<AlphaFlow>();
            var settings = UnityEngine.Object.FindFirstObjectByType<SubmissionSettings>();
            var audio = UnityEngine.Object.FindFirstObjectByType<SubmissionAudio>();
            Require(settings != null && audio != null && settings.flow == flow && settings.audioFeedback == audio, "Settings/audio references.");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "One audio listener.");
            Require(flow.game.saveSlot == "eco-city-alpha-v1", "Keep alpha save compatibility.");
            foreach (var clip in new[] { audio.successClip, audio.retryClip, audio.completeClip })
                Require(clip != null && clip.channels == 1 && clip.samples > 1000 && clip.frequency == 22050, "Generated audio imported.");
            // Buttons use the world's pop; the result tones stay connected but silent where the soundscape plays its own.
            Require(audio.clickClip != null && audio.clickClip.samples > 1000 && !audio.resultTones, "Pop button sound, no doubled result tones.");
            var view = flow.player.GetComponent<PlayerAnimationView>();
            Require(view != null && view.animator != null && view.animator.avatar.isValid && view.animator.isHuman &&
                view.animator.runtimeAnimatorController != null && !view.animator.applyRootMotion, "Humanoid locomotion connected without root motion.");
            foreach (string parameter in new[] { "Speed", "MotionSpeed", "Grounded", "Jump", "FreeFall" })
                Require(Array.Exists(view.animator.parameters, p => p.name == parameter), "Animator parameter " + parameter);
            Require(flow.game.GetComponent<InteractionHighlight>().interactor == flow.interactor, "Target highlighting connected.");
            // Little Guys third-person player: boy/girl choice swaps only the visual child and its Animator.
            var selection = flow.player.GetComponent<CharacterSelection>();
            Require(flow.player.thirdPerson && flow.interactor.proximity && selection != null && selection.characters.Length == 2 &&
                selection.buttons.Length == 2 && selection.animationView == view, "Third-person player and character choice connected.");
            for (int i = 0; i < selection.characters.Length; i++)
            {
                var animator = selection.animators[i];
                Require(animator != null && animator.avatar != null && animator.avatar.isValid && animator.isHuman && animator.runtimeAnimatorController != null &&
                    !animator.applyRootMotion && animator.transform.IsChildOf(selection.characters[i].transform), "Character " + i + " humanoid locomotion.");
                selection.Select(i, false);
                Require(selection.characters[i].activeSelf && !selection.characters[1 - i].activeSelf && view.animator == animator, "Character " + i + " selection.");
            }
            selection.Select(0, false);
            // Play-test tuning: fixed camera heading, forgiving reach, footstep events received.
            Require(flow.player.cameraFollowSpeed == 0 && flow.interactor.reach >= 2.5f, "Kid camera and reach tuning.");
            foreach (var animator in selection.animators) Require(animator.GetComponent<AnimationEventSink>() != null, "Footstep receiver on " + animator.name);
            IntegrationChecks.CheckPlayerSetupAndMovement(flow.player);
            // Standing in front of an item selects it without aiming the camera.
            var nearItem = flow.game.wasteItems[0];
            flow.player.ResetPose(new Vector3(nearItem.transform.position.x, flow.spawn.position.y, nearItem.transform.position.z - 1.2f), Quaternion.identity);
            Physics.SyncTransforms();
            Require(flow.interactor.FindTarget() == nearItem, "Proximity interaction picks the item in front.");
            flow.player.ResetPose(flow.spawn.position, flow.spawn.rotation); Physics.SyncTransforms();
            // Kid-friendly world: every interactable shows an imported model, and every material renders in the built-in pipeline.
            foreach (var target in UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                var model = target.transform.Find("모델");
                Require(model != null && target.visualRenderer != null && target.visualRenderer.transform.IsChildOf(model) && target.GetComponent<Collider>() != null &&
                    (target is GuideNpc || Vector3.Distance(target.transform.localScale, Vector3.one) < 0.001f), "Kid model on " + target.name);
            }
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                    Require(material != null && material.shader.isSupported && material.shader.name != "Hidden/InternalErrorShader", "Built-in material on " + renderer.name);
            var cues = flow.game.GetComponent<KidWorldCues>();
            var kidHud = flow.ui.GetComponent<KidHud>();
            Require(cues != null && cues.items.Length == flow.game.wasteItems.Length && cues.markers.Length == cues.items.Length && cues.targetBubble != null, "World cues connected.");
            Require(kidHud != null && kidHud.heldIcon != null && kidHud.cleanFill != null && kidHud.progressDots.Length == 5 &&
                kidHud.progressDots[0].sprite != null && kidHud.cleanFill.sprite != null, "Kid HUD connected with rounded sprites.");
            Require(kidHud.promptKey != null && kidHud.tutorialPanel != null && kidHud.tutorialStep != null && kidHud.heldEmpty != null &&
                kidHud.celebrationPanel != null && kidHud.noticePanel != null && kidHud.stageMarks.Length == 6, "Kid HUD panels connected.");
            // The helper talks only up close; action results go to a toast. No separate "ask the helper" button.
            var talk = flow.ui.GetComponent<KidHelperTalk>();
            Require(talk != null && talk.helper == UnityEngine.Object.FindFirstObjectByType<GuideNpc>().transform && talk.bubble != null && talk.bubbleText != null &&
                !talk.bubble.gameObject.activeSelf && talk.toastPanel != null && talk.toastText != null && talk.feedback == flow.ui.feedback, "Helper speech bubble on E; result toast.");
            // Mini game: booth in the park, overlay with a picture for every item and every bin.
            var mini = flow.ui.GetComponent<KidMiniGame>();
            var booths = UnityEngine.Object.FindObjectsByType<MiniGameBooth>(FindObjectsSortMode.None);
            Require(mini != null && mini.panel != null && !mini.panel.activeSelf && mini.choices.Length == 3 && mini.choiceFaces.Length == 3 && mini.picture != null &&
                mini.pictures.Length == mini.pictureCategories.Length && mini.pictures.Length == 7 && mini.categoryNames.Length == 7 &&
                mini.startButton != null && mini.againButton != null && mini.resultStars.Length == 3, "Mini game overlay connected.");
            foreach (var picture in mini.pictures) Require(picture != null, "Mini game picture.");
            // One picture per bin, none of them an item that needs preparing first.
            for (int c = 0; c < 7; c++) Require(System.Array.IndexOf(mini.pictureCategories, c) >= 0, "Mini game shows bin " + c);
            Require(mini.secondsPerQuestion >= 8, "Mini game gives young players time.");
            // One booth per region, each with its own game, none within reach of a preparation station.
            Require(booths.Length == 3, "Three mini game booths.");
            for (int i = 0; i < 3; i++)
            {
                var booth = System.Array.Find(booths, b => b.kind == (KidMiniGame.Kind)i);
                Require(booth != null && booth.miniGame == mini && booth.GetComponent<Collider>() != null &&
                    flow.game.GetComponent<KidRegions>().RegionAt(booth.transform.position) == i, "Mini game booth " + (KidMiniGame.Kind)i + " in region " + i + ".");
                foreach (var station in UnityEngine.Object.FindObjectsByType<PreparationStation>(FindObjectsSortMode.None))
                    Require(Vector3.Distance(station.transform.position, booth.transform.position) > 6, "Booth " + booth.name + " clear of stations.");
            }
            Require(mini.titleText != null && mini.introText != null && mini.questionText != null && mini.titles.Length == 3, "Mini game title, intro and question line.");
            // "City saved" ending holds the results screen while fireworks, confetti, rainbow and banner play.
            var finale = flow.game.GetComponent<KidFinale>();
            Require(finale != null && finale.flow == flow && finale.fireworks != null && finale.confetti != null && finale.rainbow != null &&
                finale.banner != null && finale.bannerCard != null && finale.flash != null && !finale.banner.activeSelf && finale.duration >= 5 &&
                Mathf.Approximately(flow.finishDelay, finale.duration) && finale.voice != null && VoiceAsSet(finale.voice, "finale"), "City-saved ending connected.");
            // The park shows its state in pictures: smog, flower patches, fountain, butterflies, restoration ring.
            var mood = flow.game.GetComponent<KidEnvironmentMood>();
            Require(mood != null && mood.game == flow.game && mood.view == flow.player.viewCamera && mood.sun != null && mood.flowers.Length >= 60 &&
                mood.fountain != null && mood.butterflies != null && mood.twinkles != null && !mood.twinkles.main.playOnAwake && mood.sunIntensitySparkle > mood.sunIntensityClean && finale.mood == mood && finale.ring != null && finale.butterflies != null && finale.bounce.Length >= 10, "Park restores visibly.");
            foreach (var flower in mood.flowers)
            {
                Require(flower != null && flower.GetComponent<MeshRenderer>().sharedMaterials.Length == 3, "Flower patch " + (flower != null ? flower.name : "?"));
                foreach (var target in UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
                {
                    var a = flower.position; var b = target.transform.position; a.y = b.y = 0;
                    Require(Vector3.Distance(a, b) >= 2f, "Flowers keep clear of " + target.name);
                }
            }
            // Items are spread over the wider park, none inside a prop, with a ground arrow that never points at a bin.
            float spreadX = 0, spreadZ = 0;
            foreach (var a in flow.game.wasteItems) foreach (var b in flow.game.wasteItems) { spreadX = Mathf.Max(spreadX, Mathf.Abs(a.transform.position.x - b.transform.position.x)); spreadZ = Mathf.Max(spreadZ, Mathf.Abs(a.transform.position.z - b.transform.position.z)); }
            Require(spreadX >= 25 && spreadZ >= 25, "Items spread over the park.");
            Physics.SyncTransforms();
            foreach (var item in flow.game.wasteItems)
                foreach (var hit in Physics.OverlapSphere(item.transform.position + Vector3.up * 0.9f, 0.5f, ~0, QueryTriggerInteraction.Ignore))
                    Require(hit.transform.IsChildOf(item.transform) || hit.gameObject.name == "Ground", item.name + " is clear of " + hit.name);
            var arrowGuide = flow.player.GetComponent<KidGuideArrow>();
            Require(arrowGuide != null && arrowGuide.arrow != null && arrowGuide.game == flow.game && arrowGuide.interactor == flow.interactor, "Ground guide arrow connected.");
            // Held item is carried in the active character's hands.
            var heldVisual = flow.player.GetComponent<KidHeldVisual>();
            Require(heldVisual != null && heldVisual.selection == flow.player.GetComponent<CharacterSelection>(), "Held item uses the character's hands.");
            // Bigger park: walls moved out around the untouched mission area.
            Require(Mathf.Abs(flow.game.transform.parent.Find("EastWall").position.x) >= 20 && flow.game.transform.parent.Find("SouthWall").position.z <= -20, "Wider park.");
            Require(flow.ui.askGuide == null && flow.ui.menuGuideText == null, "Ask-helper button removed.");
            // Three stages: 3 clean items, 2 to prepare, 1 composite; the help screen and pause track describe this order.
            int[] perLevel = new int[3];
            foreach (var item in flow.game.wasteItems) perLevel[(int)item.definition.ToSpec(item.instanceId).Level]++;
            Require(perLevel[0] == 5 && perLevel[1] == 4 && perLevel[2] == 3, "Region sizes 5 / 4 / 3 match the map and help screen.");
            // Three regions, one stage each: every item sits in its own stage's region, each region has 7 bins and 3 stations.
            var regionFlow = flow.game.GetComponent<KidRegions>();
            Require(regionFlow != null && regionFlow.flow == flow && regionFlow.spawns.Length == 3 && System.Array.TrueForAll(regionFlow.spawns, s => s != null) &&
                regionFlow.map != null && !regionFlow.map.activeSelf && regionFlow.regionButtons.Length == 3 && regionFlow.mapFromPause != null &&
                System.Array.TrueForAll(regionFlow.brightPictures, p => p != null) && System.Array.TrueForAll(regionFlow.dullPictures, p => p != null) &&
                regionFlow.finale == finaleCheck(flow) && VoiceAsSet(regionFlow.voice, "map_pick", "region_saved"), "Regions and world map connected.");
            foreach (var item in flow.game.wasteItems)
                Require(regionFlow.RegionAt(item.transform.position) == (int)item.definition.ToSpec(item.instanceId).Level, item.instanceId + " lies in its stage's region.");
            for (int r = 0; r < 3; r++)
            {
                int binCount = 0, stationCount = 0;
                foreach (var bin in UnityEngine.Object.FindObjectsByType<DisposalBin>(FindObjectsSortMode.None)) if (regionFlow.RegionAt(bin.transform.position) == r) binCount++;
                foreach (var station in UnityEngine.Object.FindObjectsByType<PreparationStation>(FindObjectsSortMode.None)) if (regionFlow.RegionAt(station.transform.position) == r) stationCount++;
                Require(binCount == 7 && stationCount == 3, "Region " + (r + 1) + " has 7 bins and 3 stations.");
                // Scenery around each region: many landmark buildings and land shapes, nothing solid, moving parts.
                var scenery = GameObject.Find(EcoCity.Editor.SubmissionKidRegionsBuilder.SceneryNames[r]);
                int buildings = 0, hills = 0, mountains = 0;
                if (scenery != null)
                    foreach (Transform part in scenery.transform)
                    {
                        if (part.name.Contains("building") || part.name.Contains("apartment") || part.name.Contains("house")) buildings++;
                        if (part.name == "언덕") hills++;
                        if (part.name == "산") mountains++;
                    }
                Require(scenery != null && buildings >= 8 && hills >= 4 && mountains >= 5 && scenery.GetComponentsInChildren<Collider>().Length == 0 &&
                    scenery.GetComponentsInChildren<KidSpin>().Length >= 1 && regionFlow.RegionAt(scenery.transform.position) == r, "Region " + (r + 1) + " has rich scenery around it.");
            }
            foreach (var item in flow.game.wasteItems) Require(kidHud.Find(item.instanceId) != null, "Held picture for " + item.instanceId);
            // Polish: sky, water, breeze, screen look, sounds, living town, sticker book, stars, rewards, settings, gamepad.
            var polishMood = flow.game.GetComponent<KidEnvironmentMood>();
            var polishCamera = flow.player.viewCamera;
            Require(RenderSettings.skybox != null && RenderSettings.skybox.shader.name == "EcoCity/KidSky" && polishMood.sky == RenderSettings.skybox &&
                polishCamera.clearFlags == CameraClearFlags.Skybox, "Gradient sky.");
            var sea = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/EcoCity/Generated/Submission/KidWorld/Materials/Sea.mat");
            var pond = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/EcoCity/Generated/Submission/KidWorld/Materials/Water.mat");
            Require(sea != null && sea.shader.name == "EcoCity/KidWater" && pond != null && pond.shader.name == "EcoCity/KidWater", "Moving water.");
            var post = polishCamera.GetComponent<KidPostFx>();
            Require(post != null && post.shader != null && post.shader.isSupported && post.mood == polishMood && post.effectOn, "Screen look.");
            Require(flow.game.GetComponent<KidSway>() != null && flow.game.GetComponent<KidSway>().plants.Length >= 200 &&
                UnityEngine.Object.FindObjectsByType<KidBob>(FindObjectsSortMode.None).Length >= 20, "Breeze and floating things.");
            var sound = flow.game.GetComponent<KidSoundscape>();
            Require(sound != null && sound.flow == flow && sound.menuMusic != null && sound.regionMusic.Length == 3 && System.Array.TrueForAll(sound.regionMusic, c => c != null) &&
                sound.regionAmbience.Length == 3 && System.Array.TrueForAll(sound.regionAmbience, c => c != null) && sound.smogAmbience != null &&
                sound.grassSteps.Length == 3 && sound.sandSteps.Length == 3 && sound.pathSteps.Length == 3 && sound.pickup != null && sound.throwSound != null && sound.lid != null &&
                sound.empty != null && sound.separate != null && sound.wash != null && sound.wrong != null && sound.regionSavedJingle != null && sound.finaleJingle != null,
                "Music, ambience and action sounds.");
            Require(File.Exists("Assets/EcoCity/Generated/Submission/KidWorld/Sound/SOURCE.md") && File.Exists("Validation/GenerateKidSounds.py"), "Sound source note and generator.");
            int lineMovers = 0, birds = 0;
            foreach (var mover in UnityEngine.Object.FindObjectsByType<KidMover>(FindObjectsSortMode.None)) { if (mover.mode == KidMover.Mode.Line) lineMovers++; else birds++; }
            Require(UnityEngine.Object.FindObjectsByType<KidWalker>(FindObjectsSortMode.None).Length >= 12 && lineMovers >= 6 && birds >= 10, "Living town: walkers, traffic and birds.");
            foreach (var item in flow.game.wasteItems) Require(item.transform.Find("파리") != null && item.transform.Find("얼룩") != null && item.transform.Find("얼룩").GetComponent<Collider>() == null, "Flies and stain on " + item.instanceId);
            var book = flow.ui.GetComponent<KidStickerBook>();
            Require(book != null && book.open != null && book.close != null && book.panel != null && !book.panel.activeSelf && book.ids.Length == flow.game.wasteItems.Length &&
                System.Array.TrueForAll(book.pictures, picture => picture != null && picture.texture != null), "Sticker book.");
            Require(regionFlow.starsRegion1 != null && regionFlow.starsRegion1.Length == 3 && regionFlow.starsRegion2.Length == 3 && regionFlow.starsRegion3.Length == 3 &&
                regionFlow.starRows.Length == 3 && regionFlow.totalText != null && regionFlow.arrivalCard != null && !regionFlow.arrivalCard.activeSelf && regionFlow.stampClip != null, "Map stars and arrival card.");
            var rewards = flow.player.GetComponent<KidRewards>();
            Require(rewards != null && rewards.regions == regionFlow && rewards.partyHat != null && rewards.crown != null && !rewards.partyHat.activeSelf && !rewards.crown.activeSelf, "Hat rewards.");
            var polishSettings = UnityEngine.Object.FindFirstObjectByType<SubmissionSettings>();
            Require(polishSettings.music != null && polishSettings.bigText != null && polishSettings.screenEffects != null && polishSettings.bigTextTargets.Length >= 4 && polishSettings.postFx == post, "Settings: music, big text, screen look.");
            Require(flow.game.GetComponent<KidFeedbackFx>().held != null && flow.ui.practiceAgain.GetComponent<KidPadButton>() != null && flow.ui.confirmYes.GetComponent<KidPadButton>() != null, "Throw into the bin and gamepad buttons.");
            var reveal = ((Component)flow.ui.resultText).GetComponent<KidReveal>();
            Require(reveal != null && reveal.text != null, "Results are written out.");
            // Crisp text: TextMeshPro SDF with the Jua font (NanumGothic fallback for symbols) on every UI label and world sign.
            var kidFont = CheckCrispUi(flow.ui.gameObject, "game");
            Require(File.Exists("Assets/EcoCity/Fonts/Jua/OFL.txt") && File.Exists("Assets/EcoCity/Fonts/Jua/SOURCE.md"), "Kid font licence present.");
            Require(UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0, "No bitmap TextMesh signs.");
            var signs = UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(signs.Length >= 10, "World signs use TextMeshPro.");
            foreach (var sign in signs) Require(sign.font == kidFont && sign.fontSharedMaterial != null, "Kid font on sign " + sign.name);
            Require(reopenedFx(flow).markPrefab != null && reopenedFx(flow).markPrefab.font == kidFont, "Effect mark uses the kid font.");
            // Replacing a visual child must not require replacing interaction/data components.
            var replacementRoot = new GameObject("Replacement visual check");
            try
            {
                var target = replacementRoot.AddComponent<GuideNpc>();
                new GameObject("World label", typeof(TextMesh)).transform.SetParent(replacementRoot.transform);
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.transform.SetParent(replacementRoot.transform);
                Require(InteractionHighlight.FindVisual(target) == visual.GetComponent<Renderer>(), "Child model discovered; text labels excluded.");
                target.visualRenderer = visual.GetComponent<Renderer>();
                Require(InteractionHighlight.FindVisual(target) == target.visualRenderer, "Explicit renderer override supported.");
            }
            finally { UnityEngine.Object.DestroyImmediate(replacementRoot); }
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing submission script.");
                    foreach (var component in child.GetComponents<Component>())
                    {
                        var property = new SerializedObject(component).GetIterator();
                        while (property.Next(true)) if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Require(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0, "Broken submission reference: " + property.propertyPath);
                    }
                }
            string folder = Path.Combine(Path.GetTempPath(), "EcoCitySubmission-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                flow.game.Initialize(Path.Combine(folder, "progress.json")); flow.Initialize(); settings.Initialize(false);
                settings.openTitle.onClick.Invoke(); flow.ui.Refresh();
                Require(settings.panel.activeSelf && flow.ui.externalOverlay && flow.player.inputBlocked &&
                    !flow.ui.screens[(int)AlphaScreen.Help].activeSelf, "Settings modal blocks underlying UI/input.");
                settings.sensitivity.value = 1.25f; settings.volume.value = 0.2f; settings.invertY.isOn = true;
                Require(flow.player.lookSensitivity == 1.25f && flow.player.invertLookY && Mathf.Abs(audio.GetComponent<AudioSource>().volume - Mathf.Clamp01(0.2f * audio.toneGain)) < 0.001f,
                    "Settings sliders/toggle apply to player and sound.");
                settings.fullscreen.isOn = false; Require(!settings.Preferences.fullscreen, "Fullscreen preference callback.");
                settings.defaults.onClick.Invoke();
                Require(flow.player.lookSensitivity == 2 && !flow.player.invertLookY && settings.Preferences.fullscreen, "Restore defaults (full screen).");
                settings.close.onClick.Invoke();
                Require(!settings.panel.activeSelf && !flow.ui.externalOverlay && flow.Screens.Current == AlphaScreen.Title, "Settings return to title.");
                flow.NewGame(); flow.Pause(); settings.openPause.onClick.Invoke(); settings.close.onClick.Invoke();
                Require(flow.Screens.Current == AlphaScreen.Paused && flow.player.inputBlocked, "Settings return to pause.");
                flow.Resume(); Require(flow.Screens.CanExplore && !flow.player.inputBlocked && !flow.interactor.inputBlocked && Time.timeScale == 1,
                    "Exploration restores input/time.");
                settings.Initialize(false); settings.Initialize(false); flow.Pause(); settings.openPause.onClick.Invoke();
                Require(settings.panel.activeSelf && flow.Screens.Current == AlphaScreen.Help, "Repeated settings binding stable.");
                flow.CloseHelp(); settings.Close();
                Require(!flow.ui.externalOverlay && flow.Screens.Current == AlphaScreen.Paused, "External Escape close restores UI.");
                // Players choose the bin themselves: the bin is named only after repeated mistakes.
                string spoken = null; ActionResult last = null;
                Action<string> listen = message => spoken = message; Action<ActionResult> capture = result => last = result;
                flow.game.MessagePublished += listen; flow.game.ResultPublished += capture;
                try
                {
                    var first = flow.game.wasteItems[0];
                    string bin = FeedbackText.Category(first.definition.targetBin) + " 통";
                    flow.game.PickUp(first); flow.game.AskGuide(true);
                    Require(spoken != null && !spoken.Contains(bin) && !first.definition.stateDescription.Contains(bin), "Guide does not name the bin before a mistake.");
                    var wrong = first.definition.targetBin == WasteCategory.General ? WasteCategory.Food : WasteCategory.General;
                    flow.game.Dispose(wrong);
                    Require(last != null && last.Code == ResultCode.WrongBin && !FeedbackText.Build(last, flow.game.Session.NeedsDetailedHint).Contains(bin), "First mistake gives a picture hint only.");
                    flow.game.Dispose(wrong);
                    Require(FeedbackText.Build(last, flow.game.Session.NeedsDetailedHint).Contains(bin), "Repeated mistakes name the bin.");
                    flow.game.AskGuide(true); Require(spoken.Contains(bin), "Guide names the bin after repeated mistakes.");
                    // Mistakes with one item must not reveal the next item's bin (play-test regression).
                    flow.game.Dispose(first.definition.targetBin);
                    var next = flow.game.wasteItems[1];
                    string nextBin = FeedbackText.Category(next.definition.targetBin) + " 통";
                    flow.game.PickUp(next);
                    Require(!flow.game.NeedsBinHint && !FeedbackText.Build(last, flow.game.NeedsBinHint).Contains(nextBin), "Pickup after earlier mistakes keeps the next bin hidden.");
                    flow.game.AskGuide(true); Require(!spoken.Contains(nextBin), "Guide keeps the next bin hidden after earlier mistakes.");
                }
                finally { flow.game.MessagePublished -= listen; flow.game.ResultPublished -= capture; }
            }
            finally { Directory.Delete(folder, true); }
            // Discard test state, retain only builder-authored scene changes.
            string guid = AssetDatabase.AssetPathToGUID(EcoCity.Editor.SubmissionSceneBuilder.ScenePath);
            EditorSceneManager.OpenScene(EcoCity.Editor.SubmissionSceneBuilder.ScenePath);
            EcoCity.Editor.SubmissionSceneBuilder.Create();
            Require(guid == AssetDatabase.AssetPathToGUID(EcoCity.Editor.SubmissionSceneBuilder.ScenePath), "Preserve submission scene on repeat.");
            EcoCity.Editor.SubmissionSceneBuilder.ConfigureProject();
            Require(EditorBuildSettings.scenes.Length > 2 && EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[1].enabled && EditorBuildSettings.scenes[2].enabled &&
                EditorBuildSettings.scenes[0].path == EcoCity.Editor.SubmissionTitleBuilder.ScenePath &&
                EditorBuildSettings.scenes[1].path == EcoCity.Editor.SubmissionCharacterBuilder.ScenePath &&
                EditorBuildSettings.scenes[2].path == EcoCity.Editor.SubmissionSceneBuilder.ScenePath, "Title, then character choice, then game.");
            var reopened = UnityEngine.Object.FindFirstObjectByType<AlphaFlow>();
            var bridge = reopened.game.GetComponent<TitleSceneBridge>();
            Require(bridge != null && bridge.flow == reopened, "Title bridge connected.");
            Require(reopened.player.GetComponent<KidHeldVisual>() != null && reopened.game.GetComponent<KidFeedbackFx>().sparkles != null &&
                VoiceAsSet(reopened.game.GetComponent<KidVoice>(), "correct", "finale"), "Held item, effects and voice as set.");
            CheckTitleScene();
            CheckCharacterScene();
            EditorSceneManager.OpenScene(EcoCity.Editor.SubmissionSceneBuilder.ScenePath);
            Require(PlayerSettings.bundleVersion == "1.0.0" && PlayerSettings.productName == "Eco-City", "Version and persistent identity.");
            Debug.Log("EcoCity submission PASSED: " + assertions + " assertions. Settings, generated audio references, humanoid controller, modal input, scene startup. No Play Mode.");
        }
        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
        // Full-screen title scene: every control connected, kid font everywhere, park picture and welcome voice present.
        private static void CheckTitleScene()
        {
            EditorSceneManager.OpenScene(EcoCity.Editor.SubmissionTitleBuilder.ScenePath);
            var menu = UnityEngine.Object.FindFirstObjectByType<TitleMenu>();
            Require(menu != null && menu.startButton != null && menu.continueButton != null && menu.helpButton != null && menu.settingsButton != null &&
                menu.quitButton != null && menu.confirmYes != null && menu.confirmNo != null && menu.helpClose != null && menu.settingsClose != null, "Title buttons.");
            Require(menu.confirmPanel != null && menu.helpPanel != null && menu.settingsPanel != null && menu.volume != null && menu.sensitivity != null, "Title popups.");
            Require(menu.characterButtons.Length == 0 && menu.background != null && menu.background.texture != null, "Title picture; characters are chosen on the next scene.");
            Require(menu.clickClip != null && (menu.welcomeClip != null) == KidVoice.Enabled && menu.sound != null, "Title sounds.");
            CheckCrispUi(menu.gameObject, "title");
            Require(menu.volumeValue != null && menu.sensitivityValue != null, "Title slider values.");
            Require(menu.music != null && menu.musicValue != null && menu.creditsButton == null && menu.creditsPanel == null, "Title music slider; no credits screen in the game.");
            // Logo picture on top; no credit or font line on any screen.
            Require(menu.logo != null && menu.logo.GetComponent<UnityEngine.UI.Image>() != null && menu.logo.GetComponent<UnityEngine.UI.Image>().sprite != null, "Title logo picture.");
            foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Require(!label.text.Contains("겜성충만") && !label.text.Contains("SIL OFL") && !label.text.Contains("만든 사람"), "No credit line in the title: " + label.name);
            var loading = UnityEngine.Object.FindFirstObjectByType<KidLoading>();
            Require(loading != null && loading.group != null && loading.tipText != null && loading.tips.Length >= 5 && !loading.group.gameObject.activeSelf, "Loading screen between scenes.");
            Require(menu.startButton.GetComponent<KidPadButton>() != null && UnityEngine.Object.FindFirstObjectByType<KidSoundscape>() != null &&
                UnityEngine.Object.FindFirstObjectByType<KidSoundscape>().menuMusic != null, "Title gamepad start and music.");
            Require(UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EcoCity/Art/Icon/EcoCityIcon.png") != null, "App icon.");
            var scaler = UnityEngine.Object.FindFirstObjectByType<UnityEngine.UI.CanvasScaler>();
            Require(scaler.referenceResolution == new Vector2(1920, 1080) && UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1, "Title canvas and input.");
        }
        // Voice on: the lines are linked. Voice off (KidVoice.Enabled = false): no clips are linked, so none are built.
        private static bool VoiceAsSet(KidVoice voice, params string[] keys)
        {
            if (voice == null) return false;
            if (!KidVoice.Enabled) return voice.clips == null || voice.clips.Length == 0;
            return voice.clips.Length >= 40 && System.Array.TrueForAll(keys, voice.Has);
        }
        private static KidFeedbackFx reopenedFx(AlphaFlow flow) { return flow.game.GetComponent<KidFeedbackFx>(); }
        // No legacy Text, no Outline/Shadow mesh effects (they smear text), only high-resolution sprites, one SDF font family.
        private static TMPro.TMP_FontAsset CheckCrispUi(GameObject root, string where, int minimumLabels = 20)
        {
            var kidFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(EcoCity.Editor.SubmissionKidUi.FontAssetPath);
            var fallback = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(EcoCity.Editor.SubmissionKidUi.FallbackAssetPath);
            Require(kidFont != null && kidFont.sourceFontFile != null && kidFont.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic &&
                kidFont.fallbackFontAssetTable.Contains(fallback) && fallback != null, "Jua SDF font with NanumGothic fallback.");
            Require(root.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length == 0, "No legacy UI Text in the " + where + " UI.");
            Require(root.GetComponentsInChildren<UnityEngine.UI.Shadow>(true).Length == 0, "No Outline/Shadow effects in the " + where + " UI.");
            var labels = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
            Require(labels.Length >= minimumLabels, "TextMeshPro labels in the " + where + " UI.");
            foreach (var label in labels) Require(label.font == kidFont && label.fontSharedMaterial != null, "Kid font on " + where + " label " + label.name);
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                Require(image.sprite == null || image.sprite.texture.width >= 64 && (image.sprite.name.StartsWith("UI2_") || AssetDatabase.GetAssetPath(image.sprite) == EcoCity.Editor.SubmissionKidUi.LogoPath), "Crisp sprite on " + where + " " + image.name);
            return kidFont;
        }
        // Character choice scene: two 3D friends on stages, name cards, start and back.
        private static void CheckCharacterScene()
        {
            EditorSceneManager.OpenScene(EcoCity.Editor.SubmissionCharacterBuilder.ScenePath);
            var picker = UnityEngine.Object.FindFirstObjectByType<CharacterPicker>();
            Require(picker != null && picker.view != null && picker.models.Length == 2 && picker.animators.Length == 2 && picker.cards.Length == 2 &&
                picker.cardFaces.Length == 2 && picker.rings.Length == 2 && picker.startButton != null && picker.backButton != null && picker.clickClip != null && (picker.welcomeClip != null) == KidVoice.Enabled, "Character scene connected.");
            for (int i = 0; i < 2; i++)
                Require(picker.animators[i] != null && picker.animators[i].isHuman && picker.animators[i].runtimeAnimatorController != null &&
                    picker.models[i].GetComponent<Collider>() != null, "Character " + i + " animated and clickable.");
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                    Require(material != null && material.shader.isSupported && material.shader.name != "Hidden/InternalErrorShader", "Built-in material on " + renderer.name);
            CheckCrispUi(UnityEngine.Object.FindFirstObjectByType<UnityEngine.Canvas>().gameObject, "character", 4);
            Require(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1, "Character scene input.");
        }
        private static KidFinale finaleCheck(AlphaFlow flow) { return flow.game.GetComponent<KidFinale>(); }
        private static void Require(bool value, string message) { assertions++; if (!value) throw new Exception("Submission check: " + message); }
    }
}
#endif
