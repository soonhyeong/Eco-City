using EcoCity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SubmissionAudio : MonoBehaviour
    {
        public EcoGame game;
        public AlphaUi ui;
        public AudioClip clickClip, successClip, retryClip, completeClip;
        public float toneGain = 1.25f;
        [Tooltip("Off where KidSoundscape plays its own pickup, throw and wrong sounds; the tones would double them.")]
        public bool resultTones = true;
        private AudioSource source;
        private Button[] buttons;
        private void OnEnable()
        {
            source = GetComponent<AudioSource>();
            if (game != null) game.ResultPublished += OnResult;
            if (ui != null)
            {
                buttons = ui.GetComponentsInChildren<Button>(true);
                foreach (var button in buttons) button.onClick.AddListener(Click);
            }
        }
        private void OnDisable()
        {
            if (game != null) game.ResultPublished -= OnResult;
            if (buttons != null) foreach (var button in buttons) if (button != null) button.onClick.RemoveListener(Click);
            buttons = null;
        }
        public void SetVolume(float value)
        {
            if (source == null) source = GetComponent<AudioSource>();
            // The short UI tones are quiet (about -17 dB peak), so they get extra gain on top of the slider.
            source.volume = Mathf.Clamp01(value * toneGain);
        }
        public void Click() { Play(clickClip); }
        private void OnResult(ActionResult result)
        {
            if (!resultTones) return;
            if (result.Code == ResultCode.Accepted) Play(game.Session.IsComplete ? completeClip : successClip);
            else if (result.Code == ResultCode.WrongBin || result.Code == ResultCode.MissingPreparation) Play(retryClip);
            else if (result.IsSuccess) Play(clickClip);
        }
        private void Play(AudioClip clip)
        { if (source != null && clip != null && isActiveAndEnabled) source.PlayOneShot(clip); }
    }
}
