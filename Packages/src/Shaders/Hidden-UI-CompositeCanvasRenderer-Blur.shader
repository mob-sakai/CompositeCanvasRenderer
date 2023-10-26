Shader "Hidden/UI/CompositeCanvasRenderer/Blur"
{
    Properties
    {
        [PerRendererData] _MainTex("Main Texture", 2D) = "white" {}
    }

    SubShader
    {
        ZTest Always
        Cull Off
        ZWrite Off
        Blend One Zero
        Fog
        {
            Mode Off
        }

        Pass
        {
            Name "Blur"

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_blur
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            half4 _Blur;

            fixed4 tex2DBlurring1D(sampler2D tex, half2 uv, half2 blur)
            {
                const int KERNEL_SIZE = 9;
                float4 o = 0;
                float sum = 0;
                for (int i = -KERNEL_SIZE / 2; i <= KERNEL_SIZE / 2; i++)
                {
                    const half2 sample_uv = half2(uv + blur * i);
                    const float weight = 1.0 / (abs(i) + 2);
                    o += tex2D(tex, sample_uv) * weight;
                    sum += weight;
                }
                return o / sum;
            }
            
            float invLerp(const float from, const float to, const float value)
            {
                return saturate(max(0, value - from) / max(0.000000001, to - from));
            }

            fixed4 frag_blur(v2f_img IN) : SV_Target
            {
                const half2 blur_factor = _Blur.xy;
                return tex2DBlurring1D(_MainTex, IN.uv, blur_factor * _MainTex_TexelSize.xy);
            }
            ENDCG
        }
        
        Pass
        {
            Name "Cutoff"

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _InnerCutoff;
            float _Multiplier;
            float _Power;
            float _Limit;
            
            float invLerp(const float from, const float to, const float value)
            {
                return (max(0, value - from) / max(0.000000001, to - from));
            }

            fixed4 frag(v2f_img IN) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.uv);
                color = pow(color, _Power);

                const half inner = invLerp(_InnerCutoff, 1, color.a);
                color *= lerp(_Multiplier, 0, inner);

                color = min(color, _Limit);
                return color;
            }
            ENDCG
        }
    }
}