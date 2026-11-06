#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using EcoCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoCity.Editor
{
    public static class PrototypeSceneBuilder
    {
        private static string root;
        [MenuItem("EcoCity/Create Prototype Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.GetType().Name != "UniversalRenderPipelineAsset")
            {
                EditorUtility.DisplayDialog("EcoCity", "이 생성기는 Built-in 또는 URP 프로젝트를 대상으로 합니다.", "확인");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string existing = "Assets/EcoCity/Generated/Submission/Prototype/EcoCityPrototype.unity";
            if (File.Exists(existing))
            {
                EditorSceneManager.OpenScene(existing, OpenSceneMode.Single);
                return;
            }
            Directory.CreateDirectory("Assets/EcoCity/Generated");
            AssetDatabase.Refresh();
            root = AssetDatabase.GenerateUniqueAssetPath("Assets/EcoCity/Generated/Submission/Prototype");
            Directory.CreateDirectory(root);
            AssetDatabase.Refresh();
            // Save prompt above protects edits; one scene avoids competing cameras/systems.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            GameObject sceneRoot = new GameObject("EcoCity Prototype");
            GameObject systems = Child("Systems", sceneRoot.transform);
            EcoGame game = systems.AddComponent<EcoGame>();
            game.saveSlot = "prototype-" + System.Guid.NewGuid().ToString("N");
            NpcFeedbackPresenter feedback = systems.AddComponent<NpcFeedbackPresenter>();
            feedback.game = game;

            Renderer ground = Shape("Ground", PrimitiveType.Cube, new Vector3(0, -0.25f, 0),
                new Vector3(28, 0.5f, 30), new Color(0.36f, 0.32f, 0.25f), sceneRoot.transform).GetComponent<Renderer>();
            Shape("WestWall", PrimitiveType.Cube, new Vector3(-14, 1, 0), new Vector3(0.3f, 2, 30), Color.gray, sceneRoot.transform);
            Shape("EastWall", PrimitiveType.Cube, new Vector3(14, 1, 0), new Vector3(0.3f, 2, 30), Color.gray, sceneRoot.transform);
            Shape("NorthWall", PrimitiveType.Cube, new Vector3(0, 1, 15), new Vector3(28, 2, 0.3f), Color.gray, sceneRoot.transform);
            Shape("SouthWall", PrimitiveType.Cube, new Vector3(0, 1, -15), new Vector3(28, 2, 0.3f), Color.gray, sceneRoot.transform);
            Light light = Child("Sun", sceneRoot.transform).AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            List<WasteItem> items = new List<WasteItem>();
            AddWaste(items, "paper-01", "깨끗한 종이 연습품", WasteCategory.Paper, Preparation.None, LearningLevel.Basic,
                new Vector3(-4, 0.45f, -3), "기본 분류에서는 재질과 같은 수거함을 찾아요.", sceneRoot.transform);
            AddWaste(items, "plastic-01", "깨끗한 플라스틱 연습품", WasteCategory.Plastic, Preparation.None, LearningLevel.Basic,
                new Vector3(0, 0.45f, -3), "기본 분류에서는 재질과 같은 수거함을 찾아요.", sceneRoot.transform);
            AddWaste(items, "metal-01", "깨끗한 캔 연습품", WasteCategory.Metal, Preparation.None, LearningLevel.Basic,
                new Vector3(4, 0.45f, -3), "기본 분류에서는 재질과 같은 수거함을 찾아요.", sceneRoot.transform);
            AddWaste(items, "container-01", "내용물이 남은 용기 연습품", WasteCategory.Plastic,
                Preparation.Empty | Preparation.Wash, LearningLevel.State, new Vector3(-3, 0.45f, 2),
                "이 가상 연습품은 내용물을 비우고 세척한 뒤 배출하도록 설정되어 있어요.", sceneRoot.transform);
            AddWaste(items, "cup-01", "분리할 부품이 있는 컵 연습품", WasteCategory.Plastic,
                Preparation.Empty | Preparation.Separate | Preparation.Wash, LearningLevel.State, new Vector3(3, 0.45f, 2),
                "이 연습의 부품은 분리대에서 따로 처리돼요. 실제 물품의 재질과 지역 기준은 별도로 확인해요.", sceneRoot.transform);
            AddWaste(items, "composite-01", "복합 재질 가상 연습품", WasteCategory.General,
                Preparation.Separate, LearningLevel.Composite, new Vector3(0, 0.45f, 5),
                "이 연습품만의 가상 정답이에요. 모든 복합 포장재가 같은 수거함으로 가는 것은 아니에요.", sceneRoot.transform);
            game.wasteItems = items.ToArray();

            WasteCategory[] categories = { WasteCategory.Paper, WasteCategory.Plastic, WasteCategory.Metal,
                WasteCategory.Glass, WasteCategory.Vinyl, WasteCategory.Food, WasteCategory.General };
            for (int i = 0; i < categories.Length; i++)
            {
                WasteCategory category = categories[i];
                GameObject bin = Shape("Bin-" + category, PrimitiveType.Cube,
                    new Vector3(-9 + i * 3, 0.75f, 10), new Vector3(1.7f, 1.5f, 1.5f), CategoryColor(category), sceneRoot.transform);
                bin.AddComponent<DisposalBin>().category = category;
                Label(bin.transform, category.ToString().ToUpperInvariant(), 1.2f);
            }
            Preparation[] steps = { Preparation.Empty, Preparation.Separate, Preparation.Wash };
            for (int i = 0; i < steps.Length; i++)
            {
                GameObject station = Shape("Station-" + steps[i], PrimitiveType.Cube,
                    new Vector3(-10, 0.65f, -3 + i * 4), new Vector3(1.8f, 1.3f, 1.8f), new Color(0.25f, 0.65f, 0.75f), sceneRoot.transform);
                station.AddComponent<PreparationStation>().step = steps[i];
                Label(station.transform, steps[i].ToString().ToUpperInvariant(), 1.2f);
            }
            GameObject npc = Shape("Guide NPC", PrimitiveType.Capsule, new Vector3(8, 1, -4),
                Vector3.one, new Color(0.95f, 0.75f, 0.2f), sceneRoot.transform);
            npc.AddComponent<GuideNpc>();
            Label(npc.transform, "GUIDE", 1.4f);

            List<Renderer> greenery = new List<Renderer>();
            for (int i = 0; i < 5; i++)
                greenery.Add(Shape("Tree-" + i, PrimitiveType.Sphere, new Vector3(-10 + i * 5, 2, 13),
                    new Vector3(2, 3, 2), Color.green, sceneRoot.transform).GetComponent<Renderer>());
            CityEnvironmentView city = systems.AddComponent<CityEnvironmentView>();
            city.game = game; city.ground = ground; city.greenery = greenery.ToArray();

            GameObject playerObject = Child("Player", sceneRoot.transform);
            playerObject.transform.position = new Vector3(0, 0.1f, -9);
            CharacterController character = playerObject.AddComponent<CharacterController>();
            character.height = 1.8f; character.radius = 0.3f; character.center = new Vector3(0, 0.9f, 0);
            GameObject cameraObject = Child("Player Camera", playerObject.transform);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = new Vector3(0, 1.55f, 0);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 100; camera.fieldOfView = 65;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.55f, 0.76f, 0.9f);
            PrototypePlayer player = playerObject.AddComponent<PrototypePlayer>(); player.viewCamera = camera;
            PlayerInteractor interactor = playerObject.AddComponent<PlayerInteractor>(); interactor.game = game; interactor.viewCamera = camera;
            PrototypeHud hud = systems.AddComponent<PrototypeHud>();
            hud.game = game; hud.player = player; hud.interactor = interactor; hud.feedback = feedback;

            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, root + "/EcoCityPrototype.unity"))
                throw new IOException("Could not save the prototype scene.");
            Selection.activeGameObject = sceneRoot;
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("EcoCity", "테스트 씬을 단독으로 열었습니다.\n\n" + root +
                "/EcoCityPrototype.unity\n\nPlay는 자동 실행하지 않습니다.", "확인");
        }

        private static GameObject Child(string name, Transform parent)
        { GameObject obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
        private static GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name; obj.transform.SetParent(parent, false); obj.transform.position = position; obj.transform.localScale = scale;
            Shader shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("URP/Lit or Standard shader is required.");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(root + "/" + name.Replace(' ', '_') + ".mat"));
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }
        private static void AddWaste(List<WasteItem> items, string id, string name, WasteCategory category,
            Preparation preparation, LearningLevel level, Vector3 position, string reason, Transform parent)
        {
            WasteDefinition definition = ScriptableObject.CreateInstance<WasteDefinition>();
            definition.displayName = name; definition.targetBin = category;
            definition.requiredPreparation = preparation; definition.level = level; definition.educationalReason = reason;
            AssetDatabase.CreateAsset(definition, root + "/" + id + ".asset");
            GameObject obj = Shape("Waste-" + id, PrimitiveType.Sphere, position, Vector3.one * 0.7f, CategoryColor(category), parent);
            WasteItem item = obj.AddComponent<WasteItem>(); item.instanceId = id; item.definition = definition; items.Add(item);
            Label(obj.transform, "L" + ((int)level + 1) + " " + id, 1.4f);
        }
        private static void Label(Transform parent, string text, float localHeight)
        {
            GameObject obj = Child("Label", parent);
            obj.transform.localPosition = new Vector3(0, localHeight, 0);
            // Counteract parent scale so every world label has the same size.
            Vector3 s = parent.lossyScale;
            obj.transform.localScale = new Vector3(1 / s.x, 1 / s.y, 1 / s.z);
            TextMesh label = obj.AddComponent<TextMesh>();
            label.text = text; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.characterSize = 0.10f; label.fontSize = 48; label.color = Color.black;
        }
        private static Color CategoryColor(WasteCategory category)
        {
            switch (category)
            {
                case WasteCategory.Paper: return new Color(0.8f, 0.65f, 0.35f);
                case WasteCategory.Plastic: return new Color(0.3f, 0.6f, 0.95f);
                case WasteCategory.Metal: return new Color(0.75f, 0.75f, 0.8f);
                case WasteCategory.Glass: return new Color(0.2f, 0.75f, 0.6f);
                case WasteCategory.Vinyl: return new Color(0.8f, 0.45f, 0.8f);
                case WasteCategory.Food: return new Color(0.9f, 0.5f, 0.25f);
                default: return new Color(0.45f, 0.45f, 0.45f);
            }
        }
    }
}
#endif
