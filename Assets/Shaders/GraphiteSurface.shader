Shader "PolyRisk/GraphiteSurface"
{
    Properties
    {
        _ContactTex ("Transient pawn contact", 2D) = "black" {}
        _Polish ("Architectural finish enabled", Float) = 0
        // Mode 2 is the calibrated architecture; 0/1 retain the previous image.
        _StudioKey ("Softbox key: center XZ, half width / length", Vector) = (8,8,9,12)
        _StudioFill ("Softbox fill: center XZ, half width / length", Vector) = (0,7,10,12)
        _ArchitecturalResponse ("Key gain, fill gain, flank contrast, bevel width", Vector) = (1.25,0.32,0.18,0.035)
        _CornerGrow ("Existing corner extension", Float) = 0.4
        _CornerTileSize ("Existing extended corner width", Float) = 1.72
        _BoardDark ("Corner inlay mesh", Float) = 0
        _PawnSurface ("Shared monolith shell", Float) = 0
        _SurfaceTint ("Graphite finish tint", Color) = (1,1,1,1)
        _EdgeSpecular ("Precision edge reflection", Float) = 0
        _BackgroundSeparation ("Outer edge separation", Float) = 0
        _BoardHalfExtent ("Board half extent", Float) = 7
        _ContactReflectionStrength ("Contact reflection", Float) = 0
        _ArrivalReflectionStrength ("Arrival reflection", Float) = 0
        _ContactOutlineGain ("Contact outline gain", Float) = 1
        _ContactReflectionFalloff ("Contact reflection falloff", Float) = 0.025
        _ContactReflectionReach ("Contact reflection reach", Float) = 0.09
        _ActiveSurfaceContrast ("Active satin contrast", Range(0,1)) = 0
        _PawnFoot ("Active pawn footprint x z radius energy", Vector) = (0,0,1,0)
        _PawnFootColor ("Active pawn reflection tint", Color) = (0,0,0,0)
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
                half4 _SurfaceTint;
                half _Polish;
                float4 _StudioKey;
                float4 _StudioFill;
                float4 _ArchitecturalResponse;
                float _CornerGrow;
                float _CornerTileSize;
                half _BoardDark;
                half _PawnSurface;
                half _EdgeSpecular;
                half _BackgroundSeparation;
                float _BoardHalfExtent;
                half _ContactReflectionStrength;
                half _ArrivalReflectionStrength;
                half _ContactOutlineGain;
                float _ContactReflectionFalloff;
                float _ContactReflectionReach;
                half _ActiveSurfaceContrast;
                float4 _PawnFoot;
                half4 _PawnFootColor;
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
                half stateIntensity : TEXCOORD6;
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
                float stateIndex = input.stateUV.x;
                if (_Polish > 1.5 && _BoardDark > .5)
                {
                    // Corner inlays are fused with the default UV index zero. Recover
                    // their existing logical corner from position; never change the mesh.
                    stateIndex = input.positionOS.x < 0 ? (input.positionOS.z < 0 ? 10 : 20) :
                        (input.positionOS.z < 0 ? 0 : 30);
                }
                half4 state = 0;
                if (_PawnSurface < .5)
                    state = SAMPLE_TEXTURE2D_LOD(_StateTex, sampler_StateTex,
                        float2((stateIndex + 0.5) / 64.0, 0.5), 0);
                output.tileLight = state.rgb * state.a;
                output.stateIntensity = state.a;
                output.tileIndex = stateIndex;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }
            half4 ShadeMonolith(Varyings input)
            {
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = NormalizeNormalPerPixel(input.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.vertexLighting = VertexLighting(input.positionWS,lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                // The shell mesh intentionally has no vertex colors. A shared material
                // supplies its albedo, and all pawn instances keep their shared geometry.
                surface.albedo = _BaseColor.rgb;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = 1;
                half4 color = UniversalFragmentPBR(lighting,surface);
                float3 reflected = reflect(-lighting.viewDirectionWS,lighting.normalWS);
                // Broad ceiling and vertical studio cards: angular rather than local
                // position based, so the same material reads coherently on every pawn.
                half ceiling = saturate(dot(reflected,normalize(float3(.58,.60,.55))));
                half front = saturate(dot(reflected,normalize(float3(-.68,-.18,-.71))));
                half lobe = pow(ceiling,7) + pow(front,5) * .65;
                half facing = saturate(dot(lighting.normalWS,lighting.viewDirectionWS));
                half3 f0 = lerp(half3(.04,.04,.04),surface.albedo,_Metallic);
                half3 fresnel = f0 + (1-f0) * pow(1-facing,5);
                color.rgb += fresnel * lobe * _StudioStrength;
                // Flat geometric facets already provide their own precision edge: a
                // restrained view-dependent catch, with no animated idle or new geometry.
                half facet = pow(1-facing,3) * (.3+.7*saturate(lighting.normalWS.y+.3));
                color.rgb += facet * _EdgeSpecular * half3(.89,.94,1);
                color.rgb = MixFog(color.rgb,input.fog);
                return color;
            }
            half4 frag(Varyings input) : SV_Target
            {
                if (_Polish > 1.5 && _PawnSurface > .5)
                    return ShadeMonolith(input);
                half finishEnabled = saturate(_Polish);
                bool architecture = _Polish > 1.5;
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
                float corner = architecture ? _BoardDark : 0;
                float2 cornerCenter = sign(input.boardPosition.xz) * (_CornerOffset + _CornerGrow * .5);
                float2 cornerLocal = input.boardPosition.xz - cornerCenter;
                if (corner > .5)
                {
                    // Matches AddCornerInlay: extended square, then 0.06 removed.
                    localAlong = cornerLocal.x;
                    inward = cornerLocal.y;
                    shortEdge = (_CornerTileSize - .06) * .5 - abs(localAlong);
                    depthEdge = (_CornerTileSize - .06) * .5 - abs(inward);
                }
                float edgeDistance = min(shortEdge, depthEdge);
                float edgePixel = max(fwidth(edgeDistance), 0.00001);
                half topMask = saturate(input.normalWS.y) * saturate(_TileSurface + corner);
                half edgeMask = (1 - smoothstep(0, max(edgePixel * _EdgeShadingPixels, 0.00001), edgeDistance)) * topMask;
                float3 shortAxis = horizontal ? float3(1,0,0) : float3(0,0,1);
                float3 inwardAxis = side == 0 ? float3(0,0,1) : side == 1 ? float3(1,0,0) :
                    side == 2 ? float3(0,0,-1) : float3(-1,0,0);
                if (corner > .5)
                {
                    shortAxis = float3(1,0,0);
                    inwardAxis = float3(0,0,1);
                }
                float3 edgeNormalOS = shortEdge < depthEdge ? shortAxis * sign(localAlong) : inwardAxis * sign(inward);
                if (architecture)
                {
                    // A bounded virtual machining bevel, with derivative coverage. It
                    // catches a softbox at native size without widening the silhouette.
                    float bevelWidth = max(_ArchitecturalResponse.w, edgePixel * .8);
                    edgeMask = (1 - smoothstep(0, bevelWidth, max(0,edgeDistance))) * topMask;
                }
                lighting.normalWS = normalize(lighting.normalWS +
                    TransformObjectToWorldDir(edgeNormalOS) * edgeMask * _EdgeNormalStrength * (architecture ? 2.25 : 1));
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.vertexLighting = VertexLighting(input.positionWS, lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = input.color.rgb * _BaseColor.rgb * lerp(half3(1,1,1), _SurfaceTint.rgb, finishEnabled);
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                if (architecture)
                {
                    // Machined caps and satin flanks share one graphite, with a material
                    // response tied to real/virtual orientation rather than random noise.
                    half up = saturate(input.normalWS.y);
                    surface.albedo *= 1 - (1 - up) * _ArchitecturalResponse.z;
                    surface.smoothness = saturate(_Smoothness + edgeMask * .14 - (1-up) * .07);
                }
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = 1;
                float bandCenter = _TileSize.y * 0.5 - _TileSize.w - _TileSize.z * 0.5;
                float distanceToBand = abs(inward - bandCenter);
                if (corner > .5)
                    // Inner corner source is cw-0.18, inlay is cw-0.06: 0.06 inset.
                    distanceToBand = abs(edgeDistance - .06);
                // Receiver lies inward of the corner's existing recessed ring. The
                // outside slot gap must remain dark instead of becoming a second halo.
                half cornerReceiver = corner > .5 ? smoothstep(.04,.065,edgeDistance) : 1;
                float pixelWidth = max(fwidth(inward), 0.00001);
                float halfBand = max(_TileSize.z, _MinimumBandPixels * pixelWidth) * 0.5;
                half band = 1 - smoothstep(max(0, halfBand - pixelWidth * 0.5),
                    halfBand + pixelWidth * 0.5, distanceToBand);
                // Match the existing category header length, leaving its corners quiet.
                band *= 1 - smoothstep(_TileSize.x * 0.5 - 0.04, _TileSize.x * 0.5 - 0.03, abs(localAlong));
                // Corner sources are already drawn by the shared neon mesh; only their
                // receiving reflection is shaded here, avoiding a second false ring.
                band *= 1 - corner;
                half spill = exp2(-distanceToBand / max(0.001, _ReflectionFalloff));
                spill *= 1 - smoothstep(_ReflectionReach * 0.5, _ReflectionReach, distanceToBand);
                // A short colored reflection follows the actual category band, never a full-tile wash.
                half bandLength = 1 - smoothstep(_TileSize.x*.5-.03, _TileSize.x*.5, abs(localAlong));
                half reflectionDirection = .72 + .28 * saturate(dot(lighting.normalWS,normalize(float3(-.5,1,-.35))));
                spill *= lerp(1,bandLength * reflectionDirection,finishEnabled);
                if (architecture)
                {
                    // Source response occupies a short physical neighborhood. Its width
                    // survives subpixel sampling, but is still capped in board units.
                    float reach = max(.001, _ReflectionReach);
                    float falloff = min(reach * .45, max(_ReflectionFalloff, pixelWidth * .65));
                    spill = exp2(-distanceToBand / max(.001,falloff)) *
                        (1 - smoothstep(reach * .35,reach,distanceToBand));
                    half sourceOrientation = .35 + .65 * saturate(dot(lighting.normalWS,
                        normalize(float3(-.40,.80,-.45))));
                    spill *= lerp(bandLength,1,corner) * sourceOrientation * cornerReceiver;
                }
                half3 sourceLight = input.tileLight;
                if(architecture)
                {
                    half peak = max(sourceLight.r,max(sourceLight.g,sourceLight.b)) * _BandBoost;
                    sourceLight /= 1 + max(0,peak-.8) * .6;
                }
                surface.emission = sourceLight * (band * _BandBoost + spill * _ReflectionStrength) * topMask;
                // Add contact only at the tile perimeter; the category/selection channel
                // remains untouched. Derivative coverage keeps a ~1.1 px line on mobile.
                float2 contactUV = float2((input.tileIndex + 0.5) / 64.0, 0.25);
                half4 contact = SAMPLE_TEXTURE2D(_ContactTex, sampler_ContactTex, contactUV);
                half3 contactStyle = SAMPLE_TEXTURE2D(_ContactTex, sampler_ContactTex, contactUV + float2(0,0.5)).rgb;
                float contactDistance = corner > .5 ? abs(edgeDistance - .06) : max(0,edgeDistance);
                half contactLine = 1 - smoothstep(edgePixel * .30, edgePixel * 1.40, architecture ? contactDistance : edgeDistance);
                half contactInner = exp2(-contactDistance / .035) *
                    (1 - smoothstep(.035,.085,contactDistance));
                if (architecture)
                {
                    // The real neon mesh is the corner source. Standard tiles receive
                    // only a fine coverage aid; the satin receiver carries the diffusion.
                    surface.emission += contact.rgb * contact.a * contactLine * contactStyle.r *
                        _ContactOutlineGain * .35 * (1-corner) * topMask;
                }
                else
                {
                    surface.emission += contact.rgb * contact.a *
                        (contactLine * contactStyle.r * _ContactOutlineGain + contactInner * contactStyle.g) * topMask;
                }
                half reflectedContact = lerp(_ContactReflectionStrength,_ArrivalReflectionStrength,contactStyle.b);
                // B's dynamic reflection covers a short, derivative-aware neighborhood.
                // Width is bounded in board space; no full-tile wash at distance.
                float contactFalloff=max(.015,_ContactReflectionFalloff);
                float contactReach=max(.02,_ContactReflectionReach);
                float coveredFalloff=min(contactReach*.45,max(contactFalloff,edgePixel*.70*_ActiveSurfaceContrast));
                half contactSpill = exp2(-contactDistance/coveredFalloff) *
                    (1-smoothstep(contactReach*.45,contactReach,contactDistance));
                if (architecture)
                {
                    // A receiving satin lobe, not a thickened outline. Perpendicular
                    // edges react differently, as real faces under one nearby source do.
                    float3 sourceDirection = normalize(float3(-.45,.82,-.35));
                    half receiver = .38 + .62 * saturate(dot(lighting.normalWS,sourceDirection));
                    contactSpill *= receiver * cornerReceiver;
                }
                surface.emission += contact.rgb*contact.a*contactSpill*reflectedContact*topMask*finishEnabled;
                surface.smoothness=saturate(surface.smoothness+contact.a*contactSpill*_ActiveSurfaceContrast*topMask);
                if (architecture && input.stateIntensity > .8 && corner < .5)
                {
                    // Selection is already validated by the state channel. Give only
                    // this important outline subpixel coverage, never every idle tile.
                    half selected = smoothstep(.8,.95,input.stateIntensity);
                    half selectionLine = 1-smoothstep(edgePixel*.30,edgePixel*1.20,max(0,edgeDistance));
                    surface.emission += sourceLight * selected * topMask *
                        (selectionLine*.25*_BandBoost + contactSpill*_ReflectionStrength*.35);
                }
                // One actual active child footprint, including its shared-tile offset.
                float2 foot = (input.boardPosition.xz-_PawnFoot.xy)/max(.001,_PawnFoot.z);
                float footRadius2=dot(foot,foot);
                half footReflection=exp2(-4*footRadius2)*(1-smoothstep(.25,1,footRadius2));
                surface.emission += _PawnFootColor.rgb*footReflection*_PawnFoot.w*saturate(input.normalWS.y)*finishEnabled;
                half4 color = UniversalFragmentPBR(lighting, surface);
                // A broad virtual neutral studio panel supplies a restrained satin response.
                // Analytic reflection only: no texture, probe, animated light or extra pass.
                float3 reflectionOS = TransformWorldToObjectDir(reflect(-lighting.viewDirectionWS, lighting.normalWS));
                float travel = max(0, 10 - input.boardPosition.y) / max(0.08, reflectionOS.y);
                half studio = 0;
                if (architecture)
                {
                    // The orthographic view direction is supplied by URP, not camera
                    // position. Both panels live on a virtual ceiling, fixed to board
                    // space, so panning never slides lighting across the material.
                    float2 reflectedPoint = input.boardPosition.xz + reflectionOS.xz * travel;
                    float2 key = reflectedPoint - _StudioKey.xy;
                    float2 keyUV = float2(dot(key,float2(.7071,-.7071)) / max(.1,_StudioKey.z),
                        dot(key,float2(.7071,.7071)) / max(.1,_StudioKey.w));
                    float2 fill = reflectedPoint - _StudioFill.xy;
                    float2 fillUV = float2(dot(fill,float2(.7071,-.7071)) / max(.1,_StudioFill.z),
                        dot(fill,float2(.7071,.7071)) / max(.1,_StudioFill.w));
                    studio = exp2(-2*dot(keyUV,keyUV)) * _ArchitecturalResponse.x +
                        exp2(-1.5*dot(fillUV,fillUV)) * _ArchitecturalResponse.y;
                }
                else if (_Polish > .5)
                {
                    float2 reflectedPoint=input.boardPosition.xz+reflectionOS.xz*travel;
                    float2 key=reflectedPoint-float2(8,10);
                    float2 keyUV=float2(dot(key,float2(.7071,-.7071))/2.7,dot(key,float2(.7071,.7071))/11);
                    float2 fill=reflectedPoint-float2(1,11);
                    float2 fillUV=float2(fill.x/3.0,fill.y/9.0);
                    studio=exp2(-2*dot(keyUV,keyUV))*.72+exp2(-2*dot(fillUV,fillUV))*.18;
                }
                else
                {
                    float2 panel = input.boardPosition.xz + reflectionOS.xz * travel - float2(12,16);
                    float2 panelCoord = float2(dot(panel, float2(0.7071,-0.7071)) / 4.0,
                        dot(panel, float2(0.7071,0.7071)) / 12.0);
                    studio = exp2(-2 * dot(panelCoord, panelCoord) / max(0.25, 1.5 - _Smoothness));
                }
                half facing = saturate(dot(lighting.normalWS, lighting.viewDirectionWS));
                half3 f0 = lerp(half3(0.04,0.04,0.04), surface.albedo, _Metallic);
                half3 fresnel = f0 + (1 - f0) * pow(1 - facing, 5);
                half regionWeight = lerp(0.35,1,saturate(dot(input.color.rgb, half3(0.2126,0.7152,0.0722)) / 0.045));
                half receiverUp = architecture ? saturate(lighting.normalWS.y * .8 + .2) : saturate(input.normalWS.y);
                color.rgb += fresnel * studio * _StudioStrength * regionWeight * receiverUp;
                if(_Polish>.5)
                {
                    half luma=dot(input.color.rgb,half3(.2126,.7152,.0722));
                    half metalEdge=smoothstep(.022,.052,luma)*(1-_TileSurface);
                    half edgeOrientation=.25+.75*saturate(dot(lighting.normalWS,normalize(float3(-.65,.7,-.3))));
                    color.rgb += (edgeMask+metalEdge*.32)*edgeOrientation*_EdgeSpecular*half3(.89,.94,1);
                    float boardEdge=min(_BoardHalfExtent-abs(input.boardPosition.x),_BoardHalfExtent-abs(input.boardPosition.z));
                    half separation=(1-smoothstep(0,max(fwidth(boardEdge)*1.2,.025),boardEdge))*(1-_TileSurface);
                    color.rgb += separation*_BackgroundSeparation*saturate(input.normalWS.y);
                }
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
