using UnityEngine;

namespace EcoCity
{
    // Every time a window or card appears it pops in: a quick fade and a small springy grow (unscaled time, works while paused).
    [DisallowMultipleComponent]
    public sealed class KidPopIn : MonoBehaviour
    {
        public float duration = 0.3f;
        [Range(0.5f, 1f)] public float from = 0.86f;
        private float startedAt;
        private bool running;
        private CanvasGroup group;

        private void OnEnable()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            startedAt = Time.unscaledTime; running = true;
            Apply(0);
        }
        private void OnDisable() { Apply(1); running = false; }
        private void Update()
        {
            if (!running) return;
            float t = (Time.unscaledTime - startedAt) / Mathf.Max(0.01f, duration);
            Apply(t);
            if (t >= 1) running = false;
        }
        private void Apply(float t)
        {
            t = Mathf.Clamp01(t);
            // Ease out with a little overshoot.
            const float back = 1.9f;
            float e = t - 1;
            float spring = 1 + e * e * ((back + 1) * e + back);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1, spring);
            if (group != null) group.alpha = Mathf.Clamp01(t * 3);
        }
    }
}
