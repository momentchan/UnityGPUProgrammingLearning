Shader "ProjectionSpray/SimpleLight/SpotLightDepth"
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
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y) * c.z;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // SpotLight Cookie
                float4 lightSpacePos = mul(_WorldToLitMatrix, half4(i.worldPos,1.0));
                float lightSpaceDepth = lightSpacePos.z;
                float4 col = float4(hsv2rgb(float3(frac(lightSpaceDepth * 0.5), 1, 1)), 1);
                return col;
            }
            ENDCG
        }
    }
}
