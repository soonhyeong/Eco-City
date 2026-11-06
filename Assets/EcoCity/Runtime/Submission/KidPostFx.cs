using UnityEngine;

namespace EcoCity
{
    // Camera look (built-in pipeline): soft bloom, pastel colour and a light vignette. The dirty city looks washed out;
    // as it gets clean the colours come back, and the saved city glows a little more. The UI canvas is drawn on top, untouched.
    [RequireComponent(typeof(Camera))]
    public sealed class KidPostFx : MonoBehaviour
    {
        public Shader shader;
        [Tooltip("Optional: colours follow the city's cleanliness and the final sparkle.")]
        public KidEnvironmentMood mood;
        public float threshold = 0.95f, knee = 0.2f, bloom = 0.25f, bloomSaved = 0.38f;
        public float dirtySaturation = 0.72f, cleanSaturation = 1.12f, contrast = 1.05f, vignette = 0.28f;
        public Color tint = new Color(1f, 0.985f, 0.96f);
        [Tooltip("Settings toggle; off draws the plain picture.")]
        public bool effectOn = true;
        private Material material;

        private void OnDisable() { if (material != null) { Destroy(material); material = null; } }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!effectOn || shader == null || !shader.isSupported) { Graphics.Blit(source, destination); return; }
            if (material == null) material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            float clean = mood != null ? mood.Shown : 1, saved = mood != null ? mood.Sparkling : 0;
            material.SetFloat("_Threshold", threshold); material.SetFloat("_Knee", knee);
            material.SetFloat("_Bloom", Mathf.Lerp(bloom, bloomSaved, saved));
            material.SetFloat("_Saturation", Mathf.Lerp(dirtySaturation, cleanSaturation, clean) + saved * 0.06f);
            material.SetFloat("_Contrast", contrast); material.SetFloat("_Vignette", vignette); material.SetColor("_Tint", tint);
            // Bloom: bright parts at half size, blurred down three times and added back up.
            const int levels = 4;
            var chain = new RenderTexture[levels];
            int w = Mathf.Max(1, source.width / 2), h = Mathf.Max(1, source.height / 2);
            chain[0] = RenderTexture.GetTemporary(w, h, 0, source.format);
            Graphics.Blit(source, chain[0], material, 0);
            for (int i = 1; i < levels; i++)
            {
                w = Mathf.Max(1, w / 2); h = Mathf.Max(1, h / 2);
                chain[i] = RenderTexture.GetTemporary(w, h, 0, source.format);
                Graphics.Blit(chain[i - 1], chain[i], material, 1);
            }
            for (int i = levels - 1; i > 0; i--) Graphics.Blit(chain[i], chain[i - 1], material, 2);
            material.SetTexture("_BloomTex", chain[0]);
            Graphics.Blit(source, destination, material, 3);
            foreach (var rt in chain) RenderTexture.ReleaseTemporary(rt);
        }
    }
}
