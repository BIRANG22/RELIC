Shader "Dustium/UI/TurnMarkBorderFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _Speed ("Flow Speed (Revolutions/s)", Float) = 0.5
        _GlowWidth ("Glow Width", Range(0.01,0.5)) = 0.2
        _GlowStrength ("Glow Strength", Range(0,8)) = 4
        _BaseBrightness ("Base Brightness", Range(0,2)) = 0.4
        _RectAspect ("Border Width / Height", Float) = 0.428571

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "TurnMarkBorderFlow"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 localPosition : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _TextureSampleAdd;
            float4 _Color;
            float4 _GlowColor;
            float _Speed;
            float _GlowWidth;
            float _GlowStrength;
            float _BaseBrightness;
            float _RectAspect;
            float4 _ClipRect;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.localPosition = input.positionOS;
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            // Vertical flow: left edge rises, right edge descends.
            // Top and bottom share the same phase on both sides, so the
            // highlight passes through the caps without a temporal jump.
            float BorderProgress(float2 uv, float widthToHeight)
            {
                float2 p = saturate(uv);
                // Left side travels from bottom (0) to top (0.5).
                // Right side travels from top (0.5) to bottom (1).
                // The alpha-masked sprite determines the visible border.
                float leftProgress = p.y * 0.5;
                float rightProgress = 1.0 - p.y * 0.5;

                // Use the left or right half of the tall sprite.  Top and
                // bottom values coincide modulo 1 at the center boundary.
                return (p.x < 0.5) ? leftProgress : rightProgress;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd;
                half4 tinted = sprite * input.color;
                // The highlight advances uniformly along the two vertical edges.
                float border = BorderProgress(input.uv, _RectAspect);
                float rotation = frac(_Time.y * _Speed);
                float delta = abs(border - rotation);
                float separation = min(delta, 1.0 - delta);
                float radius = max(0.001, _GlowWidth * 0.5);
                float glow = 1.0 - smoothstep(radius * 0.12, radius, separation);
                glow *= glow;
                float brightness = max(0.0, _BaseBrightness) + glow * max(0.0, _GlowStrength);
                half3 glowTint = lerp(tinted.rgb, tinted.rgb * _GlowColor.rgb, saturate(glow));
                half4 result = half4(saturate(glowTint * brightness), tinted.a * saturate(_BaseBrightness + glow * _GlowStrength));

                #ifdef UNITY_UI_CLIP_RECT
                    result.a *= (step(_ClipRect.x, input.localPosition.x) * step(_ClipRect.y, input.localPosition.y) * step(input.localPosition.x, _ClipRect.z) * step(input.localPosition.y, _ClipRect.w));
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDHLSL
        }
    }
    Fallback "UI/Default"
}
