Shader "ProjectionSpray/SimpleLight/SpotLightReceiver"
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
            uniform float4x4 _ProjMatrix, _WorldToLitMatrix;
            sampler2D _Cookie;

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

                float4 lightSpacePos = mul(_WorldToLitMatrix, half4(i.worldPos,1.0));
                float4 projPos = mul(_ProjMatrix, lightSpacePos);
                projPos.z *= -1;
                float2 litUV = projPos.xy / projPos.z;
                litUV = litUV * 0.5 + 0.5;
                float lightCookie = tex2D(_Cookie, litUV);
                lightCookie *= 0 < litUV.x && litUV.x < 1 && 0 < litUV.y && litUV.y < 1 && 0 < projPos.z;

                fixed4 col = max(0, atten) * lightCookie * _LitColor;
                return col;
            }
            ENDCG
        }
    }
}
