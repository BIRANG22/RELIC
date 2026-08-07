Shader "UI/DustiumBackgroundBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurRadius ("Blur Radius", Range(0,8)) = 3.5
        _Darken ("Darken", Range(0,1)) = 0.18

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
            fixed4 _Color;
            float4 _ClipRect;
            float _BlurRadius;
            float _Darken;

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

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _BlurRadius;
                fixed4 col = 0;

                col += tex2D(_MainTex, IN.texcoord) * 0.20;
                col += tex2D(_MainTex, IN.texcoord + float2( offset.x, 0)) * 0.10;
                col += tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)) * 0.10;
                col += tex2D(_MainTex, IN.texcoord + float2(0,  offset.y)) * 0.10;
                col += tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)) * 0.10;
                col += tex2D(_MainTex, IN.texcoord + float2( offset.x,  offset.y)) * 0.10;
                col += tex2D(_MainTex, IN.texcoord + float2(-offset.x,  offset.y)) * 0.10;
                col += tex2D(_MainTex, IN.texcoord + float2( offset.x, -offset.y)) * 0.10;
                col += tex2D(_MainTex, IN.texcoord + float2(-offset.x, -offset.y)) * 0.10;

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
