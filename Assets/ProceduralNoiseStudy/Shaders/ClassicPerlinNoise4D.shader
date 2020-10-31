Shader "ProceduralNoiseStudy/ClassicPerlinNoise4D"
{
    Properties
    {
        _NoiseFrequency("Noise Frequency", Float) = 8
        _NoiseSpeed("Noise Speed", Float) = 1
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "ProceduralNoise/ClassicPerlinNoise4D.cginc"

        uniform float _NoiseFrequency;
    uniform float _NoiseSpeed;

    struct appdata {
        float4 vertex  : POSITION;
        float2 uv      : TEXCOORD0;
    };

    struct v2f {
        float4 vertex   : SV_POSITION;
        float3 localPos : TEXCOORD0;
    };

    v2f vert(appdata v) {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.localPos = v.vertex.xyz;
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        fixed4 col = fixed4(0.0, 0.0, 0.0, 1.0);
        float n = 0.5 + 0.5 * perlinNoise(float4(_NoiseFrequency * i.localPos.xyz, _Time.y * _NoiseSpeed));
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
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
