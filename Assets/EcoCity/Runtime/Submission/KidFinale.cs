using System.Collections.Generic;
using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    // "에코시티를 구했어요!" ending, told with pictures rather than words. When the last item is cleared:
    // the camera lifts into a slow aerial orbit, the grey smog clears (KidEnvironmentMood), a glowing restoration
    // ring spreads from the last bin and, as it passes, flowers bloom everywhere and trees bounce back to life;
    // butterflies fly out, a rainbow rises, fireworks and confetti follow, the friends jump for joy, and only at the
    // end a short title banner pops up. AlphaFlow.finishDelay (= duration, set by the builder) holds the results screen.
    // Nothing here changes the saved game.
    [DefaultExecutionOrder(300)]
    public sealed class KidFinale : MonoBehaviour
    {
        public AlphaFlow flow;
        public KidVoice voice;
        public KidEnvironmentMood mood;
        public Vector3 parkCentre = new Vector3(0, 0, -2);
        public float duration = 9f;
        [Tooltip("Speed of the restoration ring (m/s).")]
        public float waveSpeed = 12f;
        public ParticleSystem fireworks, confetti, butterflies;
        public Renderer rainbow, ring;
        [Tooltip("Trees and bushes that bounce when the ring reaches them.")]
        public Transform[] bounce;
        public Transform helper;
        [Header("Banner (on the game canvas)")]
        public GameObject banner;
        public RectTransform bannerCard;
        public CanvasGroup flash;
        public RectTransform[] bannerStars;
        public float bannerAt = 5.2f;
        public TMPro.TMP_Text bannerTitle;
        public string finalTitle = "에코시티를 구했어요!";
        [Tooltip("Only flowers and trees this close to the centre take part (the other regions stay as they are).")]
        public float regionRadius = 45f;
        public Color[] fireworkColors =
        {
            new Color(1f, 0.55f, 0.75f), new Color(0.64f, 0.55f, 1f), new Color(0.47f, 0.8f, 1f),
            new Color(0.45f, 0.88f, 0.66f), new Color(1f, 0.84f, 0.38f), new Color(1f, 0.62f, 0.45f),
        };
        private bool playing, butterfliesOut, confettiOn, regionMode;
        private float bannerTime;
        private float startedAt, nextBurst;
        private Vector3 cameraFrom, waveOrigin; private Quaternion cameraFromRotation;
        private float orbitStart;
        private Transform playerModel, helperModel;
        private Vector3 playerModelBase, helperModelBase;
        private Color rainbowColor, ringColor;
        private Vector3[] bounceBase;
        private float[] bounceAt;
        private readonly List<Transform> waiting = new List<Transform>();
        public bool Playing { get { return playing; } }
        /// <summary>True while the short "region saved" version plays.</summary>
        public bool RegionMode { get { return regionMode; } }

        private void OnEnable()
        {
            if (flow == null) return;
            flow.Completing += Begin; flow.ScreenChanged += OnScreenChanged;
        }
        private void OnDisable()
        {
            if (flow == null) return;
            flow.Completing -= Begin; flow.ScreenChanged -= OnScreenChanged;
            if (playing) Stop();
        }
        private void Start()
        {
            if (banner != null) banner.SetActive(false);
            if (rainbow != null) { rainbowColor = rainbow.sharedMaterial.color; rainbow.gameObject.SetActive(false); }
            if (ring != null) { ringColor = ring.sharedMaterial.color; ring.gameObject.SetActive(false); }
            bounceBase = new Vector3[bounce != null ? bounce.Length : 0];
            for (int i = 0; i < bounceBase.Length; i++) if (bounce[i] != null) bounceBase[i] = bounce[i].localScale;
            bounceAt = new float[bounceBase.Length];
        }
        public void Begin() { Begin(false, finalTitle); }
        /// <summary>Short "region saved" version: same pictures around one region, banner sooner, no results afterwards.</summary>
        public void PlayRegion(Vector3 centre, string title) { parkCentre = centre; Begin(true, title); }
        public void StopRegion() { if (playing) Stop(); }
        private void Begin(bool region, string title)
        {
            if (playing || flow == null) return;
            playing = true; butterfliesOut = false; confettiOn = false; regionMode = region;
            bannerTime = region ? 3.4f : bannerAt;
            if (bannerTitle != null) bannerTitle.text = title;
            // Effects that live at fixed spots follow the region being celebrated.
            if (confetti != null) confetti.transform.position = parkCentre + Vector3.up * 14;
            if (butterflies != null) butterflies.transform.position = parkCentre + Vector3.up * 2.5f;
            startedAt = Time.unscaledTime; nextBurst = startedAt + 3.4f;
            var view = flow.player.viewCamera.transform;
            cameraFrom = view.position; cameraFromRotation = view.rotation;
            // Start the orbit behind the way the player was looking, so the park stays in front of the camera.
            Vector3 facing = view.forward; facing.y = 0; if (facing.sqrMagnitude < 0.001f) facing = Vector3.forward;
            facing.Normalize(); orbitStart = Mathf.Atan2(-facing.x, facing.z) * Mathf.Rad2Deg;
            // The restoration ring starts where the last item went in.
            waveOrigin = flow.player.transform.position; waveOrigin.y = 0.05f;
            flow.ui.externalOverlay = true; flow.ui.Refresh();
            var selection = flow.player.GetComponent<CharacterSelection>();
            playerModel = selection != null && selection.characters != null && selection.Current < selection.characters.Length ? selection.characters[selection.Current].transform : null;
            if (playerModel != null) playerModelBase = playerModel.localPosition;
            helperModel = helper != null ? helper.Find("모델") : null;
            if (helperModel != null) helperModelBase = helperModel.localPosition;
            for (int i = 0; i < bounceAt.Length; i++) bounceAt[i] = bounce[i] != null && Flat(bounce[i].position - parkCentre) < regionRadius ? -1 : float.MaxValue;
            waiting.Clear();
            if (mood != null && mood.flowers != null)
                foreach (var flower in mood.flowers)
                    if (flower != null && !flower.gameObject.activeSelf && Flat(flower.position - parkCentre) < regionRadius) waiting.Add(flower);
            if (rainbow != null) rainbow.gameObject.SetActive(true);
            if (ring != null) { ring.gameObject.SetActive(true); ring.transform.position = waveOrigin; }
            if (banner != null) banner.SetActive(false);
            // The whole city is saved: the last haze lifts and everything shines.
            if (!region && mood != null) mood.Sparkle(parkCentre);
        }
        private void LateUpdate()
        {
            if (!playing) return;
            float t = Time.unscaledTime - startedAt;
            var view = flow.player.viewCamera.transform;
            // Camera: ease up and out from behind the player into a slow orbit above the whole park.
            float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 2.6f));
            float angle = (orbitStart + t * 9f) * Mathf.Deg2Rad;
            Vector3 aerial = parkCentre + new Vector3(Mathf.Sin(angle) * 24, 15, -Mathf.Cos(angle) * 24);
            Quaternion aerialLook = Quaternion.LookRotation(parkCentre + Vector3.up * 1.5f - aerial);
            view.position = Vector3.Lerp(cameraFrom, aerial, rise);
            view.rotation = Quaternion.Slerp(cameraFromRotation, aerialLook, rise);

            // Restoration ring: grows over the park; flowers bloom and trees bounce as it passes.
            float radius = Mathf.Max(0, t - 0.4f) * waveSpeed;
            if (ring != null)
            {
                ring.transform.localScale = new Vector3(radius * 2.2f, radius * 2.2f, 1);
                var c = ringColor; c.a = ringColor.a * Mathf.Clamp01(1 - radius / 55f); ring.material.color = c;
                if (radius > 60) ring.gameObject.SetActive(false);
            }
            for (int i = waiting.Count - 1; i >= 0; i--)
                if (Flat(waiting[i].position - waveOrigin) <= radius)
                {
                    if (mood != null) mood.Bloom(waiting[i]);
                    // A little sparkle where a flower bed pops up.
                    if (fireworks != null && i % 4 == 0)
                        fireworks.Emit(new ParticleSystem.EmitParams { position = waiting[i].position + Vector3.up * 0.4f, startColor = Color.white, startSize = 0.25f, startLifetime = 0.6f, velocity = Vector3.up * 2 }, 1);
                    waiting.RemoveAt(i);
                }
            for (int i = 0; i < bounceAt.Length; i++)
            {
                if (bounce[i] == null) continue;
                if (bounceAt[i] < 0 && Flat(bounce[i].position - waveOrigin) <= radius) bounceAt[i] = t;
                if (bounceAt[i] < 0 || bounceAt[i] == float.MaxValue) continue;
                float b = (t - bounceAt[i]) / 0.7f;
                float squash = b < 1 ? Mathf.Sin(b * Mathf.PI * 2) * 0.16f * (1 - b) : 0;
                bounce[i].localScale = new Vector3(bounceBase[i].x * (1 - squash * 0.5f), bounceBase[i].y * (1 + squash), bounceBase[i].z * (1 - squash * 0.5f));
            }
            // Butterflies fly out over the waking park.
            if (!butterfliesOut && t >= 2.0f && butterflies != null) { butterfliesOut = true; butterflies.Emit(60); }
            // A rainbow rises behind the park.
            if (rainbow != null)
            {
                var c = rainbowColor; c.a = rainbowColor.a * Mathf.Clamp01((t - 2.4f) / 2f);
                rainbow.material.color = c;
                Vector3 away = parkCentre - view.position; away.y = 0; away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
                rainbow.transform.position = parkCentre + away * 36 + Vector3.up * (3 - 6 * (1 - Mathf.Clamp01((t - 2.4f) / 2f)));
                rainbow.transform.rotation = Quaternion.LookRotation(away);
            }
            // Celebration: fireworks and confetti once the park is green again.
            if (fireworks != null && Time.unscaledTime >= nextBurst)
            {
                nextBurst = Time.unscaledTime + 0.4f;
                fireworks.Emit(new ParticleSystem.EmitParams
                {
                    position = parkCentre + new Vector3(Random.Range(-16f, 16f), Random.Range(9f, 15f), Random.Range(-10f, 14f)),
                    startColor = fireworkColors[Random.Range(0, fireworkColors.Length)], applyShapeToPosition = true,
                }, 70);
            }
            if (!confettiOn && t >= 4f && confetti != null) { confettiOn = true; confetti.Play(); }
            // The friends jump for joy once the ring has passed them.
            float hop = t > 0.8f ? Mathf.Abs(Mathf.Sin(t * 5.5f)) * 0.35f : 0;
            if (playerModel != null) playerModel.localPosition = playerModelBase + Vector3.up * hop;
            if (helperModel != null) helperModel.localPosition = helperModelBase + Vector3.up * (t > 1.2f ? Mathf.Abs(Mathf.Sin(t * 5.5f + 1.2f)) * 0.3f : 0);
            // Only at the end: a short title banner.
            if (banner != null && t >= bannerTime && !banner.activeSelf)
            {
                banner.SetActive(true); banner.transform.SetAsLastSibling();
                if (voice != null) voice.Say(regionMode ? "region_saved" : "finale", true);
            }
            if (banner != null && banner.activeSelf)
            {
                float b = t - bannerTime;
                if (flash != null) flash.alpha = Mathf.Clamp01(1 - b / 0.5f) * 0.6f;
                if (bannerCard != null)
                {
                    float pop = b < 0.45f ? Mathf.Sin(b / 0.45f * Mathf.PI * 0.5f) * 1.12f : 1 + (0.12f * Mathf.Exp(-(b - 0.45f) * 6) * Mathf.Cos((b - 0.45f) * 14));
                    bannerCard.localScale = Vector3.one * Mathf.Max(0.01f, pop);
                }
                if (bannerStars != null)
                    for (int i = 0; i < bannerStars.Length; i++)
                        if (bannerStars[i] != null)
                        {
                            bannerStars[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(b * 3 + i) * 14);
                            bannerStars[i].localScale = Vector3.one * (1 + Mathf.Sin(b * 6 + i * 1.7f) * 0.08f);
                        }
            }
        }
        private static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
        private void OnScreenChanged()
        {
            if (playing && !regionMode && flow.Screens.Current != AlphaScreen.Playing) Stop();
            // A new round starts with an ordinary sky again.
            else if (!playing && flow.Screens.Current == AlphaScreen.Playing && rainbow != null && rainbow.gameObject.activeSelf) rainbow.gameObject.SetActive(false);
        }
        // Results (or the map) take over: hand the camera back and tidy up; the flowers stay as the reward.
        private void Stop()
        {
            playing = false; regionMode = false;
            flow.ui.externalOverlay = false; flow.ui.Refresh();
            if (rainbow != null) rainbow.gameObject.SetActive(false);
            if (banner != null) banner.SetActive(false);
            if (ring != null) ring.gameObject.SetActive(false);
            if (confetti != null) confetti.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (mood != null) foreach (var flower in waiting) mood.Bloom(flower, false);
            waiting.Clear();
            for (int i = 0; i < bounceBase.Length; i++) if (bounce[i] != null) bounce[i].localScale = bounceBase[i];
            if (playerModel != null) playerModel.localPosition = playerModelBase;
            if (helperModel != null) helperModel.localPosition = helperModelBase;
            if (flow.player != null) flow.player.PlaceCamera();
        }
    }
}
