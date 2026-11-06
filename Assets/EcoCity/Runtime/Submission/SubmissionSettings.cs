using EcoCity.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EcoCity
{
    [DefaultExecutionOrder(200)]
    public sealed class SubmissionSettings : MonoBehaviour
    {
        private const string Key = "EcoCity.Submission.Settings.v1";
        public AlphaFlow flow;
        public SubmissionAudio audioFeedback;
        public GameObject panel;
        public Slider sensitivity, volume, music;
        public Toggle invertY, fullscreen, bigText, screenEffects, lightGraphics;
        [Tooltip("HUD cards that grow with the big-text option (each scales around its own corner).")]
        public RectTransform[] bigTextTargets;
        public float bigTextScale = 1.12f;
        public KidPostFx postFx;
        public Graphic values;
        public Button openTitle, openPause, close, defaults;
        public SubmissionPreferences Preferences { get; private set; } = new SubmissionPreferences();
        private bool persist = true;
        private AlphaScreen returnScreen;
        private bool bound;
        private bool initialized;
        private void OnEnable() { if (initialized) Bind(); }
        private void Start() { Initialize(); }
        public void Initialize(bool readPreferences = true)
        {
            persist = readPreferences;
            Preferences = new SubmissionPreferences();
            if (readPreferences && PlayerPrefs.HasKey(Key))
            {
                try { Preferences = JsonUtility.FromJson<SubmissionPreferences>(PlayerPrefs.GetString(Key)) ?? new SubmissionPreferences(); }
                catch (System.ArgumentException) { Preferences = new SubmissionPreferences(); }
            }
            Preferences.Normalize(); initialized = true; Bind(); SyncControls(); Apply(); panel.SetActive(false);
        }
        private void Bind()
        {
            Unbind();
            openTitle.onClick.AddListener(Open); openPause.onClick.AddListener(Open);
            close.onClick.AddListener(Close); defaults.onClick.AddListener(ResetDefaults);
            sensitivity.onValueChanged.AddListener(ChangeSensitivity); volume.onValueChanged.AddListener(ChangeVolume);
            invertY.onValueChanged.AddListener(ChangeInvert); fullscreen.onValueChanged.AddListener(ChangeFullscreen);
            if (music != null) music.onValueChanged.AddListener(ChangeMusic);
            if (bigText != null) bigText.onValueChanged.AddListener(ChangeBigText);
            if (screenEffects != null) screenEffects.onValueChanged.AddListener(ChangeEffects);
            if (lightGraphics != null) lightGraphics.onValueChanged.AddListener(ChangeLight);
            bound = true;
        }
        private void Unbind()
        {
            if (!bound) return;
            openTitle.onClick.RemoveListener(Open); openPause.onClick.RemoveListener(Open);
            close.onClick.RemoveListener(Close); defaults.onClick.RemoveListener(ResetDefaults);
            sensitivity.onValueChanged.RemoveListener(ChangeSensitivity); volume.onValueChanged.RemoveListener(ChangeVolume);
            invertY.onValueChanged.RemoveListener(ChangeInvert); fullscreen.onValueChanged.RemoveListener(ChangeFullscreen);
            if (music != null) music.onValueChanged.RemoveListener(ChangeMusic);
            if (bigText != null) bigText.onValueChanged.RemoveListener(ChangeBigText);
            if (screenEffects != null) screenEffects.onValueChanged.RemoveListener(ChangeEffects);
            if (lightGraphics != null) lightGraphics.onValueChanged.RemoveListener(ChangeLight);
            bound = false;
        }
        private void Update()
        {
            if (!panel.activeSelf) return;
            if (flow.Screens.Current != AlphaScreen.Help) { Close(); return; }
            if (PrototypeInput.EscapePressed) Close();
        }
        public void Open()
        {
            if (panel.activeSelf) return;
            returnScreen = flow.Screens.Current;
            if (returnScreen != AlphaScreen.Title && returnScreen != AlphaScreen.Paused) return;
            flow.Help(); flow.ui.externalOverlay = true; flow.ui.Refresh();
            panel.SetActive(true); panel.transform.SetAsLastSibling();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(close.gameObject);
        }
        public void Close()
        {
            panel.SetActive(false);
            flow.ui.externalOverlay = false;
            if (flow.Screens.Current == AlphaScreen.Help) flow.CloseHelp();
            else flow.ui.Refresh();
        }
        public void ResetDefaults() { Preferences = new SubmissionPreferences(); SyncControls(); Apply(); Save(); }
        private void ChangeSensitivity(float value) { Preferences.sensitivity = value; Apply(); Save(); }
        private void ChangeVolume(float value) { Preferences.volume = value; Apply(); Save(); }
        private void ChangeInvert(bool value) { Preferences.invertY = value; Apply(); Save(); }
        private void ChangeFullscreen(bool value) { Preferences.fullscreen = value; Apply(); Save(); }
        private void ChangeMusic(float value) { Preferences.music = value; Apply(); Save(); }
        private void ChangeBigText(bool value) { Preferences.bigText = value; Apply(); Save(); }
        private void ChangeEffects(bool value) { Preferences.effects = value; Apply(); Save(); }
        private void ChangeLight(bool value) { Preferences.lightGraphics = value; Apply(); Save(); }
        private void SyncControls()
        {
            sensitivity.SetValueWithoutNotify(Preferences.sensitivity); volume.SetValueWithoutNotify(Preferences.volume);
            invertY.SetIsOnWithoutNotify(Preferences.invertY); fullscreen.SetIsOnWithoutNotify(Preferences.fullscreen);
            if (music != null) music.SetValueWithoutNotify(Preferences.music);
            if (bigText != null) bigText.SetIsOnWithoutNotify(Preferences.bigText);
            if (screenEffects != null) screenEffects.SetIsOnWithoutNotify(Preferences.effects);
            if (lightGraphics != null) lightGraphics.SetIsOnWithoutNotify(Preferences.lightGraphics);
        }
        private void Apply()
        {
            Preferences.Normalize();
            flow.player.lookSensitivity = Preferences.sensitivity; flow.player.invertLookY = Preferences.invertY;
            audioFeedback.SetVolume(Preferences.volume);
            KidSoundscape.EffectsVolume = Preferences.volume; KidSoundscape.MusicVolume = Preferences.music;
            BigText(Preferences.bigText);
            if (postFx != null) postFx.effectOn = Preferences.effects;
            KidGraphics.Apply(Preferences.lightGraphics);
            if (Application.isPlaying && !Application.isEditor && persist)
            {
                FullScreenMode mode = Preferences.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                if (Screen.fullScreenMode != mode)
                    Screen.SetResolution(Preferences.fullscreen ? Screen.currentResolution.width : 1280,
                        Preferences.fullscreen ? Screen.currentResolution.height : 720, mode);
            }
            if (values != null) UiText.Set(values, "카메라 " + Preferences.sensitivity.ToString("0.0") + "  ·  효과음 " + Mathf.RoundToInt(Preferences.volume * 100) + "%  ·  음악 " + Mathf.RoundToInt(Preferences.music * 100) + "%");
        }
        private Vector2[] bigTextHome;
        // Grow the corner cards; a card stacked under another card in the same corner moves away by the extra height above it.
        private void BigText(bool on)
        {
            if (bigTextTargets == null) return;
            if (bigTextHome == null || bigTextHome.Length != bigTextTargets.Length)
            {
                bigTextHome = new Vector2[bigTextTargets.Length];
                for (int i = 0; i < bigTextTargets.Length; i++) if (bigTextTargets[i] != null) bigTextHome[i] = bigTextTargets[i].anchoredPosition;
            }
            float scale = on ? bigTextScale : 1;
            for (int i = 0; i < bigTextTargets.Length; i++)
            {
                var target = bigTextTargets[i]; if (target == null) continue;
                target.localScale = Vector3.one * scale;
                bool top = target.anchorMin.y > 0.5f;
                float shift = 0;
                for (int j = 0; j < bigTextTargets.Length; j++)
                {
                    var other = bigTextTargets[j];
                    if (j == i || other == null || other.anchorMin != target.anchorMin) continue;
                    bool before = top ? bigTextHome[j].y > bigTextHome[i].y : bigTextHome[j].y < bigTextHome[i].y;
                    if (before) shift += other.rect.height * (scale - 1);
                }
                target.anchoredPosition = bigTextHome[i] + new Vector2(0, top ? -shift : shift);
            }
        }
        private void Save()
        {
            if (!persist) return;
            try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Preferences)); PlayerPrefs.Save(); }
            catch (PlayerPrefsException) { Debug.LogWarning("EcoCity settings could not be saved; current settings remain active."); }
        }
        private void OnDisable()
        {
            Unbind();
            if (flow != null && flow.ui != null) flow.ui.externalOverlay = false;
        }
    }
}
