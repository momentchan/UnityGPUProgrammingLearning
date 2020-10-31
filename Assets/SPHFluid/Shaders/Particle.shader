// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SPHFluid/Particle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius("Particle Radius", Float) = 0.05
        _Color("Color", Color) = (1,1,1,1)
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_ST;
    fixed4 _Color;

    float _Radius;

    struct v2g {
        float4 pos  : SV_POSITION;
        float4 color : Color;
    };

    struct g2f {
        float4 pos   : POSITION;
        float2 uv    : TEXCOORD0;
        float4 color : COLOR;
    };

    struct Particle {
        float2 position;
        float2 velocity;
    };

    StructuredBuffer<Particle> _ParticleBuffer;

    v2g vert(uint id : SV_VertexID) {
        v2g o = (v2g)0;
        o.pos = float4(_ParticleBuffer[id].position.xy, 0, 1);
        o.color = float4(0, 0.1, 0.1, 1);
        return o;
    }

    [maxvertexcount(4)]
    void geom(point v2g IN[1], inout TriangleStream<g2f> triStream) {
        float size = _Radius * 2;
        float h_size = _Radius;

        g2f pIn = (g2f)0;

        for (int x = 0; x < 2; x++) {
            for (int y = 0; y < 2; y++) {
                float2 uv = float2(x, y);
                pIn.pos = IN[0].pos + float4((uv * 2 - float2(1, 1)) * h_size, 0, 1);

                pIn.pos = UnityObjectToClipPos(pIn.pos);
                pIn.color = IN[0].color;
                pIn.uv = uv;
                triStream.Append(pIn);
            }
        }

        triStream.RestartStrip();
    }

    fixed4 frag(g2f input) : SV_Target{
        return tex2D(_MainTex, input.uv) * _Color;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        LOD 300

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDCG
        }
    }
}
