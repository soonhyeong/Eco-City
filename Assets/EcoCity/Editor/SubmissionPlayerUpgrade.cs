#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity.Editor
{
    // Replaces the submission player with the Little Guys boy/girl, switches to a third-person camera and
    // proximity interaction, and adds the title-screen character choice. Safe to run again: it refreshes the same root.
    public static class SubmissionPlayerUpgrade
    {
        public const string PlayerName = "Player";
        private const string Root = "Assets/EcoCity/Generated/Submission/Characters";
        private const string Sample = "Assets/LuceedStudio/Character Lab/Little Guys/Little Guys - Free Sample/";
        private const string BuiltInShader = "Assets/LuceedStudio/Character Lab/Shaders/S_BI_Stylized_Combined.shader";
        private const string Controller = "Assets/Player/Common/DEMO/Animator/StarterAssetsThirdPerson.controller";
        private static readonly string[] Prefabs = { Sample + "Man/Free Man/Free Man.prefab", Sample + "Woman/Free Woman/Free Woman.prefab" };
        private static readonly string[] Labels = { "남자아이", "여자아이" };
        private static readonly string[] Keys = { "Boy", "Girl" };

        [MenuItem("EcoCity/Submission/Apply Little Guys Third-Person Player")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(SubmissionSceneBuilder.ScenePath);
            var flow = Object.FindFirstObjectByType<AlphaFlow>();
            var oldPlayer = flow.player;
            var player = oldPlayer.name == PlayerName ? oldPlayer : CreateRoot(flow, oldPlayer);

            player.thirdPerson = true; player.speed = 3.2f; player.cameraDistance = 3.2f; player.cameraPivotHeight = 1.0f; player.cameraPitch = 20f;
            var controller = player.GetComponent<CharacterController>();
            controller.height = 1.15f; controller.radius = 0.3f; controller.center = new Vector3(0, 0.6f, 0); controller.stepOffset = 0.3f;
            var interactor = player.GetComponent<PlayerInteractor>(); interactor.proximity = true; interactor.reach = 2.1f;
            player.ResetPose(flow.spawn.position, flow.spawn.rotation);

            var view = player.GetComponent<PlayerAnimationView>();
            var selection = player.GetComponent<CharacterSelection>();
            if (selection == null) selection = player.gameObject.AddComponent<CharacterSelection>();
            AlphaSceneBuilder.UseFont(AssetDatabase.LoadAssetAtPath<Font>("Assets/EcoCity/Fonts/NanumGothic-Regular.ttf"));
            selection.animationView = view;
            selection.characters = new GameObject[Prefabs.Length];
            selection.animators = new Animator[Prefabs.Length];
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
            var portraits = new Texture2D[Prefabs.Length];
            for (int i = 0; i < Prefabs.Length; i++)
            {
                var material = BuiltInMaterial(Prefabs[i], Keys[i]);
                var model = FindOrCreateModel(player.transform, Prefabs[i], "Character-" + Keys[i]);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
                var animator = model.GetComponentInChildren<Animator>(true);
                animator.runtimeAnimatorController = animatorController; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                selection.characters[i] = model; selection.animators[i] = animator;
                portraits[i] = Portrait(Prefabs[i], material, animatorController, Keys[i]);
            }
            view.animator = selection.animators[0]; view.flow = flow; view.controller = controller;
            BuildTitleChoice(flow.ui, selection, portraits);
            UpdateGuideText(flow.ui);
            selection.Select(0, false);
            player.PlaceCamera();
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save submission scene.");
            Debug.Log("EcoCity: Little Guys third-person player applied (boy/girl choice, proximity interaction). No Play Mode.");
        }

        private static PrototypePlayer CreateRoot(AlphaFlow flow, PrototypePlayer oldPlayer)
        {
            var root = new GameObject(PlayerName);
            root.transform.SetPositionAndRotation(oldPlayer.transform.position, oldPlayer.transform.rotation);
            root.AddComponent<CharacterController>();
            var player = root.AddComponent<PrototypePlayer>();
            player.lookSensitivity = oldPlayer.lookSensitivity; player.invertLookY = oldPlayer.invertLookY;
            player.inputBlocked = oldPlayer.inputBlocked; player.viewCamera = oldPlayer.viewCamera;
            var oldInteractor = oldPlayer.GetComponent<PlayerInteractor>();
            var interactor = root.AddComponent<PlayerInteractor>();
            interactor.game = oldInteractor.game; interactor.viewCamera = oldInteractor.viewCamera;
            interactor.mask = oldInteractor.mask; interactor.inputBlocked = oldInteractor.inputBlocked;
            var view = root.AddComponent<PlayerAnimationView>();
            // The old player is a prefab instance; unpack it so its camera can move out.
            if (PrefabUtility.IsPartOfPrefabInstance(oldPlayer.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(oldPlayer.gameObject),
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // The existing camera keeps its AudioListener and every AlphaSign reference.
            oldPlayer.viewCamera.transform.SetParent(root.transform, true);
            if (oldPlayer.viewCamera.transform.parent != root.transform) throw new IOException("Cannot move the player camera.");
            var map = new Dictionary<Object, Object>
            {
                { oldPlayer, player }, { oldInteractor, interactor }, { oldPlayer.gameObject, root },
                { oldPlayer.GetComponent<CharacterController>(), root.GetComponent<CharacterController>() },
            };
            var oldView = oldPlayer.GetComponent<PlayerAnimationView>();
            if (oldView != null) map[oldView] = view;
            Remap(map, oldPlayer.transform);
            Object.DestroyImmediate(oldPlayer.gameObject);
            return root.GetComponent<PrototypePlayer>();
        }
        // Rewrites references held by other components. Transforms are skipped: their parent/child links
        // must only change through SetParent, never through serialized fields.
        private static void Remap(Dictionary<Object, Object> map, Transform removed)
        {
            foreach (var sceneRoot in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in sceneRoot.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component is Transform || component.transform.IsChildOf(removed)) continue;
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    bool changed = false;
                    while (property.Next(true))
                    {
                        Object replacement;
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null &&
                            map.TryGetValue(property.objectReferenceValue, out replacement))
                        { property.objectReferenceValue = replacement; changed = true; }
                    }
                    if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
                }
        }
        private static GameObject FindOrCreateModel(Transform player, string prefabPath, string name)
        {
            var existing = player.Find(name);
            if (existing != null) return existing.gameObject;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player);
            model.name = name;
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one;
            return model;
        }
        // The sample materials target URP; this project uses the built-in pipeline, so copy them onto the bundled built-in shader.
        private static Material BuiltInMaterial(string prefabPath, string key)
        {
            string path = Root + "/M_LittleGuy_" + key + "_BuiltIn.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponentInChildren<Renderer>(true).sharedMaterial;
            material = new Material(source) { shader = AssetDatabase.LoadAssetAtPath<Shader>(BuiltInShader) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        /// <summary>Draws both character portraits again at the current picture detail, without touching the scene.</summary>
        public static void RedrawPortraits()
        {
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
            for (int i = 0; i < Prefabs.Length; i++) SubmissionKidUi.Picture(Portrait(Prefabs[i], BuiltInMaterial(Prefabs[i], Keys[i]), animatorController, Keys[i]));
        }
        internal static Texture2D Portrait(string prefabPath, Material material, RuntimeAnimatorController controller, string key)
        {
            string path = Root + "/Portrait_" + key + ".png";
            var preview = new PreviewRenderUtility();
            try
            {
                var model = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
                preview.AddSingleGO(model);
                var animator = model.GetComponentInChildren<Animator>(true);
                animator.runtimeAnimatorController = controller; animator.Rebind(); animator.Update(0.3f);
                var camera = preview.camera;
                camera.fieldOfView = 22; camera.nearClipPlane = 0.05f; camera.farClipPlane = 20;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.82f, 0.93f, 0.98f, 1);
                camera.transform.position = new Vector3(0, 0.78f, 2.4f);
                camera.transform.LookAt(new Vector3(0, 0.62f, 0));
                preview.lights[0].intensity = 1.2f; preview.lights[0].transform.rotation = Quaternion.Euler(35, 160, 0);
                preview.lights[1].intensity = 0.6f; preview.ambientColor = new Color(0.45f, 0.45f, 0.45f);
                preview.BeginStaticPreview(new Rect(0, 0, 320 * SubmissionKidUi.Detail, 320 * SubmissionKidUi.Detail));
                camera.Render();
                var texture = preview.EndStaticPreview();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            finally { preview.Cleanup(); }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false; importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static void BuildTitleChoice(AlphaUi ui, CharacterSelection selection, Texture2D[] portraits)
        {
            Transform menu = ui.newGame.transform.parent;
            var oldRow = menu.Find("캐릭터 고르기");
            if (oldRow != null) Object.DestroyImmediate(oldRow.gameObject);
            var row = new GameObject("캐릭터 고르기", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(menu, false); row.transform.SetSiblingIndex(ui.newGame.transform.GetSiblingIndex());
            var group = row.GetComponent<HorizontalLayoutGroup>(); group.spacing = 24; group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true; group.childControlHeight = true; group.childForceExpandWidth = true; group.childForceExpandHeight = true;
            row.GetComponent<LayoutElement>().preferredHeight = 150; row.GetComponent<LayoutElement>().minHeight = 130;
            selection.buttons = new Button[portraits.Length];
            selection.frames = new Image[portraits.Length];
            for (int i = 0; i < portraits.Length; i++)
            {
                var card = AlphaSceneBuilder.Panel("캐릭터 " + Labels[i], row.transform, Color.white, Vector2.zero, Vector2.one);
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = card.GetComponent<Image>();
                var picture = new GameObject("얼굴", typeof(RectTransform), typeof(RawImage));
                picture.transform.SetParent(card, false);
                var pictureRect = picture.GetComponent<RectTransform>();
                pictureRect.anchorMin = new Vector2(0.05f, 0.08f); pictureRect.anchorMax = new Vector2(0.48f, 0.92f); pictureRect.offsetMin = pictureRect.offsetMax = Vector2.zero;
                var raw = picture.GetComponent<RawImage>(); raw.texture = portraits[i]; raw.raycastTarget = false;
                var label = AlphaSceneBuilder.FillText(card, Labels[i], 30, new Color(0.09f, 0.22f, 0.24f));
                label.rectTransform.anchorMin = new Vector2(0.5f, 0); label.rectTransform.anchorMax = Vector2.one;
                Object.DestroyImmediate(label.GetComponent<LayoutElement>());
                selection.buttons[i] = button; selection.frames[i] = card.GetComponent<Image>();
            }
            // Keep the card inside the screen: settings and quit share one row.
            var lower = menu.Find("설정과 종료");
            if (lower == null)
            {
                var pair = new GameObject("설정과 종료", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                pair.transform.SetParent(menu, false); pair.transform.SetSiblingIndex(ui.quit.transform.GetSiblingIndex());
                var pairGroup = pair.GetComponent<HorizontalLayoutGroup>(); pairGroup.spacing = 12;
                pairGroup.childControlWidth = true; pairGroup.childControlHeight = true; pairGroup.childForceExpandWidth = true; pairGroup.childForceExpandHeight = true;
                pair.GetComponent<LayoutElement>().preferredHeight = 50;
                var settings = Object.FindFirstObjectByType<SubmissionSettings>();
                if (settings != null && settings.openTitle != null) settings.openTitle.transform.SetParent(pair.transform, false);
                ui.quit.transform.SetParent(pair.transform, false);
            }
            foreach (var text in menu.GetComponentsInChildren<Text>(true))
                if (text.text.StartsWith("연습품을 찾고")) text.text = "새싹 공원을 함께 가꿔요";
        }
        private static void UpdateGuideText(AlphaUi ui)
        {
            var replacements = new Dictionary<string, string>
            {
                { "WASD 이동 · 마우스 시점 · E 행동 · Esc 메뉴", "방향키로 걷기 · 가까이 가서 E · Esc 메뉴" },
                { "WASD 이동 · 마우스 시점 · E 행동", "방향키로 걷기 · 가까이 가서 E" },
                { "마우스: 주변 살펴보기", "마우스: 카메라 돌리기 (안 써도 괜찮아요)" },
                { "E: 조준한 물체 집기", "E: 가까이 있는 물체 집기" },
            };
            foreach (var text in ui.GetComponentsInChildren<Text>(true))
                foreach (var pair in replacements) if (text.text.Contains(pair.Key)) text.text = text.text.Replace(pair.Key, pair.Value);
            var crosshair = ui.transform.Find("Playing/조준점");
            if (crosshair != null) crosshair.gameObject.SetActive(false);
        }
    }
}
#endif
