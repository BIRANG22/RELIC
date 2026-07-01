Shader "Relic/UI/RenderTexture Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("RenderTexture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Reveal ("Reveal", Range(0, 1)) = 0
        _EdgeSoftness ("Edge Softness", Range(0.0001, 1)) = 0.08
        _EdgeColor ("Edge Color", Color) = (0.05, 0.035, 0.02, 0.65)
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.025
        _Direction ("Direction 0 Left To Right 1 Right To Left", Range(0, 1)) = 0
        _NoiseStrength ("Edge Noise Strength", Range(0, 0.5)) = 0.04
        _InvertReveal ("Invert Reveal", Float) = 0
        _Color ("UI Alpha Tint", Color) = (1, 1, 1, 1)

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
            Name "Default"

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
                fixed4 color : COLOR;
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
            sampler2D _NoiseTex;

            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            float4 _ClipRect;

            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            fixed4 _EdgeColor;

            float _Reveal;
            float _EdgeSoftness;
            float _EdgeWidth;
            float _Direction;
            float _NoiseStrength;
            float _InvertReveal;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mainColor = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;

                float2 noiseUV = TRANSFORM_TEX(i.texcoord, _NoiseTex);
                float noise = saturate(tex2D(_NoiseTex, noiseUV).r);
                float softness = max(_EdgeSoftness, 0.0001);
                float reveal = saturate(_Reveal);
                float direction = saturate(_Direction);

                float axis = lerp(i.texcoord.x, 1.0 - i.texcoord.x, direction);
                axis += (noise - 0.5) * _NoiseStrength;

                float mask = smoothstep(
                    reveal - softness,
                    reveal + softness,
                    axis
                );

                mask = lerp(1.0 - mask, mask, step(0.5, _InvertReveal));
                mask *= step(0.0001, reveal);
                mask = lerp(mask, 1.0, step(0.9999, reveal));

                float edgeEnabled = step(0.0001, _EdgeWidth) * _EdgeColor.a;
                float edgeDistance = abs(axis - reveal);
                float edge = 1.0 - smoothstep(
                    _EdgeWidth,
                    _EdgeWidth + softness,
                    edgeDistance
                );
                float edgeProgressFade =
                    smoothstep(0.0, softness, reveal) *
                    smoothstep(0.0, softness, 1.0 - reveal);
                edge *= edgeEnabled * edgeProgressFade * mask;

                fixed4 color = mainColor;
                color.rgb = lerp(color.rgb, _EdgeColor.rgb, saturate(edge));
                color.a = mainColor.a * i.color.a * mask;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
