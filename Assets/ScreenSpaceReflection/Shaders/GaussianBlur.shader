Shader "ScreenSpaceReflection/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    float _BlurSize;

    fixed4 gaussian_blur_x(v2f_img i) : SV_Target
    {
        float weight[5] = { 0.2270270, 0.1945945, 0.1216216, 0.0540540, 0.0162162 };
        float2 size = _MainTex_TexelSize * _BlurSize;
        fixed4 col = tex2D(_MainTex, i.uv) * weight[0];

        for (int j = 1; j < 5; j++) {
            col += tex2D(_MainTex, i.uv + float2(j, 0) * size) * weight[j];
            col += tex2D(_MainTex, i.uv - float2(j, 0) * size) * weight[j];
        }
        return col;
    }

    fixed4 gaussian_blur_y(v2f_img i) : SV_Target
    {
        float weight[5] = { 0.2270270, 0.1945945, 0.1216216, 0.0540540, 0.0162162 };
        float2 size = _MainTex_TexelSize * _BlurSize;
        fixed4 col = tex2D(_MainTex, i.uv) * weight[0];

        for (int j = 1; j < 5; j++) {
            col += tex2D(_MainTex, i.uv + float2(0, j) * size) * weight[j];
            col += tex2D(_MainTex, i.uv - float2(0, j) * size) * weight[j];
        }
        return col;
    }
    ENDCG


    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment gaussian_blur_x
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment gaussian_blur_y
            ENDCG
        }
    }
}
