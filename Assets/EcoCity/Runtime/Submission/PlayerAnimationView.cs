using UnityEngine;

namespace EcoCity
{
    public sealed class PlayerAnimationView : MonoBehaviour
    {
        public Animator animator;
        public AlphaFlow flow;
        public CharacterController controller;
        private Vector3 lastPosition;
        private void OnEnable() { lastPosition = transform.position; }
        private void Start()
        {
            if (animator == null || animator.runtimeAnimatorController == null) { enabled = false; return; }
            foreach (string parameter in new[] { "Speed", "MotionSpeed", "Grounded", "Jump", "FreeFall" })
                if (!System.Array.Exists(animator.parameters, p => p.name == parameter))
                {
                    Debug.LogWarning("EcoCity animation controller needs parameter: " + parameter + ". Player movement remains available.", this);
                    enabled = false; return;
                }
        }
        private void LateUpdate()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            float delta = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 movement = transform.position - lastPosition; movement.y = 0;
            float speed = flow != null && flow.Screens.CanExplore ? Mathf.Min(movement.magnitude / delta, 5f) : 0;
            lastPosition = transform.position;
            animator.SetFloat("Speed", speed, 0.15f, delta);
            animator.SetFloat("MotionSpeed", 1f);
            animator.SetBool("Grounded", controller != null && controller.isGrounded);
            animator.SetBool("Jump", false); animator.SetBool("FreeFall", false);
        }
    }
}
