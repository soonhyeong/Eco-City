using UnityEngine;

namespace EcoCity
{
    public sealed class CityEnvironmentView : MonoBehaviour
    {
        public EcoGame game;
        public Renderer ground;
        public Renderer[] greenery;
        [Tooltip("Tints multiply the material colour. Textured models usually look best with white when clean.")]
        public Color groundDirty = new Color(0.36f, 0.32f, 0.25f);
        public Color groundClean = new Color(0.45f, 0.65f, 0.37f);
        public Color greeneryDirty = new Color(0.35f, 0.29f, 0.14f);
        public Color greeneryClean = new Color(0.15f, 0.60f, 0.25f);
        private MaterialPropertyBlock block;
        private void OnEnable() { if (game != null) game.StateChanged += Refresh; }
        private void Start() { Refresh(); }
        private void OnDisable() { if (game != null) game.StateChanged -= Refresh; }
        private void Refresh()
        {
            if (game == null || game.Session == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            float clean = game.Session.Cleanliness / 100f;
            Tint(ground, Color.Lerp(groundDirty, groundClean, clean));
            if (greenery != null)
                foreach (Renderer r in greenery)
                    Tint(r, Color.Lerp(greeneryDirty, greeneryClean, clean));
        }
        private void Tint(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }
}
