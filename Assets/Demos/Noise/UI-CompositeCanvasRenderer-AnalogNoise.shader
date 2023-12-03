Shader "UI/CompositeCanvasRenderer/Analog Noise"
{
    Properties
    {
        [Header(Main)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(VIGNETTE)]
        [Toggle(ENABLE_VIGNETTE)] _VIGNETTE ("Enable VIGNETTE", Float) = 0
        _VignetteIntensity ("Intensity", Range (0, 1)) = 0.3

        [Header(DISTORTION)]
        [Toggle(ENABLE_DISTORTION)] _DISTORTION ("Enable DISTORTION", Float) = 0
        _DistortionIntensity ("Intensity", Range (-1, 1)) = 0.5
        _DistortionScale ("Scale", Range (0.01, 2)) = 1.1

        [Header(NOISE)]
        [Toggle(ENABLE_NOISE)] _NOISE ("Enable NOISE", Float) = 0
        _NoiseIntensity ("Intensity", Range (0, 1)) = 0.1
        [PowerSlider(2.0)] _NoiseScale ("Scale", Range (1, 100)) = 1

        [Header(SCANNING_LINE)]
        [Toggle(ENABLE_SCANNING_LINE)] _SCANNING_LINE ("Enable SCANNING_LINE", Float) = 0
        [PowerSlider(10.0)] _ScanningLineFrequency ("Division", Range (1, 500)) = 100
        _ScanningLineIntensity ("Intensity", Range (0, 0.5)) = 0.1

        [Header(RGB_SHIFT)]
        [Toggle(ENABLE_RGB_SHIFT)] _RGB_SHIFT ("Enable RGB_SHIFT", Float) = 0
        _RgbShiftIntensity ("Intensity", Range (0, 1)) = 0.3
        _RgbShiftOffsetX ("OffsetX", Range (-1, 1)) = 1
        _RgbShiftOffsetY ("OffsetY", Range (-1, 1)) = 1

        [Header(Blend Mode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend Mode", Int) = 10

        [Header(Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
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

            #pragma shader_feature_local ENABLE_VIGNETTE
            #pragma shader_feature_local ENABLE_DISTORTION
            #pragma shader_feature_local ENABLE_NOISE
            #pragma shader_feature_local ENABLE_SCANNING_LINE
            #pragma shader_feature_local ENABLE_RGB_SHIFT

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            half _UIMaskSoftnessX;
            half _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;
            half _VignetteIntensity;
            half _DistortionIntensity;
            half _DistortionScale;
            half _ScanningLineFrequency;
            half _ScanningLineIntensity;
            half _NoiseIntensity;
            half _NoiseScale;
            half _RgbShiftIntensity;
            half _RgbShiftOffsetX;
            half _RgbShiftOffsetY;
            fixed4 _Color;

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
                const float2 pos = IN.texcoord.xy * 2 - 1;
                float2 uv = IN.texcoord.xy;

                // ==== Distortion ====
                #if ENABLE_DISTORTION
                const float2 h = pos / 2;
                const float r2 = h.x * h.x + h.y * h.y;
                const float f = 1.0 + r2 * (_DistortionIntensity * sqrt(r2));
                uv = f / _DistortionScale * h + 0.5;
                #endif

                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0 / alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;
                half4 col = tex2D(_MainTex, uv) + _TextureSampleAdd;
                half3 alpha3 = col.a;

                // ==== RGB Shift ====
                #if ENABLE_RGB_SHIFT
                const half2 offset = _RgbShiftIntensity * half2(_RgbShiftOffsetX, _RgbShiftOffsetY) / 20;
                const fixed4 shift_color_r = tex2D(_MainTex, uv + offset);
                const fixed4 shift_color_b = tex2D(_MainTex, uv - offset);
                col.r = shift_color_r.r;
                col.b = shift_color_b.b;
                alpha3.r = shift_color_r.a;
                alpha3.b = shift_color_b.a;
                #endif

                // ==== Noise ====
                #if ENABLE_NOISE
                const half2 noise_aspect = _MainTex_TexelSize.zw / _MainTex_TexelSize.z;
                const half2 noise_size = 1024 / _NoiseScale * noise_aspect;
                const half2 floor_uv = floor(IN.texcoord * noise_size) / noise_size;
                const half3 rand_n = rand(floor_uv.x * floor_uv.y * _Time.x + 1);
                col.rgb += col.rgb * (rand_n - 0.5) * _NoiseIntensity;
                #endif

                // ==== ScanningLine ====
                #if ENABLE_SCANNING_LINE
                const half sl_s = sin(IN.texcoord.y * _ScanningLineFrequency);
                const half sl_c = cos(IN.texcoord.y * _ScanningLineFrequency);

                col.rgb += half3(sl_s, sl_c, sl_s) * _ScanningLineIntensity * col.a;
                #endif

                // ==== Vignette ====
                #if ENABLE_VIGNETTE
                col.rgb -= dot(pos, pos) * _VignetteIntensity * alpha3;
                #endif

                // ==== Output Color ====
                col.rgb *= IN.color.rgb;
                col *= IN.color.a;
                return col;
            }
            ENDCG
        }
    }
}