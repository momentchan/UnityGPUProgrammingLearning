Shader "SimpleImageEffect/ImageEffectPractice02"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;


            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb = step(i.uv.x, i.uv.y) * col.rgb + step(i.uv.y, i.uv.x) * (1 - col.rgb);
                return col;
            }
            ENDCG
        }
    }
}
