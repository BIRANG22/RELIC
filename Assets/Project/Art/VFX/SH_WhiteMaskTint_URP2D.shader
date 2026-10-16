Shader "Dustium/URP2D/White Mask Tint"
{
    Properties
    {
        [MainTexture] _MainTex ("Mask Texture (Black=Transparent, White=Opaque)", 2D) = "white" {}
        [MainColor] _Color ("Tint Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Cutoff ("Brightness Threshold", Range(0,1)) = 0
        _Softness ("Threshold Softness", Range(0.0001,1)) = 0.0001
        [Toggle] _UseTextureAlpha ("Multiply Source Alpha", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        Pass
        {
            Name "Unlit2D"
            Tags { "LightMode"="Universal2D" }
            Cull Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Opacity;
                half _Cutoff;
                half _Softness;
                half _UseTextureAlpha;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // Convert grayscale brightness to visibility; RGB is *not* multiplied by the mask.
                half brightness = dot(tex.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half mask = saturate((brightness - _Cutoff) / max(_Softness, 0.0001h));
                half sourceAlpha = lerp(1.0h, tex.a, saturate(_UseTextureAlpha));
                half4 tint = _Color * input.color;
                return half4(tint.rgb, mask * tint.a * _Opacity * sourceAlpha);
            }
            ENDHLSL
        }
        // Fallback for URP forward renderer cameras as well.
        Pass
        {
            Name "UnlitForward"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Opacity;
                half _Cutoff;
                half _Softness;
                half _UseTextureAlpha;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half brightness = dot(tex.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half mask = saturate((brightness - _Cutoff) / max(_Softness, 0.0001h));
                half sourceAlpha = lerp(1.0h, tex.a, saturate(_UseTextureAlpha));
                half4 tint = _Color * input.color;
                return half4(tint.rgb, mask * tint.a * _Opacity * sourceAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
