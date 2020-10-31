Shader "Unlit/GeometryGrass"
{
    Properties
    {
        [Header(Height Parameters)] [Space(5)]
        _Height("Overall Height", Float) = 1
        _BottomHeight("Bottom Height", Float) = 1
        _MiddleHeight("Middle Height", Float) = 1
        _TopHeight("Top Height", Float) = 1

        [Header(Width Parameters)][Space(5)]
        _Width("Overall Width", Float) = 1
        _BottomWidth("Bottom Width", Float) = 1
        _MiddleWidth("Middle Width", Float) = 1
        _TopWidth("Top Width", Float) = 1

        [Header(Bend Parameters)][Space(5)]
        _BottomBend("Bottom Bend", Float) = 1
        _MiddleBend("Middle Bend", Float) = 1
        _TopBend("Top Bend", Float) = 1

        _Wind("Wind Power", Float) = 1

        [Header(Color Parameters)][Space(5)]
        _BottomColor("Bottom Color", Color) = (1,1,1,1)
        _TopColor("Top Color", Color) = (1,1,1,1)

        [Header(Map Parameters)][Space(5)]
        _HeightMap("Height Map", 2D) = "white" {}
        _RotationMap("Rotation Map", 2D) = "white" {}
        _WindMap("Wind Map", 2D) = "white" {}
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

            sampler2D _HeightMap, _RotationMap, _WindMap;
            float _Height, _BottomHeight, _MiddleHeight, _TopHeight;
            float _Width, _BottomWidth, _MiddleWidth, _TopWidth;
            float _BottomBend, _MiddleBend, _TopBend;
            float4 _BottomColor, _TopColor;
            float _Wind;

            struct v2g
            {
                float4 vertex   : SV_POSITION;
                float3 normal   : NORMAL;
                float4 height   : TEXCOORD0;
                float4 rotation : TEXCOORD1;
                float4 wind     : TEXCOORD2;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2g vert (appdata_full v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.normal = v.normal;
                o.height = tex2Dlod(_HeightMap, v.texcoord);
                o.rotation = tex2Dlod(_RotationMap, v.texcoord);
                o.wind = tex2Dlod(_WindMap, v.texcoord);
                return o;
            }

            float3 RotateAlongZInDegree(float3 dir, float degree) {
                float alpha = degree / 180 * UNITY_PI;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, dir.xy), dir.z);
            }


            [maxvertexcount(7)]
            void geom(triangle v2g i[3], inout TriangleStream<g2f> outStream) {
                float4 p0 = i[0].vertex;
                float4 p1 = i[1].vertex;
                float4 p2 = i[2].vertex;

                float3 n0 = i[0].normal;
                float3 n1 = i[1].normal;
                float3 n2 = i[2].normal;

                float height    = (i[0].height.r + i[1].height.r + i[2].height.r) / 3;
                float rotation  = (i[0].rotation.r + i[1].rotation.r + i[2].rotation.r) / 3 * 360;
                float wind      = (i[0].wind.r + i[1].wind.r + i[2].wind.r) / 3;

                float4 center = (p0 + p1 + p2) / 3;
                float4 normal = float4((n0 + n1 + n2) / 3, 1);

                float bottomHeight  = height * _Height * _BottomHeight;
                float middleHeight  = height * _Height * _MiddleHeight;
                float topHeight     = height * _Height * _TopHeight;

                float bottomWidth   = _Width * _BottomWidth;
                float middleWidth   = _Width * _MiddleWidth;
                float topWidth      = _Width * _TopWidth;

                float4 dir = float4(RotateAlongZInDegree(p2 - p0, rotation), 1);

                g2f o[7];

                o[0].pos = center - dir * bottomWidth;
                o[0].color = _BottomColor;

                o[1].pos = center + dir * bottomWidth;
                o[1].color = _BottomColor;

                o[2].pos = center - dir * middleWidth + normal * bottomHeight;
                o[2].color = lerp(_BottomColor, _TopColor, 0.33333f);

                o[3].pos = center + dir * middleWidth + normal * bottomHeight;
                o[3].color = lerp(_BottomColor, _TopColor, 0.33333f);

                o[4].pos = center - dir * topWidth + normal * (bottomHeight + middleHeight);
                o[4].color = lerp(_BottomColor, _TopColor, 0.66666f);

                o[5].pos = center + dir * topWidth + normal * (bottomHeight + middleHeight);
                o[5].color = lerp(_BottomColor, _TopColor, 0.66666f);

                o[6].pos = center + normal * (bottomHeight + middleHeight + topHeight);
                o[6].color = _TopColor;

                dir = float4(1, 0, 0, 1);
                o[2].pos += dir * (_Wind * wind * _BottomBend) * sin(_Time.y);
                o[3].pos += dir * (_Wind * wind * _BottomBend) * sin(_Time.y);
                o[4].pos += dir * (_Wind * wind * _MiddleBend) * sin(_Time.y);
                o[5].pos += dir * (_Wind * wind * _MiddleBend) * sin(_Time.y);
                o[6].pos += dir * (_Wind * wind * _TopBend) * sin(_Time.y);

                [unroll]
                for (int i = 0; i < 7; i++) {
                    o[i].pos = UnityObjectToClipPos(o[i].pos);
                    outStream.Append(o[i]);
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
