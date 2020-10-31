Shader "ProceduralNoiseStudy/ValueNoise3D"
{
    Properties
    {
        _NoiseFrequency("Noise Frequency", Float) = 8
        _NoiseSpeed("Noise Speed", Float) = 1
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "ProceduralNoise/ValueNoise3D.cginc"

    uniform float _NoiseFrequency;
    uniform float _NoiseSpeed;

    fixed4 frag(v2f_img i) : SV_Target
    {
        fixed4 col = fixed4(0.0, 0.0, 0.0, 1.0);
        float n = 0.5 + 0.5 * valueNoise(float3(_NoiseFrequency * i.uv, _Time.y * _NoiseSpeed));
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
