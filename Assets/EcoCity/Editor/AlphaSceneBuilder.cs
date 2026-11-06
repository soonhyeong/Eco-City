#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using EcoCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EcoCity.Editor
{
    public static class AlphaSceneBuilder
    {
        public const string ScenePath = "Assets/EcoCity/Generated/Submission/Alpha/EcoCityAlpha.unity";
        private const string Root = "Assets/EcoCity/Generated/Submission/Alpha";
        private static Font font;
        private static Camera camera;
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private static readonly Color Ink = new Color(0.09f, 0.22f, 0.24f);
        private static readonly Color Teal = new Color(0.09f, 0.43f, 0.39f);
        private static readonly Color Cream = new Color(0.98f, 0.97f, 0.90f);

        [MenuItem("EcoCity/Create or Open Alpha Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/EcoCity/Fonts/NanumGothic-Regular.ttf");
            if (font == null) throw new InvalidOperationException("Bundled NanumGothic font is required.");
            Directory.CreateDirectory(Root); AssetDatabase.Refresh(); materials.Clear();
            var scene = EditorSceneManager.OpenScene("Assets/EcoCity/Generated/Submission/Prototype/EcoCityPrototype.unity");
            // Save a separate scene before editing. Shared content keeps its original GUIDs.
            if (!EditorSceneManager.SaveScene(scene, ScenePath, false)) throw new IOException("Cannot create alpha scene.");
            var game = UnityEngine.Object.FindFirstObjectByType<EcoGame>();
            var hud = UnityEngine.Object.FindFirstObjectByType<PrototypeHud>();
            var player = hud.player;
            var interactor = hud.interactor;
            var feedback = hud.feedback;
            camera = player.viewCamera;
            UnityEngine.Object.DestroyImmediate(hud);
            game.saveSlot = "eco-city-alpha-v1"; game.waitForStart = true;
            Transform world = game.transform.parent;
            world.name = "에코시티 · 새싹 공원";
            // The user's prototype Plane remains in the prototype scene, not the alpha copy.
            foreach (var rootObject in scene.GetRootGameObjects())
                if (rootObject.name == "Plane") UnityEngine.Object.DestroyImmediate(rootObject);
            player.ResetPose(new Vector3(0, 0.05f, -10), Quaternion.identity);
            var spawn = Child("플레이어 시작점", world);
            spawn.position = player.transform.position;
            var flow = game.gameObject.AddComponent<AlphaFlow>();
            flow.game = game; flow.player = player; flow.interactor = interactor; flow.spawn = spawn;
            player.inputBlocked = true; interactor.inputBlocked = true;
            MakeMap(world, game);
            BuildUi(flow, feedback);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save alpha scene.");
            Debug.Log("EcoCity alpha scene created: " + ScenePath);
        }

        private static void MakeMap(Transform world, EcoGame game)
        {
            Shape("중앙 산책길", PrimitiveType.Cube, new Vector3(0, 0.008f, 0), new Vector3(3.5f, 0.012f, 24), Cream, world);
            Shape("준비 구역 길", PrimitiveType.Cube, new Vector3(-5, 0.008f, 0), new Vector3(10, 0.012f, 2.2f), Cream, world);
            Shape("분리배출 구역 길", PrimitiveType.Cube, new Vector3(0, 0.009f, 7), new Vector3(24, 0.012f, 3), Cream, world);
            Sign(world, new Vector3(0, 2.5f, -8), "새싹 공원\n앞으로: 연습품 찾기   왼쪽: 준비 구역   끝쪽: 분리배출", 0.07f);
            Sign(world, new Vector3(-9, 2.5f, 0), "준비 구역\n필요한 과정만 차례대로 해요", 0.09f);
            Sign(world, new Vector3(0, 3, 10), "분리배출 구역\n색보다 이름과 재질을 함께 살펴요", 0.09f);
            Sign(world, new Vector3(6, 2.6f, 1), "탐색 구역\n앞 단계를 마치면 다음 연습품을 집을 수 있어요", 0.075f);
            Vector3[] positions = {
                new Vector3(-3, 0.13f, -5), new Vector3(0, 0.55f, -5), new Vector3(3, 0.4f, -5),
                new Vector3(-3, 0.6f, 0), new Vector3(3, 0.55f, 0), new Vector3(5, 0.5f, 3)
            };
            string[] states = {
                "마르고 깨끗한 종이예요. 별도 처리는 없어요.", "내용물이 없는 깨끗한 플라스틱이에요.", "내용물이 없는 깨끗한 금속 캔이에요.",
                "내용물이 남았어요. 비운 뒤 씻어야 하는 가상 용기예요.", "내용물과 다른 재질 부품이 있어요. 비우고 분리한 뒤 씻어요.",
                "여러 재질이 섞인 가상 연습품이에요. 이 연습에서만 분리 후 일반으로 배출해요."
            };
            for (int i = 0; i < game.wasteItems.Length; i++)
            {
                var item = game.wasteItems[i]; var definition = item.definition;
                item.transform.position = positions[i];
                RemoveLabels(item.transform);
                if (string.IsNullOrEmpty(definition.stateDescription)) definition.stateDescription = states[i];
                if (string.IsNullOrEmpty(definition.sourceUrl))
                {
                    definition.fictionalPractice = true;
                    definition.sourceUrl = "https://www.korea.kr/briefing/policyBriefingView.do?newsId=148852389";
                    definition.sourceCheckedDate = "2026-10-21";
                    definition.sourceNote = "가상 연습 규칙. 대한민국 정책브리핑 2018-07-24 '내 손안에 쓰레기 참고서 있다'의 분리배출 일반 원칙만 참고. 품목별 정답·색상·현재 지역 조건을 검증한 근거가 아님. 대상 지역 미확정.";
                    EditorUtility.SetDirty(definition);
                }
                PrimitiveType type = i == 0 || i == 5 ? PrimitiveType.Cube : PrimitiveType.Cylinder;
                var sample = GameObject.CreatePrimitive(type);
                item.GetComponent<MeshFilter>().sharedMesh = sample.GetComponent<MeshFilter>().sharedMesh;
                UnityEngine.Object.DestroyImmediate(sample);
                foreach (var c in item.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                item.gameObject.AddComponent<BoxCollider>();
                item.transform.localScale = i == 0 ? new Vector3(0.9f, 0.13f, 0.7f) : i == 5 ? Vector3.one * 0.75f : new Vector3(0.65f, 0.45f, 0.65f);
                Sign(item.transform, item.transform.position + Vector3.up * 1.15f,
                    ((int)definition.level + 1) + "단계 · " + definition.displayName, 0.065f);
            }
            foreach (var bin in UnityEngine.Object.FindObjectsByType<DisposalBin>(FindObjectsSortMode.None))
            {
                bin.transform.position = new Vector3(-9 + (int)bin.category * 3, 0.75f, 9);
                RemoveLabels(bin.transform);
                Sign(bin.transform, bin.transform.position + Vector3.up * 1.4f,
                    ((int)bin.category + 1) + "  " + FeedbackText.Category(bin.category) + "\n수거함", 0.10f);
                Shape("수거함 입구", PrimitiveType.Cube, bin.transform.position + Vector3.up * 0.78f,
                    new Vector3(1.1f, 0.05f, 0.55f), Ink, world);
            }
            var steps = new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash };
            foreach (var station in UnityEngine.Object.FindObjectsByType<PreparationStation>(FindObjectsSortMode.None))
            {
                int index = Array.IndexOf(steps, station.step);
                station.transform.position = new Vector3(-9, 0.65f, -4 + index * 4);
                RemoveLabels(station.transform);
                Sign(station.transform, station.transform.position + Vector3.up * 1.5f,
                    (index + 1) + "  " + FeedbackText.Step(station.step), 0.1f);
                Shape("준비 도구 " + index, index == 0 ? PrimitiveType.Cylinder : index == 1 ? PrimitiveType.Cube : PrimitiveType.Sphere,
                    station.transform.position + Vector3.up * 0.95f, Vector3.one * 0.4f, Cream, world);
            }
            var npc = UnityEngine.Object.FindFirstObjectByType<GuideNpc>();
            npc.transform.position = new Vector3(7, 1, -6); RemoveLabels(npc.transform);
            Sign(npc.transform, npc.transform.position + Vector3.up * 1.5f, "?  환경 도우미\nE로 물어보세요", 0.10f);
            Shape("도우미 모자", PrimitiveType.Cylinder, new Vector3(7, 2, -6), new Vector3(1, 0.1f, 1), new Color(1, 0.78f, 0.2f), world);
            for (int i = 0; i < 3; i++)
            {
                Shape("벤치 좌석 " + i, PrimitiveType.Cube, new Vector3(10, 0.5f, -2 + i * 4), new Vector3(1.4f, 0.2f, 2), new Color(0.55f, 0.36f, 0.21f), world);
                Shape("벤치 등받이 " + i, PrimitiveType.Cube, new Vector3(10.65f, 1, -2 + i * 4), new Vector3(0.15f, 1, 2), new Color(0.55f, 0.36f, 0.21f), world);
            }
            var city = game.gameObject.AddComponent<AlphaCityView>(); city.game = game; city.restoredGardens = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                Transform garden = Child("회복 화단 " + (i + 1), world);
                for (int j = 0; j < 5; j++)
                {
                    Vector3 position = new Vector3(-6 + i * 6 + (j % 3) * 0.45f, 0.4f, 12 + (j / 3) * 0.5f);
                    Shape("꽃줄기", PrimitiveType.Cylinder, position, new Vector3(0.08f, 0.4f, 0.08f), Teal, garden);
                    Shape("꽃", PrimitiveType.Sphere, position + Vector3.up * 0.5f, Vector3.one * 0.4f, i == 1 ? Color.yellow : new Color(1, 0.6f, 0.6f), garden);
                }
                city.restoredGardens[i] = garden.gameObject; garden.gameObject.SetActive(false);
            }
        }

        private static void BuildUi(AlphaFlow flow, NpcFeedbackPresenter feedback)
        {
            var canvasObject = new GameObject("알파 UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = 0.5f;
            var ui = canvasObject.AddComponent<AlphaUi>(); ui.flow = flow; ui.feedback = feedback; flow.ui = ui;
            ui.screens = new GameObject[6];
            for (int i = 0; i < 6; i++)
                ui.screens[i] = Panel(((AlphaScreen)i).ToString(), canvasObject.transform,
                    i == 1 ? new Color(0, 0, 0, 0) : new Color(0.04f, 0.13f, 0.15f, 0.94f), Vector2.zero, Vector2.one).gameObject;
            var title = Menu(ui.screens[0].transform, "에코시티 구조대", "새싹 공원을 함께 가꿔요\n연습품을 찾고, 상태를 살피고, 알맞게 배출해 보세요.");
            ui.newGame = Button(title, "새 게임"); ui.continueGame = Button(title, "이어하기");
            ui.titleHelp = Button(title, "조작과 연습 규칙"); ui.quit = Button(title, "게임 종료");
            Label(title, "WASD 이동 · 마우스 시점 · E 행동 · Esc 메뉴\n알파 연습판 | 실제 배출은 물품 표시와 지역 기준을 확인해요", 18, 75);
            Transform play = ui.screens[1].transform;
            var mission = Card(play, "오늘의 미션", new Vector2(0.02f, 0.78f), new Vector2(0.43f, 0.98f));
            ui.missionText = Label(mission, "", 24, 65); ui.cityText = Label(mission, "", 18, 40);
            var held = Card(play, "손에 든 연습품", new Vector2(0.70f, 0.65f), new Vector2(0.98f, 0.98f));
            ui.heldText = Label(held, "", 20, 170);
            var tutorial = Card(play, "함께 해 봐요", new Vector2(0.02f, 0.43f), new Vector2(0.34f, 0.73f));
            ui.tutorialText = Label(tutorial, "", 21, 135); Label(tutorial, "H 도움말 / 건너뛰기", 16, 25);
            var response = Card(play, "", new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.25f));
            ui.feedbackText = Label(response, "", 21, 125);
            var prompt = Panel("행동 안내", play, Cream, new Vector2(0.28f, 0.29f), new Vector2(0.72f, 0.37f));
            ui.targetText = FillText(prompt, "", 21, Ink);
            var cross = Panel("조준점", play, new Color(0,0,0,0), new Vector2(0.485f, 0.48f), new Vector2(0.515f, 0.52f));
            FillText(cross, "+", 28, Ink);
            var pauseArea = Panel("메뉴 열기", play, new Color(0,0,0,0), new Vector2(0.46f,0.92f), new Vector2(0.65f,0.98f));
            ui.pause = Button(pauseArea, "Esc  메뉴"); Stretch(ui.pause.GetComponent<RectTransform>());
            var paused = Menu(ui.screens[2].transform, "잠깐 쉬어 가요", "WASD 이동 · 마우스 시점 · E 행동\nH 도움말 · Esc 메뉴 열기/닫기");
            ui.resume = Button(paused, "계속하기"); ui.pauseHelp = Button(paused, "조작과 연습 규칙");
            ui.askGuide = Button(paused, "도우미에게 물어보기"); ui.save = Button(paused, "진행 저장"); ui.toTitle = Button(paused, "타이틀로 이동");
            ui.menuGuideText = Label(paused, "", 20, 90);
            var help = Menu(ui.screens[3].transform, "공원 탐험 안내", "메뉴에서는 이동과 행동이 잠시 멈춰요.");
            ScrollText(help, "WASD / 방향키: 이동\n마우스: 주변 살펴보기\nE: 조준한 물체 집기 · 처리 · 배출 · NPC 도움\nEsc: 메뉴 · H: 이 안내 다시 보기\n\n1. 앞쪽 연습품을 주워 상태를 확인해요.\n2. 필요한 경우 왼쪽 준비 구역을 이용해요.\n3. 끝쪽 수거함 이름을 보고 배출해요.\n4. 기본 3개 → 상태 2개 → 복합 1개 순서예요.\n5. 도우미 또는 메뉴에서 도움을 요청해요.\n\n색상은 게임 속 구분이에요. 공식 수거함 색 기준이 아니에요.\n이 공원의 6개 연습품은 가상 규칙이에요. 실제 물건은 재질 표시와 지역 안내를 확인해요.\n복합 재질이 모두 일반 쓰레기는 아니며, 세척만으로 모든 오염품을 재활용할 수는 없어요.\n\n정답률: 성공한 배출 / 전체 배출 시도\n모의 깨끗함: 연습품을 배출한 비율\n모의 오염도: 남은 물체와 오답 이력을 반영한 게임 지표\n실제 환경 통계와는 달라요.\n\n글꼴: 나눔고딕 · NAVER · SIL Open Font License 1.1", 230);
            ui.skipTutorial = Button(help, "행동 안내 건너뛰기 (미션은 그대로)"); ui.helpBack = Button(help, "돌아가기");
            var results = Menu(ui.screens[4].transform, "우리 공원 연습 완료!", "스스로 판단하고 행동했어요.");
            ui.resultText = ScrollText(results, "", 340);
            ui.practiceAgain = Button(results, "다시 연습하기"); ui.resultTitle = Button(results, "타이틀로 이동");
            var confirm = Menu(ui.screens[5].transform, "새로 시작할까요?", "저장된 연습 진행과 행동 안내를 처음부터 시작해요.\n지금 진행을 이어가려면 돌아가기를 눌러 주세요.");
            ui.confirmYes = Button(confirm, "처음부터 시작"); ui.confirmNo = Button(confirm, "돌아가기");
            var notice = Panel("저장 안내", canvasObject.transform, new Color(0.04f,0.13f,0.15f,0.9f), Vector2.zero, new Vector2(1,0.04f));
            ui.noticeText = FillText(notice, "", 16, Color.white);
            var celebration = Panel("단계 완료 안내", canvasObject.transform, new Color(0,0,0,0), new Vector2(0.25f,0.82f), new Vector2(0.75f,0.91f));
            ui.celebrationText = FillText(celebration, "", 24, new Color(1,0.9f,0.35f));
            for (int i = 0; i < 6; i++) ui.screens[i].SetActive(i == 0);
            new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>().AssignDefaultActions();
        }

        internal static void UseFont(Font selected) { font = selected; }
        internal static Transform Menu(Transform parent, string title, string description)
        {
            var card = Panel(title, parent, Cream, new Vector2(0.19f,0.06f), new Vector2(0.81f,0.96f));
            Layout(card, 24, 12);
            Label(card, title, 36, 55); Label(card, description, 21, 70);
            return card;
        }
        private static Transform Card(Transform parent, string title, Vector2 min, Vector2 max)
        {
            var card = Panel(title, parent, new Color(0.98f,0.97f,0.90f,0.94f), min, max);
            Layout(card, 14, 5);
            if (!string.IsNullOrEmpty(title)) Label(card, title, 18, 24);
            return card;
        }
        internal static void Layout(Transform root, int padding, int spacing)
        {
            var group = root.gameObject.AddComponent<VerticalLayoutGroup>(); group.padding = new RectOffset(padding,padding,padding,padding);
            group.spacing = spacing; group.childControlWidth = true; group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = false;
        }
        internal static Text Label(Transform parent, string text, int size, float height)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement)); obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<Text>(); label.font = font; label.fontSize = size; label.color = Ink; label.text = text;
            label.supportRichText = false; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate; label.alignment = TextAnchor.MiddleLeft;
            var element = obj.GetComponent<LayoutElement>(); element.preferredHeight = height; element.minHeight = height * 0.7f;
            return label;
        }
        internal static Text FillText(Transform parent, string text, int size, Color color)
        {
            var label = Label(parent, text, size, 30); label.color = color; label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform); label.rectTransform.offsetMin = new Vector2(8,4); label.rectTransform.offsetMax = new Vector2(-8,-4);
            return label;
        }
        internal static Button Button(Transform parent, string title)
        {
            var root = Panel(title, parent, Teal, Vector2.zero, Vector2.one);
            var layout = root.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = 50; layout.minHeight = 40;
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = root.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(0.75f,1,0.85f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
            FillText(root, title, 23, Color.white); return button;
        }
        internal static Text ScrollText(Transform parent, string text, float height)
        {
            var root = Panel("스크롤 안내", parent, new Color(0.9f,0.94f,0.9f), Vector2.zero, Vector2.one);
            var size = root.gameObject.AddComponent<LayoutElement>(); size.preferredHeight = height; size.minHeight = 120;
            var scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = Panel("Viewport", root, new Color(0,0,0,0), Vector2.zero, Vector2.one); viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter)); content.transform.SetParent(viewport, false);
            var rect = content.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0,1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f,1);
            rect.offsetMin = new Vector2(14,0); rect.offsetMax = new Vector2(-14,0);
            var label = content.GetComponent<Text>(); label.font = font; label.fontSize = 21; label.color = Ink; label.text = text;
            label.supportRichText = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = rect; scroll.movementType = ScrollRect.MovementType.Clamped;
            return label;
        }
        internal static RectTransform Panel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = color.a > 0;
            return rect;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static Transform Child(string name, Transform parent)
        { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj.transform; }
        private static void RemoveLabels(Transform parent)
        { foreach (var label in parent.GetComponentsInChildren<TextMesh>(true)) UnityEngine.Object.DestroyImmediate(label.gameObject); }
        private static GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false);
            obj.transform.position = position; obj.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            string key = ColorUtility.ToHtmlStringRGBA(color);
            if (!materials.TryGetValue(key, out var material))
            {
                material = new Material(Shader.Find("Standard")) { color = color };
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Root + "/Park-" + key + ".mat")); materials.Add(key, material);
            }
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        private static void Sign(Transform parent, Vector3 position, string text, float size)
        {
            var root = Child("한글 안내", parent); root.position = position;
            Vector3 scale = parent.lossyScale; root.localScale = new Vector3(1/scale.x,1/scale.y,1/scale.z);
            var label = root.gameObject.AddComponent<TextMesh>(); label.text = text; label.font = font; label.fontSize = 64;
            label.characterSize = size; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = Ink;
            root.GetComponent<Renderer>().sharedMaterial = font.material;
            root.gameObject.AddComponent<AlphaSign>().viewCamera = camera;
        }
    }
}
#endif
