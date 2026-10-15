Shader "Dustium/UI/Card Stencil Dissolve Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Sprite (set by UI Image)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DissolveTex ("Dissolve Noise", 2D) = "gray" {}
        _DissolveAmount ("Dissolve Amount (0 Visible, 1 Hidden)", Range(0,1)) = 0
        _DissolveEdgeWidth ("Edge Width", Range(0,0.2)) = 0.05
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1,0.4,0.1,1)
        _EdgeIntensity ("Edge Intensity", Range(0,3)) = 1
        [Toggle] _ShowBurnEdge ("Show Burn Edge On Mask Graphic", Float) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
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
            Name "UI Stencil Dissolve Mask"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            sampler2D _DissolveTex;
            float4 _MainTex_ST;
            float4 _DissolveTex_ST;
            float4 _Color;
            float4 _ClipRect;
            float4 _DissolveEdgeColor;
            float _DissolveAmount;
            float _DissolveEdgeWidth;
            float _EdgeIntensity;
            float _ShowBurnEdge;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 sprite = tex2D(_MainTex, i.uv) * i.color;
                float2 noiseUV = i.uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
                float noise = tex2D(_DissolveTex, noiseUV).r;
                float amount = saturate(_DissolveAmount);

                // Critically, the fragment must be DISCARDED for a Unity UI Mask.
                // Merely changing output alpha never erases stencil coverage.
                // Allow all non-transparent sprite pixels at 0, none at 1.
                clip(sprite.a - 0.001);
                if (amount >= 0.9999) discard;
                if (amount > 0.0001) clip(noise - amount);

                #ifdef UNITY_UI_CLIP_RECT
                clip(UnityGet2DClipping(i.worldPosition.xy, _ClipRect) - 0.001);
                #endif

                // Render only the thin burning boundary of the mask image.
                // UI Mask's Show Mask Graphic = OFF sets ColorMask=0; its
                // stencil still updates, while the boundary remains hidden.
                float edge = 0;
                if (amount > 0.0001 && amount < 0.9999)
                {
                    float width = max(_DissolveEdgeWidth, 0.0001);
                    edge = 1.0 - smoothstep(0.0, width, noise - amount);
                }
                edge *= step(0.5, _ShowBurnEdge);
                float3 rgb = _DissolveEdgeColor.rgb * max(0.0, _EdgeIntensity);
                float alpha = sprite.a * edge * _DissolveEdgeColor.a;
                return float4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "UI/Default"
}
