using UnityEngine;

namespace EcoCity
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypePlayer : MonoBehaviour
    {
        public Camera viewCamera;
        public float speed = 4f;
        public float lookSensitivity = 2f;
        public bool inputBlocked;
        public bool invertLookY;
        [Header("Third person (child-friendly)")]
        [Tooltip("Camera follows behind the character; movement is relative to the camera and the character turns to face it.")]
        public bool thirdPerson;
        public float cameraDistance = 4.5f;
        public float cameraPivotHeight = 0.9f;
        public float cameraPitch = 24f;
        public float turnSpeed = 600f;
        [Tooltip("Degrees per second the camera drifts back behind the character while walking.")]
        public float cameraFollowSpeed = 90f;
        private CharacterController controller;
        private float pitch;
        private float verticalSpeed;
        private float cameraYaw;
        private bool cameraYawReady;
        private bool hasFocus = true;
        private readonly RaycastHit[] cameraHits = new RaycastHit[8];
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (viewCamera == null) Debug.LogError("EcoCity: PrototypePlayer needs a Camera reference.", this);
            if (thirdPerson) { pitch = cameraPitch; EnsureCameraYaw(); }
        }
        private void Start() { SetMouseCaptured(false); }
        public void SetMouseCaptured(bool captured)
        {
            // Locking the cursor reports one large mouse jump; ignore look input for a few frames so the view does not swing.
            if (captured && Cursor.lockState != CursorLockMode.Locked) ignoreLookFrames = 3;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !captured;
        }
        private int ignoreLookFrames;
        private void Update()
        {
            if (!hasFocus || viewCamera == null) return;
            if (PrototypeInput.EscapePressed) SetMouseCaptured(false);
            if (inputBlocked || Cursor.lockState != CursorLockMode.Locked)
            {
                StepMovement(Vector2.zero, Time.deltaTime);
                return;
            }
            Vector2 look = PrototypeInput.Look * lookSensitivity;
            if (ignoreLookFrames > 0) { ignoreLookFrames--; look = Vector2.zero; }
            if (thirdPerson)
            {
                EnsureCameraYaw();
                cameraYaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y * (invertLookY ? -1 : 1), 8, 50);
                StepMovement(PrototypeInput.Move, Time.deltaTime);
                return;
            }
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y * (invertLookY ? -1 : 1), -80, 80);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            StepMovement(PrototypeInput.Move, Time.deltaTime);
        }
        private void LateUpdate() { if (thirdPerson) PlaceCamera(); }
        // Gravity continues while the menu blocks horizontal movement and look.
        private void StepMovement(Vector2 input, float deltaTime)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller == null || !controller.enabled) return;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * deltaTime;
            Vector3 movement;
            if (thirdPerson)
            {
                EnsureCameraYaw();
                movement = Quaternion.Euler(0, cameraYaw, 0) * new Vector3(input.x, 0, input.y) * speed;
                if (movement.sqrMagnitude > 0.0001f)
                {
                    Quaternion facing = Quaternion.LookRotation(movement);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * deltaTime);
                    // Drift back behind the character, but never swing around when walking toward the camera.
                    if (Mathf.Abs(Mathf.DeltaAngle(cameraYaw, transform.eulerAngles.y)) < 100f)
                        cameraYaw = Mathf.MoveTowardsAngle(cameraYaw, transform.eulerAngles.y, cameraFollowSpeed * input.magnitude * deltaTime);
                }
            }
            else movement = (transform.right * input.x + transform.forward * input.y) * speed;
            movement.y = verticalSpeed;
            controller.Move(movement * deltaTime);
        }
        public void ResetPose(Vector3 position, Quaternion rotation)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = wasEnabled;
            verticalSpeed = 0; pitch = thirdPerson ? cameraPitch : 0;
            if (thirdPerson) { cameraYaw = rotation.eulerAngles.y; cameraYawReady = true; PlaceCamera(); }
            else if (viewCamera != null) viewCamera.transform.localRotation = Quaternion.identity;
        }
        // Keeps the camera a child of this root (so it never follows a bone) while choosing its world pose.
        public void PlaceCamera()
        {
            if (viewCamera == null) return;
            EnsureCameraYaw();
            if (pitch <= 0) pitch = cameraPitch;
            Vector3 pivot = transform.position + Vector3.up * cameraPivotHeight;
            Quaternion orbit = Quaternion.Euler(pitch, cameraYaw, 0);
            Vector3 back = orbit * Vector3.back;
            float distance = cameraDistance;
            int count = Physics.SphereCastNonAlloc(pivot, 0.25f, back, cameraHits, cameraDistance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!cameraHits[i].transform.IsChildOf(transform) && cameraHits[i].distance > 0)
                    distance = Mathf.Min(distance, cameraHits[i].distance);
            viewCamera.transform.SetPositionAndRotation(pivot + back * distance, orbit);
        }
        private void EnsureCameraYaw()
        {
            if (cameraYawReady) return;
            cameraYaw = transform.eulerAngles.y; cameraYawReady = true;
        }
        private void OnApplicationFocus(bool focus) { hasFocus = focus; if (!focus) SetMouseCaptured(false); }
        private void OnDisable() { SetMouseCaptured(false); }
    }
}
