#if UNITY_EDITOR
using System.Collections.Generic;
using EcoCity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EcoCity.Editor.SubmissionKidUi;

namespace EcoCity.Editor
{
    // Rebuilds the whole in-game canvas of the submission scene with the crisp kid UI kit.
    // AlphaUi, SubmissionSettings, CharacterSelection and KidHud keep their components; only their children and references are new.
    // Layout (1920x1080 reference, height match): fixed-size corner HUD cards that never stretch with the screen,
    // centred menu cards, and every text box sized for its longest real string.
    public static class SubmissionKidGameUi
    {
        private static readonly Color StarTodo = new Color(0.86f, 0.87f, 0.84f);
        private static readonly Color StarDone = new Color(1f, 0.76f, 0.12f);
        private static readonly Color Brown = new Color(0.42f, 0.26f, 0.05f);
        private static readonly string[] StageNames = { "새싹 공원", "반짝 바닷가", "알록달록 시장" };
        private static readonly string[] StageJobs = { "깨끗한 쓰레기", "비우고 씻기", "떼어내고 버리기" };

        public static void Build(AlphaFlow flow, Dictionary<string, Texture2D> itemIcons, Texture2D helperFace, Texture2D titlePicture, Color[] binColors)
        {
            var ui = flow.ui;
            var canvas = ui.transform;
            var settings = Object.FindFirstObjectByType<SubmissionSettings>();
            var selection = flow.player.GetComponent<CharacterSelection>();
            for (int i = canvas.childCount - 1; i >= 0; i--) Object.DestroyImmediate(canvas.GetChild(i).gameObject);
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1; scaler.referencePixelsPerUnit = 100;
            ui.GetComponent<Canvas>().pixelPerfect = false;

            ui.screens = new GameObject[6];
            for (int i = 0; i < 6; i++) ui.screens[i] = Full(((AlphaScreen)i).ToString(), canvas).gameObject;

            // Title (shown when the game scene is opened directly; the title scene normally comes first).
            var title = SubmissionKidUi.Title(ui.screens[(int)AlphaScreen.Title].transform, titlePicture, "제출판 " + SubmissionSceneBuilder.Version + " · 2026 SCC · 겜성충만", true);
            ui.newGame = title.start; ui.continueGame = title.continueGame; ui.titleHelp = title.help; ui.quit = title.quit;
            settings.openTitle = title.settings;
            selection.buttons = title.characters; selection.frames = title.frames;
            selection.selectedColor = Sun; selection.idleColor = Color.white;

            var hud = ui.GetComponent<KidHud>(); if (hud == null) hud = ui.gameObject.AddComponent<KidHud>();
            hud.flow = flow; hud.dotDone = StarDone; hud.dotTodo = StarTodo; hud.stageCurrent = Lavender;
            var talk = ui.GetComponent<KidHelperTalk>(); if (talk == null) talk = ui.gameObject.AddComponent<KidHelperTalk>();
            talk.flow = flow; talk.feedback = ui.feedback; talk.helper = Object.FindFirstObjectByType<GuideNpc>().transform;
            var marks = new List<Image>();
            Hud(ui, hud, talk, ui.screens[(int)AlphaScreen.Playing].transform, itemIcons, helperFace, marks);
            Menus(ui, settings, marks);
            hud.stageMarks = marks.ToArray();
            Banners(ui, hud, talk);
            SettingsPanel(settings, canvas);
            MiniGame(flow, itemIcons, binColors);
            StickerBook(flow, itemIcons);
            // Big-text option: the HUD cards in the corners grow around their own corner.
            var targets = new List<RectTransform>();
            foreach (Transform child in ui.screens[(int)AlphaScreen.Playing].transform)
            {
                var rect = child as RectTransform;
                if (rect != null && rect.anchorMin == rect.anchorMax) targets.Add(rect);
            }
            settings.bigTextTargets = targets.ToArray();
            settings.postFx = flow.player.viewCamera.GetComponent<KidPostFx>();
            FinaleBanner(flow);
            WorldMap(flow);
            for (int i = 0; i < 6; i++) ui.screens[i].SetActive(i == 0);
            BakeCharacters(ui.gameObject);
        }

