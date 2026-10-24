Shader "PolyRisk/GraphiteSurface"
{
    Properties
    {
        _BaseColor ("Surface tint", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.12
        _Smoothness ("Smoothness", Range(0,1)) = 0.23
        _StateTex ("Existing tile light state", 2D) = "black" {}
        _ReflectionStrength ("Simulated edge reflection", Range(0,0.25)) = 0
        _ReflectionFalloff ("Reflection falloff (board units)", Float) = 0.12
        _InnerExtent ("Inner tile edge", Float) = 5.04
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            TEXTURE2D(_StateTex);
            SAMPLER(sampler_StateTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _ReflectionStrength;
                float _ReflectionFalloff;
                float _InnerExtent;
            CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "GraphiteForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #define _ENVIRONMENTREFLECTIONS_OFF 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 stateUV : TEXCOORD1;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                float2 boardPosition : TEXCOORD2;
                half3 reflectedLight : TEXCOORD3;
                half fog : TEXCOORD4;
            };
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.boardPosition = input.positionOS.xz;
                half4 state = SAMPLE_TEXTURE2D_LOD(_StateTex, sampler_StateTex,
                    float2((input.stateUV.x + 0.5) / 64.0, 0.5), 0);
                output.reflectedLight = state.rgb * state.a * _ReflectionStrength;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = NormalizeNormalPerPixel(input.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.vertexLighting = VertexLighting(input.positionWS, lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = input.color.rgb * _BaseColor.rgb;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = 1;
                // Analytic spill on the inner tile edge. No light/probe or extra pass.
                float distanceToEdge = max(abs(input.boardPosition.x), abs(input.boardPosition.y)) - _InnerExtent;
                half spill = exp2(-max(0, distanceToEdge) / max(0.001, _ReflectionFalloff));
                surface.emission = input.reflectedLight * spill * saturate(lighting.normalWS.y);
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, input.fog);
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex shadowVert
            #pragma fragment depthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            float4 shadowVert(float4 positionOS : POSITION, float3 normalOS : NORMAL) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 direction = normalize(_LightPosition - positionWS);
                #else
                    float3 direction = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, direction));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }
            half4 depthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex depthVert
            #pragma fragment depthFrag
            float4 depthVert(float4 positionOS : POSITION) : SV_POSITION
            { return TransformObjectToHClip(positionOS.xyz); }
            half4 depthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
