Shader "MM/OutlineFullscreen"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.06,0.04,0.03,1)
        _OutlineThickness ("Thickness (px)", Range(0.5,4)) = 1.2
        _DepthThreshold ("Depth Threshold", Range(0.001,0.6)) = 0.04
        _NormalThreshold ("Normal Threshold", Range(0.05,2)) = 0.45
        _OutlineOpacity ("Opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "MMOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float _OutlineOpacity;

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                half4 sceneCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 texel = (_OutlineThickness.xx) / _ScreenParams.xy;
                float2 uvA = uv + texel * float2(-1,-1);
                float2 uvB = uv + texel * float2( 1, 1);
                float2 uvC = uv + texel * float2(-1, 1);
                float2 uvD = uv + texel * float2( 1,-1);

                // Profondeur : Roberts cross relatif (invariant a l'echelle)
                float dA = LinearEyeDepth(SampleSceneDepth(uvA), _ZBufferParams);
                float dB = LinearEyeDepth(SampleSceneDepth(uvB), _ZBufferParams);
                float dC = LinearEyeDepth(SampleSceneDepth(uvC), _ZBufferParams);
                float dD = LinearEyeDepth(SampleSceneDepth(uvD), _ZBufferParams);
                float dCenter = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float depthDiff = (abs(dB - dA) + abs(dD - dC)) / max(dCenter, 0.0001);
                float depthEdge = step(_DepthThreshold, depthDiff);

                // Normales : Roberts cross
                float3 nA = SampleSceneNormals(uvA);
                float3 nB = SampleSceneNormals(uvB);
                float3 nC = SampleSceneNormals(uvC);
                float3 nD = SampleSceneNormals(uvD);
                float3 nd = abs(nB - nA) + abs(nD - nC);
                float normalDiff = nd.x + nd.y + nd.z;
                float normalEdge = step(_NormalThreshold, normalDiff);

                float edge = max(depthEdge, normalEdge);

                half3 outCol = lerp(sceneCol.rgb, _OutlineColor.rgb, edge * _OutlineOpacity);
                return half4(outCol, sceneCol.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
