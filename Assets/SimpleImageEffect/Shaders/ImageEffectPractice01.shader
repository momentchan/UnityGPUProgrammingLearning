Shader "SimpleImageEffect/ImageEffectPractice01"
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
                col.rgb = step(0.5, i.uv.x) * col.rgb + step(i.uv.x, 0.5) * (1 - col.rgb);
                return col;
            }
            ENDCG
        }
    }
}
