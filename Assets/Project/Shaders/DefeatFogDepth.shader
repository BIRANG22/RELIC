Shader "Relic/DefeatFogDepth"
{
    Properties
    {
        _EdgeWidth ("Edge Width", Range(0.0, 0.3)) = 0.08
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.045
        _NoiseScale ("Noise Scale", Range(0.5, 12.0)) = 4.0
        _NoiseStrength ("Noise Strength", Range(0.0, 0.25)) = 0.07
        _NoiseSpeed ("Noise Speed", Range(0.0, 2.0)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+10"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DefeatFogDepth"
            ZWrite On
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _EdgeWidth;
                float _EdgeSoftness;
                float _NoiseScale;
                float _NoiseStrength;
                float _NoiseSpeed;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 fractionValue = frac(value);
                float2 blendValue = fractionValue * fractionValue * (3.0 - 2.0 * fractionValue);

                float bottom = lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), blendValue.x);
                float top = lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + 1.0), blendValue.x);
                return lerp(bottom, top, blendValue.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float timeValue = _Time.y * _NoiseSpeed;
                float broadNoise = ValueNoise(float2(input.uv.x * _NoiseScale + timeValue, timeValue * 0.31));
                float detailNoise = ValueNoise(float2(input.uv.x * _NoiseScale * 2.17 - timeValue * 0.7, timeValue * 0.53));
                float combinedNoise = broadNoise * 0.72 + detailNoise * 0.28;
                float irregularEdge = _EdgeWidth + (combinedNoise - 0.5) * 2.0 * _NoiseStrength;
                float fogAlpha = smoothstep(
                    irregularEdge - _EdgeSoftness,
                    irregularEdge + _EdgeSoftness,
                    input.uv.y);

                clip(fogAlpha - 0.02);
                return half4(0.0h, 0.0h, 0.0h, fogAlpha);
            }
            ENDHLSL
        }
    }
}
