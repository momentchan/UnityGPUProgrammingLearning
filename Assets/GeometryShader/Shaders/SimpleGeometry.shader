Shader "GeometryShader/SimpleGeometry"
{
    Properties
    {
        _Height ("Height", Float) = 0.5
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)
        _TopColor ("Top Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _Height;
            float4 _BottomColor, _TopColor;

            struct v2g {
                float4 vertex : SV_POSITION;
            };

            struct g2f {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2g vert (appdata_full v){
                v2g o;
                o.vertex = v.vertex;
                return o;
            }

            [maxvertexcount(12)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> outStream) {
                float4 p1 = input[0].vertex;
                float4 p2 = input[1].vertex;
                float4 p3 = input[2].vertex;
                float4 c = float4(0, 0, -_Height, 0) + (p1 + p2 + p3) * 0.333f;

                g2f o1;
                o1.vertex = UnityObjectToClipPos(p1);
                o1.color = _BottomColor;

                g2f o2;
                o2.vertex = UnityObjectToClipPos(p2);
                o2.color = _BottomColor;

                g2f o3;
                o3.vertex = UnityObjectToClipPos(p3);
                o3.color = _BottomColor;

                g2f o;
                o.vertex = UnityObjectToClipPos(c);
                o.color = _TopColor;

                outStream.Append(o1);
                outStream.Append(o2);
                outStream.Append(o3);
                outStream.RestartStrip();

                outStream.Append(o1);
                outStream.Append(o2);
                outStream.Append(o);
                outStream.RestartStrip();

                outStream.Append(o);
                outStream.Append(o2);
                outStream.Append(o3);
                outStream.RestartStrip();

                outStream.Append(o1);
                outStream.Append(o);
                outStream.Append(o3);
                outStream.RestartStrip();
            }

            fixed4 frag (g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
