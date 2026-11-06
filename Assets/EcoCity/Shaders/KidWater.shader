// Moving cartoon water for flat sea, river, pond and fountain surfaces (no extra vertices needed):
// soft drifting wave bands in a lighter colour, little twinkling glints and a glossy surface that catches the sun.
Shader "EcoCity/KidWater"
{
    Properties
    {
        _Color ("Water", Color) = (0.36, 0.72, 0.95, 1)
        _WaveColor ("Wave light", Color) = (0.75, 0.92, 1, 1)
        _WaveScale ("Wave size", Float) = 0.35
        _Speed ("Speed", Float) = 0.6
        _Glints ("Glints", Range(0, 1)) = 0.6
        _Glossiness ("Smoothness", Range(0, 1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color, _WaveColor;
        float _WaveScale, _Speed, _Glints;
        half _Glossiness;
        struct Input { float3 worldPos; };
        float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 p = IN.worldPos.xz * _WaveScale;
            float t = _Time.y * _Speed;
            // Two crossing wave trains make soft moving bands.
            float w = sin(p.x * 1.7 + t * 1.3 + sin(p.y * 0.9 + t) * 1.2) * 0.5 + 0.5;
            w *= sin(p.y * 1.3 - t * 0.9 + sin(p.x * 0.7 - t * 0.6) * 1.4) * 0.5 + 0.5;
            float band = smoothstep(0.55, 0.85, w);
            // Small glints that blink on and off in a grid of cells.
            float2 cell = floor(IN.worldPos.xz * 1.6);
            float h = hash(cell);
            float blink = saturate(sin(_Time.y * (1.5 + h * 2.5) + h * 40) * 4 - 3.2) * step(0.82, h) * _Glints;
            o.Albedo = lerp(_Color.rgb, _WaveColor.rgb, band * 0.6) + blink * 0.6;
            o.Emission = blink * 0.25 + band * _WaveColor.rgb * 0.05;
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Standard"
}
