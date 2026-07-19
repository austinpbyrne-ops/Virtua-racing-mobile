// ============================================================
// VRacer Classic — Sky Gradient Shader
// ============================================================
// Simple vertical gradient sky with optional dithering.
// Big Forest: bright blue gradient
// Bay Bridge: blue gradient with clouds
// Acropolis: orange-red sunset gradient
// ============================================================

Shader "VRacer/SkyGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.1, 0.3, 0.8, 1)
        _HorizonColor ("Horizon Color", Color) = (0.5, 0.7, 1.0, 1)
        _SkyHeight ("Sky Height", Float) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Background"
        }

        Pass
        {
            Name "SkyGradient"
            ZWrite Off
            Cull Off

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
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _HorizonColor;
                float  _SkyHeight;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Gradient from top to horizon based on world Y position
                float t = saturate(input.positionWS.y / _SkyHeight);
                float3 skyColor = lerp(_HorizonColor.rgb, _TopColor.rgb, t);
                return half4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }
}
