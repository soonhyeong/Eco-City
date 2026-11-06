using UnityEngine;

namespace EcoCity
{
    // A town kid walking a loop around a region (outside the fence, so never in the player's way).
    // While that region is dirty they shuffle slowly; when it is saved they stop, turn to the park and hop for joy.
    public sealed class KidWalker : MonoBehaviour
    {
        public Vector3[] points;
        public float speed = 1.1f;
        public int region;
        public KidRegions regions;
        public KidFinale finale;
        public Animator animator;
        [Tooltip("Clothes tint so the town kids do not all look alike (applied at start; property blocks are not saved in scenes).")]
        public Color tint = Color.white;
        private int next;
        private float hopUntil;
        private Transform model;
        private Vector3 modelHome;

        private void Start()
        {
            if (points == null || points.Length < 2) { enabled = false; return; }
            float best = float.MaxValue;
            for (int i = 0; i < points.Length; i++) { float d = (points[i] - transform.position).sqrMagnitude; if (d < best) { best = d; next = (i + 1) % points.Length; } }
            if (animator != null)
            {
                var block = new MaterialPropertyBlock(); block.SetColor("_Color", tint);
                foreach (var renderer in animator.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(block);
            }
            model = animator != null ? animator.transform : null;
            if (model != null) modelHome = model.localPosition;
        }
        private void Update()
        {
            bool celebrating = finale != null && finale.Playing && Flat(finale.parkCentre - transform.position).magnitude < 70;
            if (celebrating) hopUntil = Time.time + 1.5f;
            if (Time.time < hopUntil)
            {
                // Face the park centre and hop.
                Vector3 look = Flat((finale != null ? finale.parkCentre : transform.position) - transform.position);
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 4);
                if (model != null) model.localPosition = modelHome + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 6 + transform.position.x)) * 0.35f;
                if (animator != null) animator.speed = 0;
                return;
            }
            if (model != null) model.localPosition = modelHome;
            float clean = regions != null ? regions.RegionClean(region) : 1;
            float pace = speed * Mathf.Lerp(0.6f, 1.1f, clean);
            if (animator != null) animator.speed = Mathf.Lerp(0.7f, 1.1f, clean);
            Vector3 target = points[next];
            Vector3 step = Flat(target - transform.position);
            if (step.magnitude < 0.2f) { next = (next + 1) % points.Length; return; }
            Vector3 direction = step.normalized;
            transform.position += direction * Mathf.Min(step.magnitude, pace * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6);
        }
        private static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
