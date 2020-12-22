Shader "StrangeAttractor/AttractorParticleRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "Particle.cginc"
    #include "../../Common/Libs/Definition.cginc"

    StructuredBuffer<Particle> _Particles;

    struct v2g
    {
        float4 pos : SV_POSITION;
        float size : TEXCOORD0;
        float4 color  : TEXCOORD1;
    };

    struct g2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 color  : TEXCOORD1;
        UNITY_FOG_COORDS(2)
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4x4 _ModelMatrix;

    v2g vert(uint id : SV_VertexID)
    {
        Particle p = _Particles[id];

        v2g o;
        o.pos    = float4(p.position, 1);
        o.size   = p.size.x;
        o.color  = p.color;
        return o;
    }

    [maxvertexcount(4)]
    void geom(point v2g IN[1], inout TriangleStream<g2f> stream) {
        g2f o;

        float4 localPos = IN[0].pos;
        float4 color = IN[0].color;
        float size = IN[0].size;

        [unroll]
        for (int i = 0; i < 4; i++) {
            float3 worldPos = mul(_ModelMatrix, float4(localPos.xyz, 1.0f));

            float3 displace = g_positions[i] * size;
            worldPos += mul(unity_CameraToWorld, displace);

            o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
            o.uv = g_texcoords[i];
            o.color = color;
            UNITY_TRANSFER_FOG(o, o.pos);
            stream.Append(o);
        }
        stream.RestartStrip();
    }

    fixed4 frag(g2f i) : SV_Target
    {
        float uv = i.uv;
        fixed4 col = tex2D(_MainTex, i.uv) * i.color;
        UNITY_APPLY_FOG(i.fogCoord, col);
        return col;
    }
    ENDCG

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "RenderQueue" = "Transparent" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            LOD 100
            
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 5.0
            ENDCG
        }
    }
}
