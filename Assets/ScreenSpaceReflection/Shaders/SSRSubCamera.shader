Shader "Unlit/SSRSubCamera"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    sampler2D _CameraDepthTexture;

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float4 screen : TEXCOORD0;
    };

    half4 frag_main(v2f_img i) : SV_Target{
        float2 uv = i.uv;
        return tex2D(_MainTex, uv);
    }
    
    half4 frag_depth(v2f_img i) : SV_Target{
        float2 uv = i.uv;
        float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
        return half4(d, d, d, 1.0);
    }

    ENDCG
    SubShader
    {
        Blend Off ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_main
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_depth
            ENDCG
        }
    }
}
