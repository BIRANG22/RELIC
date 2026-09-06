Shader "Slash"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [NoScaleOffset] _MainTexture("MainTexture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _MainTiling("MainTiling", Vector) = (1,1,0,0)
        _MainOffset("MainOffset", Vector) = (-0.07,0,0,0)
        _MainScroll("MainScroll", Vector) = (0,0,0,0)
        [Toggle] _LoopMain("LoopMain", Float) = 0

        [NoScaleOffset] _Mask("Mask", 2D) = "white" {}
        _MaskScale("MaskScale", Vector) = (1,1,0,0)
        _MaskOffset("MaskOffset", Range(-1, 1)) = 0
        _MaskScroll("MaskScroll", Vector) = (0,0,0,0)
        _EdgeSharpness("EdgeSharpness", Range(0, 1)) = 0
        _StaticMask("StaticMask", 2D) = "white" {}

        [HideInInspector] _RendererColor("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0

        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            Name "Slash2D"
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
                float4 color : COLOR;
                float4 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);
            TEXTURE2D(_StaticMask);
            SAMPLER(sampler_StaticMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTiling;
                float4 _MainOffset;
                float4 _MainScroll;
                half _LoopMain;
                float4 _MaskScale;
                half _MaskOffset;
                float4 _MaskScroll;
                half _EdgeSharpness;
                float4 _StaticMask_ST;
                half4 _RendererColor;
                float4 _Flip;
            CBUFFER_END

            float2 ResolveMainTiling(float2 value)
            {
                // Old materials may have serialized the original (0,0) default.
                return dot(abs(value), float2(1.0, 1.0)) < 0.0001
                    ? float2(1.0, 1.0)
                    : value;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                input.positionOS.xy *= _Flip.xy;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color * _Color * _RendererColor;
                output.uv = input.uv;
                output.uv1 = input.uv1;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 mainUV = input.uv.xy * ResolveMainTiling(_MainTiling.xy)
                              + _MainOffset.xy;
                mainUV.x = pow(max(mainUV.x, 0.0), 1.0 + input.uv1.z);

                float scrollAmount = 1.0 - input.uv.z;
                mainUV += _MainScroll.xy * scrollAmount;

                float2 clampedMainUV = clamp(mainUV, float2(-99.0, 0.0), float2(1.0, 1.0));
                float2 sampledMainUV = _LoopMain > 0.5 ? mainUV : clampedMainUV;

                half4 mainSample = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, sampledMainUV);
                half4 spriteSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);

                float2 maskUV = input.uv.xy * _MaskScale.xy;
                maskUV += _MaskScroll.xy * (1.0 - input.uv1.x);
                maskUV.y += _MaskOffset;

                half maskValue = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, maskUV).r;
                half edgeMask = _EdgeSharpness <= 0.0001
                    ? step(input.uv.w, maskValue)
                    : smoothstep(input.uv.w, input.uv.w + _EdgeSharpness, maskValue);

                float2 staticMaskUV = input.uv.xy * _StaticMask_ST.xy + _StaticMask_ST.zw;
                half staticMask = SAMPLE_TEXTURE2D(_StaticMask, sampler_StaticMask, staticMaskUV).r;

                half brightness = input.uv1.y + 1.0h;
                half3 rgb = mainSample.rgb * spriteSample.rgb * input.color.rgb * brightness;
                half alpha = mainSample.a * spriteSample.a * edgeMask * input.color.a * staticMask;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
