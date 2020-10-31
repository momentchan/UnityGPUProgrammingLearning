Shader "ProceduralNoiseStudy/ClassicPerlinNoise2D"
{
    Properties
    {
        _NoiseFrequency("Noise Frequency", Float) = 8
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "ProceduralNoise/ClassicPerlinNoise2D.cginc"

    uniform float _NoiseFrequency;

    fixed4 frag(v2f_img i) : SV_Target
    {
        fixed4 col = fixed4(0.0, 0.0, 0.0, 1.0);
        float n = 0.5 + 0.5 * perlinNoise(float2(_NoiseFrequency * i.uv));
        col.rgb = n;
        return col;
    }
        ENDCG

        SubShader
    {
        Tags{ "RenderType" = "Opaque" }
            LOD 100
            Cull Back
            ZWrite On

            Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}
