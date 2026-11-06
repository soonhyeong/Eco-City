using System.Collections.Generic;
using UnityEngine;

namespace EcoCity
{
    // Shows how clean the park is without words: a dirty park sits under grey smog with a dull sky and weak sun;
    // every cleared item thins the smog and makes more flower patches bloom, and the fountain starts spraying
    // once the park is half clean. The ending scene (KidFinale) blooms the rest with its restoration wave.
    // When the whole Eco-City is saved, Sparkle() lifts the last haze: no fog, a vivid sky, a brighter sun and
    // ambient light, and twinkling stars over the place, until a new game starts dirty again.
    public sealed class KidEnvironmentMood : MonoBehaviour
    {
        public EcoGame game;
        [Tooltip("With regions, the look follows the region the player stands in and flowers bloom per region.")]
        public KidRegions regions;
        public Camera view;
        public Light sun;
        [Tooltip("Flower patches in reveal order; they start hidden.")]
        public Transform[] flowers;
        [Range(0, 1)] public float flowersByPlay = 0.45f;
        public ParticleSystem fountain, butterflies;
        public float maxFog = 0.024f;
        public Color fogDirty = new Color(0.64f, 0.64f, 0.6f);
        public Color skyDirty = new Color(0.68f, 0.71f, 0.72f), skyClean = new Color(0.62f, 0.83f, 0.97f);
        public Color sunDirty = new Color(0.86f, 0.83f, 0.76f), sunClean = new Color(1f, 0.97f, 0.92f);
        public float sunIntensityDirty = 0.82f, sunIntensityClean = 1.05f;
        [Tooltip("Gradient sky (EcoCity/KidSky); its horizon follows the colours above, its top these.")]
        public Material sky;
        public Color skyTopDirty = new Color(0.6f, 0.63f, 0.66f), skyTopClean = new Color(0.36f, 0.64f, 0.96f), skyTopSparkle = new Color(0.22f, 0.56f, 1f);
        [Header("Sparkle (Eco-City saved)")]
        public ParticleSystem twinkles;
        public Color skySparkle = new Color(0.45f, 0.78f, 1f), sunSparkle = new Color(1f, 0.96f, 0.86f);
        public float sunIntensitySparkle = 1.32f, ambientBoost = 1.25f, sparkleSeconds = 1.5f;
        [Tooltip("How quickly the look follows the cleanliness (per second).")]
        public float follow = 0.5f;
        /// <summary>Smoothed cleanliness the scene currently shows, 0..1.</summary>
        public float Shown { get; private set; }
        private float lastClean = -1;
        private readonly List<Transform> growing = new List<Transform>();
        private readonly List<float> grownAt = new List<float>();
        private Vector3[] baseScale;
        private int[] flowerRegion;
        private float sparkle, sparkleTarget;
        private Color ambientSky, ambientEquator, ambientGround;
        /// <summary>0..1 while the saved city shines.</summary>
        public float Sparkling { get { return sparkle; } }

