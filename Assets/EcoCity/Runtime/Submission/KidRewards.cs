using UnityEngine;

namespace EcoCity
{
    // Stars earned on the map turn into something to wear: 5 stars give a party hat, all 9 a golden crown.
    // The hat sits on the chosen character's head and follows it.
    public sealed class KidRewards : MonoBehaviour
    {
        public KidRegions regions;
        public CharacterSelection selection;
        public GameObject partyHat, crown;
        public int hatStars = 5, crownStars = 9;
        private int measuredFor = -1;
        private float above;

        private void LateUpdate()
        {
            int stars = regions != null ? regions.TotalStars : 0;
            GameObject worn = stars >= crownStars ? crown : stars >= hatStars ? partyHat : null;
            if (partyHat != null && partyHat.activeSelf != (worn == partyHat)) partyHat.SetActive(worn == partyHat);
            if (crown != null && crown.activeSelf != (worn == crown)) crown.SetActive(worn == crown);
            if (worn == null || selection == null || selection.animators == null || selection.Current >= selection.animators.Length) return;
            var animator = selection.animators[selection.Current];
            var head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null) return;
            if (measuredFor != selection.Current)
            {
                // Distance from the head bone to the top of the hair.
                measuredFor = selection.Current;
                // The meshes may sit beside the Animator, so measure the whole character model.
                float top = head.position.y;
                var model = selection.characters != null && selection.Current < selection.characters.Length && selection.characters[selection.Current] != null
                    ? selection.characters[selection.Current].transform : animator.transform;
                foreach (var r in model.GetComponentsInChildren<Renderer>()) if (r.gameObject != partyHat && r.transform.parent != partyHat.transform && r.transform.parent != crown.transform) top = Mathf.Max(top, r.bounds.max.y);
                above = top > head.position.y + 0.05f ? top - head.position.y - 0.05f : 0.3f;
            }
            // World up: the head bone's own axes differ between character rigs.
            worn.transform.position = head.position + Vector3.up * above;
            worn.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(-8, 0, 6);
        }
    }
}
