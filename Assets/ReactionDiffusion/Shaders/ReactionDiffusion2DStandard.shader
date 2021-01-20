Shader "Custom/ReactionDiffusion2DSurface"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalTex ("Normal", 2D) = "white" {}

        _Color0("Bottom Color", Color) = (1,1,1,1)
        _Color1("Top Color", Color) = (1,1,1,1)

        _Emit0("EmitColor 0", Color) = (1,1,1,1)
        _Emit1("EmitColor 1", Color) = (1,1,1,1)

        _EmitInt0("Emit Intensity 0", Range(0, 1)) = 0
        _EmitInt1("Emit Intensity 1", Range(0, 1)) = 0

        _Smoothness0("Smoothness 0", Range(0, 1)) = 0.5
        _Smoothness1("Smoothness 1", Range(0, 1)) = 0.5

        _Metallic0("Metallic 0", Range(0, 1)) = 0.0
        _Metallic1("Metallic 1", Range(0, 1)) = 0.0

        _Threshold("Threshold", Range(0, 1)) = 0.1
        _Fading("Edge Smoothing", Range(0, 1)) = 0.2
        _NormalStrength("Normal Strength", Range(0, 1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard addshadow

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        sampler2D _NormalTex;
        float4 _NormalTex_TexelSize;

        fixed4 _Color0, _Color1;
        fixed4 _Emit0, _Emit1;
        half _EmitInt0, _EmitInt1;
        half _Smoothness0, _Smoothness1;
        half _Metallic0, _Metallic1;
        half _Threshold, _Fading;
        half _NormalStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            half v = tex2D(_MainTex, IN.uv_MainTex);

            float3 norm = UnpackNormal(tex2D(_NormalTex, uv));

            half p = smoothstep(_Threshold, _Threshold + _Fading, v);

            o.Albedo = lerp(_Color0.rgb, _Color1.rgb, p);
            o.Alpha = lerp(_Color0.a, _Color1.a, p);
            o.Smoothness = lerp(_Smoothness0, _Smoothness1, p);
            o.Metallic = lerp(_Metallic0, _Metallic1, p);
            o.Normal = normalize(float3(norm.x, norm.y, 1 - _NormalStrength));
            o.Occlusion = 1;
            o.Emission = lerp(_Emit0 * _EmitInt0, _Emit1 * _EmitInt1, p).rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
