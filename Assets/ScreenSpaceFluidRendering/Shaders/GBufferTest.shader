Shader "ScreenSpaceFluidRendering/GBufferTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(DIFFUSE, SPECULAR, NORMAL, EMISSION, DEPTH, SOURCE)]
        _GBufferType("G-Buffer Type", Float) = 0
    }
    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;

    sampler2D _CameraGBufferTexture0; // rgb: diffuse,  a: occulusion
    sampler2D _CameraGBufferTexture1; // rgb: specular, a: smoothness
    sampler2D _CameraGBufferTexture2; // rgb: normal,   a: unused
    sampler2D _CameraGBufferTexture3; // rgb: emission, a: unused
    sampler2D _CameraDepthTexture;

    fixed4 frag(v2f_img i) : SV_Target
    {
        #ifdef _GBUFFERTYPE_DIFFUSE
            fixed4 col = tex2D(_CameraGBufferTexture0, i.uv);
        #elif _GBUFFERTYPE_SPECULAR
            fixed4 col = tex2D(_CameraGBufferTexture1, i.uv);
        #elif _GBUFFERTYPE_NORMAL
            fixed4 col = tex2D(_CameraGBufferTexture2, i.uv);
        #elif _GBUFFERTYPE_EMISSION
            fixed4 col = tex2D(_CameraGBufferTexture3, i.uv);
        #elif _GBUFFERTYPE_DEPTH
            fixed4 col = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
        #else 
            fixed4 col = tex2D(_MainTex, i.uv);
        #endif
        return fixed4(col.rgb * col.a, 1.0);
    }

    ENDCG
    SubShader
    {
        Cull Off ZTest Always ZWrite Off Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma shader_feature _GBUFFERTYPE_DIFFUSE _GBUFFERTYPE_SPECULAR _GBUFFERTYPE_NORMAL _GBUFFERTYPE_EMISSION _GBUFFERTYPE_DEPTH _GBUFFERTYPE_SOURCE
            ENDCG
        }
    }
}
