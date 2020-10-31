Shader "GeometryWireframe/PolygonLine"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Scale("Scale", Float) = 1
        _Speed("Speed", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define PI 3.14159265359

            float _Scale;
            float _Speed;
            int   _VertexNum;
            float4 _Color;
            float4x4 _LocalToWorldMatrix;

            struct Output{
                float4 pos : SV_POSITION;
            };

            Output vert(uint id : SV_VertexID)
            {
                Output o;
                o.pos = mul(_LocalToWorldMatrix, float4(0, 0, 0, 1));
                return o;
            }

            [maxvertexcount(65)]
            void geom(point Output input[1], inout LineStream<Output> outStream) {

                Output o;
                float rad = 2 * PI / (float)_VertexNum;
                float time = _Time.y * _Speed;

                for (int i = 0; i <= _VertexNum; i++) {
                    float angle = i * rad + time;
                    float4 pos = float4(_Scale * sin(angle), _Scale * cos(angle), 0, 1);

                    o.pos = UnityWorldToClipPos(pos);
                    outStream.Append(o);
                }
                outStream.RestartStrip();
            }


            fixed4 frag(Output i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
