using System.Collections;
using EcoCity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity
{
    // Three regions of Eco City, one per learning stage, laid out far apart in the same scene.
    // A pop-up book map opens when a round starts, after each region is saved and from the pause menu;
    // the player picks an unlocked region and is carried there. Saving a region plays a short "region saved"
    // scene (KidFinale) and returns to the map, where its sticker turns bright; the last region plays the full ending.
    public sealed class KidRegions : MonoBehaviour
    {
        public AlphaFlow flow;
        public KidFinale finale;
        public KidVoice voice;
        public Transform helper;
        [Tooltip("Per region: where the player arrives, where the helper waits and the region centre.")]
        public Transform[] spawns;
        public Vector3[] helperSpots, centres;
        public string[] regionNames = { "새싹 공원", "반짝 바닷가", "알록달록 시장" };
        [Tooltip("Banner line when a region is saved (the last one uses the finale's own title).")]
        public string[] savedTitles = { "새싹 공원을 구했어요!", "반짝 바닷가를 구했어요!", "알록달록 시장을 구했어요!" };
        public string[] levelNames = { "새싹 공원 · 깨끗한 쓰레기", "반짝 바닷가 · 비우고 씻기", "알록달록 시장 · 떼어내고 버리기" };
        public float spacing = 220f, regionSavedSeconds = 6.5f;
        [Header("Map (on the game canvas)")]
        public GameObject map;
        public Button[] regionButtons;
        public RectTransform[] cards;
        public RawImage[] pictures;
        public Texture2D[] brightPictures, dullPictures;
        public GameObject[] lockMarks, savedMarks, hereMarks;
        public Button mapFromPause;
        public AudioSource sound;
        public AudioClip clickClip, lockedClip, stampClip;
        [Header("Arrival card: number, name and job of the region just entered")]
        public GameObject arrivalCard;
        public TMPro.TMP_Text arrivalNumber, arrivalTitle, arrivalJob;
        public Image arrivalBadge;
        public Color[] regionColors;
        public string[] regionJobs = { "깨끗한 쓰레기 5개를 찾아 바로 넣어요", "비우고 씻어서 넣어요 · 4개", "떼어내고 넣어요 · 3개" };
        public float arrivalSeconds = 2.6f;
        private float arrivalUntil;
        [Header("Stars: no mistakes 3, one or two 2, more 1 (shown on the map once a region is saved)")]
        public GameObject[] starRows;
        public Image[] starsRegion1, starsRegion2, starsRegion3;
        public TMPro.TMP_Text totalText;
        public Color starOn = new Color(1f, 0.78f, 0.25f), starOff = new Color(0.86f, 0.84f, 0.9f);
        public const string MistakesKey = "EcoCity.Kid.Mistakes";
        private readonly int[] mistakes = new int[3];
        private readonly bool[] saved = new bool[3];
        private int current, justSaved = -1, shake = -1;
        private float shakeAt, openedAt;
        private AlphaScreen lastScreen = AlphaScreen.Title;
        private bool busy, fromPause;
        public int Current { get { return current; } }
        public bool MapOpen { get { return map != null && map.activeSelf; } }

        private void Awake()
        {
            FeedbackText.LevelNames = levelNames;
            starSummary = ResultStars; AlphaUi.StarSummary = starSummary;
            for (int i = 0; i < regionButtons.Length; i++) { int index = i; regionButtons[i].onClick.AddListener(() => Choose(index)); }
            if (mapFromPause != null) mapFromPause.onClick.AddListener(() => { OpenMap(); fromPause = MapOpen; });
            if (map != null) map.SetActive(false);
        }
        private void OnEnable()
        {
            if (flow == null) return;
            flow.ScreenChanged += OnScreenChanged;
            if (flow.game != null) { flow.game.StateChanged += OnStateChanged; flow.game.ResultPublished += OnResult; }
            LoadMistakes();
        }
        private void OnDisable()
        {
            if (flow == null) return;
            flow.ScreenChanged -= OnScreenChanged;
            if (flow.game != null) { flow.game.StateChanged -= OnStateChanged; flow.game.ResultPublished -= OnResult; }
        }
        /// <summary>Stars for a saved region (0 while it is not saved).</summary>
        public int Stars(int region)
        {
            if (RegionClean(region) < 1) return 0;
            int m = mistakes[region];
            return m == 0 ? 3 : m <= 2 ? 2 : 1;
        }
        public int TotalStars { get { return Stars(0) + Stars(1) + Stars(2); } }
        // A mistake is any wrong judgement with an item in hand: the wrong bin, a missing step, steps out of order
        // (washing before emptying) or a step the item does not need.
        public static bool IsMistake(ResultCode code)
        {
            return code == ResultCode.WrongBin || code == ResultCode.MissingPreparation || code == ResultCode.PrerequisiteMissing || code == ResultCode.NotRequired;
        }
        private void OnResult(EcoCity.Core.ActionResult result)
        {
            var held = flow.game.Session != null ? flow.game.Session.Held : null;
            if (held == null || !IsMistake(result.Code)) return;
            mistakes[(int)held.Level]++;
            SaveMistakes();
        }
        private void LoadMistakes()
        {
            var parts = PlayerPrefs.GetString(MistakesKey, "0,0,0").Split(',');
            for (int i = 0; i < 3; i++) { int value; mistakes[i] = i < parts.Length && int.TryParse(parts[i], out value) ? value : 0; }
        }
        private void SaveMistakes() { PlayerPrefs.SetString(MistakesKey, mistakes[0] + "," + mistakes[1] + "," + mistakes[2]); PlayerPrefs.Save(); }
        private void OnDestroy()
        {
            if (FeedbackText.LevelNames == levelNames) FeedbackText.LevelNames = null;
            if (AlphaUi.StarSummary == starSummary) AlphaUi.StarSummary = null;
        }
        private System.Func<string> starSummary;
        // Results screen: the same stars as the map, per region, and what they earned.
        private string ResultStars()
        {
            string line = "";
            for (int i = 0; i < 3; i++)
            {
                int stars = Stars(i);
                // Last word of the name ("새싹 공원" → "공원") keeps the three regions on one line.
                string name = regionNames[i].Substring(regionNames[i].LastIndexOf(' ') + 1);
                line += (i > 0 ? "  ·  " : "") + name + " " + new string('★', stars) + new string('☆', 3 - stars);
            }
            int total = TotalStars;
            string reward = total >= 9 ? "  왕관!" : total >= 5 ? "  고깔모자!" : "";
            // The first line is drawn large by AlphaUi, so it stays short.
            return "★ " + total + " / 9" + reward + "\n" + line;
        }

        /// <summary>Region the player stands in, from its position along the row of regions.</summary>
        public int RegionAt(Vector3 position) { return Mathf.Clamp(Mathf.RoundToInt(position.x / spacing), 0, 2); }
        /// <summary>0..1 cleanliness of one region (its stage's share of cleared items; later stages count as dirty).</summary>
        public float RegionClean(int region)
        {
            var session = flow != null && flow.game != null ? flow.game.Session : null;
            if (session == null) return 0;
            var level = (LearningLevel)region;
            int goal = session.Goal(level);
            return goal <= 0 ? 1 : Mathf.Clamp01(session.Cleared(level) / (float)goal);
        }
        public bool Unlocked(int region)
        {
            var session = flow.game.Session;
            return session != null && (session.IsComplete || region <= (int)session.CurrentLevel);
        }

        private void OnScreenChanged()
        {
            AlphaScreen screen = flow.Screens.Current;
            // A round begins (new game, continue, play again): start at the map.
            if (screen == AlphaScreen.Playing && (lastScreen == AlphaScreen.Title || lastScreen == AlphaScreen.Results || lastScreen == AlphaScreen.ConfirmNewGame) && !busy)
            {
                // A brand-new game starts with no mistakes counted.
                if (flow.game.Session != null && flow.game.Session.DisposedCount == 0 && flow.game.Session.Held == null) { for (int i = 0; i < 3; i++) mistakes[i] = 0; SaveMistakes(); }
                for (int i = 0; i < 3; i++) saved[i] = RegionClean(i) >= 1;
                current = RegionAt(flow.player.transform.position);
                if (flow.game.Session != null && !flow.game.Session.IsComplete) OpenMap();
            }
            lastScreen = screen;
        }
        private void OnStateChanged()
        {
            if (flow.game.Session == null || busy) return;
            for (int i = 0; i < 3; i++)
            {
                bool now = RegionClean(i) >= 1;
                if (now && !saved[i])
                {
                    saved[i] = true;
                    // The last region uses the full ending (AlphaFlow.Completing → KidFinale).
                    if (!flow.game.Session.IsComplete) StartCoroutine(RegionSaved(i));
                }
                else if (!now) saved[i] = false;
            }
        }
        private IEnumerator RegionSaved(int region)
        {
            busy = true;
            flow.player.inputBlocked = true; flow.interactor.inputBlocked = true;
            if (finale != null) finale.PlayRegion(centres[region], savedTitles[region]);
            float until = Time.unscaledTime + regionSavedSeconds;
            // A pause (Esc, or the window losing focus) does not cut the scene short: the map still follows it.
            while (Time.unscaledTime < until && InRound()) yield return null;
            if (finale != null) finale.StopRegion();
            busy = false; justSaved = region;
            while (flow.Screens.Current == AlphaScreen.Help) yield return null;
            if (flow.Screens.Current == AlphaScreen.Playing || flow.Screens.Current == AlphaScreen.Paused) OpenMap();
            else { flow.player.inputBlocked = false; flow.interactor.inputBlocked = false; }
        }
        private bool InRound()
        {
            var screen = flow.Screens.Current;
            return screen == AlphaScreen.Playing || screen == AlphaScreen.Paused || screen == AlphaScreen.Help;
        }
        public void OpenMap()
        {
            if (map == null || MapOpen || flow.game.Session == null) return;
            fromPause = false;
            if (flow.Screens.Current == AlphaScreen.Playing) flow.Pause();
            if (flow.Screens.Current != AlphaScreen.Paused) return;
            flow.ui.externalOverlay = true; flow.ui.Refresh();
            map.SetActive(true); map.transform.SetAsLastSibling(); openedAt = Time.unscaledTime;
            Refresh();
            if (voice != null) voice.Say(justSaved >= 0 ? "map_saved" : "map_pick", true);
            if (justSaved >= 0 && sound != null && stampClip != null) sound.PlayOneShot(stampClip);
        }
        private void Refresh()
        {
            for (int i = 0; i < 3 && i < cards.Length; i++)
            {
                bool open = Unlocked(i), done = RegionClean(i) >= 1;
                if (pictures[i] != null) { pictures[i].texture = done ? brightPictures[i] : dullPictures[i]; pictures[i].color = open ? Color.white : new Color(0.8f, 0.8f, 0.85f); }
                if (lockMarks[i] != null) lockMarks[i].SetActive(!open);
                if (savedMarks[i] != null) savedMarks[i].SetActive(done);
                if (hereMarks[i] != null) hereMarks[i].SetActive(open && !done);
                regionButtons[i].interactable = true;
                var row = i == 0 ? starsRegion1 : i == 1 ? starsRegion2 : starsRegion3;
                if (starRows != null && i < starRows.Length && starRows[i] != null) starRows[i].SetActive(done);
                if (row != null) for (int s = 0; s < row.Length; s++) if (row[s] != null) row[s].color = s < Stars(i) ? starOn : starOff;
            }
            if (totalText != null)
            {
                int total = TotalStars;
                totalText.text = "모은 별 ★ " + total + " / 9   ·   " + (total >= 9 ? "왕관을 받았어요!" : total >= 5 ? "고깔모자를 받았어요!  별 9개면 왕관" : "별 5개면 고깔모자, 9개면 왕관");
            }
        }
        private void Update()
        {
            if (arrivalCard != null && arrivalCard.activeSelf && (Time.unscaledTime >= arrivalUntil || MapOpen)) arrivalCard.SetActive(false);
            if (!MapOpen) return;
            // Esc (Resume) closes the map and keeps playing where the player stands.
            if (flow.Screens.Current != AlphaScreen.Paused) { map.SetActive(false); flow.ui.externalOverlay = false; flow.ui.Refresh(); justSaved = -1; return; }
            float t = Time.unscaledTime;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                bool next = hereMarks[i] != null && hereMarks[i].activeSelf;
                float s = next ? 1 + Mathf.Sin(t * 4) * 0.035f : 1;
                // The region just saved pops once; a locked one shakes when tapped.
                if (i == justSaved) { float b = (t - openedAt) / 0.6f; if (b < 1) s *= 1 + Mathf.Sin(b * Mathf.PI) * 0.18f; }
                cards[i].localScale = Vector3.one * s;
                float wobble = i == shake && t - shakeAt < 0.4f ? Mathf.Sin((t - shakeAt) * 50) * 6 * (1 - (t - shakeAt) / 0.4f) : 0;
                cards[i].localRotation = Quaternion.Euler(0, 0, (i == 1 ? 2 : -2) + wobble);
            }
            int key = PrototypeInput.NumberPressed;
            if (key >= 1 && key <= 3) Choose(key - 1);
        }
        public void Choose(int region)
        {
            if (!MapOpen) return;
            if (!Unlocked(region))
            {
                shake = region; shakeAt = Time.unscaledTime;
                if (sound != null && lockedClip != null) sound.PlayOneShot(lockedClip);
                if (voice != null) voice.Say("map_locked", true);
                return;
            }
            if (sound != null && clickClip != null) sound.PlayOneShot(clickClip);
            // Picking the region the player is already exploring (map from the pause menu) keeps them where they stand.
            bool stay = fromPause && justSaved < 0 && region == RegionAt(flow.player.transform.position);
            map.SetActive(false); flow.ui.externalOverlay = false; justSaved = -1;
            if (!stay) { Travel(region); ShowArrival(region); }
            if (flow.Screens.Current == AlphaScreen.Paused) flow.Resume(); else flow.ui.Refresh();
        }
        private void ShowArrival(int region)
        {
            if (arrivalCard == null) return;
            if (arrivalNumber != null) arrivalNumber.text = (region + 1).ToString();
            if (arrivalTitle != null) arrivalTitle.text = regionNames[region];
            if (arrivalJob != null && regionJobs != null && region < regionJobs.Length) arrivalJob.text = regionJobs[region];
            if (arrivalBadge != null && regionColors != null && region < regionColors.Length) arrivalBadge.color = regionColors[region];
            flow.ClearCelebration();
            arrivalCard.SetActive(false); arrivalCard.SetActive(true); arrivalCard.transform.SetAsLastSibling();
            arrivalUntil = Time.unscaledTime + arrivalSeconds;
        }
        /// <summary>Carries the player (and the helper) to a region.</summary>
        public void Travel(int region)
        {
            current = region;
            if (spawns != null && region < spawns.Length && spawns[region] != null) flow.player.ResetPose(spawns[region].position, spawns[region].rotation);
            if (helper != null && helperSpots != null && region < helperSpots.Length) helper.position = new Vector3(helperSpots[region].x, helper.position.y, helperSpots[region].z);
            if (finale != null && centres != null && region < centres.Length) finale.parkCentre = centres[region];
            var mood = GetComponent<KidEnvironmentMood>(); if (mood != null) mood.Snap();
        }
    }
}
