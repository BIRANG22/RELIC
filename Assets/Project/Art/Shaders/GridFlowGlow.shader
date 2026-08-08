Shader "Custom/GridFlowGlowGrayscaleGridAndNoiseAlphaFlowXY"
{
    Properties
    {
        [MainTexture] _GridTex ("Grid Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}

        [HDR] _GlowColor ("Glow Color", Color) = (0.15, 0.8, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0, 20)) = 5.0

        _NoiseTiling ("Noise Tiling", Vector) = (1, 1, 0, 0)
        _FlowX ("Flow X", Float) = 0.12
        _FlowY ("Flow Y", Float) = 0.0
        _NoiseContrast ("Noise Contrast", Range(0.1, 4.0)) = 1.0

        _GridContrast ("Grid Contrast", Range(0.1, 4.0)) = 1.0
        _Opacity ("Opacity", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

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

            TEXTURE2D(_GridTex);
            SAMPLER(sampler_GridTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _GridTex_ST;
                float4 _NoiseTex_ST;
                float4 _GlowColor;
                float4 _NoiseTiling;
                float _FlowX;
                float _FlowY;
                float _GlowIntensity;
                float _NoiseContrast;
                float _GridContrast;
                float _Opacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Grid texture grayscale value is used directly as transparency.
                // black = 0 alpha, gray = intermediate alpha, white = full alpha.
                float2 gridUV = TRANSFORM_TEX(input.uv, _GridTex);
                half gridValue = SAMPLE_TEXTURE2D(_GridTex, sampler_GridTex, gridUV).r;
                gridValue = saturate(pow(max(gridValue, 0.0001h), _GridContrast));

                // Flowing noise UV.
                float2 noiseUV = input.uv * _NoiseTiling.xy;
                noiseUV += float2(_FlowX, _FlowY) * _Time.y;
                noiseUV = noiseUV * _NoiseTex_ST.xy + _NoiseTex_ST.zw;

                half noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // Noise grayscale value is also used directly.
                // black = 0 alpha, gray = intermediate alpha, white = full alpha.
                noiseValue = saturate(pow(max(noiseValue, 0.0001h), _NoiseContrast));

                // Both grayscale masks multiply together.
                // This keeps the noise visible only where the grid has brightness,
                // while preserving every intermediate gray value from both textures.
                half visibleMask = saturate(gridValue * noiseValue);

                half3 color = _GlowColor.rgb * (_GlowIntensity * visibleMask);
                half alpha = visibleMask * _GlowColor.a * _Opacity;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
