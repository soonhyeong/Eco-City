using UnityEngine;

namespace EcoCity
{
    // A light breeze for the whole town: trees, bushes and flower beds lean gently, each a little out of step with its neighbours.
    // One component moves all of them (cheaper than one script per plant).
    public sealed class KidSway : MonoBehaviour
    {
        public Transform[] plants;
        [Tooltip("Largest lean in degrees.")]
        public float angle = 2.4f;
        public float speed = 1.3f;
        private Quaternion[] rest;
        private float[] phase;

        private void Start()
        {
            rest = new Quaternion[plants != null ? plants.Length : 0];
            phase = new float[rest.Length];
            for (int i = 0; i < rest.Length; i++)
                if (plants[i] != null) { rest[i] = plants[i].localRotation; phase[i] = plants[i].position.x * 0.21f + plants[i].position.z * 0.13f; }
        }
        private void LateUpdate()
        {
            float t = Time.time * speed;
            for (int i = 0; i < rest.Length; i++)
            {
                var plant = plants[i];
                if (plant == null || !plant.gameObject.activeInHierarchy) continue;
                // A slow gust wave crosses the town plus a quicker flutter.
                float gust = Mathf.Sin(t + phase[i]) * 0.7f + Mathf.Sin(t * 2.3f + phase[i] * 1.7f) * 0.3f;
                plant.localRotation = rest[i] * Quaternion.Euler(gust * angle, 0, gust * angle * 0.6f);
            }
        }
    }
}
