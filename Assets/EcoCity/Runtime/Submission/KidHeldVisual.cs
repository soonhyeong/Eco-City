using UnityEngine;

namespace EcoCity
{
    // Shows the item in hand: a copy of its model carried in the right hand, arm held a little out from the body,
    // so the camera behind the player sees the hand and the item beside the character. The arm is posed after the Animator each frame.
    public sealed class KidHeldVisual : MonoBehaviour
    {
        public EcoGame game;
        public Transform carryPoint;
        [Tooltip("Active character; its humanoid arms hold the item. Without it the item floats at the carry point.")]
        public CharacterSelection selection;
        public float size = 0.34f;
        [Tooltip("How far out to the side the carrying hand is held.")]
        public float sideways = 0.3f;
        private string shownId;
        private GameObject shown;
        private Vector3 centreOffset;
        private float half, shownAt;
        private Vector3 fullScale;
        /// <summary>Hands the carried model over (for the throw into the bin); the hand is empty afterwards.</summary>
        public GameObject Release()
        {
            var model = shown;
            if (model != null) model.transform.SetParent(null, true);
            shown = null; shownId = null;
            return model;
        }
        private void LateUpdate()
        {
            string heldId = game != null && game.Session != null && game.Session.Held != null ? game.Session.Held.Id : null;
            if (heldId != shownId) Show(heldId);
            if (shown == null) return;
            // A new item pops into the hand.
            float grow = Mathf.Clamp01((Time.time - shownAt) / 0.22f), e = grow - 1;
            shown.transform.localScale = fullScale * Mathf.Max(0.05f, 1 + e * e * (2.7f * e + 1.7f));
            float bob = Mathf.Sin(Time.time * 6f) * 0.015f;
            Vector3 up = transform.up, right = transform.right;
            Vector3 centre = carryPoint.position;
            Animator animator = selection != null && selection.animators != null && selection.Current < selection.animators.Length ? selection.animators[selection.Current] : null;
            if (animator != null && animator.isHuman && animator.isActiveAndEnabled)
            {
                Transform arm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm), fore = animator.GetBoneTransform(HumanBodyBones.RightLowerArm), hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (arm != null && fore != null && hand != null)
                {
                    // Short cartoon arms: the hand rests a little out and forward from the hip, the item hangs from it.
                    float reach = Vector3.Distance(arm.position, fore.position) + Vector3.Distance(fore.position, hand.position);
                    Vector3 target = arm.position + right * sideways - up * reach * 0.8f + transform.forward * 0.08f;
                    Reach(arm, fore, hand, target);
                    centre = hand.position + right * 0.03f - up * half * 0.75f;
                    // Never drag along the ground.
                    float lowest = transform.position.y + half + 0.04f;
                    if (centre.y < lowest) centre += up * (lowest - centre.y);
                }
            }
            shown.transform.position = centre + up * bob - carryPoint.TransformVector(centreOffset);
        }
        // Straight arm pointing from the shoulder at the target (simple and stable for chunky cartoon arms).
        private static void Reach(Transform upper, Transform lower, Transform hand, Vector3 target)
        {
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, target - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
        }
        private void Show(string id)
        {
            shownId = id;
            if (shown != null) Destroy(shown);
            shown = null;
            if (id == null || carryPoint == null || game == null) return;
            WasteItem item = System.Array.Find(game.wasteItems, w => w != null && w.instanceId == id);
            Transform model = item != null ? item.transform.Find("모델") : null;
            if (model == null) return;
            shown = Instantiate(model.gameObject, carryPoint, false);
            shown.name = "손에 든 물건";
            shown.SetActive(true);
            foreach (var collider in shown.GetComponentsInChildren<Collider>()) Destroy(collider);
            shown.transform.localPosition = Vector3.zero; shown.transform.localRotation = model.localRotation;
            // Fit the largest side to `size`; remember where the model's centre sits relative to its pivot.
            Bounds bounds = Measure(shown);
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > 0.001f) shown.transform.localScale = model.localScale * (size / largest);
            bounds = Measure(shown);
            half = bounds.extents.y;
            centreOffset = carryPoint.InverseTransformVector(bounds.center - shown.transform.position);
            fullScale = shown.transform.localScale; shownAt = Time.time;
        }
        private static Bounds Measure(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(target.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
