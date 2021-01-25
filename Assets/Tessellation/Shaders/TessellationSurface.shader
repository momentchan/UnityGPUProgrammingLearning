Shader "Tessellation/TessellationSurface"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "white" {}
        _DispTex("Displacement", 2D) = "black" {}

        _Color ("Color", Color) = (1,1,1,1)
        _SpecColor("Spec color", color) = (0.5,0.5,0.5,0.5)
        _Specular("Specular", Range(0,1)) = 0.0
        _Glossiness("Glossiness", Range(0,1)) = 0.5

        _EdgeLength("Edge length", Range(2,50)) = 15
        _Displacement("Displacement", Range(0, 40.0)) = 0.3

        [Toggle] _Negative("Negative Displacement", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        LOD 200

        CGPROGRAM
        #pragma surface surf BlinnPhong addshadow fullforwardshadows vertex:disp tessellate:tessEdge nolightmap
        #pragma target 4.6
        #pragma shader_feature _NEGATIVE_ON
        #include "Tessellation.cginc"

        struct appdata {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };

        struct Input {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _DispTex;
        float4 _DispTex_ST;
        float _Displacement;
        float _EdgeLength;
        float _Specular;
        float _Glossiness;
        fixed4 _Color;

        // tess method and count setting
        // this function is called per patch
        float4 tessEdge(appdata v0, appdata v1, appdata v2) {
            return UnityEdgeLengthBasedTessCull(v0.vertex, v1.vertex, v2.vertex, _EdgeLength, _Displacement);
        }

        // called in domain shader
        void disp(inout appdata v) {
            float2 uv = v.texcoord.xy * _DispTex_ST.xy + _DispTex_ST.zw;

            float d = tex2Dlod(_DispTex, float4(uv, 0, 0)).r * _Displacement;
#ifdef _NEGATIVE_ON
            v.vertex.xyz += v.normal * d;
#else
            v.vertex.xyz -= v.normal * d;
#endif
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Specular =  _Specular;
            o.Gloss = _Glossiness;
            o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
