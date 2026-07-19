// ============================================================
// VRacer Classic — Dithered Shadow Shader
// ============================================================
// Recreates the Model 1 dithering effect.
// Dark areas use a checkerboard pattern of two dark colors
// instead of smooth transparency — a Model 1 hardware trick.
// ============================================================

Shader "VRacer/DitheredTransparent"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.2, 0.2, 1)
        _DitherScale ("Dither Scale", Float) = 64.0
        _Opacity ("Opacity", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "DitheredTransparent"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv          : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _DitherScale;
                float  _Opacity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Bayer matrix dithering (4x4)
                // Recreates the Model 1 hardware dithering look
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 ditherCoord = screenUV * _ScreenParams.xy * _DitherScale;
                int2 ditherPixel = int2(fmod(ditherCoord, 4.0));
                
                // 4x4 Bayer matrix
                float bayerMatrix[16] = {
                    0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                    12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                    3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                    15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
                };
                
                float threshold = bayerMatrix[ditherPixel.y * 4 + ditherPixel.x];
                float alpha = _Opacity > threshold ? 1.0 : 0.0;
                
                clip(alpha - 0.5);
                return half4(_BaseColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
