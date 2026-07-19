// ============================================================
// VRacer Classic — Flat-Shaded Polygon Shader (Model 1 Style)
// ============================================================
// This shader recreates the 1992 Sega Model 1 look:
// - Every triangle face is ONE solid flat color
// - Single directional light, no interpolation
// - No textures, no UV maps, no smooth normals
// - Crisp polygon edges — THE defining visual element
// ============================================================

Shader "VRacer/FlatShaded"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.3
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Flat shading: NO smooth normals interpolation
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float4 vertexColor : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _AmbientStrength;
                float  _ShadowStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Transform to world space
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.vertexColor = input.color;
                
                return output;
            }

            // Compute face normal using screen-space derivatives
            // This gives the TRUE geometric normal of the triangle face
            float3 GetFaceNormal(float3 positionWS)
            {
                float3 ddxPos = ddx(positionWS);
                float3 ddyPos = ddy(positionWS);
                return normalize(cross(ddxPos, ddyPos));
            }

            half4 frag(Varyings input) : SV_Target
            {
                // === FACE NORMAL (flat, no interpolation) ===
                float3 faceNormal = GetFaceNormal(input.positionWS);
                
                // === DIRECTIONAL LIGHTING ===
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;
                
                // NdotL for the entire face (one value per triangle)
                float NdotL = dot(faceNormal, lightDir);
                
                // Quantize lighting into discrete steps (Model 1 aesthetic)
                // Instead of smooth falloff, use 3-4 discrete brightness levels
                float lightLevel;
                if (NdotL > 0.7)      lightLevel = 1.0;   // Full bright
                else if (NdotL > 0.3)  lightLevel = 0.75;  // Mid bright
                else if (NdotL > 0.0)  lightLevel = 0.5;   // Dim
                else                   lightLevel = 0.35;  // Shadow side
                
                // === FINAL COLOR ===
                float3 baseColor = _BaseColor.rgb * input.vertexColor.rgb;
                
                // Ambient fill (shadow side never goes fully black)
                float3 ambient = baseColor * _AmbientStrength;
                
                // Directional contribution
                float3 lit = baseColor * lightColor * lightLevel * _ShadowStrength;
                
                // Combine
                float3 finalColor = ambient + lit;
                
                // Clamp to avoid over-brightening
                finalColor = saturate(finalColor);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass (required for receiving shadows if enabled)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
