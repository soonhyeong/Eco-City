// Soft cartoon sky for the built-in pipeline: a gradient from the horizon to the top, a darker ground half
// and a warm glow around the sun. KidEnvironmentMood changes the colours with the city's cleanliness.
Shader "EcoCity/KidSky"
{
    Properties
    {
        _Top ("Top", Color) = (0.36, 0.66, 0.97, 1)
        _Horizon ("Horizon", Color) = (0.82, 0.92, 1, 1)
        _Bottom ("Below horizon", Color) = (0.7, 0.82, 0.9, 1)
        _SunColor ("Sun glow", Color) = (1, 0.95, 0.8, 1)
        _SunDirection ("Direction to the sun", Vector) = (0.3, 0.5, -0.6, 0)
        _SunSize ("Sun glow size", Range(1, 400)) = 60
        _Exponent ("Gradient curve", Range(0.2, 4)) = 0.8
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Horizon, _Bottom, _SunColor;
            float4 _SunDirection;
            float _SunSize, _Exponent;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = saturate(d.y);
                fixed3 sky = lerp(_Horizon.rgb, _Top.rgb, pow(up, _Exponent));
                fixed3 below = lerp(_Horizon.rgb, _Bottom.rgb, saturate(-d.y * 4));
                fixed3 c = d.y >= 0 ? sky : below;
                float sun = pow(saturate(dot(d, normalize(_SunDirection.xyz))), _SunSize);
                c += _SunColor.rgb * sun * 0.55;
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
