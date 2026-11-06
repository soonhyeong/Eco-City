using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoCity
{
    // No runtime asmdef: Assembly-CSharp can resolve the auto-referenced InputSystem
    // only when its package and ENABLE_INPUT_SYSTEM are present.
    // A gamepad works too: left stick or d-pad walk, right stick looks, A uses and confirms, Start opens the menu,
    // Back/View shows help, X / Y / B answer 1 / 2 / 3 on the map and in the mini game.
    public static class PrototypeInput
    {
        private static int usedFrame = -1;
        public static bool TryUse()
        {
            if (!UsePressed || usedFrame == Time.frameCount) return false;
            usedFrame = Time.frameCount;
            return true;
        }
        public static string DeviceNotice
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current == null || Mouse.current == null) && Gamepad.current == null ? "키보드와 마우스(또는 게임패드)를 연결해 주세요." : "";
#elif ENABLE_LEGACY_INPUT_MANAGER
                return "";
#else
                return "활성 입력 시스템이 없습니다. Player Settings를 확인하세요.";
#endif
            }
        }
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Vector2 move = Vector2.zero;
                if (k != null)
                    move = new Vector2(
                        (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                        (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
                Gamepad g = Gamepad.current;
                if (g != null)
                {
                    Vector2 stick = g.leftStick.ReadValue();
                    if (stick.magnitude > 0.2f) move += stick;
                    move += g.dpad.ReadValue();
                }
                return Vector2.ClampMagnitude(move, 1);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
#else
                return Vector2.zero;
#endif
            }
        }
        public static Vector2 Look
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 look = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * 0.08f;
                Gamepad g = Gamepad.current;
                if (g != null) { Vector2 stick = g.rightStick.ReadValue(); if (stick.magnitude > 0.15f) look += stick * 1.8f * Time.unscaledDeltaTime * 60f; }
                return look;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
                return Vector2.zero;
#endif
            }
        }
        public static bool UsePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.E);
#else
                return false;
#endif
            }
        }
        public static bool EscapePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Escape);
#else
                return false;
#endif
            }
        }
        public static bool HelpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.H);
#else
                return false;
#endif
            }
        }
        // Menu-style keys for the character choice and the mini game.
        public static bool LeftPressed { get { return Pressed(0); } }
        public static bool RightPressed { get { return Pressed(1); } }
        public static bool ConfirmPressed { get { return Pressed(2); } }
        /// <summary>Number key 1..9 pressed this frame (top row or keypad), or 0.</summary>
        public static int NumberPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Gamepad g = Gamepad.current;
                if (g != null)
                {
                    if (g.buttonWest.wasPressedThisFrame) return 1;
                    if (g.buttonNorth.wasPressedThisFrame) return 2;
                    if (g.buttonEast.wasPressedThisFrame) return 3;
                }
                Keyboard k = Keyboard.current;
                if (k == null) return 0;
                Key[] top = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 }, pad = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4 };
                for (int i = 0; i < top.Length; i++) if (k[top[i]].wasPressedThisFrame || k[pad[i]].wasPressedThisFrame) return i + 1;
                return 0;
#elif ENABLE_LEGACY_INPUT_MANAGER
                for (int i = 0; i < 4; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) return i + 1;
                return 0;
#else
                return 0;
#endif
            }
        }
        public static bool ClickPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }
        public static Vector2 PointerPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }
        private static bool Pressed(int which)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                if (which == 0 && g.dpad.left.wasPressedThisFrame) return true;
                if (which == 1 && g.dpad.right.wasPressedThisFrame) return true;
                if (which == 2 && g.buttonSouth.wasPressedThisFrame) return true;
            }
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            if (which == 0) return k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
            if (which == 1) return k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
            return k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (which == 0) return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            if (which == 1) return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}
