Shader "ProjectionSpray/SimpleLight/PointLightReceiver"
{
    Properties
    {
        _LitPos("Light Position", Vector) = (0,0,0,0)
        _Intensity("Intensity", Float) = 1
        _LitColor("Light Color", Color) = (1,1,1,1)
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

            float _Intensity;
            float4 _LitPos, _LitColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 to = _LitPos - i.worldPos;
                float3 lgihtDir = normalize(to);
                float dist = length(to);

                float atten = _Intensity * dot(to, i.normal) / (dist * dist);

                fixed4 col = max(0, atten) * _LitColor;
                return col;
            }
            ENDCG
        }
    }
}
