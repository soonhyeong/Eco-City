using System;

namespace EcoCity.Core
{
    [Serializable]
    public sealed class SubmissionPreferences
    {
        public const float DefaultVolume = 0.8f, DefaultMusic = 0.8f;
        public float sensitivity = 2f;
        public float volume = DefaultVolume;
        public float music = DefaultMusic;
        // Bigger buttons and text; the soft screen look (bloom, colours) can be turned off.
        public bool bigText;
        public bool effects = true;
        // Lighter shadows and no anti-aliasing for slow computers.
        public bool lightGraphics;
        public bool invertY;
        // Full screen by default: the title and HUD are laid out for a filled screen.
        public bool fullscreen = true;
        // 0 = saved before the louder sound defaults; such settings are raised to the new defaults once.
        public int mixRevision;
        public void Normalize()
        {
            sensitivity = IsFinite(sensitivity) ? Math.Max(0.25f, Math.Min(4f, sensitivity)) : 2f;
            volume = IsFinite(volume) ? Math.Max(0, Math.Min(1, volume)) : DefaultVolume;
            music = IsFinite(music) ? Math.Max(0, Math.Min(1, music)) : DefaultMusic;
            if (mixRevision < 1) { volume = Math.Max(volume, DefaultVolume); music = Math.Max(music, DefaultMusic); mixRevision = 1; }
        }
        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
