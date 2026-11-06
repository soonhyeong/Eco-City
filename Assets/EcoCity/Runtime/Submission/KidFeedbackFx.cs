using System.Collections;
using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    // Picture-and-motion feedback that does not need reading: a correct bin pops and sparkles with an O mark (the O/X quiz sign every Korean child knows),
    // a wrong bin shakes and shows a red X; finishing a preparation step sparkles at its station.
    // A correct item is thrown from the hand into the bin in an arc before the bin pops, and the stars earned float up ("+10 ★").
    public sealed class KidFeedbackFx : MonoBehaviour
    {
        public EcoGame game;
        public PlayerInteractor interactor;
        public KidHeldVisual held;
        public Color scoreColor = new Color(1f, 0.72f, 0.15f);
        public float throwSeconds = 0.38f;
        private int lastScore;
        public ParticleSystem sparkles;
        public TMPro.TMP_Text markPrefab;
        public Color correctColor = new Color(0.25f, 0.75f, 0.3f);
        public Color wrongColor = new Color(0.9f, 0.25f, 0.25f);
        private void OnEnable() { if (game != null) { game.ResultPublished += OnResult; lastScore = game.Session != null ? game.Session.Score : 0; } }
        private void OnDisable() { if (game != null) game.ResultPublished -= OnResult; }
        private void OnResult(ActionResult result)
        {
            int score = game.Session != null ? game.Session.Score : 0, earned = score - lastScore; lastScore = score;
            Interactable target = interactor != null ? interactor.Target : null;
            if (target == null || !isActiveAndEnabled) return;
            Vector3 top = Top(target);
            Vector3 front = Front(target);
            if (result.Code == ResultCode.Accepted)
            {
                var thrown = held != null ? held.Release() : null;
                StartCoroutine(Accepted(thrown, target, top, front, earned));
            }
            else if (result.Code == ResultCode.WrongBin)
            {
                StartCoroutine(Shake(target.transform.Find("모델")));
                StartCoroutine(Mark("X", wrongColor, front));
            }
            else if (result.Code == ResultCode.Prepared) Burst(top, 14);
            else if (result.Code == ResultCode.PickedUp) Burst(top, 10);
        }
        private IEnumerator Accepted(GameObject thrown, Interactable target, Vector3 top, Vector3 front, int earned)
        {
            if (thrown != null)
            {
                // Aim at the lid of the bin body (its sign board sits higher up).
                Vector3 lid = top;
                var body = target != null ? target.transform.Find("모델") : null;
                if (body != null)
                {
                    var renderers = body.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
                        lid = new Vector3(b.center.x, b.max.y + 0.08f, b.center.z);
                    }
                }
                Vector3 from = thrown.transform.position, size = thrown.transform.localScale;
                Quaternion spin = thrown.transform.rotation;
                for (float t = 0; t < throwSeconds; t += Time.deltaTime)
                {
                    float f = t / throwSeconds;
                    thrown.transform.position = Vector3.Lerp(from, lid, f) + Vector3.up * Mathf.Sin(f * Mathf.PI) * 0.9f;
                    thrown.transform.rotation = spin * Quaternion.Euler(f * 360, f * 200, 0);
                    thrown.transform.localScale = size * Mathf.Lerp(1, 0.45f, f * f);
                    yield return null;
                }
                Destroy(thrown);
            }
            if (target != null) StartCoroutine(Pop(target.transform.Find("모델")));
            Burst(top, 30);
            StartCoroutine(Mark("O", correctColor, front));
            if (earned > 0) StartCoroutine(Mark("+" + earned + " ★", scoreColor, front + Vector3.up * 0.55f, 0.55f));
        }
        private static Vector3 Top(Interactable target)
        {
            float top = target.transform.position.y + 0.6f;
            foreach (var collider in target.GetComponentsInChildren<Collider>()) top = Mathf.Max(top, collider.bounds.max.y);
            return new Vector3(target.transform.position.x, top + 0.2f, target.transform.position.z);
        }
        // In front of the target, below its sign board and facing the player, so the mark is never hidden.
        private Vector3 Front(Interactable target)
        {
            Bounds bounds = new Bounds(target.transform.position + Vector3.up, Vector3.zero);
            foreach (var collider in target.GetComponentsInChildren<Collider>()) bounds.Encapsulate(collider.bounds);
            Vector3 toPlayer = interactor.transform.position - bounds.center; toPlayer.y = 0;
            toPlayer = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.back;
            return new Vector3(bounds.center.x, Mathf.Max(1.1f, bounds.max.y * 0.75f), bounds.center.z) + toPlayer * (Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.45f);
        }
        private void Burst(Vector3 position, int count)
        {
            if (sparkles == null) return;
            sparkles.transform.position = position;
            sparkles.Emit(count);
        }
        private IEnumerator Pop(Transform model)
        {
            if (model == null) yield break;
            Vector3 baseScale = model.localScale;
            for (float t = 0; t < 0.35f; t += Time.deltaTime)
            {
                float s = 1 + Mathf.Sin(t / 0.35f * Mathf.PI) * 0.18f;
                model.localScale = new Vector3(baseScale.x * s, baseScale.y * (2 - s), baseScale.z * s);
                yield return null;
            }
            model.localScale = baseScale;
        }
        private IEnumerator Shake(Transform model)
        {
            if (model == null) yield break;
            Vector3 basePosition = model.localPosition;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                model.localPosition = basePosition + Vector3.right * Mathf.Sin(t * 60f) * 0.07f * (1 - t / 0.45f);
                yield return null;
            }
            model.localPosition = basePosition;
        }
        private IEnumerator Mark(string text, Color color, Vector3 position, float size = 1f)
        {
            if (markPrefab == null) yield break;
            // The "E" bubble steps aside while the mark shows.
            var cues = GetComponent<KidWorldCues>(); if (cues != null) cues.hideBubbleUntil = Time.time + 1f;
            var mark = Instantiate(markPrefab, position, Quaternion.identity);
            mark.gameObject.SetActive(true); mark.text = text; mark.color = color;
            Transform view = interactor != null && interactor.viewCamera != null ? interactor.viewCamera.transform : null;
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                mark.transform.position = position + Vector3.up * t * (size < 1 ? 0.7f : 0.35f);
                if (view != null) mark.transform.rotation = Quaternion.LookRotation(mark.transform.position - view.position);
                mark.transform.localScale = Vector3.one * size * Mathf.Lerp(0.5f, 1f, Mathf.Min(1, t * 6)) * (1 + Mathf.Sin(Mathf.Min(1, t * 4) * Mathf.PI) * 0.25f);
                var c = color; c.a = 1 - Mathf.Max(0, t - 0.6f) / 0.4f; mark.color = c;
                yield return null;
            }
            Destroy(mark.gameObject);
        }
    }
}
