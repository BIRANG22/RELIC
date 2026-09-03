Shader "Shield"
{
    Properties
    {
        _DepthFadeDistance("Depth Fade Distance", Float) = 0
        [HDR] _ShieldColor("ShieldColor", Color) = (0,0,0,0)
        _IntersectionColor("Intersection Color", Color) = (0.4338235,0.4377282,1,0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Shield2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half _DepthFadeDistance;
                half4 _ShieldColor;
                half4 _IntersectionColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                return output;
            }

            half DepthIntersection(Varyings input, half distance)
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceEyeDepth = -TransformWorldToView(input.positionWS).z;
                return 1.0h - saturate(abs(sceneEyeDepth - surfaceEyeDepth) / max(distance, 0.0001h));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half intersection = DepthIntersection(input, _DepthFadeDistance);
                half body = DepthIntersection(input, 1.55h);
                half alpha = saturate(max(intersection, body));
                half3 rgb = _ShieldColor.rgb + _IntersectionColor.rgb * intersection;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
