Shader "CommonParticles"
{
    Properties
    {
        [Header(Mask)] [NoScaleOffset] _NoiseMask("NoiseMask", 2D) = "white" {}
        [Toggle] _Mask("Mask", Float) = 0
        _MaskScroll("MaskScroll", Vector) = (0,0,0,0)
        _MaskTiling("MaskTiling", Vector) = (0,0,0,0)
        _MaskOffset("MaskOffset", Vector) = (0,0,0,0)
        _Feather("Feather", Float) = 0
        _Texture("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "CommonParticles2D"
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
                float4 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_Texture); SAMPLER(sampler_Texture);
            TEXTURE2D(_NoiseMask); SAMPLER(sampler_NoiseMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                half _Mask;
                float4 _MaskScroll;
                float4 _MaskTiling;
                float4 _MaskOffset;
                half _Feather;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.uv2 = input.uv2;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 textureUV = input.uv.xy * _Texture_ST.xy + _Texture_ST.zw;
                half4 sampleValue = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, textureUV);
                half threshold = lerp(-_Feather, 1.0h + _Feather, input.uv2.z);
                float2 maskUV = input.uv.xy * _MaskTiling.xy
                              + _MaskOffset.xy + input.uv2.w * _MaskScroll.xy;
                half noise = SAMPLE_TEXTURE2D(_NoiseMask, sampler_NoiseMask, maskUV).r;
                half maskValue = _Mask > 0.5
                    ? smoothstep(threshold - _Feather, threshold + _Feather, noise)
                    : 1.0h;
                return half4(input.uv.z * sampleValue.rgb, sampleValue.a * maskValue);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
