Shader "Custom/GPUClothRenderer"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _PositionTex("Position Texture", 2D) = "white" {}
        _NormalTex("Normal Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PositionTex;
        sampler2D _NormalTex;
    
        struct Input
        {
            float2 uv_MainTex;
            float3 vertexPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void vert(inout appdata_full v, out Input o) {
            v.vertex.xyz = tex2Dlod(_PositionTex, float4(v.texcoord.xy, 0, 0)).xyz;
            v.normal.xyz = tex2Dlod(_NormalTex, float4(v.texcoord.xy, 0, 0)).xyz;

            o.uv_MainTex = v.texcoord;
            o.vertexPos = v.vertex.xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;// *(1 - clamp(IN.vertexPos.z * 10, 0, 0.5));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
