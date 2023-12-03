Shader "UI/CompositeCanvasRenderer/Digital Noise"
{
    Properties
    {
        [Header(Main)]
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        [Header(GLITCH)]
        [Toggle(ENABLE_GLITCH)] _GLITCH ("Enable GLITCH", Float) = 1
        [PowerSlider(2)] _GlitchIntensity("Intensity", Range(0, 1)) = 0.1
        _GlitchDivision("Division", Range(1, 500)) = 100
        _GlitchFrequency("Frequency", Range(1, 500)) = 100
        _GlitchProbability("Probability", Range(0, 1)) = 1

        [Header(ERROR)]
        [Toggle(ENABLE_ERROR)] _ERROR ("Enable ERROR", Float) = 1
        _ErrorColor("Error Color", Color) = (0,0,0,1)
        [PowerSlider(2)] _ErrorProbability("Probability", Range(0, 0.5)) = 0.05

        [Header(Blend Mode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend Mode", Int) = 10

        [Header(Stencil)]
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #pragma shader_feature_local ENABLE_GLITCH
            #pragma shader_feature_local ENABLE_ERROR

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _GlitchIntensity;
            half _GlitchDivision;
            half _GlitchFrequency;
            half _GlitchProbability;
            fixed4 _ErrorColor;
            half _ErrorProbability;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float rand(const float seed)
            {
                return frac(sin(seed * 12.9898) * 43758.5453);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                const half rand_y = rand(floor(IN.texcoord.y * _GlitchDivision));
                const half rand_t = rand(floor((_Time.x + rand_y * 100) * _GlitchFrequency));
                half2 uv = IN.texcoord;

                // ==== GLITCH ====
                #if ENABLE_GLITCH
                const fixed is_glitch = step(rand_t, _GlitchProbability - 0.0001);
                uv.x = lerp(uv.x, frac(uv.x + rand_t * _GlitchIntensity), is_glitch);
                #endif

                fixed4 color = tex2D(_MainTex, uv);

                // ==== ERROR ====
                #if ENABLE_ERROR
                const fixed is_error = step(rand_t, _ErrorProbability - 0.0001);
                const fixed3 error = lerp(color.rgb, _ErrorColor.rgb * color.a, _ErrorColor.a);
                color.rgb = lerp(color.rgb, error, is_error);
                #endif

                // ==== Output Color ====
                color.rgb *= IN.color.rgb;
                color *= IN.color.a;

                return color;
            }
            ENDCG
        }
    }
}