Shader "Unlit/AttractorParticleRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "Particle.cginc"

    StructuredBuffer<Particle> _Particles;

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv     : TEXCOORD0;
        float4 vertex : SV_POSITION;
        float4 color  : TEXCOORD1;
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4x4 _ModelMatrix;

    v2f vert(appdata v, uint id : SV_InstanceID)
    {
        Particle p = _Particles[id];
        float3 localPosition = v.vertex.xyz * p.size.x + p.position;
        float3 worldPosition = mul(_ModelMatrix, float4(localPosition, 1.0f));

        v2f o;
        o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1));
        o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
        o.color  = p.color;
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        fixed4 col = tex2D(_MainTex, i.uv) * i.color;
        return col;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
