Shader "GeometryWireframe/WaveLine"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _ScaleX("Scale X", Float) = 1
        _ScaleY("Scale Y", Float) = 1
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
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define PI 3.14159265359

            float _ScaleX;
            float _ScaleY;
            float _Speed;
            int   _VertexNum;
            float4 _Color;

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (uint id : SV_VertexID)
            {
                float div = (float)id / (_VertexNum - 1);

                float4 pos = float4((div - 0.5f) * _ScaleX, sin(2 * PI * div + _Time.y * _Speed) * _ScaleY, 0, 1);
                
                v2f o;
                o.vertex = UnityObjectToClipPos(pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
