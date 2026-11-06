using UnityEngine;

namespace EcoCity
{
    // The shared locomotion clips call OnFootstep/OnLand; without a receiver Unity logs an error every step.
    // This sits next to each character Animator, accepts the events and passes real steps on (KidSoundscape plays them).
    public sealed class AnimationEventSink : MonoBehaviour
    {
        /// <summary>A foot touched the ground at this position (blended-out clips are ignored).</summary>
        public static event System.Action<Vector3> Footstep;
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f || Footstep == null) return;
            Footstep(transform.position);
        }
        private void OnLand(AnimationEvent animationEvent) { }
    }
}
