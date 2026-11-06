using UnityEngine;

namespace EcoCity
{
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Optional renderer for target highlighting. Keep interaction/data on this root and replace its visual children freely.")]
        public Renderer visualRenderer;
        public abstract string GetPrompt(EcoGame game);
        public abstract void Interact(EcoGame game);
    }
}
