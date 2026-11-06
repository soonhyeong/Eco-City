using UnityEngine;

namespace EcoCity
{
    public sealed class InteractionHighlight : MonoBehaviour
    {
        public PlayerInteractor interactor;
        private Renderer highlighted;
        private Interactable observedTarget;
        private MaterialPropertyBlock original;
        private void LateUpdate()
        {
            var root = interactor != null && !interactor.inputBlocked ? interactor.Target : null;
            if (root == observedTarget) return;
            Restore();
            observedTarget = root;
            var target = FindVisual(root);
            if (target == null) return;
            highlighted = target;
            original = new MaterialPropertyBlock(); target.GetPropertyBlock(original);
            var highlight = new MaterialPropertyBlock(); target.GetPropertyBlock(highlight);
            var material = target.sharedMaterial;
            bool baseColor = material != null && material.HasProperty("_BaseColor");
            bool standardColor = material != null && material.HasProperty("_Color");
            Color color = baseColor ? material.GetColor("_BaseColor") : standardColor ? material.GetColor("_Color") : Color.white;
            Color tint = Color.Lerp(color, new Color(1,0.95f,0.55f), 0.45f);
            if (baseColor) highlight.SetColor("_BaseColor", tint);
            if (standardColor) highlight.SetColor("_Color", tint);
            target.SetPropertyBlock(highlight);
        }
        public static Renderer FindVisual(Interactable target)
        {
            if (target == null) return null;
            if (target.visualRenderer != null) return target.visualRenderer;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
                if (renderer.enabled && renderer.GetComponent<TextMesh>() == null && renderer.GetComponent<TMPro.TMP_Text>() == null) return renderer;
            return null;
        }
        private void Restore()
        { if (highlighted != null) highlighted.SetPropertyBlock(original); highlighted = null; observedTarget = null; }
        private void OnDisable() { Restore(); }
    }
}
