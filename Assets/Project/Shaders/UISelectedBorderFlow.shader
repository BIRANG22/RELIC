Shader "UI/Dustium/SelectedBorderFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _Speed ("Flow Speed", Range(-5,5)) = 0.6
        _GlowWidth ("Glow Width", Range(0.01,0.5)) = 0.12
        _GlowStrength ("Glow Strength", Range(0,8)) = 2.0
        _BaseBrightness ("Base Brightness", Range(0,2)) = 1.0

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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
            fixed4 _Color;
            fixed4 _GlowColor;
            float4 _ClipRect;
            float _Speed;
            float _GlowWidth;
            float _GlowStrength;
            float _BaseBrightness;

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

            float Perimeter01(float2 uv)
            {
                float dLeft = uv.x;
                float dRight = 1.0 - uv.x;
                float dBottom = uv.y;
                float dTop = 1.0 - uv.y;

                float minD = min(min(dLeft, dRight), min(dBottom, dTop));

                if (minD == dBottom)
                    return uv.x * 0.25;
                if (minD == dRight)
                    return 0.25 + uv.y * 0.25;
                if (minD == dTop)
                    return 0.50 + (1.0 - uv.x) * 0.25;

                return 0.75 + (1.0 - uv.y) * 0.25;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord) * IN.color;

                float perimeter = Perimeter01(IN.texcoord);
                float head = frac(_Time.y * _Speed);

                float delta = abs(perimeter - head);
                delta = min(delta, 1.0 - delta);

                float halfWidth = max(0.0001, _GlowWidth * 0.5);
                float glow = 1.0 - smoothstep(0.0, halfWidth, delta);
                glow *= glow;

                fixed3 baseRgb = tex.rgb * _BaseBrightness;
                fixed3 glowRgb = _GlowColor.rgb * (_GlowStrength * glow) * tex.a;
                fixed4 color = fixed4(baseRgb + glowRgb, tex.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
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
