using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    // A soft arrow on the ground next to the player, pointing where to go next without words:
    // empty hands → the nearest "!" item that can be picked up now; holding a dirty item → the station for the next step.
    // Holding a clean item it disappears: choosing the bin is the player's own job (no bin hints before mistakes).
    public sealed class KidGuideArrow : MonoBehaviour
    {
        public EcoGame game;
        public PlayerInteractor interactor;
        public Transform arrow;
        public float radius = 1.25f, hideCloserThan = 2.6f;
        [Tooltip("Only targets in the player's own region (regions are this far apart).")]
        public float regionSpacing = 220f;
        private PreparationStation[] stations;
        private Renderer arrowRenderer;

        private void Start()
        {
            stations = FindObjectsByType<PreparationStation>(FindObjectsSortMode.None);
            if (arrow != null) { arrowRenderer = arrow.GetComponent<Renderer>(); arrow.gameObject.SetActive(false); }
        }
        private void LateUpdate()
        {
            if (arrow == null) return;
            Vector3? target = Target();
            Vector3 here = transform.position;
            bool show = target.HasValue && Flat(target.Value - here).magnitude > hideCloserThan;
            if (arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
            if (!show) return;
            Vector3 direction = Flat(target.Value - here).normalized;
            float pulse = 1 + Mathf.Sin(Time.unscaledTime * 4) * 0.08f;
            arrow.position = here + direction * (radius + Mathf.Sin(Time.unscaledTime * 4) * 0.08f) + Vector3.up * 0.06f;
            // The quad lies flat with its picture's "up" pointing along the direction.
            arrow.rotation = Quaternion.LookRotation(Vector3.down, direction);
            arrow.localScale = Vector3.one * 0.9f * pulse;
        }
        private Vector3? Target()
        {
            if (game == null || game.Session == null || interactor == null || interactor.inputBlocked) return null;
            var session = game.Session;
            if (session.IsComplete) return null;
            if (session.Held != null)
            {
                Preparation next = WasteRules.NextStep(session.Held.Required & ~session.CompletedPreparation);
                if (next == Preparation.None || stations == null) return null;
                Vector3? near = null; float nearest = float.MaxValue;
                foreach (var station in stations)
                    if (station != null && station.step == next)
                    {
                        float d = Flat(station.transform.position - transform.position).sqrMagnitude;
                        if (d < nearest) { nearest = d; near = station.transform.position; }
                    }
                return near;
            }
            Vector3 here = transform.position; float best = float.MaxValue; Vector3? found = null;
            foreach (var item in game.wasteItems)
            {
                if (item == null || item.definition == null || session.IsDisposed(item.instanceId)) continue;
                if (!session.IsUnlocked(item.definition.ToSpec(item.instanceId))) continue;
                if (Mathf.Abs(item.transform.position.x - here.x) > regionSpacing / 2) continue;
                float distance = Flat(item.transform.position - here).sqrMagnitude;
                if (distance < best) { best = distance; found = item.transform.position; }
            }
            return found;
        }
        private static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
