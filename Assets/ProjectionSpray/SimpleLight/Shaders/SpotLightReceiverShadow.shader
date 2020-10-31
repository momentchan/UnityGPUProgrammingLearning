Shader "ProjectionSpray/SimpleLight/SpotLightReceiverShadow"
{
    Properties
    {
        _LitPos("Light Position", Vector) = (0,0,0,0)
        _Intensity("Intensity", Float) = 1
        _LitColor("Light Color", Color) = (1,1,1,1)
        _Cookie("Cookie Texture", 2D) = "white"{}
        _LitDepth("Depth Texture", 2D) = "white"{}
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
            sampler2D _Cookie, _LitDepth;

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
                // Diffuse Lightening
                float3 to = _LitPos - i.worldPos;
                float3 lgihtDir = normalize(to);
                float dist = length(to);
                float atten = _Intensity * dot(to, i.normal) / (dist * dist);
                
                // SpotLight Cookie
                float4 lightSpacePos = mul(_WorldToLitMatrix, half4(i.worldPos,1.0));
                float4 projPos = mul(_ProjMatrix, lightSpacePos);
                projPos.z *= -1;
                float2 litUV = projPos.xy / projPos.z;
                litUV = litUV * 0.5 + 0.5;
                float lightCookie = tex2D(_Cookie, litUV);
                lightCookie *= 0 < litUV.x && litUV.x < 1 && 0 < litUV.y && litUV.y < 1 && 0 < projPos.z;

                // Shadow
                float lightDepth = tex2D(_LitDepth, litUV).r;
                atten *= 1.0 - saturate(10 * abs(lightSpacePos.z) - 10 * lightDepth);

                fixed4 col = max(0, atten) * lightCookie * _LitColor;
                return col;
            }
            ENDCG
        }
    }
}
