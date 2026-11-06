using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    // Spoken guidance for players who cannot read yet (currently off, see Enabled). Clips are pre-generated (see Validation/GenerateKidVoice.ps1)
    // and looked up by key; the bin is only spoken after repeated mistakes, matching the on-screen hints.
    public sealed class KidVoice : MonoBehaviour
    {
        /// <summary>Spoken lines are off: the game speaks through text, sounds and the helper's bubbles only.
        /// Clips and keys stay connected so the voice can be turned back on here.</summary>
        public static bool Enabled = false;
        public AlphaFlow flow;
        public AudioSource source;
        [Tooltip("Volume follows the effects volume from the settings screen.")]
        public AudioSource volumeFrom;
        public string[] keys;
        public AudioClip[] clips;
        private string lastTutorial, lastCelebration;
        private bool doneSpoken;
        private string pending;
        private bool wasPlaying;
        private void OnEnable()
        {
            if (flow == null || flow.game == null) return;
            flow.game.ResultPublished += OnResult;
            flow.game.MessagePublished += OnMessage;
        }
        private void OnDisable()
        {
            if (flow == null || flow.game == null) return;
            flow.game.ResultPublished -= OnResult;
            flow.game.MessagePublished -= OnMessage;
        }
        private void Update()
        {
            if (flow == null || flow.game == null || flow.game.Session == null) return;
            bool playing = flow.Screens.CanExplore;
            if (playing && !wasPlaying) lastTutorial = null;
            wasPlaying = playing;
            if (!playing) return;
            if (flow.Celebration != lastCelebration)
            {
                lastCelebration = flow.Celebration;
                if (!string.IsNullOrEmpty(lastCelebration)) Say(flow.game.Session.IsComplete ? "complete" : "levelup", true);
            }
            // Tutorial lines wait until the current line has finished.
            string tutorial = TutorialText.Key(flow.game.Session);
            if (tutorial != lastTutorial)
            {
                // "Practice finished" is said once, not again after every menu visit.
                lastTutorial = tutorial; pending = tutorial == "tut_done" && doneSpoken ? null : tutorial;
                if (tutorial == "tut_done") doneSpoken = true;
            }
            if (pending != null && source != null && !source.isPlaying) { Say(pending, false); pending = null; }
        }
        private void OnResult(ActionResult result)
        {
            var game = flow.game;
            switch (result.Code)
            {
                case ResultCode.PickedUp: Say("pick_" + (result.Item != null ? result.Item.Id : ""), true); break;
                case ResultCode.Prepared: Say("done_" + result.Step, true); break;
                case ResultCode.Accepted: if (!game.Session.IsComplete) Say("correct", true); break;
                case ResultCode.WrongBin:
                    Say(game.NeedsBinHint && result.Item != null ? "hint_" + result.Item.Category : "wrong", true); break;
                case ResultCode.MissingPreparation: case ResultCode.PrerequisiteMissing: Say("need_" + result.Step, true); break;
                case ResultCode.EmptyHands: Say("empty", true); break;
                case ResultCode.HandsFull: Say("full", true); break;
                case ResultCode.Locked: Say("locked", true); break;
                case ResultCode.NotRequired: Say("notneeded", true); break;
            }
        }
        // Guide requests: same rule as the text — bin only after repeated mistakes with the item in hand.
        private void OnMessage(string message)
        {
            var session = flow.game.Session;
            if (session == null || !flow.Screens.CanExplore) return;
            if (session.Held == null) { Say("start", true); return; }
            Preparation next = WasteRules.NextStep(session.Held.Required & ~session.CompletedPreparation);
            if (next != Preparation.None) Say("need_" + next, true);
            else Say(flow.game.NeedsBinHint ? "hint_" + session.Held.Category : "guide_find", true);
        }
        public bool Has(string key) { return Find(key) != null; }
        private AudioClip Find(string key)
        {
            if (keys == null || clips == null || key == null) return null;
            for (int i = 0; i < keys.Length && i < clips.Length; i++) if (keys[i] == key) return clips[i];
            return null;
        }
        public void Say(string key, bool interrupt)
        {
            var clip = Find(key);
            if (!Enabled || clip == null || source == null || !isActiveAndEnabled) return;
            if (source.isPlaying && !interrupt) return;
            if (volumeFrom != null) source.volume = Mathf.Clamp01(volumeFrom.volume * 1.6f);
            source.Stop(); source.clip = clip; source.Play();
        }
    }
}