        // Picture of what to do next, on the held item's picture: the step still needed, or the bin once the hints name it.
        // Replaces an older badge, so it can also be added to a scene built before it existed.
        public static void NextStepBadge(KidHud hud, Transform held)
        {
            var old = held.Find("다음 할 일"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var badge = Node("다음 할 일", held, new Vector2(0.5f, 1), new Vector2(112, -64), new Vector2(96, 96));
            var ring = Circle(badge, Sky);
            var window = Node("그림 창", badge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(82, 82));
            var face = Circle(window, Color.white); face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            // Outside a full rebuild the kit's sprites are not loaded yet: use the saved circle.
            if (ring.sprite == null)
            {
                Sprite saved = null;
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("UI2_Circle t:Sprite", new[] { Root }))
                    saved = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                ring.sprite = saved; face.sprite = saved;
            }
            var picture = Node("그림", window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(82, 82));
            hud.nextIcon = picture.gameObject.AddComponent<RawImage>(); hud.nextIcon.raycastTarget = false;
            hud.nextPanel = badge.gameObject; hud.nextRing = badge.GetComponent<Image>();
            hud.nextStepColor = Sky; hud.nextBinColor = Green;
            hud.stepIcons = new Texture2D[3];
            string[] steps = { "Step_Empty", "Step_Separate", "Step_Wash" };
            for (int i = 0; i < 3; i++) hud.stepIcons[i] = Picture(UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Icons/" + steps[i] + ".png"));
            hud.binIcons = new Texture2D[7];
            for (int c = 0; c < 7; c++) hud.binIcons[c] = Picture(UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Icons/Bin_" + (WasteCategory)c + ".png"));
            badge.gameObject.SetActive(false);
        }

        private static void Hud(AlphaUi ui, KidHud hud, KidHelperTalk talk, Transform play, Dictionary<string, Texture2D> itemIcons, Texture2D helperFace, List<Image> marks)
        {
            // Mission card, top left: stage track 1-2-3, stage name, progress stars, cleanliness and score, cleanliness bar.
            Card("미션 카드", play, new Vector2(0, 1), new Vector2(Margin, -Margin), new Vector2(460, 250), 32, Paper, out var mission);
            Tag(mission, "★ 오늘의 미션", Lavender, new Vector2(0, 1), new Vector2(20, -20), 216, 46);
            for (int i = 0; i < 3; i++)
            {
                var mark = Node("단계 " + (i + 1), mission, new Vector2(1, 1), new Vector2(-20 - (2 - i) * 46, -24), new Vector2(38, 38));
                marks.Add(Circle(mark, StarTodo));
                FillText("숫자", mark, (i + 1).ToString(), 24, Color.white, SoftMaterial);
            }
            ui.missionText = Line(Text("미션", mission, "", 34, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(22, -78), new Vector2(416, 54)), 22);
            // One star per item of the region (up to five).
            hud.progressDots = new Image[5];
            for (int i = 0; i < 5; i++)
                hud.progressDots[i] = SubmissionKidUi.Sprite(Node("별 " + (i + 1), mission, new Vector2(0, 1), new Vector2(18 + i * 42, -140), new Vector2(40, 40)), StarSprite, StarTodo);
            ui.cityText = Line(Text("깨끗함", mission, "", 24, Soft, TextAlignmentOptions.MidlineRight, new Vector2(1, 1), new Vector2(-22, -140), new Vector2(206, 46)), 15);
            var bar = Node("깨끗함 막대", mission, new Vector2(0, 1), new Vector2(22, -204), new Vector2(416, 24));
            Paint(bar, new Color(0.88f, 0.9f, 0.86f), 12);
            var fill = Full("채움", bar); fill.anchorMax = new Vector2(0, 1);
            hud.cleanFill = Paint(fill, new Color(0.4f, 0.78f, 0.35f), 12);

            // What-to-do card under the mission card: one meaningful sentence, the practice counter while it lasts.
            var tutorialGroup = Card("할 일 카드", play, new Vector2(0, 1), new Vector2(Margin, -Margin - 250 - 16), new Vector2(460, 224), 32, Paper, out var tutorial);
            Tag(tutorial, "지금 할 일", Sky, new Vector2(0, 1), new Vector2(20, -20), 176, 46);
            hud.tutorialStep = Text("연습 순서", tutorial, "", 24, Sky, TextAlignmentOptions.MidlineRight, new Vector2(1, 1), new Vector2(-22, -24), new Vector2(160, 40));
            ui.tutorialText = Fit(Text("할 일", tutorial, "", 28, Ink, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(22, -82), new Vector2(416, 102)), 20);
            Text("도움말", tutorial, "H 도움말 · 안내 건너뛰기", 20, Soft, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(22, 10), new Vector2(416, 30));
            hud.tutorialPanel = tutorialGroup.gameObject;

            // Held item card, top right: picture in a circle and the item's name and next step.
            Card("손 카드", play, new Vector2(1, 1), new Vector2(-Margin, -Margin), new Vector2(340, 326), 32, Paper, out var held);
            Tag(held, "♥ 손에 든 것", Orange, new Vector2(0.5f, 1), new Vector2(0, -20), 212, 46);
            var window = Node("그림 창", held, new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(150, 150));
            Circle(window, Cream).gameObject.AddComponent<Mask>().showMaskGraphic = true;
            hud.heldEmpty = Text("빈 손", window, "?", 76, new Color(0.88f, 0.78f, 0.6f), TextAlignmentOptions.Center).gameObject;
            Stretch(hud.heldEmpty.GetComponent<RectTransform>());
            var picture = Node("그림", window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            hud.heldIcon = picture.gameObject.AddComponent<RawImage>(); hud.heldIcon.raycastTarget = false; hud.heldIcon.enabled = false;
            hud.iconIds = new string[itemIcons.Count]; hud.icons = new Texture2D[itemIcons.Count];
            int index = 0; foreach (var pair in itemIcons) { hud.iconIds[index] = pair.Key; hud.icons[index++] = Picture(pair.Value); }
            ui.heldText = Fit(Text("이름", held, "", 27, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -238), new Vector2(308, 72)), 18);
            NextStepBadge(hud, held);

            // Menu button, top centre.
            ui.pause = Candy("메뉴 열기", play, "메뉴  <size=72%><color=#8A93A3>Esc</color></size>", Color.white, new Vector2(0.5f, 1), new Vector2(0, -Margin), new Vector2(212, 72), 32, Ink);

            // Helper speech bubble: floats over the helper's head (KidHelperTalk places it) after E is pressed next to the helper.
            var bubble = Node("도우미 말풍선", play, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 200));
            bubble.pivot = new Vector2(0.5f, 0);
            var bubbleBody = Full("판", bubble, 0, 26, 0, 0);
            DropShadow(bubbleBody, 18, 10, 0.26f);
            Paint(bubbleBody, Paper, 40, false);
            var tail = Node("꼬리", bubble, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(40, 40));
            tail.pivot = new Vector2(0.5f, 0.5f); tail.localRotation = Quaternion.Euler(0, 0, 45); Paint(tail, Paper, 8);
            Tag(bubbleBody, "도우미", Green, new Vector2(0, 1), new Vector2(24, 20), 120, 40);
            var helperFace2 = Node("얼굴", bubbleBody, new Vector2(0, 0.5f), new Vector2(18, -6), new Vector2(112, 112));
            Circle(helperFace2, Sun);
            var faceWindow = Node("얼굴 창", helperFace2, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(98, 98));
            Circle(faceWindow, Cream).gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var faceImage = Node("그림", faceWindow, new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(110, 110)).gameObject.AddComponent<RawImage>();
            faceImage.texture = Picture(helperFace); faceImage.raycastTarget = false;
            var said = Fit(Text("말", bubbleBody, "", 28, Ink, TextAlignmentOptions.MidlineLeft), 18);
            Stretch(said.rectTransform, 146, 14, 28, 22);
            talk.bubble = bubble; talk.bubbleText = said; bubble.gameObject.SetActive(false);
            ui.feedbackText = null;

            // Action prompt, bottom right: a yellow pill that hugs its text, with an E key cap when the target can be used.
            var prompt = Hug("행동 안내", play, new Vector2(1, 0), new Vector2(-Margin, Margin + 4), 100, Sun, 20, 34, out var promptGroup);
            var key = KeyCap(prompt, "E", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68, 66), 40);
            key.gameObject.AddComponent<LayoutElement>().preferredWidth = 68;
            hud.promptKey = key.gameObject;
            var promptText = Text("글", prompt, "", 36, Brown, TextAlignmentOptions.MidlineLeft); promptText.textWrappingMode = TextWrappingModes.NoWrap;
            promptText.rectTransform.sizeDelta = new Vector2(0, 64);
            ui.targetText = promptText;
            promptGroup.spacing = 16;
        }

