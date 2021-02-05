Shader "SpaceColonization/SkinnedTubularEdgesStandard"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Thickness("Thickness", Range(0.01, 0.1)) = 0.1

        [HDR] _Emission("Emission", Color) = (1, 1, 1, 1)

        [Space]
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0, 1)) = 0
    }

     SubShader
    {
        Tags { "RenderType" = "Opaque"  }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "Deferred"}
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma noshadowmask nodynlightmap nodirlightmap nolightmap
            #include "Includes/SkinnedTubularEdgesStandardCommon.cginc"
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_shadowcaster noshadowmask nodynlightmap nodirlightmap nolightmap
            #include "Includes/SkinnedTubularEdgesStandardCommon.cginc"
            ENDCG
        }
    }
}