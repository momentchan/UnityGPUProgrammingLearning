Shader "Cellular Growth/ParticleSphere"
{
    Properties
    {
        _Palette("Palette", 2D) = "white" {}
        _Spec("Specular", Color) = (0.1, 0.1, 0.1, 0.8)
        _Size("Size", Range(0.0, 1.0)) = 0.75
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "Particle.cginc"
    #include "../../Common/Libs/Random.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float4 color : COlOR;
        float2 uv : TEXCOORD0;
        float3 vposition : TEXCOORD1;
        float3 vright : TEXCOORD2;
        float3 vup : TEXCOORD3;
        float3 vforward : TEXCOOR4;
    };

    StructuredBuffer<Particle> _Particles;

    sampler2D _Texture;
    sampler2D _Palette;

    float4x4 _Local2World;
    float4   _Spec;
    float    _Size;

    v2f vert(appdata v, uint id : SV_InstanceID)
    {
        v2f o;
        Particle p = _Particles[id];

        float3 localPos = v.vertex.xyz * p.alive * p.radius * 2.0 * _Size;
        float3 worldPos = float3(p.position.xy, 0) + mul(_Local2World, float4(localPos, 1));
        float u = saturate(nrand(float2(id, 0)));
        float4 grad = tex2Dlod(_Palette, float4(u, 0.5, 0, 0));

        o.vertex    = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
        o.vposition = mul(UNITY_MATRIX_V, float4(worldPos, 1));
        o.vright    = normalize(UNITY_MATRIX_V[0].xyz);
        o.vup       = normalize(UNITY_MATRIX_V[1].xyz);
        o.vforward  = normalize(UNITY_MATRIX_V[2].xyz);
        o.uv = v.uv;
        o.color = grad;
        return o;
    }

    void frag(
        v2f i,
        out half4 outDiffuse        : SV_Target0,
        out half4 outSpecSmoothness : SV_Target1,
        out half4 outNormal         : SV_Target2,
        out half4 outEmission       : SV_Target3,
        out half  outDepth          : SV_Depth
    )
    {
        // Compute normal 
        half3 normal;
        normal.xy = i.uv * 2.0 - 1.0;
        half r2 = dot(normal.xy, normal.xy);
        if (r2 > 1.0)
            discard;
        normal.z = sqrt(1 - r2);

        // view pos -> clip pos -> depth
        half4 vp = half4(i.vposition.xyz + normal * _Size, 1.0);
        half4 cp = mul(UNITY_MATRIX_P, vp);
#if defined(SHADER_API_D3D11)
        outDepth = cp.z / cp.w;
#else
        outDepth = (cp.z / cp.w) * 0.5 + 0.5f;
#endif
        if (outDepth <= 0)
            discard;

        outDiffuse = i.color;
        outSpecSmoothness = _Spec;
        outNormal.xyz = normalize(normal.x * i.vright + normal.y * i.vup + normal.z * i.vforward);
        outNormal = half4(outNormal.xyz * 0.5 + 0.5, 1);
        outEmission = 0;
    }
    ENDCG

    SubShader
    {
        Tags{ "RenderType" = "Opaque"  "LightMode" = "Deferred" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