        private static void Menus(AlphaUi ui, SubmissionSettings settings, List<Image> marks)
        {
            // Pause: where am I in the three stages, then the big Continue and four small buttons.
            var pause = MenuCard(ui.screens[(int)AlphaScreen.Paused].transform, "잠깐 쉬어요", null, new Vector2(760, 750), Lavender);
            for (int i = 0; i < 3; i++)
            {
                var stage = Node("단계 " + (i + 1), pause, new Vector2(0.5f, 1), new Vector2(-216 + i * 216, -82), new Vector2(200, 124));
                Paint(stage, Mint, 24);
                var mark = Node("표시", stage, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(44, 44));
                marks.Add(Circle(mark, StarTodo));
                FillText("숫자", mark, (i + 1).ToString(), 26, Color.white, SoftMaterial);
                Text("이름", stage, StageNames[i] + "\n<size=80%><color=#8E7D93>" + StageJobs[i] + "</color></size>", 22, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(190, 60));
            }
            ui.menuGuideText = null; ui.askGuide = null;
            ui.resume = Candy("계속하기", pause, "계속하기", Lavender, new Vector2(0.5f, 1), new Vector2(0, -232), new Vector2(640, 100), 44);
            ui.pauseHelp = Candy("어떻게 해요", pause, "어떻게 해요?", Sky, new Vector2(0.5f, 1), new Vector2(-164, -356), new Vector2(312, 88), 34);
            settings.openPause = Candy("설정", pause, "설정", Pink, new Vector2(0.5f, 1), new Vector2(164, -356), new Vector2(312, 88), 34);
            // The map replaces "save" (the game saves by itself on every menu visit and region change).
            var mapButton = Candy("지도", pause, "지도 보기", Orange, new Vector2(0.5f, 1), new Vector2(-164, -462), new Vector2(312, 88), 34);
            var regions = Object.FindFirstObjectByType<KidRegions>(); if (regions != null) regions.mapFromPause = mapButton;
            ui.save = null;
            ui.toTitle = Candy("처음 화면으로", pause, "처음 화면으로", Gray, new Vector2(0.5f, 1), new Vector2(164, -462), new Vector2(312, 88), 34);
            // Sticker book of every kind of trash sorted so far, with a dot when a new sticker is waiting.
            var bookButton = Candy("도감", pause, "분리배출 도감", Green, new Vector2(0.5f, 1), new Vector2(0, -568), new Vector2(640, 88), 34);
            var book = ui.GetComponent<KidStickerBook>(); if (book == null) book = ui.gameObject.AddComponent<KidStickerBook>();
            book.open = bookButton;
            var dot = Node("새 스티커", bookButton.transform, new Vector2(1, 1), new Vector2(-14, -10), new Vector2(34, 34)); dot.pivot = new Vector2(0.5f, 0.5f);
            Circle(dot, Pink); book.newDot = dot.gameObject; dot.gameObject.SetActive(false);
            Text("단축키", pause, "Esc 계속하기 · H 도움말", 22, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(640, 34));

            // How to play, in pictures.
            var help = MenuCard(ui.screens[(int)AlphaScreen.Help].transform, "어떻게 해요?", null, new Vector2(1160, 840), Sky);
            HowTo(help, -82);
            ui.skipTutorial = Candy("안내 그만 보기", help, "안내 그만 보기", Sky, new Vector2(0.5f, 1), new Vector2(-198, -712), new Vector2(380, 90), 34);
            ui.helpBack = Candy("돌아가기", help, "돌아가기", Green, new Vector2(0.5f, 1), new Vector2(198, -712), new Vector2(380, 90), 34);

            var results = MenuCard(ui.screens[(int)AlphaScreen.Results].transform, "에코시티를 구했어요!", "세 곳 모두 깨끗해졌어요! 스스로 생각하고 해냈어요.", new Vector2(820, 860), Orange);
            // Rescue medal on the corner of the results card.
            var medal = Node("구조대 메달", results, new Vector2(0, 1), new Vector2(-34, 54), new Vector2(150, 150));
            medal.pivot = new Vector2(0.5f, 0.5f); medal.anchoredPosition = new Vector2(46, -34); medal.localRotation = Quaternion.Euler(0, 0, 12);
            DropShadow(medal, 12, 8, 0.3f); Circle(medal, Sun);
            Circle(Node("안쪽", medal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124, 124)), new Color(1f, 0.9f, 0.55f));
            SubmissionKidUi.Sprite(Node("별", medal, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(84, 84)), StarSprite, Color.white);
            Text("글", medal, "구조대", 22, new Color(0.55f, 0.36f, 0.1f), TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(120, 30));
            ui.resultText = ScrollText(results, -142, new Vector2(720, 560), 28, TextAlignmentOptions.Top);
            // The results are written out line by line, a tick for every star.
            var reveal = ui.resultText.GetComponent<KidReveal>(); if (reveal == null) reveal = ui.resultText.gameObject.AddComponent<KidReveal>();
            reveal.text = (TMP_Text)ui.resultText; reveal.sound = ui.GetComponent<AudioSource>();
            var audio = ui.GetComponent<SubmissionAudio>(); reveal.tick = audio != null ? audio.clickClip : null;
            ui.practiceAgain = Candy("한 번 더 하기", results, "한 번 더 하기", Lavender, new Vector2(0.5f, 1), new Vector2(-178, -726), new Vector2(340, 90), 34);
            ui.practiceAgain.gameObject.AddComponent<KidPadButton>();
            ui.resultTitle = Candy("처음 화면으로", results, "처음 화면으로", Gray, new Vector2(0.5f, 1), new Vector2(178, -726), new Vector2(340, 90), 34);

