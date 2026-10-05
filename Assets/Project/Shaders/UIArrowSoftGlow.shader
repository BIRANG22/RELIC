Shader "UI/Dustium/ArrowSoftGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,1,1,0.7)
        _GlowRadius ("Glow Radius", Range(1,12)) = 6
        _GlowSoftness ("Glow Softness", Range(0.5,3)) = 1.6
        _GlowStrength ("Glow Strength", Range(0,4)) = 2
        _ContentScale ("Content Scale", Vector) = (1.56,1.233333,0,0)

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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "SoftGlow"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowRadius;
            float _GlowSoftness;
            float _GlowStrength;
            float4 _ContentScale;
            float4 _ClipRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            float SampleAlpha(float2 uv)
            {
                float2 inside = step(0.0, uv) * step(uv, 1.0);
                return tex2D(_MainTex, saturate(uv)).a * inside.x * inside.y;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = (input.texcoord - 0.5) * _ContentScale.xy + 0.5;
                float2 radius = _MainTex_TexelSize.xy * _GlowRadius;
                const float diagonal = 0.70710678;

                float blur = SampleAlpha(uv) * 0.12;
                blur += SampleAlpha(uv + float2( radius.x, 0)) * 0.08;
                blur += SampleAlpha(uv + float2(-radius.x, 0)) * 0.08;
                blur += SampleAlpha(uv + float2(0,  radius.y)) * 0.08;
                blur += SampleAlpha(uv + float2(0, -radius.y)) * 0.08;
                blur += SampleAlpha(uv + float2( radius.x,  radius.y) * diagonal) * 0.07;
                blur += SampleAlpha(uv + float2(-radius.x,  radius.y) * diagonal) * 0.07;
                blur += SampleAlpha(uv + float2( radius.x, -radius.y) * diagonal) * 0.07;
                blur += SampleAlpha(uv + float2(-radius.x, -radius.y) * diagonal) * 0.07;

                float2 outerRadius = radius * 2.0;
                blur += SampleAlpha(uv + float2( outerRadius.x, 0)) * 0.045;
                blur += SampleAlpha(uv + float2(-outerRadius.x, 0)) * 0.045;
                blur += SampleAlpha(uv + float2(0,  outerRadius.y)) * 0.045;
                blur += SampleAlpha(uv + float2(0, -outerRadius.y)) * 0.045;
                blur += SampleAlpha(uv + float2( outerRadius.x,  outerRadius.y) * diagonal) * 0.035;
                blur += SampleAlpha(uv + float2(-outerRadius.x,  outerRadius.y) * diagonal) * 0.035;
                blur += SampleAlpha(uv + float2( outerRadius.x, -outerRadius.y) * diagonal) * 0.035;
                blur += SampleAlpha(uv + float2(-outerRadius.x, -outerRadius.y) * diagonal) * 0.035;

                float alpha = pow(saturate(blur * _GlowStrength), _GlowSoftness);
                alpha *= _GlowColor.a * input.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(_GlowColor.rgb * input.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
