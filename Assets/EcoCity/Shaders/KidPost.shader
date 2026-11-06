// Screen look for the built-in pipeline (used by KidPostFx): soft bloom from bright spots, saturation and contrast,
// a warm tint and a light vignette. Pass 0 picks bright pixels, 1 blurs down, 2 adds back up, 3 composes the picture.
Shader "Hidden/EcoCity/KidPost"
{
    Properties { _MainTex ("Source", 2D) = "white" {} _BloomTex ("Bloom", 2D) = "black" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _BloomTex;
    float4 _MainTex_TexelSize;
    float _Threshold, _Knee, _Bloom, _Saturation, _Contrast, _Vignette;
    fixed4 _Tint;
    fixed3 Box(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float2(-d, d).xxyy;
        return (tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb) * 0.25;
    }
    fixed4 Prefilter(v2f_img i) : SV_Target
    {
        fixed3 c = Box(i.uv, 1);
        float b = max(c.r, max(c.g, c.b));
        float soft = clamp(b - _Threshold + _Knee, 0, 2 * _Knee); soft = soft * soft / (4 * _Knee + 0.0001);
        float w = max(soft, b - _Threshold) / max(b, 0.0001);
        return fixed4(c * w, 1);
    }
    fixed4 Down(v2f_img i) : SV_Target { return fixed4(Box(i.uv, 1), 1); }
    fixed4 Up(v2f_img i) : SV_Target { return fixed4(Box(i.uv, 0.5), 1); }
    fixed4 Compose(v2f_img i) : SV_Target
    {
        fixed3 c = tex2D(_MainTex, i.uv).rgb + tex2D(_BloomTex, i.uv).rgb * _Bloom;
        float grey = dot(c, float3(0.299, 0.587, 0.114));
        c = lerp(grey.xxx, c, _Saturation);
        c = (c - 0.5) * _Contrast + 0.5;
        c *= _Tint.rgb;
        float2 d = i.uv - 0.5;
        c *= 1 - dot(d, d) * _Vignette * 2;
        return fixed4(saturate(c), 1);
    }
    ENDCG
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Prefilter
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Down
            ENDCG }
        Pass { Blend One One
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Up
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Compose
            ENDCG }
    }
    Fallback Off
}
