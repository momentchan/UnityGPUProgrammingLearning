Shader "Hidden/ScreenSpaceFluidRendering/BilateralFilterBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    float  _BlurRadius; 
    float  _BlurScale;
    float  _BlurDepthFallOff;
    float2 _BlurDir;
    

    fixed4 frag(v2f_img i) : SV_Target
    {
        float depth = tex2D(_MainTex, i.uv);

        if (depth <= 0.0) {
            return 0;
        }

        float sum   = 0.0;
        float wsum = 0.0;

        float2 blurDir = _MainTex_TexelSize * _BlurDir;

        [unroll(32)]
        for (float x = -_BlurRadius; x <= _BlurRadius; x += 1.0) {
            float sampleDepth = tex2D(_MainTex, i.uv.xy + x * blurDir.xy).x;
            
            // spatial domain
            float r = x * _BlurScale;
            float w = exp(-r * r);

            // range domain
            float2 r2 = (sampleDepth - depth) * _BlurDepthFallOff;
            float g = exp(-r2 * r2);

            sum += sampleDepth * w * g;
            wsum += w * g;
        }
        if (wsum > 0.0)
            sum /= wsum;
        return sum;
    }

    ENDCG

    SubShader
    {
        Cull Off ZTest Always ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}
