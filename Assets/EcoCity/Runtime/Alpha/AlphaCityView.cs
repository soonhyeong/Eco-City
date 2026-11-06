using UnityEngine;

namespace EcoCity
{
    public sealed class AlphaCityView : MonoBehaviour
    {
        public EcoGame game;
        public GameObject[] restoredGardens;
        private void OnEnable() { if (game != null) game.StateChanged += Refresh; }
        private void OnDisable() { if (game != null) game.StateChanged -= Refresh; }
        private void Start() { Refresh(); }
        public void Refresh()
        {
            if (game == null || game.Session == null) return;
            for (int i = 0; i < restoredGardens.Length; i++)
                if (restoredGardens[i] != null) restoredGardens[i].SetActive(game.Session.CompletedMissions > i);
        }
    }
}
