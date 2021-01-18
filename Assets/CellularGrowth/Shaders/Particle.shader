Shader "Cellular Growth/Particle"
{
    Properties
    {
        _Texture ("Texture", 2D) = "white" {}
        _Palette ("Palette", 2D)  = "white" {}
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
        float4 color : COLOR;
        float2 uv : TEXCOORD0;
    };

    StructuredBuffer<Particle> _Particles;

    sampler2D _Texture;
    sampler2D _Palette;

    float4x4 _Local2World;
    float    _Size;

    v2f vert(appdata v, uint id : SV_InstanceID)
    {
        v2f o;
        Particle p = _Particles[id];

        float3 localPos = v.vertex.xyz * p.alive * p.radius * 2.0 * _Size;
        float3 worldPos = float3(p.position.xy, 0) + mul(_Local2World, float4(localPos, 1));
        float u = saturate(nrand(float2(id, 0)));
        float4 grad = tex2Dlod(_Palette, float4(u, 0.5, 0, 0));

        o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
        o.uv = v.uv;
        o.color = grad;
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        return tex2D(_Texture, i.uv) * i.color;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType" = "Opaque"  "Queue"="Transparent" }
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
