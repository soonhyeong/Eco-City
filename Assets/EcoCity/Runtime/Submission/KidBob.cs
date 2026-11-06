using UnityEngine;

namespace EcoCity
{
    // Gentle floating motion: boats bob and roll on the water, balloons rise and sink, clouds drift sideways and back,
    // the foam on the shore washes in and out.
    public sealed class KidBob : MonoBehaviour
    {
        public Vector3 move = new Vector3(0, 0.15f, 0);
        [Tooltip("Roll and pitch in degrees.")]
        public Vector3 tilt = new Vector3(3, 0, 4);
        public float speed = 1f;
        private Vector3 home;
        private Quaternion rest;
        private float phase;

        private void Start() { home = transform.localPosition; rest = transform.localRotation; phase = transform.position.x * 0.37f + transform.position.z * 0.11f; }
        private void Update()
        {
            float t = Time.time * speed + phase;
            transform.localPosition = home + new Vector3(move.x * Mathf.Sin(t * 0.7f), move.y * Mathf.Sin(t), move.z * Mathf.Sin(t * 0.8f + 1));
            transform.localRotation = rest * Quaternion.Euler(tilt.x * Mathf.Sin(t * 1.1f), tilt.y * Mathf.Sin(t * 0.5f), tilt.z * Mathf.Sin(t * 0.9f + 0.5f));
        }
    }
}
