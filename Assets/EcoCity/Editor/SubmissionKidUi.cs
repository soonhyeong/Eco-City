#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace EcoCity.Editor
{
    // Crisp kid UI kit shared by the game scene and the title scene.
    // Text is TextMeshPro SDF (Jua, NanumGothic fallback for symbols), shapes are high-resolution generated sprites
    // whose corner radius is set per element, and every element is placed by explicit anchors on a 1920x1080 reference.
    // Depth comes from soft shadow sprites and darker "lip" layers instead of Outline/Shadow mesh effects, which blur text.
    public static class SubmissionKidUi
    {
        public const string Root = "Assets/EcoCity/Generated/Submission/KidWorld";
        public const string FontFolder = Root + "/Fonts";
        public const string FontAssetPath = FontFolder + "/Jua SDF.asset";
        public const string FallbackAssetPath = FontFolder + "/NanumGothic SDF.asset";
        public const string NanumPath = "Assets/EcoCity/Fonts/NanumGothic-Regular.ttf";
        public const float Margin = 32;

        // Palette and type scale.
        // Trendy pastel "clay" palette: cocoa text on cream cards, lavender as the main action colour,
        // mint / sky / peach / pink / butter accents, lilac-tinted shadows instead of grey ones.
        public static readonly Color Ink = new Color(0.3f, 0.22f, 0.27f);
        public static readonly Color Soft = new Color(0.56f, 0.49f, 0.57f);
        public static readonly Color Paper = new Color(1f, 0.99f, 0.975f, 0.98f);
        public static readonly Color Cream = new Color(1f, 0.95f, 0.91f);
        public static readonly Color Mint = new Color(0.95f, 0.94f, 1f);
        public static readonly Color Lavender = new Color(0.64f, 0.55f, 1f);
        public static readonly Color Green = new Color(0.36f, 0.8f, 0.62f);
        public static readonly Color Sky = new Color(0.47f, 0.7f, 1f);
        public static readonly Color Orange = new Color(1f, 0.6f, 0.5f);
        public static readonly Color Sun = new Color(1f, 0.83f, 0.38f);
        public static readonly Color Pink = new Color(1f, 0.56f, 0.75f);
        public static readonly Color Gray = new Color(0.73f, 0.7f, 0.84f);
        public static readonly Color Dim = new Color(0.25f, 0.19f, 0.44f, 0.5f);
        public static readonly Color ShadowTint = new Color(0.3f, 0.22f, 0.55f);
        public const string LogoPath = "Assets/EcoCity/Art/Logo/EcoCityLogo.png";
        public const int H1 = 46, Body = 28, Small = 22, Caption = 19, ButtonText = 36;

        public static TMP_FontAsset Font { get; private set; }
        public static Material PlainMaterial { get; private set; }
        public static Material SoftMaterial { get; private set; }
        public static Material StickerMaterial { get; private set; }
        public static Material WorldOutlineMaterial { get; private set; }
        public static Material MarkMaterial { get; private set; }
        public static Sprite RoundSprite { get; private set; }
        public static Sprite CircleSprite { get; private set; }
        public static Sprite ShadowSprite { get; private set; }
        public static Sprite StarSprite { get; private set; }
        public static Sprite LeafSprite { get; private set; }
        public static Sprite CheckSprite { get; private set; }
        public static Sprite FadeSprite { get; private set; }
        public static Sprite RainbowSprite { get; private set; }
        public static Sprite RingSprite { get; private set; }
        public static Sprite ButterflySprite { get; private set; }
        public static Sprite ArrowSprite { get; private set; }
        public static Sprite LockSprite { get; private set; }
        private const float RoundBorder = 48, ShadowBorder = 60;

        // ---------- Assets ----------
        public static void Prepare()
        {
            Directory.CreateDirectory(FontFolder); AssetDatabase.Refresh();
            var fallback = FontAsset(FallbackAssetPath, NanumPath);
            Font = FontAsset(FontAssetPath, SubmissionKidMakeover.KidFontPath);
            if (Font.fallbackFontAssetTable == null || !Font.fallbackFontAssetTable.Contains(fallback))
            { Font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback }; EditorUtility.SetDirty(Font); }
            PlainMaterial = Font.material;
            // Korean lines break between words (spaces), not in the middle of a word.
            if (!TMP_Settings.useModernHangulLineBreakingRules) { TMP_Settings.useModernHangulLineBreakingRules = true; EditorUtility.SetDirty(TMP_Settings.instance); }
            SoftMaterial = Preset("Jua SDF Soft", material =>
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.28f));
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.55f); material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
            });
            StickerMaterial = Preset("Jua SDF Sticker", material =>
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
                material.SetColor(ShaderUtilities.ID_OutlineColor, Color.white); material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.3f, 0.2f, 0.6f, 0.5f));
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.75f); material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.4f);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.2f);
            });
            // World signs: a dark rim keeps white and yellow letters readable on grass and sky.
            WorldOutlineMaterial = Preset("Jua SDF World Outline", material =>
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
                material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.12f, 0.16f, 0.22f)); material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
            });
            // Correct / wrong O and X: a thick white rim on any background.
            MarkMaterial = Preset("Jua SDF Mark", material =>
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
                material.SetColor(ShaderUtilities.ID_OutlineColor, Color.white); material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
            });
            RoundSprite = Shape("UI2_Round", 128, RoundBorder, (x, y) => Alpha(RoundedBox(x, y, 64, 64, RoundBorder)), color: (x, y) =>
            {
                // Clay look (tinted by the Image colour): light from above, a bevel along the rim, a soft darker bottom.
                float depth = Mathf.Clamp01(-RoundedBox(x, y, 64, 64, RoundBorder) / 12f), up = y / 64f;
                float value = 0.93f + up * 0.05f + (1 - depth) * up * 0.12f;
                value = Mathf.Clamp01(value); return new Color(value, value, value);
            });
            CircleSprite = Shape("UI2_Circle", 256, 0, (x, y) => Alpha(Mathf.Sqrt(x * x + y * y) - 126));
            ShadowSprite = Shape("UI2_Shadow", 160, ShadowBorder, (x, y) =>
            {
                // Soft drop shadow: a rounded box blurred over ~26 px.
                float d = RoundedBox(x, y, 52, 52, 30);
                return Mathf.Pow(1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((d + 6) / 32)), 1.6f);
            });
            StarSprite = Shape("UI2_Star", 256, 0, (x, y) =>
            {
                float px = x / 128f, py = y / 128f;
                float angle = Mathf.Atan2(px, py), radius = Mathf.Sqrt(px * px + py * py);
                float t = Mathf.Abs(Mathf.Repeat(angle, Mathf.PI * 2 / 5) - Mathf.PI / 5) / (Mathf.PI / 5);
                return Alpha((radius - Mathf.Lerp(0.95f, 0.48f, Mathf.SmoothStep(0, 1, 1 - t))) * 128);
            }, supersample: true);
            LeafSprite = Shape("UI2_Leaf", 256, 0, (x, y) =>
            {
                float px = x / 128f, py = y / 128f;
                float u = (px + py) * 0.7071f, v = (py - px) * 0.7071f;
                bool inside = u * u + (v + 0.55f) * (v + 0.55f) < 1.05f && u * u + (v - 0.55f) * (v - 0.55f) < 1.05f && Mathf.Abs(u) < 0.95f;
                return inside ? 1 : 0;
            }, supersample: true, color: (x, y) =>
            {
                float px = x / 128f, py = y / 128f;
                float u = (px + py) * 0.7071f, v = (py - px) * 0.7071f;
                return Mathf.Abs(v) < 0.035f && Mathf.Abs(u) < 0.8f ? new Color(0.78f, 0.93f, 0.58f) : Color.Lerp(new Color(0.33f, 0.7f, 0.28f), new Color(0.5f, 0.82f, 0.35f), (u + 1) / 2);
            });
            CheckSprite = Shape("UI2_Check", 128, 0, (x, y) =>
            {
                var p = new Vector2(x, y) / 64f;
                float d = Mathf.Min(Segment(p, new Vector2(-0.6f, 0.02f), new Vector2(-0.18f, -0.42f)), Segment(p, new Vector2(-0.18f, -0.42f), new Vector2(0.62f, 0.45f)));
                return Alpha((d - 0.16f) * 64);
            });
            FadeSprite = Shape("UI2_Fade", 64, 0, (x, y) => Mathf.Pow(Mathf.Clamp01(0.5f - y / 64f), 1.5f));
            // Padlock: a thick shackle over a rounded body with a key hole.
            LockSprite = Shape("UI2_Lock", 128, 0, (x, y) =>
            {
                float body = RoundedBox(x, y + 18, 40, 30, 10);
                float ring = Mathf.Abs(Mathf.Sqrt(x * x + (y - 14) * (y - 14)) - 26) - 8;
                bool hole = Mathf.Sqrt(x * x + (y + 14) * (y + 14)) < 7 || (Mathf.Abs(x) < 3.5f && y < -14 && y > -32);
                bool inside = body <= 0 || (ring <= 0 && y >= 12);
                return inside && !hole ? 1 : 0;
            }, supersample: true);
            // Chunky rounded arrow (points up in the picture) for the ground guide.
            ArrowSprite = Shape("UI2_Arrow", 128, 0, (x, y) =>
            {
                bool head = y >= -4 && y <= 54 && Mathf.Abs(x) <= (54 - y) * 0.95f;
                bool stem = Mathf.Abs(x) <= 17 && y >= -54 && y < 0;
                return head || stem ? 1 : 0;
            }, supersample: true);
            // Soft glowing ring for the restoration wave.
            RingSprite = Shape("UI2_Ring", 256, 0, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                return Mathf.Clamp01(Mathf.Exp(-Mathf.Pow((d - 110) / 12f, 2)) + 0.4f * Mathf.Exp(-Mathf.Pow((d - 92) / 20f, 2)));
            });
            // Butterfly: two pairs of rounded wings and a slim body (tinted per particle).
            ButterflySprite = Shape("UI2_Butterfly", 128, 0, (x, y) =>
            {
                float ax = Mathf.Abs(x);
                float upper = Mathf.Pow((ax - 26) / 24f, 2) + Mathf.Pow((y - 14) / 26f, 2);
                float lower = Mathf.Pow((ax - 20) / 17f, 2) + Mathf.Pow((y + 20) / 17f, 2);
                float body = Mathf.Pow(x / 5f, 2) + Mathf.Pow(y / 30f, 2);
                return upper <= 1 || lower <= 1 || body <= 1 ? 1 : 0;
            }, supersample: true, color: (x, y) =>
            {
                float ax = Mathf.Abs(x);
                if (Mathf.Pow(x / 5f, 2) + Mathf.Pow(y / 30f, 2) <= 1) return new Color(0.35f, 0.3f, 0.4f);
                float spot = Mathf.Pow((ax - 30) / 8f, 2) + Mathf.Pow((y - 16) / 8f, 2);
                return spot <= 1 ? new Color(1, 1, 1) : new Color(0.92f, 0.92f, 0.92f);
            });
            // Pastel rainbow arc (centre below the middle, so the arc sits in the upper part), soft at the edges.
            RainbowSprite = Shape("UI2_Rainbow", 512, 0, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + (y + 128) * (y + 128));
                return Mathf.Clamp01((d - 150) / 4f) * Mathf.Clamp01((250 - d) / 4f);
            }, color: (x, y) =>
            {
                Color[] bands = { new Color(1f, 0.55f, 0.66f), new Color(1f, 0.72f, 0.5f), new Color(1f, 0.9f, 0.5f), new Color(0.6f, 0.9f, 0.62f), new Color(0.55f, 0.78f, 1f), new Color(0.72f, 0.62f, 1f) };
                float d = Mathf.Sqrt(x * x + (y + 128) * (y + 128));
                float band = Mathf.Clamp((250 - d) / 100f * bands.Length, 0, bands.Length - 0.001f);
                int i = (int)band; float f = band - i;
                return i + 1 < bands.Length && f > 0.75f ? Color.Lerp(bands[i], bands[i + 1], (f - 0.75f) * 4) : bands[i];
            });
            AssetDatabase.SaveAssets();
        }
        private static TMP_FontAsset FontAsset(string path, string sourcePath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null) return asset;
            var source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(sourcePath);
            if (source == null) throw new FileNotFoundException("Missing font: " + sourcePath);
            // Dynamic SDF atlas: Korean glyphs are added as text needs them, sharp at any size.
            asset = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = asset.name + " Atlas"; AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material"; AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
            return asset;
        }
        private static Material Preset(string name, System.Action<Material> setup)
        {
            string path = FontFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Font.material); AssetDatabase.CreateAsset(material, path); }
            material.CopyPropertiesFromMaterial(Font.material); material.shaderKeywords = Font.material.shaderKeywords; material.name = name;
            setup(material);
            EditorUtility.SetDirty(material);
            return material;
        }
        /// <summary>Adds every character used by labels to the atlas now, so the first frame never pops glyphs in.</summary>
        public static void BakeCharacters(GameObject root)
        {
            var text = new System.Text.StringBuilder(" 0123456789%/!?.,:()★☆♥");
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) text.Append(label.text);
            Font.TryAddCharacters(text.ToString(), out string _, true);
            foreach (var fallback in Font.fallbackFontAssetTable) if (fallback != null) fallback.TryAddCharacters(text.ToString(), out string _, true);
            EditorUtility.SetDirty(Font);
        }
        private static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - halfWidth + radius, qy = Mathf.Abs(y) - halfHeight + radius;
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
        }
        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a; float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }
        // Signed distance in pixels to a one-pixel anti-aliased edge.
        private static float Alpha(float distance) { return Mathf.Clamp01(0.5f - distance); }
        /// <summary>Pixels per design pixel for every generated picture, so the UI stays sharp on large (1440p, 4K) screens.
        /// Shapes keep their on-screen size: pixels per unit grows by the same factor.</summary>
        public const int Detail = 2;
        private static Sprite Shape(string name, int size, float border, System.Func<float, float, float> alpha, bool supersample = false, System.Func<float, float, Color> color = null)
        {
            string path = Root + "/" + name + ".png";
            // Drawn at Detail x the design size; alpha and color still take design-space coordinates.
            int pixels = size * Detail;
            var texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
            int samples = supersample ? 4 : 2;
            for (int y = 0; y < pixels; y++)
                for (int x = 0; x < pixels; x++)
                {
                    float sum = 0;
                    for (int sy = 0; sy < samples; sy++)
                        for (int sx = 0; sx < samples; sx++)
                            sum += alpha((x + (sx + 0.5f) / samples) / Detail - size / 2f, (y + (sy + 0.5f) / samples) / Detail - size / 2f);
                    Color c = color != null ? color((x + 0.5f) / Detail - size / 2f, (y + 0.5f) / Detail - size / 2f) : Color.white;
                    c.a = sum / (samples * samples); texture.SetPixel(x, y, c);
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100 * Detail;
            // Mipmaps keep shapes smooth when drawn smaller than their pixels (small windows, little icons).
            importer.mipmapEnabled = true; importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
            importer.spriteBorder = new Vector4(border, border, border, border) * Detail;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        /// <summary>Pictures drawn smaller than their pixels: mipmaps and no compression keep them smooth instead of jagged.</summary>
        public static Texture2D Picture(Texture2D texture, bool mipmaps = true)
        {
            if (texture == null) return null;
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            // Full size (no power-of-two squeeze, which blurred and stretched wide pictures) and uncompressed.
            if (importer != null && (importer.mipmapEnabled != mipmaps || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize < 4096 || importer.anisoLevel < 4))
            {
                importer.mipmapEnabled = mipmaps; importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096; importer.anisoLevel = 4;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(texture));
        }

        /// <summary>UI input from the project's own action asset (a persistent asset, unlike the default actions created on the fly).</summary>
        public static void UseUiActions(InputSystemUIInputModule module)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (asset != null && asset.FindActionMap("UI") != null) module.actionsAsset = asset;
            else module.AssignDefaultActions();
        }

        // ---------- Primitives ----------
        public static RectTransform Node(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        public static RectTransform Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }
        public static RectTransform Full(string name, Transform parent, float left = 0, float bottom = 0, float right = 0, float top = 0)
        { return Stretch(Node(name, parent, Vector2.zero, Vector2.zero, Vector2.zero), left, bottom, right, top); }
        /// <summary>Rounded shape with an exact corner radius in canvas units; a huge radius gives a pill.</summary>
        public static Image Paint(RectTransform rect, Color color, float radius, bool raycast = false)
        {
            var image = rect.GetComponent<Image>(); if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = raycast;
            if (radius > 0) { image.sprite = RoundSprite; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = RoundBorder / radius; }
            else image.sprite = null;
            return image;
        }
        public static Image Circle(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = CircleSprite; image.color = color; image.raycastTarget = false; image.preserveAspect = true;
            return image;
        }
        public static Image Sprite(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; image.preserveAspect = true;
            return image;
        }
        /// <summary>Soft shadow placed just before the given rect in its parent, matching its anchors.</summary>
        public static RectTransform DropShadow(RectTransform target, float spread = 20, float drop = 10, float opacity = 0.26f)
        {
            var shadow = Node(target.name + " 그림자", target.parent, target.pivot, target.anchoredPosition, target.sizeDelta);
            shadow.anchorMin = target.anchorMin; shadow.anchorMax = target.anchorMax; shadow.pivot = target.pivot;
            shadow.anchoredPosition = target.anchoredPosition; shadow.sizeDelta = target.sizeDelta + Vector2.one * spread * 2;
            // Grow evenly around the target whatever its pivot, then drop slightly.
            shadow.anchoredPosition += new Vector2((target.pivot.x - 0.5f) * spread * 2, (target.pivot.y - 0.5f) * spread * 2 - drop);
            shadow.SetSiblingIndex(target.GetSiblingIndex());
            var image = shadow.gameObject.AddComponent<Image>(); image.sprite = ShadowSprite; image.type = Image.Type.Sliced;
            image.color = new Color(ShadowTint.r, ShadowTint.g, ShadowTint.b, opacity); image.raycastTarget = false;
            // Inside layout groups the shadow must not take a slot of its own.
            shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return shadow;
        }
        /// <summary>White card with a soft shadow. The returned group holds both, so hiding it hides the shadow too.</summary>
        public static RectTransform Card(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float radius, Color color, out RectTransform body)
        {
            var group = Node(name, parent, anchor, position, size);
            body = Full("판", group);
            DropShadow(body, 22, 12, 0.22f);
            Paint(body, color, radius, true);
            return group;
        }
        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment, Material material = null)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.font = Font; text.fontSharedMaterial = material != null ? material : PlainMaterial;
            text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = -8; text.fontStyle = FontStyles.Normal;
            return text;
        }
        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment,
            Vector2 anchor, Vector2 position, Vector2 box, Material material = null)
        {
            var text = Text(name, parent, value, size, color, alignment, material);
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = box;
            return text;
        }
        public static TextMeshProUGUI Fit(TextMeshProUGUI text, float min)
        {
            text.enableAutoSizing = true; text.fontSizeMax = text.fontSize; text.fontSizeMin = min;
            return text;
        }
        public static TextMeshProUGUI FillText(string name, Transform parent, string value, float size, Color color, Material material = null,
            float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            var text = Text(name, parent, value, size, color, TextAlignmentOptions.Center, material);
            Stretch(text.rectTransform, left, bottom, right, top);
            return text;
        }
        /// <summary>Glossy candy button: a darker lip below a coloured face, white label with a soft underlay.</summary>
        public static Button Candy(string name, Transform parent, string label, Color color, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, Color? labelColor = null)
        {
            var rect = Node(name, parent, anchor, position, size);
            float radius = Mathf.Min(size.y * 0.46f, 44);
            var glow = DropShadow(rect, 14, 10, 0.34f); glow.GetComponent<Image>().color = new Color(color.r * 0.7f, color.g * 0.6f, color.b * 0.9f, 0.38f);
            Paint(rect, Darker(color, 0.8f), radius, true);
            var face = Full("면", rect, 0, 7, 0, 0);
            var faceImage = Paint(face, color, radius);
            var gloss = Node("빛", face, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            // A thin shine along the top edge only, so it never washes over the label.
            gloss.anchorMin = new Vector2(0, 1); gloss.anchorMax = Vector2.one; gloss.offsetMin = new Vector2(18, -Mathf.Round(size.y * 0.2f) - 5); gloss.offsetMax = new Vector2(-18, -5);
            Paint(gloss, new Color(1, 1, 1, 0.3f), 40);
            var text = FillText("글자", face, label, fontSize, labelColor ?? Color.white, labelColor.HasValue ? PlainMaterial : SoftMaterial, 16, 2, 16, 0);
            Fit(text, Mathf.Round(fontSize * 0.7f)); text.textWrappingMode = TextWrappingModes.NoWrap;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = faceImage;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(0.93f, 0.97f, 1f); colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.82f, 0.84f); colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f); colors.fadeDuration = 0.08f;
            button.colors = colors;
            var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            return button;
        }
        /// <summary>Small coloured label pill used as a card header.</summary>
        public static RectTransform Tag(Transform parent, string label, Color color, Vector2 anchor, Vector2 position, float width, float height = 42)
        {
            var tag = Node("머리표", parent, anchor, position, new Vector2(width, height));
            Paint(tag, color, height);
            Fit(FillText("글자", tag, label, Mathf.Round(height * 0.56f), Color.white, SoftMaterial, 12, 1, 12, 0), 16).textWrappingMode = TextWrappingModes.NoWrap;
            return tag;
        }
        /// <summary>Full-screen dim layer with a centred card and a ribbon title that overlaps the card's top edge.</summary>
        public static RectTransform MenuCard(Transform screen, string title, string subtitle, Vector2 size, Color ribbon, Vector2 offset = default)
        {
            Paint((RectTransform)screen, Dim, 0, true);
            PopIn(Card("메뉴 카드", screen, new Vector2(0.5f, 0.5f), offset, size, 44, Paper, out var body));
            float ribbonWidth = Mathf.Clamp(title.Length * H1 + 120, 300, size.x - 120);
            var band = Node("제목 띠", body, new Vector2(0.5f, 1), new Vector2(0, 42), new Vector2(ribbonWidth, 92));
            DropShadow(band, 14, 6, 0.22f);
            Paint(band, Darker(ribbon, 0.8f), 46);
            var face = Full("면", band, 0, 7, 0, 0); Paint(face, ribbon, 46);
            FillText("제목", face, title, H1, Color.white, SoftMaterial, 20, 2, 20, 0).textWrappingMode = TextWrappingModes.NoWrap;
            if (!string.IsNullOrEmpty(subtitle))
                Fit(Text("설명", body, subtitle, Small + 2, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(size.x - 100, 64)), 18);
            return body;
        }
        /// <summary>Loading card between scenes (lives on from the title scene): a tip, a spinning star, soft fade.</summary>
        public static void LoadingScreen()
        {
            var old = GameObject.Find("로딩 화면"); if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("로딩 화면", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var screen = Full("화면", root.transform);
            var group = screen.gameObject.AddComponent<CanvasGroup>();
            Paint(screen, new Color(0.95f, 0.93f, 1f), 0, true);
            var card = Node("카드", screen, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, 360)); card.pivot = new Vector2(0.5f, 0.5f);
            var body = Full("판", card); DropShadow(body, 24, 12, 0.25f); Paint(body, Paper, 60);
            var star = Node("별", card, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(110, 110)); star.pivot = new Vector2(0.5f, 0.5f); star.anchoredPosition = new Vector2(0, -80);
            Sprite(star, StarSprite, Sun);
            Text("기다려요", card, "잠깐만 기다려요!", 50, Lavender, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(900, 70), StickerMaterial);
            var tip = Text("팁", card, "", 32, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -252), new Vector2(880, 90));
            foreach (var spot in new[] { new Vector2(-430, 130), new Vector2(430, -120) }) { var leaf = Node("잎", card, new Vector2(0.5f, 0.5f), spot, Vector2.one * 80); Sprite(leaf, LeafSprite, Color.white); }
            var loading = root.AddComponent<KidLoading>();
            loading.group = group; loading.tipText = tip; loading.spinner = star;
            screen.gameObject.SetActive(false);
        }
        /// <summary>Background menu music for the title and character scenes.</summary>
        public static void MenuMusic(Transform parent)
        {
            var old = parent.Find("배경 음악"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var music = new GameObject("배경 음악").AddComponent<KidSoundscape>(); music.transform.SetParent(parent, false);
            music.menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoCity/Generated/Submission/KidWorld/Sound/music/title.wav");
        }
        /// <summary>Makes a window pop in (fade and springy grow) whenever it appears.</summary>
        public static RectTransform PopIn(RectTransform target, float duration = 0.3f, float from = 0.86f)
        {
            if (target.GetComponent<CanvasGroup>() == null) target.gameObject.AddComponent<CanvasGroup>();
            var pop = target.GetComponent<KidPopIn>(); if (pop == null) pop = target.gameObject.AddComponent<KidPopIn>();
            pop.duration = duration; pop.from = from;
            return target;
        }
        public static Color Darker(Color color, float factor) { return new Color(color.r * factor, color.g * factor, color.b * factor, color.a); }

        public static Slider Slider(Transform card, string title, float top, float min, float max, float width, out TextMeshProUGUI value)
        {
            var row = Node(title, card, new Vector2(0.5f, 1), new Vector2(0, top), new Vector2(width, 110));
            Text("이름", row, title, Body, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), Vector2.zero, new Vector2(width * 0.7f, 44));
            value = Text("값", row, "", Body, Green, TextAlignmentOptions.MidlineRight, new Vector2(1, 1), Vector2.zero, new Vector2(width * 0.3f, 44));
            var track = Node("막대", row, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(width - 40, 26));
            Paint(track, new Color(0.87f, 0.89f, 0.86f), 13, true);
            var fillArea = Full("Fill Area", track);
            var fill = Full("Fill", fillArea); Paint(fill, Green, 13);
            var handleArea = Full("Handle Area", track, 20, 0, 20, 0);
            var handle = Node("Handle", handleArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52));
            var ring = Circle(handle, Green); ring.raycastTarget = true;
            Circle(Node("가운데", handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36)), Color.white);
            var slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = ring;
            slider.minValue = min; slider.maxValue = max; slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            var navigation = slider.navigation; navigation.mode = Navigation.Mode.None; slider.navigation = navigation;
            return slider;
        }
        public static Toggle Toggle(Transform card, string title, float top, float width)
        {
            var row = Node(title, card, new Vector2(0.5f, 1), new Vector2(0, top), new Vector2(width, 60));
            Paint(row, new Color(0, 0, 0, 0), 0, true);
            var box = Node("상자", row, new Vector2(0, 0.5f), Vector2.zero, new Vector2(52, 52));
            var boxImage = Paint(box, new Color(0.9f, 0.92f, 0.89f), 14, true);
            var check = Node("체크", box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            var checkImage = Sprite(check, CheckSprite, Green);
            Text("이름", row, title, Body, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(72, 0), new Vector2(width - 72, 52));
            var toggle = row.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = boxImage; toggle.graphic = checkImage;
            var navigation = toggle.navigation; navigation.mode = Navigation.Mode.None; toggle.navigation = navigation;
            return toggle;
        }
        /// <summary>Scrolling text panel on a soft background, for help and results.</summary>
        public static TextMeshProUGUI ScrollText(Transform card, float top, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var area = Node("글 상자", card, new Vector2(0.5f, 1), new Vector2(0, top), size);
            Paint(area, Mint, 28, true);
            var viewport = Full("Viewport", area, 28, 18, 40, 18);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Node("Content", viewport, new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 0));
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.sizeDelta = Vector2.zero;
            var text = Text("글", content, "", fontSize, Ink, alignment);
            Stretch(text.rectTransform); text.lineSpacing = 6;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            var bar = Node("Scrollbar", area, new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(10, size.y - 40));
            Paint(bar, new Color(0, 0, 0, 0.07f), 5, true);
            var slide = Full("Sliding Area", bar);
            var handle = Full("Handle", slide); Paint(handle, new Color(0.3f, 0.6f, 0.9f, 0.55f), 5, true);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle; scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var scroll = area.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return text;
        }

        /// <summary>Keyboard key cap: white face on a grey lip.</summary>
        public static RectTransform KeyCap(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
        {
            var key = Node("키 " + label, parent, anchor, position, size);
            Paint(key, new Color(0.72f, 0.75f, 0.79f), 14);
            var face = Full("면", key, 0, 5, 0, 0); Paint(face, Color.white, 14);
            FillText("글자", face, label, fontSize, Ink, PlainMaterial, 4, 0, 4, 0).textWrappingMode = TextWrappingModes.NoWrap;
            return key;
        }
        private static Texture2D Icon(string name) { return Picture(AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Icons/" + name + ".png")); }
        private static RawImage RoundPicture(Transform parent, Texture2D texture, Vector2 position, Vector2 size, float radius)
        {
            var window = Node("그림 창", parent, new Vector2(0.5f, 1), position, size);
            Paint(window, Color.white, radius).gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var picture = Full("그림", window).gameObject.AddComponent<RawImage>(); picture.texture = texture; picture.raycastTarget = false;
            return picture;
        }
        private static void Badge(Transform parent, string mark, Color color, Vector2 anchor, Vector2 position, float size)
        {
            var badge = Node("표시 " + mark, parent, anchor, position, Vector2.one * size);
            Circle(badge, color);
            FillText("글자", badge, mark, size * 0.6f, Color.white, SoftMaterial);
        }
        /// <summary>
        /// Picture how-to: key caps for the controls, four numbered picture cards for the loop, and the stage order.
        /// Fills a 1160-wide menu card from <paramref name="top"/> down to about 640 px below it.
        /// </summary>
        public static void HowTo(Transform card, float top)
        {
            // Controls: arrow keys, E, Esc, H, each with a short word.
            var controls = Node("조작", card, new Vector2(0.5f, 1), new Vector2(0, top), new Vector2(1080, 116));
            Paint(controls, Cream, 28);
            var arrows = Node("방향키", controls, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(172, 104));
            KeyCap(arrows, "↑", new Vector2(0.5f, 1), Vector2.zero, new Vector2(54, 48), 30);
            KeyCap(arrows, "←", new Vector2(0, 0), Vector2.zero, new Vector2(54, 48), 30);
            KeyCap(arrows, "↓", new Vector2(0.5f, 0), Vector2.zero, new Vector2(54, 48), 30);
            KeyCap(arrows, "→", new Vector2(1, 0), Vector2.zero, new Vector2(54, 48), 30);
            Text("걷기", controls, "걷기", 30, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(228, 0), new Vector2(90, 50));
            KeyCap(controls, "E", new Vector2(0, 0.5f), new Vector2(366, 2), new Vector2(68, 64), 36);
            Text("E 설명", controls, "줍기 · 넣기", 30, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(450, 0), new Vector2(170, 50));
            KeyCap(controls, "Esc", new Vector2(0, 0.5f), new Vector2(660, 2), new Vector2(88, 64), 28);
            Text("Esc 설명", controls, "메뉴", 30, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(764, 0), new Vector2(80, 50));
            KeyCap(controls, "H", new Vector2(0, 0.5f), new Vector2(878, 2), new Vector2(68, 64), 36);
            Text("H 설명", controls, "도움말", 30, Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(962, 0), new Vector2(110, 50));

            // The loop in four pictures.
            string[] titles = { "주워요", "깨끗하게", "맞는 통에", "도우미" };
            string[] lines = { "반짝이는 ! 쓰레기 앞에서\nE를 눌러요", "더러우면 준비 구역에서\n비우고, 떼고, 씻어요", "통 위 그림이 같은 통\n앞에서 E를 눌러요", "도우미 앞에서 E를 누르면\n힌트를 알려 줘요" };
            Color[] colors = { Orange, Sky, Green, Pink };
            for (int i = 0; i < 4; i++)
            {
                var step = Node("순서 " + (i + 1), card, new Vector2(0.5f, 1), new Vector2(-414 + i * 276, top - 136), new Vector2(252, 356));
                Paint(step, Mint, 28);
                var area = new Vector2(0, -22);
                if (i == 0)
                {
                    RoundPicture(step, Icon("Item_plastic-01"), area, new Vector2(184, 184), 26);
                    KeyCap(step, "E", new Vector2(0.5f, 1), new Vector2(70, -150), new Vector2(60, 58), 32);
                }
                else if (i == 1)
                {
                    string[] steps = { "Step_Empty", "Step_Separate", "Step_Wash" };
                    for (int s = 0; s < 3; s++) RoundPicture(step, Icon(steps[s]), new Vector2(-78 + s * 78, -52), new Vector2(72, 72), 14);
                    Text("순서 글", step, "비우기 → 떼기 → 씻기", 21, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(240, 40));
                }
                else if (i == 2)
                {
                    // Looks like the sign above the plastic bin in the park: coloured board, picture, name.
                    var board = Node("통 표지판", step, new Vector2(0.5f, 1), area, new Vector2(184, 184));
                    Paint(board, new Color(1f, 0.6f, 0.15f), 26);
                    RoundPicture(board, Icon("Bin_Plastic"), new Vector2(0, -10), new Vector2(128, 128), 18);
                    FillText("이름", board, "플라스틱", 26, Color.white, SoftMaterial, 4, 4, 4, 140);
                    Badge(step, "O", Green, new Vector2(0.5f, 1), new Vector2(84, -6), 58);
                }
                else
                {
                    var face = Node("얼굴 창", step, new Vector2(0.5f, 1), area, new Vector2(184, 184));
                    Circle(face, Sun);
                    var inner = Node("안쪽", face, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(164, 164));
                    Circle(inner, Cream).gameObject.AddComponent<Mask>().showMaskGraphic = true;
                    var picture = Node("얼굴", inner, new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(184, 184)).gameObject.AddComponent<RawImage>();
                    picture.texture = Icon("Helper"); picture.raycastTarget = false;
                }
                Badge(step, (i + 1).ToString(), colors[i], new Vector2(0, 1), new Vector2(10, -10), 52);
                Text("제목", step, titles[i], 32, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -218), new Vector2(236, 44));
                Fit(Text("설명", step, lines[i], 22, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0, -268), new Vector2(236, 72)), 17);
            }
            // Stage order, so players know how long the game is.
            var stages = Node("단계 순서", card, new Vector2(0.5f, 1), new Vector2(0, top - 512), new Vector2(1080, 60));
            Paint(stages, new Color(0.9f, 0.95f, 1f), 30);
            Fit(FillText("글", stages, "<color=#3E9B42>1 새싹 공원</color> 깨끗한 쓰레기 5개   ▶   <color=#3E9B42>2 반짝 바닷가</color> 비우고 씻기 4개   ▶   <color=#3E9B42>3 알록달록 시장</color> 떼어내기 3개",
                26, Ink, PlainMaterial, 24, 0, 24, 0), 18).textWrappingMode = TextWrappingModes.NoWrap;
            Text("주의", card, "통 색깔과 그림은 게임 속 구분이에요. 진짜 분리수거는 어른과 함께 우리 동네 안내를 확인해요.", Caption + 1, Soft, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0, top - 580), new Vector2(1080, 32));
        }

        // ---------- Title layout (title scene and the in-game fallback title) ----------
        public sealed class TitleParts
        {
            public Button start, continueGame, help, settings, quit;
            public Button[] characters = new Button[2];
            public Image[] frames = new Image[2];
            public RawImage background;
            public RectTransform logo;
            public RectTransform[] leaves;
        }
        /// <param name="characters">The game scene's fallback title keeps the boy/girl cards; the title scene sends players to the character scene instead.</param>
        /// <summary>Top of the "에코시티 세 곳을 구해요!" pill on the full title, just under the logo.</summary>
        public const float SubtitleTop = -404;
        public static TitleParts Title(Transform root, Texture2D picture, string version, bool characters)
        {
            var parts = new TitleParts();
            var background = Full("배경", root);
            parts.background = background.gameObject.AddComponent<RawImage>(); parts.background.texture = Picture(picture, false); parts.background.raycastTarget = true;
            // Soft lavender wash top and bottom: the park stays visible, the buttons and logo pop.
            var shade = Node("아래 그늘", root, new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 520));
            shade.anchorMin = new Vector2(0, 0); shade.anchorMax = new Vector2(1, 0);
            var shadeImage = shade.gameObject.AddComponent<Image>(); shadeImage.sprite = FadeSprite; shadeImage.color = new Color(0.42f, 0.33f, 0.72f, 0.5f); shadeImage.raycastTarget = false;
            var top = Node("위 그늘", root, new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 420));
            top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1); top.localScale = new Vector3(1, -1, 1);
            var topImage = top.gameObject.AddComponent<Image>(); topImage.sprite = FadeSprite; topImage.color = new Color(1f, 0.96f, 1f, 0.55f); topImage.raycastTarget = false;

            // Logo: the team's own image when it is in place (Assets/EcoCity/Art/Logo/EcoCityLogo.png), otherwise a sticker-style text logo.
            float logoHeight = characters ? 172 : 330;
            var logoImage = LogoSprite();
            if (logoImage != null)
            {
                parts.logo = LogoImage(root, characters);
            }
            else
            {
                var logo = Text("로고", root, "에코시티 구조대", characters ? 136 : 172, new Color(0.47f, 0.38f, 0.92f), TextAlignmentOptions.Center,
                    new Vector2(0.5f, 1), new Vector2(0, characters ? -40 : -90), new Vector2(1400, logoHeight - 60), StickerMaterial);
                logo.textWrappingMode = TextWrappingModes.NoWrap; parts.logo = logo.rectTransform;
            }
            var subtitle = Node("부제", root, new Vector2(0.5f, 1), new Vector2(0, characters ? -220 : SubtitleTop), new Vector2(560, 68));
            DropShadow(subtitle, 14, 8, 0.3f);
            Paint(subtitle, Darker(Sun, 0.86f), 34); var subFace = Full("면", subtitle, 0, 6, 0, 0); Paint(subFace, Sun, 34);
            FillText("글자", subFace, "에코시티 세 곳을 구해요!", 32, new Color(0.45f, 0.3f, 0.12f), PlainMaterial, 16, 0, 16, 0);
            var leafSpots = new[] { new Vector2(-720, -110), new Vector2(720, -90), new Vector2(-620, -300), new Vector2(630, -310) };
            parts.leaves = new RectTransform[leafSpots.Length];
            for (int i = 0; i < leafSpots.Length; i++)
            {
                var leaf = Node("잎 " + (i + 1), root, new Vector2(0.5f, 1), leafSpots[i], Vector2.one * (i < 2 ? 92 : 68));
                Sprite(leaf, LeafSprite, Color.white); parts.leaves[i] = leaf;
            }
            string[] names = { "남자아이", "여자아이" }, keys = { "Boy", "Girl" };
            if (characters)
            {
                var question = Node("질문", root, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(420, 56));
                Paint(question, new Color(0.36f, 0.28f, 0.62f, 0.55f), 30);
                FillText("글자", question, "누구랑 같이 할까요?", 32, Color.white, PlainMaterial, 12, 0, 12, 0);
                for (int i = 0; i < 2; i++)
                {
                    var card = Node("캐릭터 " + names[i], root, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -164 : 164, 0), new Vector2(280, 312));
                    DropShadow(card, 20, 12, 0.3f);
                    var frame = Paint(card, Color.white, 40, true);
                    var inner = Full("안쪽", card, 10, 10, 10, 10); Paint(inner, Paper, 32);
                    var portraitBack = Node("배경", inner, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(244, 214));
                    Paint(portraitBack, i == 0 ? new Color(0.84f, 0.9f, 1f) : new Color(1f, 0.88f, 0.94f), 26);
                    var portrait = Node("얼굴", portraitBack, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(244, 214));
                    var raw = portrait.gameObject.AddComponent<RawImage>(); raw.raycastTarget = false;
                    raw.texture = Picture(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EcoCity/Generated/Submission/Characters/Portrait_" + keys[i] + ".png"));
                    Text("이름", inner, names[i], 36, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(240, 58));
                    var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = frame;
                    var colors = button.colors; colors.highlightedColor = new Color(1, 0.97f, 0.9f); colors.pressedColor = new Color(0.85f, 0.85f, 0.85f); colors.selectedColor = Color.white; button.colors = colors;
                    var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
                    parts.characters[i] = button; parts.frames[i] = frame;
                }
            }
            else
            {
                // Both friends wave from the sides; the choice itself is made on the next screen.
                for (int i = 0; i < 2; i++)
                {
                    var friend = Node("친구 " + names[i], root, new Vector2(i, 0), new Vector2(i == 0 ? 150 : -150, 40), new Vector2(420, 480));
                    var raw = friend.gameObject.AddComponent<RawImage>(); raw.raycastTarget = false;
                    raw.texture = Picture(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EcoCity/Generated/Submission/Characters/CutOut_" + keys[i] + ".png"));
                }
                parts.characters = new Button[0]; parts.frames = new Image[0];
            }

            parts.start = Candy("시작하기", root, "시작하기", Lavender, new Vector2(0.5f, 0), new Vector2(0, characters ? 184 : 196), new Vector2(540, 120), 58);
            // A row lays out the secondary buttons, so hiding Continue (no save yet) re-centres Help.
            var row = Node("보조 버튼", root, new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(760, 96));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 28; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            parts.continueGame = Candy("이어하기", row, "이어하기", Green, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 90), 38);
            parts.help = Candy("어떻게 해요", row, "어떻게 해요?", Sky, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 90), 38);
            parts.settings = Candy("설정", root, "설정", Pink, new Vector2(1, 1), new Vector2(-Margin, -Margin), new Vector2(156, 72), 32);
            parts.quit = Candy("그만하기", root, "그만하기", Gray, new Vector2(0, 1), new Vector2(Margin, -Margin), new Vector2(180, 72), 32);
            // No credit or font line on screen; keep source licences with the original assets.
            return parts;
        }
        /// <summary>Logo picture at the top of a title screen (Assets/EcoCity/Art/Logo/EcoCityLogo.png, from Validation/GenerateKidLogo.py or the team).</summary>
        public static RectTransform LogoImage(Transform root, bool small)
        {
            var sprite = LogoSprite();
            float width = small ? 620 : 1100, height = width * sprite.rect.height / sprite.rect.width;
            var logo = Node("로고", root, new Vector2(0.5f, 1), new Vector2(0, small ? -24 : -26), new Vector2(width, height));
            var image = logo.gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return logo;
        }
        /// <summary>The team's logo image as a sprite, or null while it has not been added yet.</summary>
        public static Sprite LogoSprite()
        {
            if (!File.Exists(LogoPath)) return null;
            var importer = (TextureImporter)AssetImporter.GetAtPath(LogoPath);
            // Up to 4096 wide: the logo is drawn large enough for a 4K title screen.
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize < 4096 || importer.filterMode != FilterMode.Trilinear))
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true; importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096; importer.filterMode = FilterMode.Trilinear; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        }
    }
}
#endif
