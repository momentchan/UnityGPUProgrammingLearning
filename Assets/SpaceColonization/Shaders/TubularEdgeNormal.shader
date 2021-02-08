﻿Shader "SpaceColonization/TubularEdge"
{
    Properties
    {
        _Thickness("Thickness", Range(0.01, 0.1)) = 0.1
        _Color("Color", Color) = (1, 1, 1, 1)

        
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        CGINCLUDE
        
        #include "Includes/Edge.cginc"
        #include "Includes/Node.cginc"
        #include "UnityCG.cginc"
        #include "UnityLightingCommon.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
            uint vid : SV_VertexID;
        };

        struct v2g
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;
            float2 uv2 : TEXCOORD1;
            float3 viewDir : TEXCOORD2;
            float alpha : COLOR;
        };

        struct g2f {
            float4 position : SV_POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
            float2 uv2 : TEXCOORD1;
        };

        StructuredBuffer<Node> _Nodes;
        StructuredBuffer<Edge> _Edges;
        uint _EdgesCount;

        float4x4 _Local2World;
        float4 _Color;
        half _Thickness;

        v2g vert(appdata v, uint iid : SV_InstanceID)
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

            v2g o;
            o.position = float4(worldPos, 1);
            o.viewDir = WorldSpaceViewDir(float4(worldPos, 1));
            o.uv = v.uv;
            o.uv2 = float2(lerp(na.offset, nb.offset, v.vid), 0);
            o.alpha = (na.active && nb.active) && (iid < _EdgesCount);
            return o;
        }


        g2f Create(float3 wpos, float3 wnorm, float2 uv, float2 uv2) {
            g2f o;
            o.position = UnityWorldToClipPos(wpos);
            o.uv = uv;
            o.uv2 = uv2;
            o.normal = wnorm;
            return o;
        }


        [maxvertexcount(64)]
        void geom(line v2g IN[2], inout TriangleStream<g2f> outStream) {
            v2g p0 = IN[0];
            v2g p1 = IN[1];

            float alpha = p0.alpha;
            float thickness = alpha * _Thickness;
            float3 tp = lerp(p0.position, p1.position, alpha);

            float3 t = normalize(p1.position - p0.position);
            float3 n = normalize(p0.viewDir);
            float3 bn = cross(t, n);
            n = cross(t, bn);

            static const uint rows = 6, cols = 6;
            static const float rows_inv = 1.0 / rows, cols_inv = 1.0 / (cols - 1);


            for (uint col = 0; col < cols; col++) {
                float r_col = (col * cols_inv) * UNITY_TWO_PI;

                float s, c;
                sincos(r_col, s, c);

                float3 normal = normalize(n * c + bn * s);

                float3 w0 = p0.position + normal * thickness;
                float3 w1 = p1.position + normal * thickness;

                g2f o0 = Create(w0, normal, p0.uv, p0.uv2);
                outStream.Append(o0);

                g2f o1 = Create(w1, normal, p1.uv, p1.uv2);
                outStream.Append(o1);
            }
            outStream.RestartStrip();

            // half circle
            for (uint row = 0; row < rows; row++) {
                float r_row_0 = (row * rows_inv) * UNITY_HALF_PI;
                float r_row_1 = ((row + 1) * rows_inv) * UNITY_HALF_PI;

                for (uint col = 0; col < cols; col++) {
                    float r_col = (col * cols_inv) * UNITY_TWO_PI;

                    float s, c;
                    sincos(r_col, s, c);

                    float3 n0 = normalize(n * c * (1 - sin(r_row_0)) + bn * s * (1 - sin(r_row_0)) + t * sin(r_row_0));
                    float3 n1 = normalize(n * c * (1 - sin(r_row_1)) + bn * s * (1 - sin(r_row_1)) + t * sin(r_row_1));

                    float3 w0 = tp + n0 * thickness;
                    float3 w1 = tp + n1 * thickness;


                    g2f o0 = Create(w0, n0, p0.uv, p0.uv2);
                    outStream.Append(o0);

                    g2f o1 = Create(w1, n1, p1.uv, p1.uv2);
                    outStream.Append(o1);
                }
                outStream.RestartStrip();
            }
        }


        fixed4 frag(g2f IN) : SV_Target
        {
            float3 normal = IN.normal;
            fixed3 normal01 = (normal + 1.0) * 0.5;
            fixed4 col = _Color;
            col.rgb*= normal01;
            return  col;
        }

        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDCG
        }
    }
}
