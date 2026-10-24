Shader "PolyRisk/GraphiteSurface"
{
    Properties
    {
        _ContactTex ("Transient pawn contact", 2D) = "black" {}
        _BaseColor ("Surface tint", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.12
        _Smoothness ("Smoothness", Range(0,1)) = 0.23
        _StateTex ("Existing tile light state", 2D) = "black" {}
        _ReflectionStrength ("Simulated edge reflection", Range(0,0.25)) = 0
        _ReflectionFalloff ("Reflection falloff (board units)", Float) = 0.12
        _InnerExtent ("Inner tile edge", Float) = 5.04
        _TileSurface ("Tile surface", Float) = 0
        _StudioStrength ("Neutral satin reflection", Range(0,0.4)) = 0
        _EdgeNormalStrength ("Virtual edge normal", Range(0,0.25)) = 0.13
        _EdgeShadingPixels ("Edge shading pixels", Range(0,1.5)) = 0.7
        _MinimumBandPixels ("Minimum category band coverage", Range(0,2)) = 0.95
        _BandBoost ("Band intensity", Float) = 1.15
        _ReflectionReach ("Maximum colored reflection reach", Float) = 0.1
        _GridPitch ("Tile pitch", Float) = 1.1
        _CornerOffset ("Corner center offset", Float) = 5.7
        _TileSize ("Tile width, depth, band width, band inset", Vector) = (0.92,1.32,0.01656,0.02)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            TEXTURE2D(_ContactTex);
            SAMPLER(sampler_ContactTex);
            TEXTURE2D(_StateTex);
            SAMPLER(sampler_StateTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _ReflectionStrength;
                float _ReflectionFalloff;
                float _InnerExtent;
                half _TileSurface;
                half _StudioStrength;
                half _EdgeNormalStrength;
                half _EdgeShadingPixels;
                half _MinimumBandPixels;
                half _BandBoost;
                float _ReflectionReach;
                float _GridPitch;
                float _CornerOffset;
                float4 _TileSize;
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
                float3 boardPosition : TEXCOORD2;
                half3 tileLight : TEXCOORD3;
                half fog : TEXCOORD4;
                float tileIndex : TEXCOORD5;
            };
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.boardPosition = input.positionOS.xyz;
                half4 state = SAMPLE_TEXTURE2D_LOD(_StateTex, sampler_StateTex,
                    float2((input.stateUV.x + 0.5) / 64.0, 0.5), 0);
                output.tileLight = state.rgb * state.a;
                output.tileIndex = input.stateUV.x;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = NormalizeNormalPerPixel(input.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                // Reconstruct only shading coordinates; the fused mesh and its UVs stay intact.
                float side = floor(input.tileIndex * 0.1 + 0.05);
                bool horizontal = side == 0 || side == 2;
                float along = horizontal ? input.boardPosition.x : input.boardPosition.z;
                float localAlong = along - round(along / max(_GridPitch, 0.001)) * _GridPitch;
                float inward = side == 0 ? input.boardPosition.z + _CornerOffset :
                    side == 1 ? input.boardPosition.x + _CornerOffset :
                    side == 2 ? _CornerOffset - input.boardPosition.z : _CornerOffset - input.boardPosition.x;
                float shortEdge = _TileSize.x * 0.5 - abs(localAlong);
                float depthEdge = _TileSize.y * 0.5 - abs(inward);
                float edgeDistance = min(shortEdge, depthEdge);
                float edgePixel = max(fwidth(edgeDistance), 0.00001);
                half topMask = saturate(input.normalWS.y) * _TileSurface;
                half edgeMask = (1 - smoothstep(0, max(edgePixel * _EdgeShadingPixels, 0.00001), edgeDistance)) * topMask;
                float3 shortAxis = horizontal ? float3(1,0,0) : float3(0,0,1);
                float3 inwardAxis = side == 0 ? float3(0,0,1) : side == 1 ? float3(1,0,0) :
                    side == 2 ? float3(0,0,-1) : float3(-1,0,0);
                float3 edgeNormalOS = shortEdge < depthEdge ? shortAxis * sign(localAlong) : inwardAxis * sign(inward);
                lighting.normalWS = normalize(lighting.normalWS +
                    TransformObjectToWorldDir(edgeNormalOS) * edgeMask * _EdgeNormalStrength);
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
                float bandCenter = _TileSize.y * 0.5 - _TileSize.w - _TileSize.z * 0.5;
                float distanceToBand = abs(inward - bandCenter);
                float pixelWidth = max(fwidth(inward), 0.00001);
                float halfBand = max(_TileSize.z, _MinimumBandPixels * pixelWidth) * 0.5;
                half band = 1 - smoothstep(max(0, halfBand - pixelWidth * 0.5),
                    halfBand + pixelWidth * 0.5, distanceToBand);
                // Match the existing category header length, leaving its corners quiet.
                band *= 1 - smoothstep(_TileSize.x * 0.5 - 0.04, _TileSize.x * 0.5 - 0.03, abs(localAlong));
                half spill = exp2(-distanceToBand / max(0.001, _ReflectionFalloff));
                spill *= 1 - smoothstep(_ReflectionReach * 0.5, _ReflectionReach, distanceToBand);
                surface.emission = input.tileLight * (band * _BandBoost + spill * _ReflectionStrength) * topMask;
                // Add contact only at the tile perimeter; the category/selection channel
                // remains untouched. Derivative coverage keeps a ~1.1 px line on mobile.
                float2 contactUV = float2((input.tileIndex + 0.5) / 64.0, 0.25);
                half4 contact = SAMPLE_TEXTURE2D(_ContactTex, sampler_ContactTex, contactUV);
                half2 contactStyle = SAMPLE_TEXTURE2D(_ContactTex, sampler_ContactTex, contactUV + float2(0,0.5)).rg;
                half contactLine = 1 - smoothstep(edgePixel * .30, edgePixel * 1.40, edgeDistance);
                half contactInner = exp2(-max(0,edgeDistance) / .035) *
                    (1 - smoothstep(.035,.085,edgeDistance));
                surface.emission += contact.rgb * contact.a *
                    (contactLine * contactStyle.r + contactInner * contactStyle.g) * topMask;
                half4 color = UniversalFragmentPBR(lighting, surface);
                // A broad virtual neutral studio panel supplies a restrained satin response.
                // Analytic reflection only: no texture, probe, animated light or extra pass.
                float3 reflectionOS = TransformWorldToObjectDir(reflect(-lighting.viewDirectionWS, lighting.normalWS));
                float travel = max(0, 10 - input.boardPosition.y) / max(0.08, reflectionOS.y);
                float2 panel = input.boardPosition.xz + reflectionOS.xz * travel - float2(12,16);
                float2 panelCoord = float2(dot(panel, float2(0.7071,-0.7071)) / 4.0,
                    dot(panel, float2(0.7071,0.7071)) / 12.0);
                half studio = exp2(-2 * dot(panelCoord, panelCoord) / max(0.25, 1.5 - _Smoothness));
                half facing = saturate(dot(lighting.normalWS, lighting.viewDirectionWS));
                half3 f0 = lerp(half3(0.04,0.04,0.04), surface.albedo, _Metallic);
                half3 fresnel = f0 + (1 - f0) * pow(1 - facing, 5);
                half regionWeight = lerp(0.35,1,saturate(dot(input.color.rgb, half3(0.2126,0.7152,0.0722)) / 0.045));
                color.rgb += fresnel * studio * _StudioStrength * regionWeight * saturate(input.normalWS.y);
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
