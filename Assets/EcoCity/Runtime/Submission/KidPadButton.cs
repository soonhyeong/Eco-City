using UnityEngine;
using UnityEngine.UI;

namespace EcoCity
{
    // Lets the gamepad A button (or Enter / Space) press the main button of a screen: "시작하기" on the title,
    // "한 번 더 하기" on the results, "처음부터 하기" when asked to confirm. Nothing happens while a popup covers it.
    [RequireComponent(typeof(Button))]
    public sealed class KidPadButton : MonoBehaviour
    {
        public GameObject[] blockers;
        private static int pressedFrame = -1;
        private Button button;

        private void Awake() { button = GetComponent<Button>(); }
        private void Update()
        {
            if (!PrototypeInput.ConfirmPressed || pressedFrame == Time.frameCount || !button.interactable) return;
            if (blockers != null) foreach (var blocker in blockers) if (blocker != null && blocker.activeInHierarchy) return;
            pressedFrame = Time.frameCount;
            button.onClick.Invoke();
        }
    }
}
