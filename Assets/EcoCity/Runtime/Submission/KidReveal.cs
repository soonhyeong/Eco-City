using TMPro;
using UnityEngine;

namespace EcoCity
{
    // Writes a text out letter by letter when it appears (the results card: stars first, then the lines below),
    // with a soft tick for each star. Unscaled time, so it also runs on paused screens.
    public sealed class KidReveal : MonoBehaviour
    {
        public TMP_Text text;
        public float delay = 0.35f, charactersPerSecond = 45f;
        public AudioSource sound;
        public AudioClip tick;
        private float startedAt;
        private int ticked;

        private void OnEnable() { startedAt = Time.unscaledTime; ticked = 0; if (text != null) text.maxVisibleCharacters = 0; }
        private void OnDisable() { if (text != null) text.maxVisibleCharacters = 99999; }
        private void Update()
        {
            if (text == null) return;
            float t = Time.unscaledTime - startedAt - delay;
            int visible = t <= 0 ? 0 : Mathf.FloorToInt(t * charactersPerSecond);
            int total = text.textInfo != null ? text.textInfo.characterCount : 0;
            text.maxVisibleCharacters = total > 0 && visible >= total ? 99999 : visible;
            if (sound == null || tick == null || text.textInfo == null) return;
            // One tick per star shown.
            int stars = 0;
            for (int i = 0; i < Mathf.Min(visible, total); i++) if (text.textInfo.characterInfo[i].character == '★') stars++;
            if (stars > ticked) { ticked = stars; sound.PlayOneShot(tick, 0.6f); }
        }
    }
}
