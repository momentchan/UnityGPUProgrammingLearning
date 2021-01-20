Shader "Unlit/Sample3D"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "" {}
        _OffsetUV("UV Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
            };

            sampler3D _MainTex;
            float3 _OffsetUV;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xyz + _OffsetUV;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex3D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
