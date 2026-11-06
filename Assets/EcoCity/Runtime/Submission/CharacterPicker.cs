using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoCity
{
    // Character choice scene (after "시작하기" on the title): both friends stand on round stages in 3D and idle.
    // Click a friend or its name card (or press ← →) to choose; the chosen one turns to the camera, hops and glows.
    // "이 친구랑 시작!" stores the choice for CharacterSelection in the game scene and loads the game.
    public sealed class CharacterPicker : MonoBehaviour
    {
        public Camera view;
        public Transform[] models;
        public Animator[] animators;
        public Renderer[] rings;
        public Button[] cards;
        public Image[] cardFaces;
        public GameObject[] checks;
        public Button startButton, backButton;
        public AudioSource sound;
        public AudioClip clickClip, welcomeClip;
        public Color ringOn = new Color(0.72f, 0.62f, 1f), ringOff = new Color(0.9f, 0.88f, 0.96f);
        public Color cardOn = new Color(0.64f, 0.55f, 1f), cardOff = Color.white;
        private int current = -1;
        private Quaternion[] restRotation;
        private Vector3[] restPosition;
        private float chosenAt;

        private void Start()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            restRotation = new Quaternion[models.Length]; restPosition = new Vector3[models.Length];
            for (int i = 0; i < models.Length; i++) { restRotation[i] = models[i].rotation; restPosition[i] = models[i].position; }
            for (int i = 0; i < cards.Length; i++) { int index = i; cards[i].onClick.AddListener(() => Choose(index, true)); }
            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (backButton != null) backButton.onClick.AddListener(Back);
            var volume = TitleMenu.LoadPreferences().volume;
            if (sound != null) { sound.volume = volume; if (KidVoice.Enabled && welcomeClip != null) sound.PlayOneShot(welcomeClip); }
            Choose(PlayerPrefs.GetInt(CharacterSelection.PreferenceKey, 0), false);
        }
        private void Update()
        {
            foreach (var animator in animators)
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    // Stand still on the ground: the locomotion controller then plays its idle loop.
                    animator.SetFloat("Speed", 0); animator.SetFloat("MotionSpeed", 1); animator.SetBool("Grounded", true);
                }
            float since = Time.time - chosenAt;
            for (int i = 0; i < models.Length; i++)
            {
                bool chosen = i == current;
                Quaternion face = chosen ? Quaternion.LookRotation(Flat(view.transform.position - models[i].position)) : restRotation[i];
                models[i].rotation = Quaternion.Slerp(models[i].rotation, face, Time.deltaTime * 6);
                float hop = chosen && since < 0.45f ? Mathf.Sin(since / 0.45f * Mathf.PI) * 0.25f : 0;
                models[i].position = restPosition[i] + Vector3.up * hop;
                float scale = Mathf.Lerp(models[i].localScale.x, chosen ? 1.08f : 0.94f, Time.deltaTime * 8);
                models[i].localScale = Vector3.one * scale;
            }
            if (PrototypeInput.LeftPressed) Choose(0, true);
            if (PrototypeInput.RightPressed) Choose(1, true);
            if (PrototypeInput.ConfirmPressed) StartGame();
            if (PrototypeInput.EscapePressed) Back();
            if (PrototypeInput.ClickPressed && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) && view != null)
            {
                // Clicking the 3D friend itself also chooses it.
                if (Physics.Raycast(view.ScreenPointToRay(PrototypeInput.PointerPosition), out RaycastHit hit, 100))
                    for (int i = 0; i < models.Length; i++) if (hit.transform.IsChildOf(models[i]) || hit.transform == models[i]) Choose(i, true);
            }
        }
        public void Choose(int index, bool remember)
        {
            index = Mathf.Clamp(index, 0, models.Length - 1);
            if (index == current) return;
            current = index; chosenAt = Time.time;
            for (int i = 0; i < models.Length; i++)
            {
                bool on = i == current;
                if (rings != null && i < rings.Length && rings[i] != null) rings[i].material.color = on ? ringOn : ringOff;
                if (cardFaces != null && i < cardFaces.Length && cardFaces[i] != null) cardFaces[i].color = on ? cardOn : cardOff;
                if (checks != null && i < checks.Length && checks[i] != null) checks[i].SetActive(on);
            }
            if (remember) { Save(); Click(); }
        }
        public int Current { get { return current; } }
        private void Save() { PlayerPrefs.SetInt(CharacterSelection.PreferenceKey, current); PlayerPrefs.Save(); }
        public void StartGame()
        {
            Save(); Click();
            if (GameStartRequest.Pending == GameStartRequest.Mode.None) GameStartRequest.Pending = GameStartRequest.Mode.NewGame;
            GameStartRequest.LaunchedFromTitle = true;
            KidLoading.Load(GameStartRequest.GameScene);
        }
        public void Back() { Click(); GameStartRequest.Pending = GameStartRequest.Mode.None; KidLoading.Load(GameStartRequest.TitleScene); }
        private void Click() { if (sound != null && clickClip != null) sound.PlayOneShot(clickClip); }
        private static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 0.0001f ? Vector3.back : v; }
    }
}
