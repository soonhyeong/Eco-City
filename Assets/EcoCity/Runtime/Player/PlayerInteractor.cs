using UnityEngine;

namespace EcoCity
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public EcoGame game;
        public Camera viewCamera;
        public float distance = 3.5f;
        public LayerMask mask = ~0;
        public bool inputBlocked;
        [Tooltip("Pick the nearest object in front of the character instead of aiming the camera centre. Used by the third-person player.")]
        public bool proximity;
        public float reach = 2f;
        public Interactable Target { get; private set; }
        private readonly Collider[] nearby = new Collider[16];
        private void Start()
        {
            if (game == null || viewCamera == null) Debug.LogError("EcoCity: PlayerInteractor needs EcoGame and Camera references.", this);
        }
        private void Update()
        {
            Target = null;
            if (inputBlocked || game == null || game.Session == null || viewCamera == null || Cursor.lockState != CursorLockMode.Locked) return;
            Target = FindTarget();
            if (Target != null && !PrototypeInput.EscapePressed && PrototypeInput.TryUse()) Target.Interact(game);
        }
        public Interactable FindTarget()
        {
            if (!proximity)
            {
                RaycastHit hit;
                return Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out hit,
                    distance, mask, QueryTriggerInteraction.Ignore) ? hit.collider.GetComponentInParent<Interactable>() : null;
            }
            // Closest object wins; objects behind the character are ignored so turning toward a bin selects it.
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Interactable best = null;
            float bestScore = float.MaxValue;
            int count = Physics.OverlapSphereNonAlloc(origin, reach, nearby, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var candidate = nearby[i].GetComponentInParent<Interactable>();
                if (candidate == null) continue;
                Vector3 offset = nearby[i].ClosestPoint(origin) - origin; offset.y = 0;
                float range = offset.magnitude;
                float facing = range < 0.01f ? 1 : Vector3.Dot(transform.forward, offset / range);
                if (facing < -0.2f) continue;
                float score = range - facing * 0.75f;
                if (score < bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }
    }
}
