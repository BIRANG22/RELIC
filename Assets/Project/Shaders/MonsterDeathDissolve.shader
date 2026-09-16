Shader "Relic/Monster Death Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1, 1, 1, 1)
        _DissolveProgress ("Dissolve Progress", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0.001, 0.25)) = 0.06
        [HDR] _EdgeColor ("Edge Color", Color) = (0.5, 0.15, 1, 1)
        _NoiseScale ("Noise Scale", Range(1, 64)) = 16
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "CanUseSpriteAtlas" = "True" }
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _DissolveProgress;
                float _EdgeWidth;
                float4 _EdgeColor;
                float _NoiseScale;
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
                float2 local = frac(value);
                local = local * local * (3.0 - 2.0 * local);
                return lerp(lerp(Hash21(cell), Hash21(cell + float2(1, 0)), local.x), lerp(Hash21(cell + float2(0, 1)), Hash21(cell + float2(1, 1)), local.x), local.y);
            }
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color * input.color;
                float noise = ValueNoise(input.uv * _NoiseScale);
                float edge = 1.0 - smoothstep(_DissolveProgress, _DissolveProgress + _EdgeWidth, noise);
                clip(sprite.a - 0.001);
                clip(noise - _DissolveProgress);
                sprite.rgb = lerp(sprite.rgb, _EdgeColor.rgb, edge * _EdgeColor.a);
                return sprite;
            }
            ENDHLSL
        }
    }
}
