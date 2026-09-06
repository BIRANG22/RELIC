Shader "Circle"
{
    Properties
    {
        _TextureSample0("Texture Sample 0", 2D) = "white" {}
        _ScaleUV("ScaleUV", Range(-1,1)) = 0
        _TextureSample1("Texture Sample 1", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Circle2D"
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_TextureSample0); SAMPLER(sampler_TextureSample0);
            TEXTURE2D(_TextureSample1); SAMPLER(sampler_TextureSample1);
            CBUFFER_START(UnityPerMaterial)
                float4 _TextureSample0_ST;
                float4 _TextureSample1_ST;
                half _ScaleUV;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 direction = input.uv - 0.5;
                direction *= rsqrt(max(dot(direction, direction), 1.175494351e-38));
                float2 distortedUV = saturate(input.uv + _ScaleUV * direction);
                half4 mainSample = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, distortedUV);
                float2 maskUV = input.uv * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
                half maskAlpha = SAMPLE_TEXTURE2D(_TextureSample1, sampler_TextureSample1, maskUV).a;
                return half4(mainSample.rgb, mainSample.a * maskAlpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
