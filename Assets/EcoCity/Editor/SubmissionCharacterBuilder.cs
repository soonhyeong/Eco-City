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
    // Character choice scene shown after "시작하기": the two Little Guys idle on round pastel stages in 3D,
    // with name cards under them and one big start button. Also renders transparent full-body cut-outs for the title.
    public static class SubmissionCharacterBuilder
    {
        public const string ScenePath = "Assets/Scenes/EcoCityCharacter.unity";
        private const string Characters = "Assets/EcoCity/Generated/Submission/Characters/";
        private const string Sample = "Assets/LuceedStudio/Character Lab/Little Guys/Little Guys - Free Sample/";
        private const string Controller = "Assets/Player/Common/DEMO/Animator/StarterAssetsThirdPerson.controller";
        private const string Toon = "Assets/Loading Games/Toon City Pack/Prefabs/";
        private static readonly string[] Prefabs = { Sample + "Man/Free Man/Free Man.prefab", Sample + "Woman/Free Woman/Free Woman.prefab" };
        private static readonly string[] Keys = { "Boy", "Girl" };
        private static readonly string[] Names = { "남자아이", "여자아이" };

        [MenuItem("EcoCity/Submission/Build Character Select Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
            for (int i = 0; i < 2; i++) CutOut(i, controller);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Soft pastel stage.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.86f, 0.84f, 0.98f); RenderSettings.ambientEquatorColor = new Color(0.8f, 0.76f, 0.86f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.5f, 0.6f);
            var cameraObject = new GameObject("Character Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.9f, 0.88f, 1f);
            camera.fieldOfView = 30; camera.transform.position = CameraSpot; camera.transform.LookAt(CameraLook);
            camera.allowMSAA = true;
            var lightObject = new GameObject("Sun", typeof(Light)); var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.05f; light.color = new Color(1f, 0.97f, 0.92f); light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(42, -28, 0);
            var floor = Part("바닥", PrimitiveType.Cylinder, new Vector3(0, -0.05f, 1.2f), new Vector3(14, 0.05f, 9), FloorColor);
            Backdrop(camera);
            foreach (var spot in new[] { new Vector3(-4.2f, 0, 4.4f), new Vector3(4.3f, 0, 4.2f), new Vector3(-3.4f, 0, 2.6f), new Vector3(3.5f, 0, 2.8f) })
                Spawn(Toon + "Vegetation/" + (spot.z > 4 ? "tree-type-B" : "bush-type-A") + ".prefab", spot, spot.x * 20, spot.z > 4 ? 0.55f : 0.8f);

            var picker = new GameObject("Character Picker").AddComponent<CharacterPicker>();
            picker.view = camera;
            picker.models = new Transform[2]; picker.animators = new Animator[2]; picker.rings = new Renderer[2];
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.95f : 0.95f;
                Part("무대 " + (i + 1), PrimitiveType.Cylinder, new Vector3(x, 0.07f, 0), new Vector3(1.25f, 0.07f, 1.25f), Color.white);
                var ring = Part("빛 고리 " + (i + 1), PrimitiveType.Cylinder, new Vector3(x, 0.02f, 0), new Vector3(1.5f, 0.03f, 1.5f), new Color(0.9f, 0.88f, 0.96f));
                picker.rings[i] = ring.GetComponent<Renderer>();
                var model = Character(i, controller);
                model.transform.position = new Vector3(x, 0.14f, 0);
                model.transform.rotation = Quaternion.Euler(0, 180 + (i == 0 ? 22 : -22), 0);
                var collider = model.AddComponent<CapsuleCollider>(); collider.center = new Vector3(0, 0.6f, 0); collider.height = 1.2f; collider.radius = 0.35f;
                picker.models[i] = model.transform; picker.animators[i] = model.GetComponentInChildren<Animator>();
            }
            Object.DestroyImmediate(floor.GetComponent<Collider>());

            var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UseUiActions(events.GetComponent<InputSystemUIInputModule>());
            var soundObject = new GameObject("Sound", typeof(AudioSource)); var sound = soundObject.GetComponent<AudioSource>(); sound.playOnAwake = false;
            picker.sound = sound;
            picker.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoCity/Generated/Submission/KidWorld/Sound/sfx/pop.wav");
            picker.welcomeClip = KidVoice.Enabled ? AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Voice/char_pick.wav") : null;
            SubmissionKidUi.MenuMusic(picker.transform.root);

            var canvasObject = new GameObject("Character UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var root = canvasObject.transform;
            var title = Node("제목", root, new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(720, 104));
            DropShadow(title, 16, 10, 0.3f);
            Paint(title, Darker(Lavender, 0.8f), 52); var titleFace = Full("면", title, 0, 8, 0, 0); Paint(titleFace, Lavender, 52);
            FillText("글자", titleFace, "누구랑 같이 할까요?", 52, Color.white, SoftMaterial, 20, 0, 20, 0);
            Text("설명", root, "친구를 눌러서 골라요  ·  ← → 키로도 돼요", 28, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -164), new Vector2(900, 44));
            picker.cards = new Button[2]; picker.cardFaces = new Image[2]; picker.checks = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                var card = Candy("이름 " + Names[i], root, Names[i], Color.white, new Vector2(0.5f, 0), new Vector2(i == 0 ? -420 : 420, 214), new Vector2(320, 100), 42, Ink);
                picker.cards[i] = card; picker.cardFaces[i] = card.transform.Find("면").GetComponent<Image>();
                var check = Node("고름 표시", card.transform, new Vector2(1, 1), new Vector2(18, 18), new Vector2(56, 56));
                Circle(check, Green); SubmissionKidUi.Sprite(Node("체크", check, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40)), CheckSprite, Color.white);
                picker.checks[i] = check.gameObject;
            }
            picker.cardOn = new Color(0.86f, 0.82f, 1f); picker.cardOff = Color.white;
            picker.ringOn = Lavender; picker.ringOff = new Color(0.9f, 0.88f, 0.96f);
            picker.startButton = Candy("시작", root, "이 친구랑 시작!", Lavender, new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(580, 120), 54);
            picker.backButton = Candy("뒤로", root, "뒤로", Gray, new Vector2(0, 1), new Vector2(Margin, -Margin), new Vector2(160, 72), 32);
            BakeCharacters(canvasObject);
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save character scene.");
            Debug.Log("EcoCity: character select scene built at " + ScenePath + ". No Play Mode.");
        }

        // Closer than before so the two friends fill more of the screen (the name cards still sit under their feet).
        public static readonly Vector3 CameraSpot = new Vector3(0, 1.2f, -5.3f), CameraLook = new Vector3(0, 0.6f, 0);
        // Lawn that joins the painted hills (lighter shades wash out to white under the stage light).
        public static readonly Color FloorColor = new Color(0.56f, 0.82f, 0.6f);
        public const string BackdropPath = "Assets/EcoCity/Art/Backdrop/CharacterBackdrop.png";
        /// <summary>Painted sky, rainbow and hills (Validation/GenerateKidBackdrop.py) on a board that fills the view behind the stage.</summary>
        public static GameObject Backdrop(Camera camera)
        {
            var old = GameObject.Find("뒤 벽"); if (old != null) Object.DestroyImmediate(old);
            old = GameObject.Find("배경 그림"); if (old != null) Object.DestroyImmediate(old);
            var texture = Picture(AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropPath), true);
            string path = Root + "/Materials/CharacterBackdrop.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Unlit/Texture")); AssetDatabase.CreateAsset(material, path); }
            material.mainTexture = texture; EditorUtility.SetDirty(material);
            // Size the board to what the camera sees at its distance (16:10 and wider screens), from just under the floor up.
            const float z = 5.4f;
            float distance = z - camera.transform.position.z;
            float halfHeight = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float centre = camera.transform.position.y + camera.transform.forward.y / camera.transform.forward.z * distance;
            float top = centre + halfHeight * 1.08f, bottom = -0.2f;
            float height = top - bottom, width = Mathf.Max(height * texture.width / texture.height, halfHeight * 2 * 2.1f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Quad); board.name = "배경 그림";
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.transform.position = new Vector3(0, (top + bottom) / 2, z); board.transform.localScale = new Vector3(width, height, 1);
            var renderer = board.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            return board;
        }
        private static GameObject Character(int index, RuntimeAnimatorController controller)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs[index]));
            model.name = "친구 " + Names[index];
            var material = AssetDatabase.LoadAssetAtPath<Material>(Characters + "M_LittleGuy_" + Keys[index] + "_BuiltIn.mat");
            if (material == null) throw new FileNotFoundException("Run 'Apply Little Guys Third-Person Player' first.");
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
            var animator = model.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (animator.GetComponent<AnimationEventSink>() == null) animator.gameObject.AddComponent<AnimationEventSink>();
            return model;
        }
        /// <summary>Transparent full-body picture of a friend (idle pose), used by the title screen.</summary>
        public static Texture2D CutOut(int index, RuntimeAnimatorController controller)
        {
            string path = Characters + "CutOut_" + Keys[index] + ".png";
            var model = Character(index, controller);
            var cameraObject = new GameObject("cut-out camera"); cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var lightObject = new GameObject("cut-out light"); lightObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                model.transform.position = new Vector3(0, -50, 0); model.transform.rotation = Quaternion.Euler(0, 180 + (index == 0 ? 18 : -18), 0);
                var animator = model.GetComponentInChildren<Animator>();
                animator.Rebind(); animator.SetBool("Grounded", true); animator.Update(0.3f);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f; lightObject.transform.rotation = Quaternion.Euler(35, -20, 0);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0, 0, 0, 0);
                camera.fieldOfView = 24; camera.transform.position = new Vector3(0, -50 + 0.62f, -3.6f); camera.transform.LookAt(new Vector3(0, -50 + 0.6f, 0));
                int width = 560 * Detail, height = 640 * Detail;
                var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                camera.targetTexture = target; camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var picture = new Texture2D(width, height, TextureFormat.RGBA32, false); picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply();
                RenderTexture.active = previous; camera.targetTexture = null; Object.DestroyImmediate(target);
                File.WriteAllBytes(path, picture.EncodeToPNG()); Object.DestroyImmediate(picture);
            }
            finally { Object.DestroyImmediate(model); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject); }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096; importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static GameObject Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type); part.name = name;
            part.transform.position = position; part.transform.localScale = scale;
            string path = Root + "/Materials/Stage_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); material.SetFloat("_Glossiness", 0.15f); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }
        // Pack props go through the makeover's spawner, which converts their materials to the built-in pipeline.
        private static void Spawn(string path, Vector3 position, float yaw, float scale) { SubmissionKidMakeover.Spawn(path, null, position, yaw, scale); }
    }
}
#endif
