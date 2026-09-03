Shader "Blood"
{
    Properties
    {
        [NoScaleOffset] _MainTexture("MainTexture", 2D) = "white" {}
        _MainTiling("MainTiling", Vector) = (1,1,0,0)
        _MainOffset("MainOffset", Vector) = (-0.07,0,0,0)
        _MainScroll("MainScroll", Vector) = (0,0,0,0)
        [Toggle] _LoopMain("LoopMain", Float) = 0
        [NoScaleOffset] _Mask("Mask", 2D) = "white" {}
        _MaskScale("MaskScale", Vector) = (1,1,0,0)
        _MaskOffset("MaskOffset", Range(-1,1)) = 0
        _MaskScroll("MaskScroll", Vector) = (0,0,0,0)
        _EdgeSharpness("EdgeSharpness", Range(0,1)) = 0
        _StaticMask("StaticMask", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Blood2D"
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

            TEXTURE2D(_MainTexture); SAMPLER(sampler_MainTexture);
            TEXTURE2D(_Mask); SAMPLER(sampler_Mask);
            TEXTURE2D(_StaticMask); SAMPLER(sampler_StaticMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTiling;
                float4 _MainOffset;
                float4 _MainScroll;
                half _LoopMain;
                float4 _MaskScale;
                half _MaskOffset;
                float4 _MaskScroll;
                half _EdgeSharpness;
                float4 _StaticMask_ST;
            CBUFFER_END

            float2 ResolveMainTiling(float2 value)
            {
                return dot(abs(value), 1.0.xx) < 0.0001 ? 1.0.xx : value;
            }

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
                float2 mainUV = input.uv.xy * ResolveMainTiling(_MainTiling.xy) + _MainOffset.xy;
                mainUV += _MainScroll.xy * (1.0 - input.uv.z);
                float2 sampleUV = _LoopMain > 0.5 ? mainUV : saturate(mainUV);
                half4 mainSample = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, sampleUV);

                float2 maskUV = input.uv.xy * _MaskScale.xy;
                maskUV += _MaskScroll.xy * (1.0 - input.uv2.x);
                maskUV.y += _MaskOffset;
                half maskValue = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, maskUV).r;
                half edge = _EdgeSharpness <= 0.0001
                    ? step(input.uv.w, maskValue)
                    : smoothstep(input.uv.w, input.uv.w + _EdgeSharpness, maskValue);

                float2 staticUV = input.uv.xy * _StaticMask_ST.xy + _StaticMask_ST.zw;
                half staticMask = SAMPLE_TEXTURE2D(_StaticMask, sampler_StaticMask, staticUV).r;
                half3 rgb = mainSample.rgb * input.color.rgb * (input.uv2.y + 1.0h);
                half alpha = mainSample.a * edge * input.color.a * staticMask;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
