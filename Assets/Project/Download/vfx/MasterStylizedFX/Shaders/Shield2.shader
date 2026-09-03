Shader "ASESampleShaders/MultiPassDistortion"
{
    Properties
    {
        _FresnelScale("FresnelScale", Float) = 1
        _FresnelPower("FresnelPower", Float) = 1
        [HDR] _IntersectionColor("Intersection Color", Color) = (0.4338235,0.4377282,1,0)
        _FresnelScale2("FresnelScale2", Float) = 1
        _FresnelPower2("FresnelPower2", Float) = 1
        _FresnelColor2("FresnelColor2", Color) = (0,0,0,0)
        _BottomMask("BottomMask", Range(-1,1)) = 0
        _IntersectionDistance("IntersectionDistance", Float) = 0
        _IntersectionIntensity("IntersectionIntensity", Range(-1,1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ShieldFresnel2D"
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
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 normalOS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half _FresnelScale;
                half _FresnelPower;
                half4 _IntersectionColor;
                half _FresnelScale2;
                half _FresnelPower2;
                half4 _FresnelColor2;
                half _BottomMask;
                half _IntersectionDistance;
                half _IntersectionIntensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalOS = input.normalOS;
                return output;
            }

            half DepthIntersection(Varyings input)
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceEyeDepth = -TransformWorldToView(input.positionWS).z;
                half fade = 1.0h - saturate(
                    abs(sceneEyeDepth - surfaceEyeDepth) / max(_IntersectionDistance, 0.0001h));
                return saturate(fade * _IntersectionIntensity);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half ndotv = saturate(dot(normalize(input.normalWS), viewDirection));
                half bottom = smoothstep(_BottomMask, 1.0h, input.normalOS.y);
                half fresnel1 = _FresnelScale
                              * pow(1.0h - ndotv, max(_FresnelPower, 0.0001h));
                half fresnel2 = _FresnelScale2
                              * pow(1.0h - ndotv, max(_FresnelPower2, 0.0001h));

                half primaryAlpha = saturate(fresnel1 * bottom);
                half4 primary = half4(_IntersectionColor.rgb, primaryAlpha);
                half secondary = saturate(bottom * fresnel2);
                half4 surface = lerp(primary, _FresnelColor2 * secondary, secondary);

                half intersection = DepthIntersection(input);
                surface.rgb += _IntersectionColor.rgb * intersection;
                surface.a = saturate(max(surface.a, intersection));
                return surface;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
