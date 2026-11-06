using UnityEngine;

namespace EcoCity
{
    // Slow, endless spin for scenery: wind turbine blades and the market's ferris wheel.
    public sealed class KidSpin : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        [Tooltip("Degrees per second.")]
        public float speed = 30f;

        private void Update() { transform.Rotate(axis, speed * Time.deltaTime, Space.Self); }
    }
}
