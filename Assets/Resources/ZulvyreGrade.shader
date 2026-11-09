Shader "Hidden/Zulvyre/Grade"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Contrast;
            float _Saturation;
            float _Vignette;
            float4 _Tint;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float luma = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(luma.xxx, c.rgb, _Saturation);
                c.rgb = (c.rgb - 0.5) * _Contrast + 0.5;
                c.rgb *= _Tint.rgb;
                float2 d = i.uv - 0.5;
                c.rgb *= saturate(1.0 - dot(d, d) * _Vignette);
                return c;
            }
            ENDCG
        }
    }
}
