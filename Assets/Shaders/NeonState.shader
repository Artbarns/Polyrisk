// NeonState.shader — toutes les rainures du plateau en un seul draw call.
//
// Chaque sommet porte dans uv2.x l'index de sa case (0..39). Le shader va lire
// couleur et intensité dans une texture 64x1 mise à jour par PolyRiskBoard.
// Allumer une case coûte donc l'écriture d'un pixel, pas un changement d'état
// de rendu.
//
// La lecture se fait en vertex shader : une seule fetch par sommet au lieu
// d'une par pixel. Sur mobile c'est la différence qui compte.

Shader "PolyRisk/NeonState"
{
    Properties
    {
        _ContactTex ("Transient pawn contact", 2D) = "black" {}
        _StateTex   ("État des cases (64x1)", 2D) = "black" {}
        _ContactGain ("Contact outline gain", Float) = 1
        _Count      ("Largeur de la texture d'état", Float) = 64
        _Boost      ("Multiplicateur global", Float) = 1
        _Falloff    ("Adoucissement des bords", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry+10"
        }

        Pass
        {
            Name "NeonUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ContactTex);
            SAMPLER(sampler_ContactTex);
            TEXTURE2D(_StateTex);
            SAMPLER(sampler_StateTex);

            CBUFFER_START(UnityPerMaterial)
                float _ContactGain;
                float _Count;
                float _Boost;
                float _Falloff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;      // teinte du groupe, repli si état vide
                float2 uv2        : TEXCOORD1;  // .x = index de case
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 emissive   : TEXCOORD0;
                float  rim        : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;

                float u = (IN.uv2.x + 0.5) / max(_Count, 1.0);
                float4 state = SAMPLE_TEXTURE2D_LOD(_StateTex, sampler_StateTex, float2(u, 0.5), 0);

                // état inexistant (case jamais initialisée) : on retombe sur la
                // teinte encodée dans le mesh, à intensité nulle
                float3 tint = state.a > 0.0001 ? state.rgb : IN.color.rgb;
                float4 contact = SAMPLE_TEXTURE2D_LOD(_ContactTex, sampler_ContactTex, float2(u, 0.25), 0);
                OUT.emissive = tint * state.a * _Boost + contact.rgb * contact.a * _ContactGain;

                // les faces horizontales rendent un peu plus fort que les parois :
                // ça creuse la lecture du logement sans coûter un second matériau
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.rim = lerp(1.0 - _Falloff, 1.0, saturate(abs(nWS.y)));

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(IN.emissive * IN.rim, 1.0);
            }
            ENDHLSL
        }

        // Ombres : les rainures sont fines, on les laisse projeter pour que les
        // parois de logement gardent leur assise. Supprimer ce Pass si le budget
        // shadow map est serré sur mobile.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
