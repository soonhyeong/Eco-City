using UnityEngine;

namespace EcoCity
{
    // In-world guidance without reading: a bouncing "!" over items that can be picked up now,
    // and an "E" bubble over whatever the player would use. Markers never point at the correct bin.
    public sealed class KidWorldCues : MonoBehaviour
    {
        public EcoGame game;
        public PlayerInteractor interactor;
        public WasteItem[] items;
        public Transform[] markers;
        public Transform targetBubble;
        public float bobHeight = 0.15f;
        public float bobSpeed = 3f;
        [System.NonSerialized] public float hideBubbleUntil;
        private Vector3[] baseHeights;
        private void LateUpdate()
        {
            if (items == null || markers == null) return;
            if (baseHeights == null || baseHeights.Length != markers.Length)
            {
                baseHeights = new Vector3[markers.Length];
                for (int i = 0; i < markers.Length; i++) if (markers[i] != null) baseHeights[i] = markers[i].localPosition;
            }
            bool exploring = game != null && game.Session != null && interactor != null && !interactor.inputBlocked;
            Interactable target = exploring ? interactor.Target : null;
            Interactable bubbleTarget = Time.time < hideBubbleUntil ? null : target;
            float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
            for (int i = 0; i < markers.Length && i < items.Length; i++)
            {
                if (markers[i] == null || items[i] == null) continue;
                bool show = exploring && game.Session.Held == null && items[i] != target && items[i].definition != null &&
                    game.Session.IsUnlocked(items[i].definition.ToSpec(items[i].instanceId));
                if (markers[i].gameObject.activeSelf != show) markers[i].gameObject.SetActive(show);
                if (show) markers[i].localPosition = baseHeights[i] + Vector3.up * bob;
            }
            if (targetBubble == null) return;
            if (targetBubble.gameObject.activeSelf != (bubbleTarget != null)) targetBubble.gameObject.SetActive(bubbleTarget != null);
            if (bubbleTarget == null) return;
            // In front of the target at mid height, so it never covers the sign board above a bin.
            var colliders = target.GetComponentsInChildren<Collider>();
            Bounds bounds = colliders.Length > 0 ? colliders[0].bounds : new Bounds(target.transform.position, Vector3.one);
            foreach (var collider in colliders) bounds.Encapsulate(collider.bounds);
            Vector3 toPlayer = interactor.transform.position - bounds.center; toPlayer.y = 0;
            toPlayer = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.back;
            float front = Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.3f;
            targetBubble.position = new Vector3(bounds.center.x, Mathf.Max(bounds.max.y * 0.6f, 0.8f) + bob * 0.5f, bounds.center.z) + toPlayer * front;
        }
    }
}
