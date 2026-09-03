Shader "PowDissolve"
{
    Properties
    {
        _Base("Base", 2D) = "white" {}
        [NoScaleOffset] _Noise("Noise", 2D) = "white" {}
        _MaskTiling("MaskTiling", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PowDissolve2D"
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
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_Base); SAMPLER(sampler_Base);
            TEXTURE2D(_Noise); SAMPLER(sampler_Noise);
            CBUFFER_START(UnityPerMaterial)
                float4 _Base_ST;
                float4 _MaskTiling;
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
                float2 baseUV = input.uv.xy * _Base_ST.xy + _Base_ST.zw;
                half4 baseSample = SAMPLE_TEXTURE2D(_Base, sampler_Base, baseUV);
                float2 noiseUV = input.uv.xy * _MaskTiling.xy + float2(input.uv2.y * _Time.y, 1.0);
                half4 noiseSample = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, noiseUV);
                half4 dissolve = baseSample.a * input.color.a * pow(max(noiseSample, 0.0001h), input.uv.z);
                half alpha = saturate((dissolve + (dissolve - 0.1h) * input.uv2.x).r);
                half3 rgb = input.color.rgb * baseSample.rgb * (input.uv.w + 1.0h);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
