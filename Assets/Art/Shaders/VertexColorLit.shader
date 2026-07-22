// Shader URP simple qui LIT LES COULEURS DE VERTEX (Color Attribute de Blender).
// URP/Lit les ignore : c'est pour ça que les sapins (Material.008) sortent tout blancs
// alors qu'ils sont colorés dans Blender.
Shader "VillageDefense/VertexColorLit"
{
    Properties
    {
        [MainColor] _BaseColor("Teinte", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Texture (optionnelle)", 2D) = "white" {}
        _VertexColorStrength("Force couleur de vertex", Range(0,1)) = 1
        [Toggle] _VertexColorSRGB("Couleurs de vertex en sRGB (Blender)", Float) = 1
        _Smoothness("Lissage", Range(0,1)) = 0.05
        _Metallic("Metallique", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _VertexColorStrength;
            half   _VertexColorSRGB;
            half   _Smoothness;
            half   _Metallic;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                half3  normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                half4  color       : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                half3  vertexSH    : TEXCOORD5;
                float  fogFactor   : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS  = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                OUT.normalWS    = nrm.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color       = IN.color;
                OUT.shadowCoord = GetShadowCoord(pos);
                OUT.vertexSH    = SampleSH(nrm.normalWS);
                OUT.fogFactor   = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // Blender ecrit les couleurs de vertex en sRGB ; le rendu Unity est lineaire.
                half3 vcol = IN.color.rgb;
                vcol = lerp(vcol, SRGBToLinear(vcol), _VertexColorSRGB);
                vcol = lerp(half3(1, 1, 1), vcol, _VertexColorStrength);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = map.rgb * _BaseColor.rgb * vcol;
                surface.alpha      = 1;
                surface.metallic   = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion  = 1;
                surface.normalTS   = half3(0, 0, 1);

                InputData input = (InputData)0;
                input.positionWS          = IN.positionWS;
                input.normalWS            = normalize(IN.normalWS);
                input.viewDirectionWS     = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                input.shadowCoord         = IN.shadowCoord;
                input.fogCoord            = IN.fogFactor;
                input.bakedGI             = IN.vertexSH;
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                input.shadowMask          = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return OUT;
            }

            half4 ShadowFrag(ShadowVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes IN)
            {
                DepthVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthFrag(DepthVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
