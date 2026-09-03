Shader "EmissiveParticle"
{
    Properties
    {
        _TextureSample0("Texture Sample 0", 2D) = "white" {}
        _FresnelScale("FresnelScale", Float) = 0
        _FresnelPower("FresnelPower", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "EmissiveParticle2D"
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
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float4 uv : TEXCOORD2;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_TextureSample0); SAMPLER(sampler_TextureSample0);
            CBUFFER_START(UnityPerMaterial)
                float4 _TextureSample0_ST;
                half _FresnelScale;
                half _FresnelPower;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv.xy * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                half4 sampleValue = input.color * (input.uv.z + 1.0h)
                                  * SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, uv);
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = _FresnelScale * pow(
                    saturate(1.0h - dot(normalize(input.normalWS), viewDirection)),
                    max(_FresnelPower, 0.0001h));
                return half4(sampleValue.rgb, saturate(sampleValue.a + fresnel));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
