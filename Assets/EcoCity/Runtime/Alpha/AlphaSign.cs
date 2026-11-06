using UnityEngine;

namespace EcoCity
{
    public sealed class AlphaSign : MonoBehaviour
    {
        public Camera viewCamera;
        [Tooltip("Hide this sign while the camera is closer than this (0 = never). Keeps big boards from filling a third-person view.")]
        public float hideCloserThan;
        private Renderer[] renderers;
        private bool hidden;
        private void LateUpdate()
        {
            if (viewCamera == null) return;
            Vector3 direction = transform.position - viewCamera.transform.position;
            if (hideCloserThan > 0)
            {
                bool hide = direction.magnitude < hideCloserThan;
                if (hide != hidden)
                {
                    if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers) if (renderer != null) renderer.enabled = !hide;
                    hidden = hide;
                }
            }
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
