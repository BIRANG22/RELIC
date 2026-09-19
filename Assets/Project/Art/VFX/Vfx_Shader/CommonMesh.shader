Shader "CommonMesh2Side"
{
    Properties
    {
        [Header(Main)] [NoScaleOffset] _Main("Main", 2D) = "white" {}
        _Tiling("Tiling", Vector) = (1,1,0,0)
        _Offset("Offset", Vector) = (0,0,0,0)
        _Scroll("Scroll", Vector) = (1,0,0,0)
        [Header(LimitUV)] _LimitUVRange("LimitUVRange", Vector) = (0,1,0,1)
        [Toggle] _LimitUV("LimitUV", Float) = 0
        [Header(StretchUV)] _StretchUVDes("StretchUVDes", Vector) = (0,0,0,0)
        _StretchMultiplier("StretchMultiplier", Vector) = (0,0,0,0)
        [Toggle] _Stretch("Stretch", Float) = 0
        [Header(Mask)] [NoScaleOffset] _NoiseMask("NoiseMask", 2D) = "white" {}
        [Toggle] _Mask("Mask", Float) = 1
        _MaskScroll("MaskScroll", Vector) = (0,0,0,0)
        _MaskTiling("MaskTiling", Vector) = (0,0,0,0)
        _MaskOffset("MaskOffset", Vector) = (0,0,0,0)
        _Feather("Feather", Range(0,1)) = 0
        [Header(StaticMask)] _StaticMask("StaticMask", 2D) = "white" {}
        _SmoothStep("SmoothStep", Vector) = (0,1,0,0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "CommonMesh2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_Main); SAMPLER(sampler_Main);
            TEXTURE2D(_NoiseMask); SAMPLER(sampler_NoiseMask);
            TEXTURE2D(_StaticMask); SAMPLER(sampler_StaticMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tiling;
                float4 _Offset;
                float4 _Scroll;
                float4 _LimitUVRange;
                half _LimitUV;
                float4 _StretchUVDes;
                float4 _StretchMultiplier;
                half _Stretch;
                half _Mask;
                float4 _MaskScroll;
                float4 _MaskTiling;
                float4 _MaskOffset;
                half _Feather;
                float4 _StaticMask_ST;
                float4 _SmoothStep;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;
                output.uv2 = input.uv2;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 stretch = _StretchMultiplier.xy * (_StretchUVDes.xy - input.uv.xy) * input.uv.w;
                float2 mainUV = input.uv.xy * _Tiling.xy + input.uv.z * _Scroll.xy
                              + _Offset.xy + (_Stretch > 0.5 ? stretch : 0.0.xx);
                float2 limitedUV = clamp(mainUV, _LimitUVRange.xz, _LimitUVRange.yw);
                half4 mainSample = SAMPLE_TEXTURE2D(
                    _Main, sampler_Main, _LimitUV > 0.5 ? limitedUV : mainUV);

                half threshold = lerp(-_Feather, 1.0h + _Feather, input.uv2.z);
                float2 maskUV = input.uv.xy * _MaskTiling.xy + _MaskOffset.xy
                              + input.uv2.w * _MaskScroll.xy;
                half noise = SAMPLE_TEXTURE2D(_NoiseMask, sampler_NoiseMask, maskUV).r;
                half dissolve = _Mask > 0.5
                    ? smoothstep(threshold - _Feather, threshold + _Feather, noise)
                    : 1.0h;
                float2 staticUV = input.uv.xy * _StaticMask_ST.xy + _StaticMask_ST.zw;
                half staticMask = smoothstep(
                    _SmoothStep.x, _SmoothStep.y,
                    SAMPLE_TEXTURE2D(_StaticMask, sampler_StaticMask, staticUV).r);

                half3 rgb = input.color.rgb * mainSample.rgb * (1.0h + input.uv2.x);
                half alpha = input.color.a * mainSample.a * dissolve * staticMask;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
