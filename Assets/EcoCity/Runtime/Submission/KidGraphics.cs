using UnityEngine;

namespace EcoCity
{
    // "가벼운 화면" setting for slow school computers: hard, short-range shadows, no anti-aliasing and one pixel light.
    // The project's quality values are remembered the first time and put back when the setting is off or the game stops,
    // so playing in the editor never rewrites the quality settings asset.
    public static class KidGraphics
    {
        public const float LightShadowDistance = 30f;
        private static bool captured, light;
        private static ShadowQuality shadows;
        private static ShadowResolution resolution;
        private static float shadowDistance;
        private static int antiAliasing, cascades, pixelLights;
        public static bool Light { get { return light; } }

        public static void Apply(bool on)
        {
            if (!Application.isPlaying) return;
            if (!captured)
            {
                shadows = QualitySettings.shadows; resolution = QualitySettings.shadowResolution; shadowDistance = QualitySettings.shadowDistance;
                antiAliasing = QualitySettings.antiAliasing; cascades = QualitySettings.shadowCascades; pixelLights = QualitySettings.pixelLightCount;
                captured = true;
                Application.quitting += Restore;
            }
            light = on;
            if (on)
            {
                QualitySettings.shadows = shadows == ShadowQuality.Disable ? ShadowQuality.Disable : ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = Mathf.Min(shadowDistance, LightShadowDistance);
                QualitySettings.shadowCascades = 1;
                QualitySettings.antiAliasing = 0;
                QualitySettings.pixelLightCount = 1;
            }
            else Restore();
        }
        private static void Restore()
        {
            if (!captured) return;
            QualitySettings.shadows = shadows; QualitySettings.shadowResolution = resolution; QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = cascades; QualitySettings.antiAliasing = antiAliasing; QualitySettings.pixelLightCount = pixelLights;
        }
    }
}
