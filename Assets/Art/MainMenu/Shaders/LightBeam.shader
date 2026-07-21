Shader "MM/LightBeam"
{
    Properties
    {
        [MainTexture] _MainTex ("Noise Texture", 2D) = "white" {}
        [HDR] _Color ("Beam Color", Color) = (1, 0.9, 0.6, 1)
        _ScrollSpeed ("Scroll Speed", Float) = 0.5
        _Intensity ("Intensity", Range(0, 3)) = 1.0
        _FadePower ("Fade Power", Range(0.1, 5)) = 1.5
        _NoiseScale ("Noise Scale", Float) = 2.0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.6
        _SoftEdges ("Soft Edges", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "LightBeam"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _ScrollSpeed;
                half _Intensity;
                half _FadePower;
                half _NoiseScale;
                half _NoiseStrength;
                half _SoftEdges;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.positionOS = IN.positionOS.xyz;
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                uv.y += _Time.y * _ScrollSpeed;
                uv *= _NoiseScale;

                half noise = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;
                half noise2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * 1.5 + 0.5).r;
                half combinedNoise = lerp(1.0, noise * noise2, _NoiseStrength);

                half vertFade = saturate(IN.positionOS.y + 0.5);
                vertFade = pow(vertFade, _FadePower) * pow(1.0 - vertFade, _FadePower) * 4.0;

                half horizCenter = abs(IN.uv.x - 0.5) * 2.0;
                half edgeFade = 1.0 - smoothstep(1.0 - _SoftEdges, 1.0, horizCenter);

                half alpha = combinedNoise * vertFade * edgeFade * _Intensity;
                half3 color = _Color.rgb * alpha;

                color = MixFog(color, IN.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
