using UnityEngine;

namespace EcoCity
{
    // Moves a car, bus or train straight along a road or track and starts again from the other end.
    // Birds use the circle mode instead: they glide round in a ring and flap now and then.
    public sealed class KidMover : MonoBehaviour
    {
        public enum Mode { Line, Circle }
        public Mode mode = Mode.Line;
        [Header("Line")]
        public Vector3 from, to;
        public float speed = 4f;
        [Header("Circle")]
        public Vector3 centre;
        public float radius = 10f, height = 12f;
        public Transform[] wings;
        private float travelled, angle, length;

        private void Start()
        {
            length = Vector3.Distance(from, to);
            travelled = mode == Mode.Line && length > 0 ? Vector3.Distance(from, Flat(transform.localPosition, from.y)) % length : 0;
            angle = Mathf.Atan2(transform.localPosition.z - centre.z, transform.localPosition.x - centre.x);
        }
        private void Update()
        {
            if (mode == Mode.Line)
            {
                if (length <= 0) return;
                travelled = (travelled + speed * Time.deltaTime) % length;
                transform.localPosition = Vector3.Lerp(from, to, travelled / length);
                transform.localRotation = Quaternion.LookRotation(to - from);
                return;
            }
            angle += speed / Mathf.Max(1, radius) * Time.deltaTime;
            Vector3 at = centre + new Vector3(Mathf.Cos(angle) * radius, height + Mathf.Sin(angle * 3) * 0.8f, Mathf.Sin(angle) * radius);
            Vector3 heading = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)) * Mathf.Sign(speed);
            transform.localPosition = at;
            transform.localRotation = Quaternion.LookRotation(heading) * Quaternion.Euler(0, 0, -12 * Mathf.Sign(speed));
            if (wings == null) return;
            // Flap in bursts, then glide.
            float flap = Mathf.Sin(Time.time * 1.3f + radius) > 0.2f ? Mathf.Sin(Time.time * 14 + radius) * 35 : 8;
            for (int i = 0; i < wings.Length; i++) if (wings[i] != null) wings[i].localRotation = Quaternion.Euler(0, 0, (i == 0 ? 1 : -1) * flap);
        }
        private static Vector3 Flat(Vector3 v, float y) { v.y = y; return v; }
    }
}
