Shader "UI/URP_Glassmorphism_Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (Mask)", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.05, 0.05, 0.1, 0.6)
        _BlurSize ("Blur Size", Range(0.001, 0.05)) = 0.015
    }

    SubShader
    {
        Tags
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Glassmorphism"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Access to the background opaque texture
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float _BlurSize;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Object space to clip space
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // Compute screen coordinates for sampling the background
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.color = input.color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Normalize screen UV
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // 9-Tap Box Blur approach grabbing the pixels behind
                half3 blurColor = half3(0.0, 0.0, 0.0);
                float2 offsets[9] = {
                    float2(-1, -1), float2(0, -1), float2(1, -1),
                    float2(-1,  0), float2(0,  0), float2(1,  0),
                    float2(-1,  1), float2(0,  1), float2(1,  1)
                };

                // The screen-space aspect ratio fix is often needed, but since we just want a "smear", basic offset suffices.
                for(int i = 0; i < 9; i++)
                {
                    float2 currentUV = screenUV + offsets[i] * _BlurSize;
                    blurColor += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, currentUV).rgb;
                }
                blurColor /= 9.0;

                // Sample the UI PNG Mask / Image (your 28px Rounded Mask)
                half4 maskColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Blend the blurred background with the cyber Tint Color
                // Using alpha of TintColor to control darkness mix
                half3 mixedColor = lerp(blurColor, _TintColor.rgb, _TintColor.a);

                // Use the alpha channel of your rounded mask PNG and the UI element's vertex Color (extinction logic)
                half finalAlpha = maskColor.a * input.color.a;

                return half4(mixedColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
