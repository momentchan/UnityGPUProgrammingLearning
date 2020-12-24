Shader "Cellular Growth/Edge"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Length("Length", Range(1, 10)) = 2.0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "Particle.cginc"
    #include "Edge.cginc"
    #include "../../Common/Libs/Random.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        uint vid : SV_VertexID;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float4 alpha : COlOR;
    };

    StructuredBuffer<Particle> _Particles;
    StructuredBuffer<Edge> _Edges;

    sampler2D _Texture;

    float4x4 _Local2World;

    float4 _Color;
    float _Length;

    v2f vert(appdata v, uint id : SV_InstanceID)
    {
        v2f o;
        
        Edge e = _Edges[id];
        Particle pa = _Particles[e.a];
        Particle pb = _Particles[e.b];

        float3 localPos = lerp(float3(pa.position, 0), float3(pb.position, 0), v.vid); // v.vid is either 0 or 1 becuase there is only two vertex
        float3 worldPos = mul(_Local2World, float4(localPos, 1));

        float u = saturate(nrand(float2(id, 0)));

        o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));

		float r = pa.radius + pb.radius;
		float d = distance(pa.position, pb.position);
		float alpha = saturate(r * _Length / d);
		o.alpha = alpha * e.alive;
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        return _Color * i.alpha;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType" = "Opaque"  "Queue"="Transparent-1" }
        LOD 100

        Pass
        {
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
