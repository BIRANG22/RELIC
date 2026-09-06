Shader "UI/DustiumBackgroundBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _UIBlurSourceTexture ("Blur Source", 2D) = "black" {}
        [HideInInspector] _UIBlurUiTexture ("UI Blur Source", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurRadius ("Blur Radius", Range(0,8)) = 4.0
        _Darken ("Darken", Range(0,1)) = 0.75
        _Saturation ("Saturation", Range(0,1)) = 0.4
        _Contrast ("Contrast", Range(0.5,1.5)) = 0.8

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
            sampler2D _UIBlurSourceTexture;
            float4 _UIBlurSourceTexture_TexelSize;
            sampler2D _UIBlurUiTexture;
            fixed4 _Color;
            float4 _ClipRect;
            float _BlurRadius;
            float _Darken;
            float _Saturation;
            float _Contrast;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 SampleBlurSource(float2 uv)
            {
                fixed4 world = tex2D(_UIBlurSourceTexture, uv);
                fixed4 ui = tex2D(_UIBlurUiTexture, uv);
                float uiAlpha = saturate(ui.a);
                float maxUiRgb = max(ui.r, max(ui.g, ui.b));
                float3 uiRgb = maxUiRgb <= uiAlpha + 0.001
                    ? ui.rgb / max(uiAlpha, 0.0001)
                    : ui.rgb;
                world.rgb = lerp(world.rgb, uiRgb, uiAlpha);
                world.a = max(world.a, uiAlpha);
                return world;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 중심을 유지하면서 주변 픽셀을 촘촘하게 섞어,
                // 화면 전체가 번지는 느낌보다 부드럽게 초점이 빠진 배경을 만듭니다.
                float2 texel = _UIBlurSourceTexture_TexelSize.xy;
                // 이전 캡처 방식은 4배 다운샘플 텍스처를 사용했습니다.
                // 이제 원본 해상도를 읽으므로 동일한 Inspector Radius 체감을 위해
                // 샘플 반경만 확대합니다.
                float radius = _BlurRadius * 3.0;
                float2 nearOffset = texel * radius * 0.5;
                float2 farOffset = texel * radius;

                fixed4 col = SampleBlurSource(IN.texcoord) * 0.30;

                // 가까운 십자 방향
                col += SampleBlurSource(IN.texcoord + float2( nearOffset.x, 0.0)) * 0.10;
                col += SampleBlurSource(IN.texcoord + float2(-nearOffset.x, 0.0)) * 0.10;
                col += SampleBlurSource(IN.texcoord + float2(0.0,  nearOffset.y)) * 0.10;
                col += SampleBlurSource(IN.texcoord + float2(0.0, -nearOffset.y)) * 0.10;

                // 가까운 대각선 방향
                col += SampleBlurSource(IN.texcoord + float2( nearOffset.x,  nearOffset.y)) * 0.05;
                col += SampleBlurSource(IN.texcoord + float2(-nearOffset.x,  nearOffset.y)) * 0.05;
                col += SampleBlurSource(IN.texcoord + float2( nearOffset.x, -nearOffset.y)) * 0.05;
                col += SampleBlurSource(IN.texcoord + float2(-nearOffset.x, -nearOffset.y)) * 0.05;

                // 바깥쪽은 낮은 가중치만 사용해 과도한 번짐을 억제합니다.
                col += SampleBlurSource(IN.texcoord + float2( farOffset.x, 0.0)) * 0.025;
                col += SampleBlurSource(IN.texcoord + float2(-farOffset.x, 0.0)) * 0.025;
                col += SampleBlurSource(IN.texcoord + float2(0.0,  farOffset.y)) * 0.025;
                col += SampleBlurSource(IN.texcoord + float2(0.0, -farOffset.y)) * 0.025;

                // 채도 감소: 0이면 완전 흑백, 1이면 원본 색상입니다.
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(luminance.xxx, col.rgb, saturate(_Saturation));

                // 대비 조절: 1이 원본, 1보다 작으면 부드럽게, 크면 선명하게 만듭니다.
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;

                // 전체 밝기를 낮춰 참고 이미지처럼 뒤쪽 UI를 눌러줍니다.
                col.rgb *= 1.0 - saturate(_Darken);

                col *= IN.color;

            #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
            #endif

            #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
            #endif

                return col;
            }
            ENDCG
        }
    }
}
