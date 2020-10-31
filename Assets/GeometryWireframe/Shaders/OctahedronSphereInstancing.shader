Shader "GeometryWireframe/OctahedronSphereInstancing"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Blend One One
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "OctahedronSphereData.cginc"
            #include "../../Common/Libs/Quaternion.cginc"

            StructuredBuffer<OctahedronSphere> _ParticleBuffer;

            #define PI 3.14159265359

            float4 _Color;

            struct v2g {
                float4 pos : SV_POSITION;
                float scale : TEXCOORD0;
                int level : TEXCOORD1;
                float4 rotation : TEXCOORD2;
                int id : TEXCOORD3;
            };

            struct g2f {
                float4 pos : SV_POSITION;
            };

            v2g vert(uint id : SV_VertexID)
            {
                v2g o;
                uint idx = id / 8;
                OctahedronSphere data = _ParticleBuffer[idx];

                o.pos = float4(data.position, 0);
                o.scale = data.scale;
                o.level = data.level;
                o.rotation = data.rotation;
                o.id = id;
                return o;
            }

            [maxvertexcount(256)]
            void geom(point v2g input[1], inout LineStream<g2f> outStream) {

                g2f o1, o2, o3;
                float3 pos  = input[0].pos;
                float4 rot  = input[0].rotation;
                float scale = input[0].scale;
                int n       = input[0].level;

                float4 init_vectors[24];
                // 0 : the triangle vertical to (1,1,1)
                init_vectors[0] = float4(0, 1, 0, 0);
                init_vectors[1] = float4(0, 0, 1, 0);
                init_vectors[2] = float4(1, 0, 0, 0);
                // 1 : to (1,-1,1)
                init_vectors[3] = float4(0, -1, 0, 0);
                init_vectors[4] = float4(1, 0, 0, 0);
                init_vectors[5] = float4(0, 0, 1, 0);
                // 2 : to (-1,1,1)
                init_vectors[6] = float4(0, 1, 0, 0);
                init_vectors[7] = float4(-1, 0, 0, 0);
                init_vectors[8] = float4(0, 0, 1, 0);
                // 3 : to (-1,-1,1)
                init_vectors[9] = float4(0, -1, 0, 0);
                init_vectors[10] = float4(0, 0, 1, 0);
                init_vectors[11] = float4(-1, 0, 0, 0);
                // 4 : to (1,1,-1)
                init_vectors[12] = float4(0, 1, 0, 0);
                init_vectors[13] = float4(1, 0, 0, 0);
                init_vectors[14] = float4(0, 0, -1, 0);
                // 5 : to (-1,1,-1)
                init_vectors[15] = float4(0, 1, 0, 0);
                init_vectors[16] = float4(0, 0, -1, 0);
                init_vectors[17] = float4(-1, 0, 0, 0);
                // 6 : to (-1,-1,-1)
                init_vectors[18] = float4(0, -1, 0, 0);
                init_vectors[19] = float4(-1, 0, 0, 0);
                init_vectors[20] = float4(0, 0, -1, 0);
                // 7 : to (1,-1,-1)
                init_vectors[21] = float4(0, -1, 0, 0);
                init_vectors[22] = float4(0, 0, -1, 0);
                init_vectors[23] = float4(1, 0, 0, 0);

                int i = (input[0].id % 8) * 3;
                {

                    for (int p = 0; p < n; p++) {
                        // edge index 1
                        float4 edge_p1 = qslerp(init_vectors[i], init_vectors[i + 2], (float)p / n);
                        float4 edge_p2 = qslerp(init_vectors[i + 1], init_vectors[i + 2], (float)p / n);
                        float4 edge_p3 = qslerp(init_vectors[i], init_vectors[i + 2], (float)(p + 1) / n);
                        float4 edge_p4 = qslerp(init_vectors[i + 1], init_vectors[i + 2], (float)(p + 1) / n);

                        for (int q = 0; q < (n - p); q++) {
                            float4 a = qslerp(edge_p1, edge_p2, (float)q / (n - p));
                            float4 b = qslerp(edge_p1, edge_p2, (float)(q + 1) / (n - p));
                            float4 c, d;

                            if (distance(edge_p3, edge_p4) < 0.00001)
                            {
                                c = edge_p3;
                                d = edge_p3;
                            }
                            else {
                                c = qslerp(edge_p3, edge_p4, (float)q / (n - p - 1));
                                d = qslerp(edge_p3, edge_p4, (float)(q + 1) / (n - p - 1));
                            }

                            o1.pos = UnityObjectToClipPos(input[0].pos + float4(rotateWithQuaternion(a, rot) * scale, 1));
                            o2.pos = UnityObjectToClipPos(input[0].pos + float4(rotateWithQuaternion(b, rot) * scale, 1));
                            o3.pos = UnityObjectToClipPos(input[0].pos + float4(rotateWithQuaternion(c, rot) * scale, 1));

                            outStream.Append(o1);
                            outStream.Append(o2);
                            outStream.Append(o3);
                            outStream.RestartStrip();

                            if (q < (n - p - 1))
                            {

                                o1.pos = UnityObjectToClipPos(input[0].pos + float4(rotateWithQuaternion(c, rot) * scale, 1));
                                o2.pos = UnityObjectToClipPos(input[0].pos + float4(rotateWithQuaternion(b, rot) * scale, 1));
                                o3.pos = UnityObjectToClipPos(input[0].pos + float4(rotateWithQuaternion(d, rot) * scale, 1));

                                outStream.Append(o1);
                                outStream.Append(o2);
                                outStream.Append(o3);
                                outStream.RestartStrip();

                            }
                        }
                    }
                }
            }


            fixed4 frag(g2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
