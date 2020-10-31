Shader "ScreenSpaceReflection/MipMap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            int _LOD;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 frag(v2f_img i) : SV_Target
            {
                return tex2Dlod(_MainTex, float4(i.uv, 0, _LOD));
            }
            ENDCG
        }
    }
}
