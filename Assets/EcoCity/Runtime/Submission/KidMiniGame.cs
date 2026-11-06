using EcoCity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity
{
    // One mini game booth per region, each with its own game on the same card: six quick picture questions,
    // a shrinking timer bar, O/X feedback and up to three stars. It pauses the world while it is open and
    // never changes the saved mission progress (only each game's best score is remembered).
    //   Bins      (park, "번개 분리수거")   which bin does this go in?          three bins
    //   FirstStep (beach, "먼저 뭐 할까?")  what do you do first with this?     empty / remove / wash / straight in
    //   TrueFalse (market, "맞아요 틀려요") is this sentence about the picture right?  O or X
    public sealed class KidMiniGame : MonoBehaviour
    {
        public enum Kind { Bins, FirstStep, TrueFalse }
        public const string BestKey = "EcoCity.MiniGame.Best";
        public AlphaFlow flow;
        public KidVoice voice;
        public GameObject panel, intro, question, result;
        public Button startButton, againButton, introClose, resultClose, quitButton;
        public Button[] choices;
        public Image[] choiceFaces;
        public TMP_Text[] choiceLabels;
        public RawImage picture;
        public TMP_Text progressText, scoreText, resultText, bestText, markText;
        public Image timerFill, markBadge;
        public Image[] resultStars;
        public Texture2D[] pictures;
        public int[] pictureCategories;
        public string[] categoryNames;
        public Color[] categoryColors;
        [Header("Per game (Bins, FirstStep, TrueFalse)")]
        [Tooltip("Card title, intro line and the line asked above each picture.")]
        public TMP_Text titleText, introText, questionText;
        public string[] titles = { "번개 분리수거!", "먼저 뭐 할까?", "맞아요 틀려요!" };
        public string[] introLines =
        {
            "그림을 보고 맞는 통을 골라요!",
            "그림을 보고 제일 먼저 할 일을 골라요!",
            "그림과 글을 보고 맞으면 O, 틀리면 X!",
        };
        public string[] asks = { "어느 통에 넣을까요?", "제일 먼저 무엇을 할까요?", "" };
        public string[] stepNames = { "비우기", "떼어내기", "씻기", "바로 넣기" };
        public Color[] stepColors = { new Color(0.55f, 0.75f, 1f), new Color(1f, 0.72f, 0.5f), new Color(0.5f, 0.85f, 0.9f), new Color(0.55f, 0.85f, 0.6f) };
        public Color trueColor = new Color(0.45f, 0.82f, 0.6f), falseColor = new Color(1f, 0.6f, 0.66f);
        public int rounds = 6;
        [Tooltip("Young children need time to look and think; the bar still adds a little excitement.")]
        public float secondsPerQuestion = 10f;
        public Color starOn = new Color(1f, 0.78f, 0.25f), starOff = new Color(0.88f, 0.86f, 0.94f);
        public Kind Current { get; private set; }
        // Sentences for "맞아요 틀려요": the item whose picture is shown, the sentence and whether it is right.
        private static readonly string[] FactItems = { "pet-02", "container-01", "glass-02", "can-02", "box-02", "cup-01", "food-01", "paper-01", "glass-01" };
        private static readonly string[] FactLines =
        {
            "페트병의 비닐 라벨은 떼고 넣어요", "음식이 남은 통은 그대로 넣어도 돼요", "소스 묻은 유리병은 비우고 씻어서 넣어요",
            "음료가 남은 캔은 그냥 넣어도 돼요", "택배 상자의 테이프는 떼고 종이 통에 넣어요", "빨대 꽂힌 컵은 빨대를 꽂은 채로 넣어요",
            "먹고 남은 사과 속은 플라스틱 통에 넣어요", "깨끗한 종이 상자는 종이 통에 넣어요", "깨끗한 유리병은 유리 통에 넣어요",
        };
        private static readonly bool[] FactTrue = { true, false, true, false, true, false, false, true, true };
        private int[] order, options = new int[3];
        private Vector2[] choiceHome, shadowOffset;
        private RectTransform[] choiceShadows;
        private Texture2D[] poolPictures;
        private int[] poolAnswers;
        private int round, correct, answer;
        private float questionStart, feedbackUntil;
        private bool waiting;
        public bool IsOpen { get { return panel != null && panel.activeSelf; } }

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(Begin);
            if (againButton != null) againButton.onClick.AddListener(Begin);
            foreach (var close in new[] { introClose, resultClose, quitButton }) if (close != null) close.onClick.AddListener(Close);
            for (int i = 0; i < choices.Length; i++) { int index = i; choices[i].onClick.AddListener(() => Choose(index)); }
            choiceHome = new Vector2[choices.Length]; choiceShadows = new RectTransform[choices.Length]; shadowOffset = new Vector2[choices.Length];
            for (int i = 0; i < choices.Length; i++)
            {
                var rect = (RectTransform)choices[i].transform;
                choiceHome[i] = rect.anchoredPosition;
                // Candy buttons carry their drop shadow as a sibling ("<name> 그림자"); it moves and hides with the button.
                choiceShadows[i] = rect.parent != null ? rect.parent.Find(rect.name + " 그림자") as RectTransform : null;
                if (choiceShadows[i] != null) shadowOffset[i] = choiceShadows[i].anchoredPosition - rect.anchoredPosition;
            }
            if (panel != null) panel.SetActive(false);
        }
        public string Title(Kind kind) { return titles != null && (int)kind < titles.Length ? titles[(int)kind] : ""; }
        public void Open() { Open(Kind.Bins); }
        public void Open(Kind kind)
        {
            if (IsOpen || flow == null || flow.Screens.Current != AlphaScreen.Playing) return;
            Current = kind;
            // The world waits under the mini game, like the pause menu.
            flow.Pause(); flow.ui.externalOverlay = true; flow.ui.Refresh();
            panel.SetActive(true); panel.transform.SetAsLastSibling();
            if (titleText != null) titleText.text = Title(kind);
            if (introText != null && (int)kind < introLines.Length)
                introText.text = introLines[(int)kind] + "\n<size=80%><color=#8E7D93>" + rounds + "문제 · 숫자 키나 마우스로 골라요 · 빨리빨리!</color></size>";
            Show(intro);
        }
        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            flow.ui.externalOverlay = false;
            if (flow.Screens.Current == AlphaScreen.Paused) flow.Resume();
            else flow.ui.Refresh();
        }
        private void Update()
        {
            if (!IsOpen) return;
            // Esc (or anything else leaving the pause state) closes the mini game.
            if (flow.Screens.Current != AlphaScreen.Paused) { panel.SetActive(false); flow.ui.externalOverlay = false; flow.ui.Refresh(); return; }
            if (!question.activeSelf) { if (PrototypeInput.ConfirmPressed) Begin(); return; }
            float now = Time.unscaledTime;
            if (waiting)
            {
                if (now >= feedbackUntil) { waiting = false; Next(); }
                return;
            }
            float left = 1 - (now - questionStart) / secondsPerQuestion;
            if (timerFill != null) timerFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(left), 1);
            if (left <= 0) { Feedback(false, "시간 끝!"); return; }
            int key = PrototypeInput.NumberPressed;
            if (key >= 1 && key <= OptionCount) Choose(key - 1);
        }
        private int OptionCount { get { return Current == Kind.TrueFalse ? 2 : 3; } }
        public void Begin()
        {
            // A game closed during the O/X feedback must not skip the first question of the next one.
            round = 0; correct = 0; waiting = false;
            BuildPool();
            order = new int[poolPictures.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int swap = order[i]; order[i] = order[j]; order[j] = swap; }
            Show(question);
            Ask();
        }
        // Pictures and right answers for the current game. Bins uses its prepared pictures; the other two use the
        // pictures of the items in the world (the same ones as the held-item card).
        private void BuildPool()
        {
            if (Current == Kind.Bins || flow == null || flow.game == null)
            {
                poolPictures = pictures; poolAnswers = pictureCategories; return;
            }
            var hud = flow.ui != null ? flow.ui.GetComponent<KidHud>() : null;
            var shown = new System.Collections.Generic.List<Texture2D>(); var answers = new System.Collections.Generic.List<int>();
            if (Current == Kind.FirstStep)
            {
                foreach (var item in flow.game.wasteItems)
                {
                    var icon = hud != null ? hud.Find(item.instanceId) : null; if (icon == null) continue;
                    Preparation first = WasteRules.NextStep(item.definition.ToSpec(item.instanceId).Required);
                    shown.Add(icon); answers.Add(first == Preparation.Empty ? 0 : first == Preparation.Separate ? 1 : first == Preparation.Wash ? 2 : 3);
                }
            }
            else
            {
                for (int i = 0; i < FactItems.Length; i++)
                {
                    var icon = hud != null ? hud.Find(FactItems[i]) : null; if (icon == null) continue;
                    // The answer keeps the fact's index too: 0/1 is O/X, the sentence is looked up when asked.
                    shown.Add(icon); answers.Add(i * 2 + (FactTrue[i] ? 0 : 1));
                }
            }
            poolPictures = shown.ToArray(); poolAnswers = answers.ToArray();
        }
        private void Ask()
        {
            int index = order[round % order.Length];
            picture.texture = poolPictures[index];
            int count = OptionCount;
            string ask = (int)Current < asks.Length ? asks[(int)Current] : "";
            if (Current == Kind.TrueFalse)
            {
                answer = poolAnswers[index] % 2;
                ask = FactLines[poolAnswers[index] / 2];
                options[0] = 0; options[1] = 1;
            }
            else
            {
                answer = poolAnswers[index];
                int choicesFrom = Current == Kind.Bins ? categoryNames.Length : stepNames.Length;
                // The right answer plus different wrong ones, in random places.
                options[0] = answer;
                for (int slot = 1; slot < count; slot++)
                {
                    int pick;
                    do pick = Random.Range(0, choicesFrom); while (System.Array.IndexOf(options, pick, 0, slot) >= 0);
                    options[slot] = pick;
                }
                for (int i = count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int swap = options[i]; options[i] = options[j]; options[j] = swap; }
            }
            if (questionText != null) questionText.text = ask;
            for (int i = 0; i < choices.Length; i++)
            {
                bool used = i < count;
                choices[i].gameObject.SetActive(used);
                if (choiceShadows[i] != null) choiceShadows[i].gameObject.SetActive(used);
                if (!used) continue;
                // Two buttons (O / X) sit closer together around the middle.
                var rect = (RectTransform)choices[i].transform;
                rect.anchoredPosition = count == 2 ? new Vector2(i == 0 ? -170 : 170, choiceHome[i].y) : choiceHome[i];
                if (choiceShadows[i] != null) choiceShadows[i].anchoredPosition = rect.anchoredPosition + shadowOffset[i];
                choiceFaces[i].color = OptionColor(options[i]);
                choiceLabels[i].text = "<size=70%>" + (i + 1) + "</size>  " + OptionName(options[i]);
                choices[i].interactable = true;
            }
            if (progressText != null) progressText.text = (round + 1) + " / " + rounds;
            if (scoreText != null) scoreText.text = "★ " + correct;
            if (markBadge != null) markBadge.gameObject.SetActive(false);
            questionStart = Time.unscaledTime;
        }
        private string OptionName(int option)
        {
            if (Current == Kind.Bins) return categoryNames[option];
            if (Current == Kind.FirstStep) return stepNames[option];
            return option == 0 ? "O  맞아요" : "X  틀려요";
        }
        private Color OptionColor(int option)
        {
            if (Current == Kind.Bins) return categoryColors[option];
            if (Current == Kind.FirstStep) return stepColors[option];
            return option == 0 ? trueColor : falseColor;
        }
        private void Choose(int index)
        {
            if (!question.activeSelf || waiting || index < 0 || index >= OptionCount) return;
            bool right = options[index] == answer;
            if (right) correct++;
            string wrong = Current == Kind.Bins ? OptionName(answer) + " 통이에요" : Current == Kind.FirstStep ? "먼저 '" + OptionName(answer) + "'!" : (answer == 0 ? "맞는 말이에요" : "틀린 말이에요");
            Feedback(right, right ? "딩동댕!" : wrong);
        }
        private void Feedback(bool right, string text)
        {
            waiting = true; feedbackUntil = Time.unscaledTime + (right ? 0.8f : 1.4f);
            // Only the right answer stays bright, so a wrong answer still shows what was right.
            for (int i = 0; i < OptionCount; i++) choices[i].interactable = options[i] == answer;
            if (markBadge != null)
            {
                markBadge.gameObject.SetActive(true);
                markBadge.color = right ? new Color(0.36f, 0.8f, 0.62f) : new Color(1f, 0.5f, 0.55f);
                markText.text = (right ? "O" : "X") + "\n<size=28%>" + text + "</size>";
            }
            if (scoreText != null) scoreText.text = "★ " + correct;
            if (voice != null) voice.Say(right ? "correct" : "wrong", true);
        }
        /// <summary>Best score key per game; the park game keeps the original key.</summary>
        public static string BestKeyFor(Kind kind) { return kind == Kind.Bins ? BestKey : BestKey + "." + kind; }
        private void Next()
        {
            round++;
            if (round < rounds) { Ask(); return; }
            int stars = correct >= rounds ? 3 : correct >= rounds - 2 ? 2 : correct >= 2 ? 1 : 0;
            string key = BestKeyFor(Current);
            int best = Mathf.Max(PlayerPrefs.GetInt(key, 0), correct);
            PlayerPrefs.SetInt(key, best); PlayerPrefs.Save();
            for (int i = 0; i < resultStars.Length; i++) resultStars[i].color = i < stars ? starOn : starOff;
            resultText.text = rounds + "문제 중 <color=#7B61F0>" + correct + "개</color> 맞혔어요!";
            bestText.text = (stars == 3 ? "완벽해요! 분리수거 박사!" : stars == 2 ? "정말 잘했어요!" : stars == 1 ? "좋아요! 한 번 더 하면 별이 늘어나요." : "괜찮아요! 그림을 잘 보고 다시 해 봐요.") +
                "   ·   최고 기록 " + best + "개";
            Show(result);
        }
        private void Show(GameObject state)
        {
            foreach (var part in new[] { intro, question, result }) if (part != null) part.SetActive(part == state);
        }
    }
}
