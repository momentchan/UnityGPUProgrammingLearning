Shader "ProjectionSpray/SimpleSpray/ProjectionSpray"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white"{}

        _DrawerPos("Drawer Position", Vector) = (0,0,0,0)
        _Color("Drawer Color", Color) = (1,1,1,1)
        _Emission("Intensity", Float) = 1

        _Cookie("Cookie Texture", 2D) = "white"{}
        _DrawerDepth("Drawer Depth Texture", 2D) = "white"{}
    }
    SubShader
    {
        Cull Off ZWrite Off Ztest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _Emission;
            float4 _DrawerPos, _Color;
            uniform float4x4 _ProjMatrix, _WorldToDrawerMatrix;
            sampler2D _Cookie, _DrawerDepth;
            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v.uv2.y = 1 - v.uv2.y;

                v2f o;
                o.vertex = float4(v.uv2 * 2.0 - 1.0, 0.0, 1.0);
                o.uv = v.uv2;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Diffuse Lightening
                float3 to = _DrawerPos - i.worldPos;
                float3 lgihtDir = normalize(to);
                float dist = length(to);
                float atten = _Emission * dot(to, i.normal) / (dist * dist);

                // SpotLight Cookie
                float4 drawerSpacePos = mul(_WorldToDrawerMatrix, half4(i.worldPos,1.0));
                float4 projPos = mul(_ProjMatrix, drawerSpacePos);
                projPos.z *= -1;
                float2 drawerUV = projPos.xy / projPos.z;
                drawerUV = drawerUV * 0.5 + 0.5;
                float cookie = tex2D(_Cookie, drawerUV);
                cookie *= 0 < drawerUV.x && drawerUV.x < 1 && 0 < drawerUV.y && drawerUV.y < 1 && 0 < projPos.z;

                // Shadow
                float drawerDepth = tex2D(_DrawerDepth, drawerUV).r;
                atten *= 1.0 - saturate(10 * abs(drawerSpacePos.z) - 10 * drawerDepth);

                i.uv.y = 1 - i.uv.y;
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb = lerp(col.rgb, _Color.rgb, saturate(col.a * _Emission * atten * cookie));
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
