#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity.Editor
{
    public static class SubmissionSceneBuilder
    {
        public const string ScenePath = "Assets/1. Scene/EcoCitySubmission.unity";
        public const string Version = "1.0.0";
        private const string Root = "Assets/EcoCity/Generated/Submission";
        [MenuItem("EcoCity/Submission/Create or Open Submission Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(AlphaSceneBuilder.ScenePath);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, false)) throw new IOException("Cannot create submission scene.");
            var flow = UnityEngine.Object.FindFirstObjectByType<AlphaFlow>();
            flow.game.transform.parent.name = "에코시티 구조대 · 제출판";
            AlphaSceneBuilder.UseFont(AssetDatabase.LoadAssetAtPath<Font>("Assets/EcoCity/Fonts/NanumGothic-Regular.ttf"));
            foreach (var text in flow.ui.GetComponentsInChildren<Text>(true))
                text.text = text.text.Replace("알파 연습판", "제출판 " + Version + " · 2026 SCC · 겜성충만");
            var source = flow.game.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0.5f;
            var audio = flow.game.gameObject.AddComponent<SubmissionAudio>(); audio.game = flow.game; audio.ui = flow.ui;
            audio.clickClip = Tone("선택", 0.10f, new[] { 523.25 });
            audio.successClip = Tone("배출 성공", 0.30f, new[] { 523.25, 659.25 });
            audio.retryClip = Tone("다시 생각하기", 0.25f, new[] { 392.0, 349.23 });
            audio.completeClip = Tone("미션 완료", 0.55f, new[] { 523.25, 659.25, 783.99, 1046.5 });
            if (flow.player.viewCamera.GetComponent<AudioListener>() == null) flow.player.viewCamera.gameObject.AddComponent<AudioListener>();
            var settings = flow.game.gameObject.AddComponent<SubmissionSettings>(); settings.flow = flow; settings.audioFeedback = audio;
            BuildSettingsPanel(settings);
            var highlight = flow.game.gameObject.AddComponent<InteractionHighlight>(); highlight.interactor = flow.interactor;
            var animator = flow.player.GetComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Player/Common/DEMO/Animator/StarterAssetsThirdPerson.controller");
            if (animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman && controller != null)
            {
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                var animationView = flow.player.gameObject.AddComponent<PlayerAnimationView>();
                animationView.animator = animator; animationView.flow = flow; animationView.controller = flow.player.GetComponent<CharacterController>();
            }
            else Debug.LogWarning("EcoCity: existing avatar/controller cannot provide humanoid locomotion; character appearance is preserved.");
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save submission scene.");
        }
        [MenuItem("EcoCity/Submission/Configure Windows Submission")]
        public static void ConfigureProject()
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("Create the submission scene first.");
            // The full-screen title scene starts the game when it exists; the game scene follows it.
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != SubmissionTitleBuilder.ScenePath && s.path != SubmissionCharacterBuilder.ScenePath).ToList();
            // Title → character choice → game.
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            if (File.Exists(SubmissionCharacterBuilder.ScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(SubmissionCharacterBuilder.ScenePath, true));
            if (File.Exists(SubmissionTitleBuilder.ScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(SubmissionTitleBuilder.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            // Retain company/product identity: changing these would relocate existing progress.
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            // App icon (placeholder until the team logo arrives; generated by Validation/GenerateKidIcon.py).
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EcoCity/Art/Icon/EcoCityIcon.png");
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            SharpQuality();
            AssetDatabase.SaveAssets();
        }
        /// <summary>The PC quality level (Ultra): smoother edges and crisper shadows on large screens.
        /// The in-game "가벼운 화면" switch still lowers these at run time on slow computers.</summary>
        public static void SharpQuality()
        {
            int previous = QualitySettings.GetQualityLevel();
            int ultra = System.Array.IndexOf(QualitySettings.names, "Ultra");
            if (ultra < 0) return;
            QualitySettings.SetQualityLevel(ultra, false);
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            // A shorter shadow range spreads the shadow map over what the close follow camera actually sees, so edges stay sharp.
            QualitySettings.shadowDistance = 90;
            QualitySettings.shadowCascades = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.SetQualityLevel(previous, false);
        }
        private static void BuildSettingsPanel(SubmissionSettings settings)
        {
            var ui = settings.flow.ui;
            settings.openTitle = AlphaSceneBuilder.Button(ui.newGame.transform.parent, "환경 설정"); settings.openTitle.transform.SetSiblingIndex(5);
            settings.openPause = AlphaSceneBuilder.Button(ui.resume.transform.parent, "환경 설정"); settings.openPause.transform.SetSiblingIndex(5);
            var root = AlphaSceneBuilder.Panel("환경 설정 화면", ui.transform, new Color(0.04f,0.13f,0.15f,0.98f), Vector2.zero, Vector2.one);
            settings.panel = root.gameObject;
            var menu = AlphaSceneBuilder.Menu(root, "편하게 조절해요", "설정은 진행 저장과 따로 기억해요.\n게임을 초기화해도 이 설정은 유지돼요.");
            settings.values = AlphaSceneBuilder.Label(menu, "", 21, 40);
            settings.sensitivity = Slider(menu, "마우스 감도", 0.25f, 4f, 2f);
            settings.volume = Slider(menu, "효과음 음량", 0, 1, EcoCity.Core.SubmissionPreferences.DefaultVolume);
            settings.invertY = Toggle(menu, "시선 위아래 반전"); settings.fullscreen = Toggle(menu, "전체 화면 (PC 실행 파일에서 적용)");
            settings.defaults = AlphaSceneBuilder.Button(menu, "기본 설정으로"); settings.close = AlphaSceneBuilder.Button(menu, "돌아가기");
            AlphaSceneBuilder.Label(menu, "겜성충만 · 성결대학교 2026 SCC\n나눔고딕: NHN / OFL 1.1 · 에셋 출처는 동봉 문서 참조", 16, 50);
            root.gameObject.SetActive(false);
        }
        private static Slider Slider(Transform parent, string title, float min, float max, float value)
        {
            var row = AlphaSceneBuilder.Panel(title, parent, new Color(0,0,0,0), Vector2.zero, Vector2.one);
            var size = row.gameObject.AddComponent<LayoutElement>(); size.preferredHeight = 65; size.minHeight = 52;
            AlphaSceneBuilder.Layout(row, 0, 4); AlphaSceneBuilder.Label(row, title, 20, 25);
            var bar = AlphaSceneBuilder.Panel("Slider", row, new Color(0.68f,0.75f,0.72f), Vector2.zero, Vector2.one);
            var barSize = bar.gameObject.AddComponent<LayoutElement>(); barSize.preferredHeight = 24; barSize.minHeight = 20;
            var fill = AlphaSceneBuilder.Panel("Fill", bar, new Color(0.09f,0.43f,0.39f), Vector2.zero, Vector2.one);
            var handle = AlphaSceneBuilder.Panel("Handle", bar, Color.white, Vector2.zero, new Vector2(0,1));
            handle.offsetMin = new Vector2(-9,-3); handle.offsetMax = new Vector2(9,3);
            var slider = bar.gameObject.AddComponent<Slider>(); slider.minValue = min; slider.maxValue = max;
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.value = value;
            return slider;
        }
        private static Toggle Toggle(Transform parent, string title)
        {
            var row = AlphaSceneBuilder.Panel(title, parent, new Color(0,0,0,0), Vector2.zero, Vector2.one);
            row.GetComponent<Image>().raycastTarget = true;
            var size = row.gameObject.AddComponent<LayoutElement>(); size.preferredHeight = 38; size.minHeight = 30;
            var background = AlphaSceneBuilder.Panel("Box", row, new Color(0.7f,0.78f,0.74f), new Vector2(0,0.15f), new Vector2(0,0.85f));
            background.sizeDelta = new Vector2(28,0); background.anchoredPosition = new Vector2(14,0);
            var check = AlphaSceneBuilder.Panel("Checked", background, new Color(0.09f,0.43f,0.39f), new Vector2(0.15f,0.15f), new Vector2(0.85f,0.85f));
            var label = AlphaSceneBuilder.FillText(row, title, 20, new Color(0.09f,0.22f,0.24f)); label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(40,0); label.rectTransform.offsetMax = Vector2.zero;
            var toggle = row.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = background.GetComponent<Image>(); toggle.graphic = check.GetComponent<Image>();
            return toggle;
        }
        private static AudioClip Tone(string name, float seconds, double[] notes)
        {
            string path = Root + "/" + name + ".wav";
            const int rate = 22050; int count = (int)(rate * seconds);
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                double duration = seconds / notes.Length;
                for (int i = 0; i < count; i++)
                {
                    double t = (double)i / rate; int note = Math.Min(notes.Length - 1, (int)(t / duration));
                    double local = (t - note * duration) / duration;
                    double envelope = Math.Sin(Math.PI * Math.Min(1, local));
                    writer.Write((short)(Math.Sin(2 * Math.PI * notes[note] * t) * envelope * 0.14 * short.MaxValue));
                }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
#endif
