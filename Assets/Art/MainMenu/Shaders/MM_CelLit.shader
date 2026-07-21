Shader "MM/CelLit"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Steps ("Shading Steps", Range(1,16)) = 12
        _ShadeTint ("Shadow Tint", Color) = (0.50,0.42,0.55,1)
        _ShadeStrength ("Shadow Strength", Range(0,1)) = 0.85
        _ShadowSoftness ("Shadow Softness", Range(0,1)) = 0.75
        _RimColor ("Rim Color", Color) = (1,0.82,0.55,1)
        _RimPower ("Rim Power", Range(0.5,10)) = 2.5
        _RimStrength ("Rim Strength", Range(0,2)) = 0.4
        _RimSoftness ("Rim Softness", Range(0,1)) = 0.5
        _AmbientStrength ("Ambient Fill", Range(0,1.5)) = 0.6
        _SpecularColor ("Specular Color", Color) = (1,0.95,0.85,1)
        _SpecularPower ("Specular Power", Range(1,128)) = 48
        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.3
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "UniversalMaterialType"="Lit" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _ShadeTint;
            half4 _RimColor;
            half4 _SpecularColor;
            half _Steps;
            half _ShadeStrength;
            half _ShadowSoftness;
            half _RimPower;
            half _RimStrength;
            half _RimSoftness;
            half _AmbientStrength;
            half _SpecularPower;
            half _SpecularStrength;
            half _BumpScale;
            half _Cutoff;
        CBUFFER_END

        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);
        ENDHLSL

        // ---- ForwardLit (cel) ----
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float2 uv:TEXCOORD0;
                float3 positionWS:TEXCOORD1;
                float3 normalWS:TEXCOORD2;
                float4 tangentWS:TEXCOORD3;
                half   fogFactor:TEXCOORD4;
            };

            Varyings vert (Attributes IN)
            {
                Varyings o = (Varyings)0;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nrm.normalWS;
                o.tangentWS = float4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 albedo = baseTex * _BaseColor;
                #ifdef _ALPHATEST_ON
                    clip(albedo.a - _Cutoff);
                #endif

                float3 N = normalize(IN.normalWS);
                #ifdef _NORMALMAP
                    half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                    float3 T = normalize(IN.tangentWS.xyz);
                    float3 B = normalize(cross(N, T) * IN.tangentWS.w);
                    N = normalize(mul(nTS, float3x3(T, B, N)));
                #endif

                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL = dot(N, mainLight.direction);
                half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                half diff = saturate(NdotL) * atten;
                half steps = max(2.0h, _Steps);
                half stepSize = 1.0h / steps;
                // Genshin-style soft cel: blend between bands with wide smoothstep
                half scaledDiff = diff * steps;
                half bandIndex = floor(scaledDiff);
                half bandFrac = frac(scaledDiff);
                half softWidth = _ShadowSoftness * 0.8h; // wide transition zone
                half smoothEdge = smoothstep(0.5h - softWidth, 0.5h + softWidth, bandFrac);
                half banded = saturate((bandIndex + smoothEdge) * stepSize);
                // Add subtle gradient within each band for less flat look
                half innerGradient = lerp(0.95h, 1.0h, bandFrac * 0.3h);
                banded *= innerGradient;

                half3 litColor = albedo.rgb * mainLight.color;
                half3 shadeColor = albedo.rgb * lerp(half3(1,1,1), _ShadeTint.rgb, _ShadeStrength);
                half3 diffuse = lerp(shadeColor, litColor, banded);

                half3 ambient = SampleSH(N) * albedo.rgb * _AmbientStrength;

                half3 color = diffuse + ambient;

                half3 H = normalize(mainLight.direction + V);
                half NdotH = saturate(dot(N, H));
                half spec = pow(NdotH, _SpecularPower) * atten;
                // Genshin-style soft specular: smoothstep for wider, softer highlight
                half specSoft = smoothstep(0.5h, 1.0h, spec * 2.0h);
                color += _SpecularColor.rgb * specSoft * _SpecularStrength;

                half rim = 1.0h - saturate(dot(V, N));
                half rimMask = smoothstep(1.0h - _RimSoftness, 1.0h, rim);
                rimMask *= pow(rim, _RimPower);
                rimMask *= _RimStrength * saturate(NdotL * 0.5h + 0.5h);
                color += _RimColor.rgb * rimMask;

                color += _EmissionColor.rgb;

                color = MixFog(color, IN.fogFactor);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // ---- ShadowCaster ----
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            float4 GetShadowPositionHClip(A input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            V ShadowVert (A input)
            {
                V o;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.positionCS = GetShadowPositionHClip(input);
                return o;
            }

            half4 ShadowFrag (V input) : SV_TARGET
            {
                #ifdef _ALPHATEST_ON
                    half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---- DepthOnly ----
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local _ALPHATEST_ON

            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            V DepthVert (A input)
            {
                V o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 DepthFrag (V input) : SV_TARGET
            {
                #ifdef _ALPHATEST_ON
                    half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---- DepthNormals (source de l'outline post-process) ----
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormVert
            #pragma fragment DepthNormFrag
            #pragma shader_feature_local _ALPHATEST_ON

            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 normalWS:TEXCOORD1; };

            V DepthNormVert (A input)
            {
                V o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 DepthNormFrag (V input) : SV_TARGET
            {
                #ifdef _ALPHATEST_ON
                    half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return half4(normalize(input.normalWS), 0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
