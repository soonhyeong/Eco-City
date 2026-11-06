#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static EcoCity.Editor.SubmissionKidUi;

namespace EcoCity.Editor
{
    // Separate full-screen title scene (1920x1080 reference): park picture, sticker logo, boy/girl cards,
    // a big start button and small secondary buttons, like a store-ready mobile game. Run after the kid makeover.
    public static class SubmissionTitleBuilder
    {
        public const string ScenePath = "Assets/Scenes/EcoCityTitle.unity";
        private const string Kid = "Assets/EcoCity/Generated/Submission/KidWorld/";

        [MenuItem("EcoCity/Submission/Build Title Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var picture = AssetDatabase.LoadAssetAtPath<Texture2D>(Kid + "TitleBackground.png");
            if (picture == null) throw new FileNotFoundException("Run 'Apply Kid-Friendly World and UI' first.");
            Prepare();
            // The character scene (and the friends' cut-outs the title shows) is built first.
            SubmissionCharacterBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Title Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.62f, 0.83f, 0.97f);
            var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UseUiActions(events.GetComponent<InputSystemUIInputModule>());
            var soundObject = new GameObject("Title Sound", typeof(AudioSource));
            var sound = soundObject.GetComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 0;

            var canvasObject = new GameObject("Title UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1f;
            var root = canvasObject.transform;
            var menu = canvasObject.AddComponent<TitleMenu>();
            menu.sound = sound;
            menu.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoCity/Generated/Submission/KidWorld/Sound/sfx/pop.wav");
            // Spoken lines are off (KidVoice.Enabled); the clip stays in the project but is not linked or built.
            menu.welcomeClip = KidVoice.Enabled ? AssetDatabase.LoadAssetAtPath<AudioClip>(Kid + "Voice/title.wav") : null;
            SubmissionKidUi.MenuMusic(menu.transform.root);

            var parts = Title(root, picture, "제출판 " + SubmissionSceneBuilder.Version + " · 2026 SCC · 겜성충만", false);
            menu.background = parts.background; menu.logo = parts.logo; menu.leaves = parts.leaves;
            menu.characterButtons = parts.characters; menu.characterFrames = parts.frames;
            menu.selectedColor = Sun; menu.idleColor = Color.white;
            menu.startButton = parts.start; menu.continueButton = parts.continueGame; menu.helpButton = parts.help;
            menu.settingsButton = parts.settings; menu.quitButton = parts.quit;

            // Popups.
            var confirm = Popup(root, "처음부터 할까요?", new Vector2(760, 430), Orange, out var confirmCard);
            Text("설명", confirmCard, "저장된 진행이 지워지고 처음부터 시작해요.\n지금 진행을 이어가려면 돌아가기를 눌러요.", 27, Ink, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0, -88), new Vector2(660, 140));
            menu.confirmYes = Candy("처음부터 하기", confirmCard, "처음부터 하기", Orange, new Vector2(0.5f, 1), new Vector2(-170, -264), new Vector2(320, 96), 36);
            menu.confirmNo = Candy("돌아가기", confirmCard, "돌아가기", Green, new Vector2(0.5f, 1), new Vector2(170, -264), new Vector2(320, 96), 36);
            menu.confirmPanel = confirm;

            var help = Popup(root, "어떻게 해요?", new Vector2(1160, 840), Sky, out var helpCard);
            HowTo(helpCard, -82);
            menu.helpClose = Candy("돌아가기", helpCard, "돌아가기", Green, new Vector2(0.5f, 1), new Vector2(0, -712), new Vector2(420, 90), 36);
            menu.helpPanel = help;

            var settings = Popup(root, "설정", new Vector2(780, 690), Pink, out var settingsCard);
            menu.volume = SubmissionKidUi.Slider(settingsCard, "효과음 크기", -100, 0, 1, 640, out var volumeValue);
            menu.music = SubmissionKidUi.Slider(settingsCard, "음악 크기", -232, 0, 1, 640, out var musicValue);
            menu.sensitivity = SubmissionKidUi.Slider(settingsCard, "카메라 돌리는 속도", -364, 0.25f, 4, 640, out var sensitivityValue);
            menu.volumeValue = volumeValue; menu.sensitivityValue = sensitivityValue; menu.musicValue = musicValue;
            menu.settingsClose = Candy("돌아가기", settingsCard, "돌아가기", Green, new Vector2(0.5f, 1), new Vector2(0, -530), new Vector2(420, 90), 36);
            menu.settingsPanel = settings;
            // Gamepad A / Enter starts the game unless a popup is open.
            var pad = menu.startButton.GetComponent<KidPadButton>(); if (pad == null) pad = menu.startButton.gameObject.AddComponent<KidPadButton>();
            // No credits screen in the game; keep source licences with the original assets.
            pad.blockers = new[] { confirm, help, settings };
            var yes = menu.confirmYes.gameObject.AddComponent<KidPadButton>();
            SubmissionKidUi.LoadingScreen();

            BakeCharacters(canvasObject);
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save title scene.");
            SubmissionSceneBuilder.ConfigureProject();
            Debug.Log("EcoCity: title scene built at " + ScenePath + ". No Play Mode.");
        }
        private static GameObject Popup(Transform root, string title, Vector2 size, Color ribbon, out RectTransform card)
        {
            var screen = Full(title + " 창", root);
            card = MenuCard(screen, title, null, size, ribbon);
            screen.gameObject.SetActive(false);
            return screen.gameObject;
        }
    }
}
#endif