            var confirm = MenuCard(ui.screens[(int)AlphaScreen.ConfirmNewGame].transform, "처음부터 다시 할까요?", null, new Vector2(760, 440), Orange);
            Text("설명", confirm, "저장된 진행이 지워지고 처음부터 시작해요.\n지금 진행을 이어가려면 돌아가기를 눌러요.", 29, Ink, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0, -86), new Vector2(680, 150));
            ui.confirmYes = Candy("처음부터 하기", confirm, "처음부터 하기", Orange, new Vector2(0.5f, 1), new Vector2(-170, -272), new Vector2(320, 96), 36);
            ui.confirmYes.gameObject.AddComponent<KidPadButton>();
            ui.confirmNo = Candy("돌아가기", confirm, "돌아가기", Green, new Vector2(0.5f, 1), new Vector2(170, -272), new Vector2(320, 96), 36);
        }

        private static void Banners(AlphaUi ui, KidHud hud, KidHelperTalk talk)
        {
            var canvas = ui.transform;
            // Result of the player's own action: a short toast with an O / X / ! badge, above the bottom row.
            var toast = Hug("결과 안내", ui.screens[(int)AlphaScreen.Playing].transform, new Vector2(0.5f, 0), new Vector2(0, Margin + 4 + 100 + 22), 104, Color.white, 18, 36, out var toastLayout);
            toastLayout.spacing = 16;
            var badge = Node("표시", toast, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66, 66));
            badge.gameObject.AddComponent<LayoutElement>().preferredWidth = 66;
            talk.toastBadge = Circle(badge, Green);
            talk.toastMark = FillText("글자", badge, "O", 40, Color.white, SoftMaterial);
            var toastText = Text("글", toast, "", 28, Ink, TextAlignmentOptions.MidlineLeft); toastText.textWrappingMode = TextWrappingModes.NoWrap;
            toastText.rectTransform.sizeDelta = new Vector2(0, 84);
            talk.toastText = toastText; talk.toastPanel = toast.gameObject; toast.gameObject.SetActive(false);

            // Save and device notices: a small dark toast above the action prompt (also visible on the title).
            var notice = Hug("저장 안내", canvas, new Vector2(1, 0), new Vector2(-Margin, Margin + 4 + 100 + 22), 56, new Color(0.08f, 0.17f, 0.2f, 0.88f), 28, 28, out _, lip: false);
            var noticeText = Text("글", notice, "", 24, Color.white, TextAlignmentOptions.MidlineLeft); noticeText.textWrappingMode = TextWrappingModes.NoWrap;
            noticeText.rectTransform.sizeDelta = new Vector2(0, 44);
            ui.noticeText = noticeText; hud.noticePanel = notice.gameObject;

            // Stage-complete banner, top centre under the menu button.
            // 520 wide keeps it clear of the corner cards on 16:9 and 16:10 screens.
            var party = Node("단계 완료 안내", canvas, new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(520, 116));
            DropShadow(party, 16, 8, 0.26f);
            Paint(party, Darker(Sun, 0.8f), 40);
            var face = Full("면", party, 0, 7, 0, 0); Paint(face, Sun, 40);
            var cheer = Fit(FillText("글", face, "", 32, Brown, null, 64, 4, 64, 4), 18); cheer.textWrappingMode = TextWrappingModes.NoWrap;
            ui.celebrationText = cheer;
            for (int side = 0; side < 2; side++)
            {
                var star = Node(side == 0 ? "왼쪽 별" : "오른쪽 별", party, new Vector2(side, 0.5f), Vector2.zero, new Vector2(52, 52));
                star.pivot = new Vector2(0.5f, 0.5f); star.sizeDelta = new Vector2(52, 52);
                star.anchoredPosition = new Vector2(side == 0 ? 38 : -38, 4); star.localRotation = Quaternion.Euler(0, 0, side == 0 ? 12 : -12);
                SubmissionKidUi.Sprite(star, StarSprite, Color.white);
            }
            hud.celebrationPanel = party.gameObject;
        }

        private static void SettingsPanel(SubmissionSettings settings, Transform canvas)
        {
            var root = Full("환경 설정 화면", canvas);
            // Two columns: sound and camera sliders on the left, switches on the right.
            var card = MenuCard(root, "설정", null, new Vector2(1160, 760), Pink);
            settings.values = Line(Text("값 요약", card, "", 24, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(1000, 36)), 16);
            settings.sensitivity = SubmissionKidUi.Slider(card, "카메라 돌리는 속도", -128, 0.25f, 4, 500, out var sensitivityValue);
            settings.volume = SubmissionKidUi.Slider(card, "효과음 크기", -254, 0, 1, 500, out var volumeValue);
            settings.music = SubmissionKidUi.Slider(card, "음악 크기", -380, 0, 1, 500, out var musicValue);
            // The summary line above shows the values; the per-slider labels are not needed here.
            foreach (var value in new[] { sensitivityValue, volumeValue, musicValue }) Object.DestroyImmediate(value.gameObject);
            foreach (var slider in new[] { settings.sensitivity, settings.volume, settings.music }) ((RectTransform)slider.transform.parent).anchoredPosition += new Vector2(-270, 0);
            settings.invertY = SubmissionKidUi.Toggle(card, "카메라 위아래 반대로", -150, 460);
            settings.bigText = SubmissionKidUi.Toggle(card, "글자와 안내 크게", -236, 460);
            settings.screenEffects = SubmissionKidUi.Toggle(card, "반짝임·색감 화면 효과", -322, 460);
            settings.fullscreen = SubmissionKidUi.Toggle(card, "전체 화면 (실행 파일에서)", -408, 460);
            LightGraphicsToggle(settings, card);
            settings.defaults = Candy("처음 설정으로", card, "처음 설정으로", Sky, new Vector2(0.5f, 1), new Vector2(-170, -556), new Vector2(320, 88), 32);
            settings.close = Candy("돌아가기", card, "돌아가기", Green, new Vector2(0.5f, 1), new Vector2(170, -556), new Vector2(320, 88), 32);
            settings.panel = root.gameObject; root.gameObject.SetActive(false);
        }

        // Right column of switches, 80 apart, with "가벼운 화면" last; also adds the switch to a settings card built before it existed.
        public static void LightGraphicsToggle(SubmissionSettings settings, Transform card)
        {
            var old = card.Find(LightGraphicsTitle); if (old != null) Object.DestroyImmediate(old.gameObject);
            // A copy of the full-screen switch keeps its font, sprites and size even outside a full rebuild.
            var copy = Object.Instantiate(settings.fullscreen.gameObject, card); copy.name = LightGraphicsTitle;
            copy.GetComponentInChildren<TMP_Text>(true).text = LightGraphicsTitle;
            settings.lightGraphics = copy.GetComponent<Toggle>(); settings.lightGraphics.SetIsOnWithoutNotify(false);
            var column = new[] { settings.invertY, settings.bigText, settings.screenEffects, settings.fullscreen, settings.lightGraphics };
            for (int i = 0; i < column.Length; i++) ((RectTransform)column[i].transform).anchoredPosition = new Vector2(290, -140 - i * 80);
        }
        private const string LightGraphicsTitle = "가벼운 화면 (느린 컴퓨터용)";
        public const float StarRowWidth = 198;

        // "번개 분리수거" mini game overlay, opened from the park booth.
        private static void MiniGame(AlphaFlow flow, Dictionary<string, Texture2D> itemIcons, Color[] binColors)
        {
            var ui = flow.ui;
            var game = ui.GetComponent<KidMiniGame>(); if (game == null) game = ui.gameObject.AddComponent<KidMiniGame>();
            game.flow = flow; game.voice = flow.game.GetComponent<KidVoice>();
            // Set explicitly: a value already saved in the scene would otherwise win over the script default.
            game.rounds = 6; game.secondsPerQuestion = 10;
            var root = Full("미니 게임 화면", ui.transform);
            var card = MenuCard(root, "번개 분리수거!", null, new Vector2(1040, 800), Pink);
            game.panel = root.gameObject;
            // Picture pool: park items that go straight into a bin (items that must be emptied, washed or separated first
            // would teach the wrong answer without that step), plus one picture for each other bin. No picture repeats.
            var pictures = new List<Texture2D>(); var categories = new List<int>();
            foreach (var item in flow.game.wasteItems)
            {
                Texture2D icon; if (!itemIcons.TryGetValue(item.instanceId, out icon)) continue;
                if (item.definition.ToSpec(item.instanceId).Required != Preparation.None || categories.Contains((int)item.definition.targetBin)) continue;
                pictures.Add(Picture(icon)); categories.Add((int)item.definition.targetBin);
            }
            var names = new string[7]; var colors = new Color[7];
            for (int c = 0; c < 7; c++)
            {
                var category = (WasteCategory)c;
                names[c] = FeedbackText.Category(category);
                colors[c] = Color.Lerp(binColors[c], Color.white, 0.2f);
                var binIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Icons/Bin_" + category + ".png");
                if (binIcon != null && !categories.Contains(c)) { pictures.Add(Picture(binIcon)); categories.Add(c); }
            }
            game.pictures = pictures.ToArray(); game.pictureCategories = categories.ToArray(); game.categoryNames = names; game.categoryColors = colors;

            // Intro.
            var intro = Full("시작 안내", card); game.intro = intro.gameObject;
            var star = Node("큰 별", intro, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(200, 200));
            SubmissionKidUi.Sprite(star, StarSprite, Sun);
            Text("설명", intro, "그림을 보고 맞는 통을 골라요!\n<size=80%><color=#8E7D93>6문제 · 숫자 1 2 3 키나 마우스로 골라요 · 빨리빨리!</color></size>", 40, Ink, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0, -320), new Vector2(900, 170));
            Text("최고", intro, "정답을 많이 맞히면 별을 3개까지 받아요", 26, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -500), new Vector2(900, 40));
            game.startButton = Candy("시작", intro, "시작!", Lavender, new Vector2(0.5f, 1), new Vector2(-180, -590), new Vector2(340, 110), 48);
            game.introClose = Candy("나가기", intro, "나가기", Gray, new Vector2(0.5f, 1), new Vector2(180, -590), new Vector2(340, 110), 40);

            // Question.
            var question = Full("문제", card); game.question = question.gameObject;
            game.progressText = Text("몇 번째", question, "1 / 6", 34, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(60, -90), new Vector2(200, 50));
            game.scoreText = Text("맞힌 수", question, "★ 0", 34, new Color(0.95f, 0.62f, 0.15f), TextAlignmentOptions.MidlineRight, new Vector2(1, 1), new Vector2(-60, -90), new Vector2(200, 50));
            var timer = Node("시간 막대", question, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(920, 26));
            Paint(timer, new Color(0.9f, 0.88f, 0.96f), 13);
            var timeFill = Full("남은 시간", timer); timeFill.anchorMax = new Vector2(1, 1);
            game.timerFill = Paint(timeFill, Lavender, 13);
            var frame = Node("그림 판", question, new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(300, 300));
            DropShadow(frame, 18, 10, 0.25f); Paint(frame, Mint, 44);
            var window = Node("그림 창", frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 260));
            Paint(window, Color.white, 36).gameObject.AddComponent<Mask>().showMaskGraphic = true;
            game.picture = Full("그림", window).gameObject.AddComponent<RawImage>(); game.picture.raycastTarget = false;
            var mark = Node("결과 표시", frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 240));
            game.markBadge = Circle(mark, Green); game.markBadge.color = new Color(0.36f, 0.8f, 0.62f, 0.95f);
            game.markText = FillText("글", mark, "O", 120, Color.white, SoftMaterial, 10, 10, 10, 10);
            mark.gameObject.SetActive(false);
            game.choices = new Button[3]; game.choiceFaces = new Image[3]; game.choiceLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var choice = Candy("고르기 " + (i + 1), question, "통", Sky, new Vector2(0.5f, 1), new Vector2(-310 + i * 310, -566), new Vector2(290, 130), 40);
                game.choices[i] = choice; game.choiceFaces[i] = choice.transform.Find("면").GetComponent<Image>();
                game.choiceLabels[i] = choice.GetComponentInChildren<TMP_Text>();
            }
            game.quitButton = Candy("그만", question, "그만하기", Gray, new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(220, 64), 28);

            // Result.
            var result = Full("결과", card); game.result = result.gameObject;
            game.resultStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var resultStar = Node("별 " + (i + 1), result, new Vector2(0.5f, 1), new Vector2(-190 + i * 190, i == 1 ? -90 : -120), Vector2.one * (i == 1 ? 190 : 150));
                game.resultStars[i] = SubmissionKidUi.Sprite(resultStar, StarSprite, Sun);
            }
            game.resultText = Text("결과 글", result, "", 46, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(900, 70));
            game.bestText = Text("최고 기록", result, "", 30, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -410), new Vector2(900, 44));
            Text("응원", result, "미니 게임 점수는 공원 미션 점수와 따로 기록돼요", 24, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(900, 36));
            game.againButton = Candy("한 번 더", result, "한 번 더!", Lavender, new Vector2(0.5f, 1), new Vector2(-180, -590), new Vector2(340, 110), 46);
            game.resultClose = Candy("나가기", result, "공원으로", Green, new Vector2(0.5f, 1), new Vector2(180, -590), new Vector2(340, 110), 40);
            question.gameObject.SetActive(false); result.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            MiniGameKinds(game);
            foreach (var booth in Object.FindObjectsByType<MiniGameBooth>(FindObjectsSortMode.None)) booth.miniGame = game;
        }
        /// <summary>Parts the three booth games share on one card: the title and intro line they rewrite, a question line
        /// above a slightly smaller picture, and wording that fits every region. Also updates a card built before them.</summary>
        public static void MiniGameKinds(KidMiniGame game)
        {
            var card = game.intro.transform.parent;
            foreach (var label in card.parent.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.text == "번개 분리수거!" || label == game.titleText) game.titleText = label;
                if (label.name == "설명" && label.transform.parent == game.intro.transform) game.introText = label;
                if (label.name == "응원") label.text = "미니 게임 점수는 미션 점수와 따로 기록돼요";
            }
            var closeLabel = game.resultClose.GetComponentInChildren<TMP_Text>(true); if (closeLabel != null) closeLabel.text = "돌아가기";
            var old = game.question.transform.Find("질문 글"); if (old != null) Object.DestroyImmediate(old.gameObject);
            // A copy of the progress label keeps the kid font and material.
            var ask = Object.Instantiate(game.progressText.gameObject, game.question.transform).GetComponent<TMP_Text>();
            ask.name = "질문 글"; ask.text = ""; ask.alignment = TextAlignmentOptions.Center; ask.fontSize = 34; ask.color = Ink;
            ask.enableAutoSizing = true; ask.fontSizeMin = 22; ask.fontSizeMax = 34; ask.textWrappingMode = TextWrappingModes.NoWrap;
            var rect = ask.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -172); rect.sizeDelta = new Vector2(940, 50);
            game.questionText = ask;
            var frame = (RectTransform)game.picture.transform.parent.parent;
            frame.anchoredPosition = new Vector2(0, -230); frame.sizeDelta = new Vector2(268, 268);
            ((RectTransform)game.picture.transform.parent).sizeDelta = new Vector2(232, 232);
        }

        // "에코시티를 구했어요!" banner for the ending scene: a white flash, a medal, the big title and bouncing stars.
        private static void FinaleBanner(AlphaFlow flow)
        {
            var finale = Object.FindFirstObjectByType<KidFinale>();
            if (finale == null) return;
            var root = Full("도시 구출 축하", flow.ui.transform);
            var flashImage = Full("반짝", root).gameObject.AddComponent<Image>(); flashImage.color = Color.white; flashImage.raycastTarget = false;
            finale.flash = flashImage.gameObject.AddComponent<CanvasGroup>();
            finale.flash.blocksRaycasts = false; finale.flash.interactable = false;
            // Upper half of the screen, so the sparkling park and the friends stay visible below it.
            // Small title card high on the screen: the park itself shows the rescue, the words only name it.
            var card = Node("축하 카드", root, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(820, 170));
            card.pivot = new Vector2(0.5f, 0.5f); card.anchoredPosition = new Vector2(0, -170);
            var body = Full("판", card); DropShadow(body, 22, 12, 0.3f); Paint(body, Paper, 60);
            var medal = Node("메달", card, new Vector2(0, 0.5f), new Vector2(-30, 0), new Vector2(170, 170));
            medal.pivot = new Vector2(0.5f, 0.5f); medal.anchoredPosition = new Vector2(20, 0);
            DropShadow(medal, 14, 8, 0.3f); Circle(medal, Sun);
            Circle(Node("안쪽", medal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(144, 144)), new Color(1f, 0.9f, 0.55f));
            SubmissionKidUi.Sprite(Node("별", medal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 104)), StarSprite, Color.white);
            var title = Text("제목", card, "에코시티를 구했어요!", 76, Lavender, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(60, 4), new Vector2(680, 120), StickerMaterial);
            Fit(title, 48).textWrappingMode = TextWrappingModes.NoWrap;
            finale.bannerTitle = title;
            Color[] colours = { Sun, Pink, Sky, Green };
            Vector2[] spots = { new Vector2(-470, 70), new Vector2(470, 80), new Vector2(-440, -80), new Vector2(450, -70) };
            finale.bannerStars = new RectTransform[spots.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                var star = Node("별 " + (i + 1), card, new Vector2(0.5f, 0.5f), spots[i], Vector2.one * (i < 2 ? 120 : 90));
                star.pivot = new Vector2(0.5f, 0.5f);
                SubmissionKidUi.Sprite(star, StarSprite, colours[i]); finale.bannerStars[i] = star;
            }
            finale.banner = root.gameObject; finale.bannerCard = card;
            root.gameObject.SetActive(false);
        }

        // Pop-up book map of Eco City: three region stickers on a winding path, locked / go / saved marks.
        private static void WorldMap(AlphaFlow flow)
        {
            var regions = Object.FindFirstObjectByType<KidRegions>();
            if (regions == null) return;
            var root = Full("에코시티 지도", flow.ui.transform);
            Paint(root, new Color(0.11f, 0.17f, 0.22f, 0.94f), 0, true);
            // The open book: cover edge, two cream pages, the fold.
            var book = Node("책", root, new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(1700, 880));
            book.pivot = new Vector2(0.5f, 0.5f);
            DropShadow(book, 30, 18, 0.5f); PopIn(book, 0.4f, 0.8f);
            Paint(book, new Color(0.47f, 0.38f, 0.78f), 64);
            var left = Node("왼쪽 쪽", book, new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(836, 830)); left.pivot = new Vector2(0, 0.5f);
            Paint(left, new Color(1f, 0.97f, 0.9f), 46);
            var right = Node("오른쪽 쪽", book, new Vector2(1, 0.5f), new Vector2(-26, 0), new Vector2(836, 830)); right.pivot = new Vector2(1, 0.5f);
            Paint(right, new Color(0.99f, 0.95f, 0.87f), 46);
            var fold = Node("접힌 곳", book, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 830)); fold.pivot = new Vector2(0.5f, 0.5f);
            Paint(fold, new Color(0.83f, 0.74f, 0.66f, 0.55f), 0);
            // A soft green land painted on the pages.
            var land = Node("땅", book, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1560, 600)); land.pivot = new Vector2(0.5f, 0.5f);
            Paint(land, new Color(0.78f, 0.92f, 0.7f, 0.85f), 220);
            // Title ribbon over the top edge and a line under it.
            var band = Node("제목 띠", book, new Vector2(0.5f, 1), new Vector2(0, 46), new Vector2(620, 100));
            DropShadow(band, 14, 8, 0.3f); Paint(band, Darker(Lavender, 0.8f), 50);
            var bandFace = Full("면", band, 0, 8, 0, 0); Paint(bandFace, Lavender, 50);
            FillText("제목", bandFace, "에코시티 지도", 54, Color.white, SoftMaterial, 20, 0, 20, 0);
            Text("설명", book, "구하러 갈 곳을 골라요!", 32, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(900, 50));
            Text("키 안내", book, "숫자 1 2 3 키나 마우스로 골라요   ·   Esc: 지금 있는 곳으로", 22, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(1200, 34));
            // Clouds and leaves around the edge.
            foreach (var spot in new[] { new Vector2(-760, 330), new Vector2(740, 340), new Vector2(-720, -340), new Vector2(760, -330) })
                for (int k = 0; k < 3; k++)
                    Circle(Node("구름", book, new Vector2(0.5f, 0.5f), spot + new Vector2(k * 44 - 44, k == 1 ? 14 : 0), Vector2.one * (k == 1 ? 90 : 66)), new Color(1, 1, 1, 0.95f));
            foreach (var spot in new[] { new Vector2(-260, 300), new Vector2(280, 300), new Vector2(-640, 40), new Vector2(660, 60) })
            { var leaf = Node("잎", book, new Vector2(0.5f, 0.5f), spot, Vector2.one * 70); SubmissionKidUi.Sprite(leaf, LeafSprite, Color.white); }

            Vector2[] centres = { new Vector2(-540, -40), new Vector2(0, 14), new Vector2(540, -40) };
            Color[] colours = { Green, Sky, Orange };
            string[] jobs = { "깨끗한 쓰레기 5개", "비우고 씻기 4개", "떼어내기 3개" };
            // Dotted path joining the stickers.
            for (int s = 0; s < 2; s++)
                for (int k = 1; k < 10; k++)
                {
                    float f = k / 10f;
                    var at = Vector2.Lerp(centres[s], centres[s + 1], f) + new Vector2(0, -190 - Mathf.Sin(f * Mathf.PI) * 40);
                    Circle(Node("길 점", book, new Vector2(0.5f, 0.5f), at, Vector2.one * 18), new Color(0.62f, 0.52f, 0.45f, 0.8f));
                }
            regions.regionButtons = new Button[3]; regions.cards = new RectTransform[3]; regions.pictures = new RawImage[3];
            regions.lockMarks = new GameObject[3]; regions.savedMarks = new GameObject[3]; regions.hereMarks = new GameObject[3];
            regions.brightPictures = new Texture2D[3]; regions.dullPictures = new Texture2D[3];
            for (int i = 0; i < 3; i++)
            {
                RegionPicture(flow, i, out regions.brightPictures[i], out regions.dullPictures[i]);
                var card = Node("지역 " + (i + 1), book, new Vector2(0.5f, 0.5f), centres[i], new Vector2(440, 380)); card.pivot = new Vector2(0.5f, 0.5f);
                card.localRotation = Quaternion.Euler(0, 0, i == 1 ? 2 : -2);
                var body = Full("판", card); DropShadow(body, 20, 12, 0.35f);
                var frame = Paint(body, Color.white, 38, true);
                var window = Node("그림 창", body, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(404, 246));
                Paint(window, Color.white, 26).gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var picture = Full("그림", window).gameObject.AddComponent<RawImage>(); picture.raycastTarget = false; picture.texture = regions.dullPictures[i];
                regions.pictures[i] = picture;
                var name = Node("이름 띠", body, new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(360, 64));
                Paint(name, Darker(colours[i], 0.82f), 32); var nameFace = Full("면", name, 0, 6, 0, 0); Paint(nameFace, colours[i], 32);
                FillText("글", nameFace, (i + 1) + "  " + SubmissionKidRegionsBuilder.Names[i], 34, Color.white, SoftMaterial, 12, 0, 12, 0);
                Text("할 일", body, jobs[i], 22, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(380, 34));
                // Locked: grey veil and a padlock over the picture.
                var locked = Full("잠김", window); Paint(locked, new Color(0.25f, 0.22f, 0.32f, 0.62f), 0);
                SubmissionKidUi.Sprite(Node("자물쇠", locked, new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(96, 96)), LockSprite, Color.white);
                Text("글", locked, "앞의 곳을 먼저 구해요", 24, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, -56), new Vector2(380, 36), SoftMaterial);
                regions.lockMarks[i] = locked.gameObject;
                // Saved: a round pink stamp with stars.
                // Marks are groups so their drop shadow (a sibling of the body) hides with them.
                var stampGroup = Node("구했어요 도장", card, new Vector2(1, 1), new Vector2(-30, 20), new Vector2(160, 160)); stampGroup.pivot = new Vector2(0.5f, 0.5f);
                stampGroup.localRotation = Quaternion.Euler(0, 0, -14);
                var stamp = Full("몸", stampGroup); DropShadow(stamp, 12, 8, 0.3f); Circle(stamp, Pink);
                Circle(Node("안", stamp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132, 132)), Paper);
                Text("글", stamp, "구했어요!", 34, Pink, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(140, 44));
                // A leaf, not stars: the star row on the card shows how many stars the region earned.
                SubmissionKidUi.Sprite(Node("잎", stamp, new Vector2(0.5f, 0.5f), new Vector2(0, 36), Vector2.one * 34), LeafSprite, Color.white);
                regions.savedMarks[i] = stampGroup.gameObject;
                // Next to save: a bouncing "출발!" tag above the sticker.
                var hereGroup = Node("출발 표시", card, new Vector2(0.5f, 1), new Vector2(0, 58), new Vector2(190, 66)); hereGroup.pivot = new Vector2(0.5f, 0.5f);
                var here = Full("몸", hereGroup); DropShadow(here, 12, 8, 0.3f);
                Paint(here, Darker(Sun, 0.82f), 33); var hereFace = Full("면", here, 0, 6, 0, 0); Paint(hereFace, Sun, 33);
                FillText("글", hereFace, "출발!", 34, new Color(0.45f, 0.3f, 0.12f), PlainMaterial, 10, 0, 10, 0);
                var tip = Node("꼬리", here, new Vector2(0.5f, 0), new Vector2(0, -6), new Vector2(26, 26)); tip.pivot = new Vector2(0.5f, 0.5f); tip.localRotation = Quaternion.Euler(0, 0, 45);
                Paint(tip, Darker(Sun, 0.82f), 4); tip.SetAsFirstSibling();
                regions.hereMarks[i] = hereGroup.gameObject;
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = frame;
                var colors = button.colors; colors.highlightedColor = new Color(1, 0.97f, 0.88f); colors.pressedColor = new Color(0.85f, 0.85f, 0.85f); colors.selectedColor = Color.white; button.colors = colors;
                var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
                regions.regionButtons[i] = button; regions.cards[i] = card;
                // Wide enough for the third star (stars sit at 32, 84 and 136 from the left edge, 46 wide).
                var starRow = Node("별 줄", card, new Vector2(0, 1), new Vector2(34, -30), new Vector2(StarRowWidth, 56)); starRow.pivot = new Vector2(0, 1);
                Paint(starRow, new Color(1, 1, 1, 0.92f), 28);
                var rowStars = new Image[3];
                for (int s = 0; s < 3; s++) rowStars[s] = SubmissionKidUi.Sprite(Node("별 " + (s + 1), starRow, new Vector2(0, 0.5f), new Vector2(32 + s * 52, 0), Vector2.one * 46), StarSprite, Sun);
                if (i == 0) regions.starsRegion1 = rowStars; else if (i == 1) regions.starsRegion2 = rowStars; else regions.starsRegion3 = rowStars;
                if (regions.starRows == null || regions.starRows.Length != 3) regions.starRows = new GameObject[3];
                regions.starRows[i] = starRow.gameObject;
            }
            regions.totalText = Line(Text("별 합계", book, "", 28, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(1300, 44)), 20);
            regions.map = root.gameObject; root.gameObject.SetActive(false);
            ArrivalCard(flow, regions, colours);
        }
        // 분리배출 도감: twelve stickers in two rows; each shows the picture, the name, the steps and the bin once it has been sorted.
        private static void StickerBook(AlphaFlow flow, Dictionary<string, Texture2D> itemIcons)
        {
            var ui = flow.ui;
            var book = ui.GetComponent<KidStickerBook>(); if (book == null) return;
            book.flow = flow; book.sound = ui.GetComponent<AudioSource>(); book.stickerClip = SubmissionKidMakeover.Sound("sfx/pop");
            var old = ui.transform.Find("도감 화면"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = Full("도감 화면", ui.transform);
            var card = MenuCard(root, "분리배출 도감", "바르게 넣은 쓰레기마다 스티커가 생겨요. 한 번 모은 스티커는 계속 남아요.", new Vector2(1560, 1000), Green);
            book.countText = Text("모은 수", card, "", 30, Green, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -124), new Vector2(600, 44));
            var items = new List<WasteItem>(flow.game.wasteItems);
            items.Sort((a, b) => a.definition.ToSpec(a.instanceId).Level.CompareTo(b.definition.ToSpec(b.instanceId).Level));
            int n = items.Count;
            book.ids = new string[n]; book.pictures = new RawImage[n]; book.lockedMarks = new GameObject[n]; book.details = new GameObject[n];
            Color[] levelColours = { Green, Sky, Orange };
            for (int i = 0; i < n; i++)
            {
                var item = items[i]; var spec = item.definition.ToSpec(item.instanceId);
                int col = i % 6, row = i / 6;
                var sticker = Node("스티커 " + (i + 1), card, new Vector2(0.5f, 1), new Vector2(-620 + col * 248, -184 - row * 330), new Vector2(228, 310));
                sticker.pivot = new Vector2(0.5f, 1);
                var body = Full("판", sticker); DropShadow(body, 10, 6, 0.2f); Paint(body, Color.white, 30, true);
                var window = Node("그림 창", body, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(200, 150));
                Paint(window, Color.Lerp(levelColours[(int)spec.Level], Color.white, 0.75f), 22).gameObject.AddComponent<Mask>().showMaskGraphic = true;
                Texture2D icon; itemIcons.TryGetValue(item.instanceId, out icon);
                var picture = Node("그림", window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140)).gameObject.AddComponent<RawImage>();
                picture.texture = icon != null ? Picture(icon) : null; picture.raycastTarget = false;
                var locked = Node("물음표", window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
                FillText("글", locked, "?", 96, Color.white, StickerMaterial, 0, 0, 0, 0);
                var info = Full("설명", body, 10, 10, 10, 166);
                Line(Text("이름", info, spec.Name, 26, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(208, 36)), 16);
                string steps = "";
                foreach (var step in new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash })
                    if ((spec.Required & step) != 0) steps += (steps.Length > 0 ? " → " : "") + (step == Preparation.Empty ? "비우기" : step == Preparation.Separate ? "떼어내기" : "씻기");
                Text("방법", info, (steps.Length > 0 ? steps : "그대로") + "\n<color=#5CAE6A>" + FeedbackText.Category(spec.Category) + " 통</color>", 22, Soft, TextAlignmentOptions.Center,
                    new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(208, 90));
                var hidden = Text("아직", body, "아직 못 만났어요", 22, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(208, 40));
                hidden.transform.SetParent(locked, true);
                book.ids[i] = item.instanceId; book.pictures[i] = picture; book.lockedMarks[i] = locked.gameObject; book.details[i] = info.gameObject;
            }
            book.close = Candy("닫기", card, "닫기", Gray, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(300, 80), 34);
            book.panel = root.gameObject; root.gameObject.SetActive(false);
        }
        // "2 반짝 바닷가" card shown for a moment after travelling: region number badge, name and job.
        private static void ArrivalCard(AlphaFlow flow, KidRegions regions, Color[] colours)
        {
            var root = Node("지역 도착 카드", flow.ui.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(860, 210));
            root.pivot = new Vector2(0.5f, 0.5f); PopIn(root, 0.45f, 0.6f);
            var body = Full("판", root); DropShadow(body, 22, 12, 0.3f); Paint(body, Paper, 70);
            var badge = Node("번호", root, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(150, 150)); badge.pivot = new Vector2(0, 0.5f);
            DropShadow(badge, 10, 6, 0.25f); regions.arrivalBadge = Circle(badge, colours[0]);
            regions.arrivalNumber = FillText("글", badge, "1", 84, Color.white, SoftMaterial, 4, 4, 4, 10);
            regions.arrivalTitle = Line(Text("이름", root, "새싹 공원", 70, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(220, 30), new Vector2(600, 90), StickerMaterial), 44);
            regions.arrivalTitle.rectTransform.pivot = new Vector2(0, 0.5f);
            regions.arrivalJob = Line(Text("할 일", root, "", 32, Soft, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(222, -46), new Vector2(600, 50)), 22);
            regions.arrivalJob.rectTransform.pivot = new Vector2(0, 0.5f);
            regions.regionColors = colours; regions.arrivalCard = root.gameObject; root.gameObject.SetActive(false);
        }
        // Bird's-eye picture of a region for its map sticker, plus a faded grey version for "not saved yet".
        internal static void RegionPicture(AlphaFlow flow, int region, out Texture2D bright, out Texture2D dull)
        {
            var offset = SubmissionKidRegionsBuilder.Offset(region);
            var go = new GameObject("map camera"); go.hideFlags = HideFlags.HideAndDontSave;
            var hidden = new List<Renderer>();
            try
            {
                var cam = go.AddComponent<Camera>(); cam.CopyFrom(flow.player.viewCamera); cam.fieldOfView = 46; cam.farClipPlane = 140;
                go.transform.position = offset + new Vector3(0, 30, -36); go.transform.LookAt(offset + new Vector3(0, 0, -1));
                foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (renderer.enabled && (renderer.GetComponentInParent<PrototypePlayer>() != null || renderer.GetComponentInParent<GuideNpc>() != null || renderer.GetComponent<TMPro.TMP_Text>() != null))
                    { renderer.enabled = false; hidden.Add(renderer); }
                // Twice the card's 1080p size, so the map stays sharp on 1440p and 4K screens.
                int width = 640 * Detail, height = 390 * Detail;
                var rt = new RenderTexture(width, height, 24) { antiAliasing = 8 }; cam.targetTexture = rt; cam.Render();
                var previous = RenderTexture.active; RenderTexture.active = rt;
                var picture = new Texture2D(width, height, TextureFormat.RGB24, false); picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply();
                RenderTexture.active = previous; cam.targetTexture = null; Object.DestroyImmediate(rt);
                var grey = new Texture2D(width, height, TextureFormat.RGB24, false);
                var pixels = picture.GetPixels();
                for (int p = 0; p < pixels.Length; p++)
                {
                    float g = pixels[p].grayscale;
                    pixels[p] = Color.Lerp(pixels[p], new Color(g, g, g), 0.75f) * 0.82f + new Color(0.08f, 0.08f, 0.09f);
                }
                grey.SetPixels(pixels); grey.Apply();
                bright = Save(picture, "Region_" + (region + 1)); dull = Save(grey, "Region_" + (region + 1) + "_dull");
            }
            finally { foreach (var r in hidden) if (r != null) r.enabled = true; Object.DestroyImmediate(go); }
        }
        private static Texture2D Save(Texture2D picture, string name)
        {
            string path = Root + "/" + name + ".png";
            System.IO.File.WriteAllBytes(path, picture.EncodeToPNG()); Object.DestroyImmediate(picture);
            UnityEditor.AssetDatabase.ImportAsset(path);
            return Picture(UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }

        // ---------- helpers ----------
        private static TextMeshProUGUI Line(TextMeshProUGUI text, float min)
        {
            Fit(text, min); text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        /// <summary>Pill whose width follows its content (anchored by its pivot corner), with a soft shadow and an optional lip.</summary>
        private static RectTransform Hug(string name, Transform parent, Vector2 anchor, Vector2 position, float height, Color color, int left, int right,
            out HorizontalLayoutGroup layout, bool lip = true)
        {
            var rect = Node(name, parent, anchor, position, new Vector2(200, height));
            layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(left, right, 0, lip ? 7 : 0); layout.spacing = 12; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true; layout.childControlHeight = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            rect.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var shadow = Full("그림자", rect, -16, -16 - 6, -16, -16 + 6);
            var shadowImage = shadow.gameObject.AddComponent<Image>(); shadowImage.sprite = ShadowSprite; shadowImage.type = Image.Type.Sliced;
            shadowImage.color = new Color(0.04f, 0.12f, 0.1f, 0.24f); shadowImage.raycastTarget = false;
            shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            if (lip) { var under = Full("아래", rect); Paint(under, Darker(color, 0.8f), Mathf.Min(height / 2, 40)); under.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; }
            var face = Full("면", rect, 0, lip ? 7 : 0, 0, 0); Paint(face, color, Mathf.Min(height / 2, 40));
            face.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return rect;
        }
    }
}
#endif
