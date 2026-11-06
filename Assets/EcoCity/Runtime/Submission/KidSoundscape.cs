using System.Collections;
using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    // Everything you hear besides the voice: a music loop per region (menu music on the title, the map and the results)
    // that crossfades as you travel, ambience that turns from smoggy wind into birds, waves or market chatter as the
    // region gets clean, footsteps that match the ground, and small sounds for picking up, preparing, throwing and the lid.
    // Without an AlphaFlow (title, character scenes) it simply plays the menu music.
    public sealed class KidSoundscape : MonoBehaviour
    {
        /// <summary>Set by the settings screen (0..1).</summary>
        public static float MusicVolume = SubmissionPreferences.DefaultMusic, EffectsVolume = SubmissionPreferences.DefaultVolume;
        public AlphaFlow flow;
        public KidRegions regions;
        public KidEnvironmentMood mood;
        public KidFinale finale;
        [Header("Music")]
        public AudioClip menuMusic;
        public AudioClip[] regionMusic;
        public AudioClip regionSavedJingle, finaleJingle;
        public float musicLevel = 0.7f, fadeSeconds = 1.2f;
        [Header("Ambience")]
        public AudioClip[] regionAmbience;
        public AudioClip smogAmbience;
        public float ambienceLevel = 0.6f;
        [Header("Effects")]
        public AudioClip[] grassSteps, sandSteps, pathSteps;
        public AudioClip pickup, throwSound, lid, empty, separate, wash, wrong;
        public float effectsLevel = 1f, stepLevel = 0.45f;
        private AudioSource[] music;
        private AudioSource ambience, smog, effects;
        private int active;
        private float fade = 1, lastStep;
        private int ambienceRegion = -1;
        private bool finaleWas;
        private Preparation prepared;

        private void Awake()
        {
            music = new[] { Source(true), Source(true) };
            ambience = Source(true); smog = Source(true); effects = Source(false);
            var saved = TitleMenu.LoadPreferences();
            MusicVolume = saved.music; EffectsVolume = saved.volume;
        }
        private AudioSource Source(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0; source.volume = 0;
            return source;
        }
        private void OnEnable()
        {
            AnimationEventSink.Footstep += OnFootstep;
            if (flow != null && flow.game != null) flow.game.ResultPublished += OnResult;
        }
        private void OnDisable()
        {
            AnimationEventSink.Footstep -= OnFootstep;
            if (flow != null && flow.game != null) flow.game.ResultPublished -= OnResult;
        }
        private void Update()
        {
            int region = flow != null && regions != null ? regions.RegionAt(flow.player.transform.position) : -1;
            bool inGame = flow != null && flow.game != null && flow.game.Session != null;
            AlphaScreen screen = inGame ? flow.Screens.Current : AlphaScreen.Title;
            bool exploring = inGame && (screen == AlphaScreen.Playing || screen == AlphaScreen.Paused || screen == AlphaScreen.Help) && (regions == null || !regions.MapOpen);
            // Music: the region's loop while exploring, the menu loop elsewhere; jingles over a quiet bed during the endings.
            AudioClip wanted = exploring && region >= 0 && regionMusic != null && region < regionMusic.Length ? regionMusic[region] : menuMusic;
            bool finalePlaying = finale != null && finale.Playing;
            if (finalePlaying && !finaleWas)
            {
                var jingle = finale.RegionMode ? regionSavedJingle : finaleJingle;
                if (jingle != null) effects.PlayOneShot(jingle, 1f);
            }
            finaleWas = finalePlaying;
            if (wanted != music[active].clip)
            {
                active = 1 - active; fade = 0;
                music[active].clip = wanted; music[active].time = 0;
                if (wanted != null) music[active].Play();
            }
            fade = Mathf.MoveTowards(fade, 1, Time.unscaledDeltaTime / fadeSeconds);
            float level = musicLevel * MusicVolume * (finalePlaying ? 0.3f : 1) * (screen == AlphaScreen.Paused && exploring ? 0.6f : 1);
            music[active].volume = level * fade;
            music[1 - active].volume = level * (1 - fade);
            if (fade >= 1 && music[1 - active].isPlaying) music[1 - active].Stop();

            // Ambience: smog wind when dirty, the region's own sounds when clean.
            if (exploring && regionAmbience != null && region >= 0 && region < regionAmbience.Length)
            {
                if (region != ambienceRegion) { ambienceRegion = region; ambience.clip = regionAmbience[region]; if (ambience.clip != null) ambience.Play(); }
                if (smog.clip != smogAmbience) { smog.clip = smogAmbience; if (smog.clip != null) smog.Play(); }
                float clean = mood != null ? mood.Shown : 1;
                float quiet = screen == AlphaScreen.Playing ? 1 : 0.5f;
                ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceLevel * MusicVolume * Mathf.Lerp(0.2f, 1, clean) * quiet, Time.unscaledDeltaTime);
                smog.volume = Mathf.MoveTowards(smog.volume, ambienceLevel * MusicVolume * (1 - clean) * 0.7f * quiet, Time.unscaledDeltaTime);
            }
            else
            {
                ambience.volume = Mathf.MoveTowards(ambience.volume, 0, Time.unscaledDeltaTime);
                smog.volume = Mathf.MoveTowards(smog.volume, 0, Time.unscaledDeltaTime);
            }
            effects.volume = effectsLevel * EffectsVolume * 1.6f;
        }
        private void OnFootstep(Vector3 at)
        {
            if (flow == null || flow.player == null || Time.time - lastStep < 0.2f) return;
            if ((at - flow.player.transform.position).sqrMagnitude > 4) return;
            if (flow.Screens.Current != AlphaScreen.Playing) return;
            lastStep = Time.time;
            int region = regions != null ? regions.RegionAt(at) : 0;
            var set = region == 1 ? sandSteps : region == 2 ? pathSteps : grassSteps;
            if (set == null || set.Length == 0) return;
            effects.pitch = Random.Range(0.92f, 1.08f);
            effects.PlayOneShot(set[Random.Range(0, set.Length)], stepLevel);
            effects.pitch = 1;
        }
        private void OnResult(ActionResult result)
        {
            var session = flow.game.Session;
            switch (result.Code)
            {
                case ResultCode.PickedUp: Play(pickup, 1f); prepared = session.CompletedPreparation; break;
                case ResultCode.Prepared:
                    Preparation added = session.CompletedPreparation & ~prepared; prepared = session.CompletedPreparation;
                    Play((added & Preparation.Empty) != 0 ? empty : (added & Preparation.Separate) != 0 ? separate : wash, 1f);
                    break;
                case ResultCode.Accepted: Play(throwSound, 0.95f); StartCoroutine(Later(lid, 0.38f)); prepared = Preparation.None; break;
                case ResultCode.WrongBin: case ResultCode.MissingPreparation: Play(wrong, 0.8f); break;
            }
        }
        private IEnumerator Later(AudioClip clip, float seconds) { yield return new WaitForSeconds(seconds); Play(clip, 1f); }
        private void Play(AudioClip clip, float level) { if (clip != null && effects != null) effects.PlayOneShot(clip, level); }
    }
}
