using System.Collections.Generic;
using System.IO;
using EcoCity.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoCity
{
    // Full-screen title scene: start (then the character scene) / continue / help / settings / quit.
    // Saves and settings use the same keys as the game scene, so both stay in sync.
    public sealed class TitleMenu : MonoBehaviour
    {
        public const string SettingsKey = "EcoCity.Submission.Settings.v1";
        public string saveSlot = "eco-city-alpha-v1";
        public Button startButton, continueButton, helpButton, settingsButton, quitButton;
        public Button confirmYes, confirmNo, helpClose, settingsClose;
        public GameObject confirmPanel, helpPanel, settingsPanel;
        public Button[] characterButtons;
        public Image[] characterFrames;
        public Slider volume, sensitivity;
        public TMPro.TMP_Text volumeValue, sensitivityValue, musicValue;
        public Slider music;
        [Tooltip("만든 사람들: team, fonts, asset packs and their licences.")]
        public Button creditsButton, creditsClose;
        public GameObject creditsPanel;
        public RawImage background;
        public RectTransform logo;
        public RectTransform[] leaves;
        public AudioSource sound;
        public AudioClip clickClip, welcomeClip;
        public Color selectedColor = new Color(1f, 0.8f, 0.2f), idleColor = Color.white;
        private readonly List<KeyValuePair<Button, UnityAction>> listeners = new List<KeyValuePair<Button, UnityAction>>();
        private SubmissionPreferences preferences;
        private Vector2 logoBase;
        private Vector2[] leafBase;

        public bool HasSave
        {
            get
            {
                string slot = saveSlot;
                foreach (char c in Path.GetInvalidFileNameChars()) slot = slot.Replace(c, '_');
                return File.Exists(Path.Combine(Application.persistentDataPath, "EcoCity", slot + ".json"));
            }
        }
        private void OnEnable()
        {
            Unbind();
            Listen(startButton, StartPressed); Listen(continueButton, () => Launch(GameStartRequest.Mode.Continue));
            Listen(helpButton, () => Show(helpPanel, true)); Listen(helpClose, () => Show(helpPanel, false));
            Listen(settingsButton, () => Show(settingsPanel, true)); Listen(settingsClose, CloseSettings);
            Listen(confirmYes, () => Launch(GameStartRequest.Mode.NewGame)); Listen(confirmNo, () => Show(confirmPanel, false));
            Listen(quitButton, Quit);
            Listen(creditsButton, () => Show(creditsPanel, true)); Listen(creditsClose, () => Show(creditsPanel, false));
            for (int i = 0; i < characterButtons.Length; i++) { int index = i; Listen(characterButtons[i], () => Choose(index, true)); }
            if (volume != null) volume.onValueChanged.AddListener(VolumeChanged);
            if (sensitivity != null) sensitivity.onValueChanged.AddListener(SensitivityChanged);
            if (music != null) music.onValueChanged.AddListener(MusicChanged);
        }
        private void OnDisable()
        {
            Unbind();
            if (volume != null) volume.onValueChanged.RemoveListener(VolumeChanged);
            if (sensitivity != null) sensitivity.onValueChanged.RemoveListener(SensitivityChanged);
            if (music != null) music.onValueChanged.RemoveListener(MusicChanged);
        }
        private void Start()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            preferences = LoadPreferences();
            if (volume != null) volume.SetValueWithoutNotify(preferences.volume);
            if (sensitivity != null) sensitivity.SetValueWithoutNotify(preferences.sensitivity);
            if (music != null) music.SetValueWithoutNotify(preferences.music);
            ShowValues(); ApplyVolume(); KidGraphics.Apply(preferences.lightGraphics);
            Choose(PlayerPrefs.GetInt(CharacterSelection.PreferenceKey, 0), false);
            if (continueButton != null) continueButton.gameObject.SetActive(HasSave);
            Show(confirmPanel, false); Show(helpPanel, false); Show(settingsPanel, false); Show(creditsPanel, false);
            if (logo != null) logoBase = logo.anchoredPosition;
            if (leaves != null) { leafBase = new Vector2[leaves.Length]; for (int i = 0; i < leaves.Length; i++) leafBase[i] = leaves[i].anchoredPosition; }
            if (KidVoice.Enabled && sound != null && welcomeClip != null) sound.PlayOneShot(welcomeClip);
        }
        private void Update()
        {
            float t = Time.unscaledTime;
            // Gentle life: the park picture drifts, the logo bobs and the leaves sway.
            if (background != null) background.uvRect = new Rect(0.04f + Mathf.Sin(t * 0.05f) * 0.03f, 0.03f, 0.92f, 0.92f);
            if (logo != null) logo.anchoredPosition = logoBase + new Vector2(0, Mathf.Sin(t * 1.6f) * 6f);
            if (leaves != null && leafBase != null)
                for (int i = 0; i < leaves.Length; i++)
                    if (leaves[i] != null)
                    {
                        leaves[i].anchoredPosition = leafBase[i] + new Vector2(Mathf.Sin(t * 0.7f + i) * 14f, Mathf.Sin(t * 1.1f + i * 2) * 10f);
                        leaves[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.9f + i) * 18f);
                    }
            if (PrototypeInput.EscapePressed) { Show(confirmPanel, false); Show(helpPanel, false); Show(creditsPanel, false); CloseSettings(); }
        }
        private void StartPressed()
        {
            if (HasSave) Show(confirmPanel, true);
            else Launch(GameStartRequest.Mode.NewGame);
        }
        public void Launch(GameStartRequest.Mode mode)
        {
            Click();
            GameStartRequest.Pending = mode; GameStartRequest.LaunchedFromTitle = true;
            // A new game first goes to the character choice scene; continuing keeps the saved friend.
            KidLoading.Load(mode == GameStartRequest.Mode.NewGame ? GameStartRequest.CharacterScene : GameStartRequest.GameScene);
        }
        public void Choose(int index, bool remember)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, characterFrames.Length - 1));
            for (int i = 0; i < characterFrames.Length; i++)
                if (characterFrames[i] != null)
                {
                    characterFrames[i].color = i == index ? selectedColor : idleColor;
                    characterFrames[i].transform.localScale = Vector3.one * (i == index ? 1.04f : 1f);
                }
            if (remember) { PlayerPrefs.SetInt(CharacterSelection.PreferenceKey, index); PlayerPrefs.Save(); Click(); }
        }
        private void Show(GameObject panel, bool show)
        {
            if (panel == null) return;
            if (show) Click();
            panel.SetActive(show);
        }
        private void CloseSettings()
        {
            if (settingsPanel == null || !settingsPanel.activeSelf) return;
            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(preferences)); PlayerPrefs.Save();
            Show(settingsPanel, false);
        }
        private void VolumeChanged(float value) { preferences.volume = value; preferences.Normalize(); ShowValues(); ApplyVolume(); }
        private void SensitivityChanged(float value) { preferences.sensitivity = value; preferences.Normalize(); ShowValues(); }
        private void MusicChanged(float value) { preferences.music = value; preferences.Normalize(); ShowValues(); ApplyVolume(); }
        private void ShowValues()
        {
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(preferences.volume * 100) + "%";
            if (sensitivityValue != null) sensitivityValue.text = preferences.sensitivity.ToString("0.0");
            if (musicValue != null) musicValue.text = Mathf.RoundToInt(preferences.music * 100) + "%";
        }
        private void ApplyVolume() { if (sound != null) sound.volume = preferences.volume; KidSoundscape.EffectsVolume = preferences.volume; KidSoundscape.MusicVolume = preferences.music; }
        private void Click() { if (sound != null && clickClip != null) sound.PlayOneShot(clickClip); }
        private void Quit()
        {
            Click();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public static SubmissionPreferences LoadPreferences()
        {
            var loaded = new SubmissionPreferences();
            if (PlayerPrefs.HasKey(SettingsKey))
            {
                try { loaded = JsonUtility.FromJson<SubmissionPreferences>(PlayerPrefs.GetString(SettingsKey)) ?? new SubmissionPreferences(); }
                catch (System.ArgumentException) { loaded = new SubmissionPreferences(); }
            }
            loaded.Normalize();
            return loaded;
        }
        private void Listen(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action); listeners.Add(new KeyValuePair<Button, UnityAction>(button, action));
        }
        private void Unbind()
        {
            foreach (var listener in listeners) if (listener.Key != null) listener.Key.onClick.RemoveListener(listener.Value);
            listeners.Clear();
        }
    }
}