        private void Awake()
        {
            baseScale = new Vector3[flowers != null ? flowers.Length : 0];
            flowerRegion = new int[baseScale.Length];
            for (int i = 0; i < flowerRegion.Length; i++) if (flowers[i] != null && regions != null) flowerRegion[i] = regions.RegionAt(flowers[i].position);
            for (int i = 0; i < baseScale.Length; i++)
                if (flowers[i] != null) { baseScale[i] = flowers[i].localScale; flowers[i].gameObject.SetActive(false); }
        }
        private void Start()
        {
            // Work on a copy so playing never edits the sky asset.
            if (sky != null) { sky = new Material(sky); RenderSettings.skybox = sky; }
            ambientSky = RenderSettings.ambientSkyColor; ambientEquator = RenderSettings.ambientEquatorColor; ambientGround = RenderSettings.ambientGroundColor;
            if (twinkles != null) twinkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            float clean = Clean();
            Shown = Here(); lastClean = clean;
            RevealForPlay(false);
            Apply(Shown);
        }
        private void Update()
        {
            float clean = Clean();
            // A new game starts dirty again: hide everything that bloomed.
            if (clean < lastClean - 0.001f) { HideAll(); Shown = clean; StopSparkle(); }
            lastClean = clean;
            Shown = Mathf.MoveTowards(Shown, Here(), follow * Time.deltaTime);
            sparkle = Mathf.MoveTowards(sparkle, sparkleTarget, Time.unscaledDeltaTime / Mathf.Max(0.01f, sparkleSeconds));
            RevealForPlay(true);
            Apply(Shown);
            for (int i = growing.Count - 1; i >= 0; i--)
            {
                // Pop up with a little overshoot.
                float t = (Time.time - grownAt[i]) / 0.45f;
                int index = System.Array.IndexOf(flowers, growing[i]);
                float s = t >= 1 ? 1 : Mathf.Sin(t * Mathf.PI * 0.5f) * (1 + 0.25f * Mathf.Sin(t * Mathf.PI));
                growing[i].localScale = baseScale[index] * Mathf.Max(0.01f, s);
                if (t >= 1) { growing.RemoveAt(i); grownAt.RemoveAt(i); }
            }
        }
        private float Clean() { return game != null && game.Session != null ? game.Session.Cleanliness / 100f : 0; }
        // Cleanliness the camera should show: the player's own region, or the whole park without regions.
        private float Here()
        {
            if (regions == null || game == null || game.Session == null || regions.flow == null) return Clean();
            return regions.RegionClean(regions.RegionAt(regions.flow.player.transform.position));
        }
        /// <summary>Jump straight to the current region's look (after travelling).</summary>
        public void Snap() { Shown = Here(); Apply(Shown); }
        /// <summary>The whole city is clean: clear every bit of haze and let the place twinkle around the centre.</summary>
        public void Sparkle(Vector3 centre)
        {
            sparkleTarget = 1; Shown = 1;
            if (twinkles != null) { twinkles.transform.position = centre + Vector3.up * 1.5f; twinkles.Play(); }
        }
        private void StopSparkle()
        {
            sparkle = sparkleTarget = 0;
            if (twinkles != null) twinkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void Apply(float clean)
        {
            float smog = Mathf.Pow(1 - Mathf.Clamp01(clean), 0.7f);
            smog *= 1 - sparkle;
            RenderSettings.fog = smog > 0.01f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = maxFog * smog;
            RenderSettings.fogColor = Color.Lerp(skyClean, fogDirty, smog);
            Color horizon = Color.Lerp(Color.Lerp(skyClean, skySparkle, sparkle), skyDirty, smog);
            if (view != null) view.backgroundColor = horizon;
            if (sky != null)
            {
                sky.SetColor("_Horizon", Color.Lerp(horizon, Color.white, 0.35f * (1 - smog)));
                sky.SetColor("_Top", Color.Lerp(Color.Lerp(skyTopClean, skyTopSparkle, sparkle), skyTopDirty, smog));
                sky.SetColor("_Bottom", horizon * 0.92f);
                sky.SetColor("_SunColor", Color.Lerp(new Color(1f, 0.95f, 0.8f), new Color(0.5f, 0.5f, 0.48f), smog));
            }
            if (sun != null)
            {
                sun.color = Color.Lerp(Color.Lerp(sunClean, sunSparkle, sparkle), sunDirty, smog);
                sun.intensity = Mathf.Lerp(Mathf.Lerp(sunIntensityClean, sunIntensitySparkle, sparkle), sunIntensityDirty, smog);
            }
            float boost = Mathf.Lerp(1, ambientBoost, sparkle);
            RenderSettings.ambientSkyColor = ambientSky * boost; RenderSettings.ambientEquatorColor = ambientEquator * boost; RenderSettings.ambientGroundColor = ambientGround * boost;
            if (fountain != null)
            {
                bool spray = (regions != null ? regions.RegionClean(0) : clean) >= 0.5f;
                if (spray && !fountain.isPlaying) fountain.Play();
                else if (!spray && fountain.isPlaying) fountain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        // Part of each region's flowers bloom during play, in order, as that region gets cleaner.
        private void RevealForPlay(bool animate)
        {
            if (flowers == null) return;
            for (int region = 0; region < 3; region++)
            {
                float clean = regions != null ? regions.RegionClean(region) : (region == 0 ? Clean() : 0);
                int total = 0; foreach (int r in flowerRegion) if (r == region) total++;
                int target = Mathf.FloorToInt(total * flowersByPlay * clean), shown = 0;
                for (int i = 0; i < flowers.Length && shown < target; i++)
                {
                    if (flowerRegion[i] != region || flowers[i] == null) continue;
                    shown++;
                    if (!flowers[i].gameObject.activeSelf) Bloom(flowers[i], animate);
                }
                if (regions == null) break;
            }
        }
        public void Bloom(Transform flower, bool animate = true)
        {
            if (flower == null || flower.gameObject.activeSelf) return;
            flower.gameObject.SetActive(true);
            int index = System.Array.IndexOf(flowers, flower);
            if (!animate || index < 0) { if (index >= 0) flower.localScale = baseScale[index]; return; }
            flower.localScale = baseScale[index] * 0.01f;
            growing.Add(flower); grownAt.Add(Time.time);
        }
        private void HideAll()
        {
            growing.Clear(); grownAt.Clear();
            for (int i = 0; i < flowers.Length; i++) if (flowers[i] != null) { flowers[i].gameObject.SetActive(false); flowers[i].localScale = baseScale[i]; }
            if (butterflies != null) butterflies.Clear();
        }
    }
}
