Shader "SpaceColonization/Edge"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend One One
            ZTest Always
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Edge.cginc"
            #include "Node.cginc"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vid : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float alpha : COLOR;
            };

            StructuredBuffer<Node> _Nodes;
            StructuredBuffer<Edge> _Edges;
            uint _EdgesCount;

            float4x4 _Local2World;
            float4 _Color;

            v2f vert (appdata v, uint iid : SV_InstanceID)
            {
                Edge e = _Edges[iid];
                Node na = _Nodes[e.a];
                Node nb = _Nodes[e.b];

                float3 ap = na.position;
                float3 bp = nb.position;
                float3 dir = bp - ap;
                bp = ap + dir * nb.t;
                float3 localPos = lerp(ap, bp, v.vid);
                float3 worldPos = mul(_Local2World, float4(localPos, 1));

                v2f o;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
                o.uv = v.uv;
                o.uv2 = float2(lerp(na.offset, nb.offset, v.vid), 0);
                o.alpha = (na.active && nb.active) && (iid < _EdgesCount);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color * i.alpha;
            }
            ENDCG
        }
    }
}
