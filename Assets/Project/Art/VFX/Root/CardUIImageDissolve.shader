Shader "Dustium/UI/Card Image Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Card Sprite (set by UI Image)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DissolveTex ("Dissolve Noise", 2D) = "gray" {}
        _DissolveAmount ("Dissolve Amount (0 Visible, 1 Hidden)", Range(0,1)) = 0
        _DissolveSoftness ("Dissolve Softness", Range(0.001,0.3)) = 0.06
        _DissolveEdgeWidth ("Edge Width", Range(0,0.2)) = 0.034
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1,1,1,1)
        _EdgeIntensity ("Edge Intensity", Range(0,3)) = 1

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
            Name "UI Card Dissolve"
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
            float4 _DissolveEdgeColor;
            float4 _ClipRect;
            float _DissolveAmount;
            float _DissolveSoftness;
            float _DissolveEdgeWidth;
            float _EdgeIntensity;

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
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;
                float2 noiseUV = i.uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
                float noise = tex2D(_DissolveTex, noiseUV).r;
                float amount = saturate(_DissolveAmount);
                float soft = max(_DissolveSoftness, 0.001);
                // Ends are exact: 0 = original sprite; 1 = transparent.
                float visible = 1.0 - smoothstep(noise - soft, noise + soft, amount);
                visible = lerp(1.0, visible, step(0.0001, amount));
                visible *= (1.0 - step(0.9999, amount));
                // Make the edge a visible colored band, not a subtle tint added
                // to the sprite. The band also contributes alpha on the
                // dissolving side so its color remains visible there.
                float edgeWidth = max(_DissolveEdgeWidth, 0.0001);
                float distanceToEdge = abs(noise - amount);
                float edge = 1.0 - smoothstep(0.0, edgeWidth, distanceToEdge);
                edge *= step(0.0001, amount) * (1.0 - step(0.9999, amount));

                // Tint the edge directly so the selected HDR edge color is
                // recognizable even over bright or strongly colored sprites.
                float3 edgeRGB = _DissolveEdgeColor.rgb * max(_EdgeIntensity, 0.0);
                float3 resultRGB = lerp(tex.rgb, edgeRGB, edge);
                float resultAlpha = tex.a * saturate(max(visible, edge * _DissolveEdgeColor.a));
                #ifdef UNITY_UI_CLIP_RECT
                resultAlpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(resultAlpha - 0.001);
                #endif
                return float4(resultRGB, resultAlpha);
            }
            ENDHLSL
        }
    }
    Fallback "UI/Default"
}
