Shader "VillageDefense/InvisibleWallGlow"
{
    Properties
    {
        [HDR] _GlowColor    ("Glow Color", Color)         = (1,1,1,1)
        _GlowCenter         ("Glow Center (UV)", Vector)  = (0.5,0.5,0,0)
        _GlowRadius         ("Glow Radius (UV)", Float)   = 0.35
        _GlowStrength       ("Glow Strength", Range(0,4)) = 0
        _Aspect             ("Aspect (w/h)", Float)       = 1
        _EdgeFade           ("Edge Fade", Range(0.001,0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "InvisibleWallGlow"
            Tags { "LightMode" = "UniversalForward" }

            // Additif : le halo n'assombrit jamais la scene, il ne fait que blanchir.
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float4 _GlowCenter;
                float  _GlowRadius;
                float  _GlowStrength;
                float  _Aspect;
                float  _EdgeFade;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // Halo circulaire centre sur la projection de la camera sur le mur.
                // On corrige par l'aspect du quad pour garder un disque et pas une ellipse.
                float2 d = IN.uv - _GlowCenter.xy;
                d.x *= _Aspect;

                float halo = 1.0 - smoothstep(0.0, max(_GlowRadius, 1e-4), length(d));
                halo *= halo; // degrade plus doux, evite le bord de disque net

                // Adoucit les bords du quad : sans ca on devine le rectangle du mur.
                float2 e    = min(IN.uv, 1.0 - IN.uv);
                float  edge = saturate(min(e.x, e.y) / _EdgeFade);

                float alpha = halo * edge * _GlowStrength * _GlowColor.a;
                return half4(_GlowColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
