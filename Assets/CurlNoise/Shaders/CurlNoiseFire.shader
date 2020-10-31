Shader "CurlNoise/CurlNoiseFire"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityLightingCommon.cginc"
    #include "AutoLight.cginc"

    struct Particle {
        float3 emitPos;
        float3 position;
        float4 velocity;   // xyz = velocity, w = velocity coef
        float3 life;       // x = time elapsed, y = life time, z = isActive (1 or -1)
        float3 size;       // x = current size, y = start size, z = target size
        float4 color;
        float4 startColor;
        float4 endColor;
    };

    StructuredBuffer <Particle> _ParticleBuffer;

    sampler2D _MainTex;
    float4x4 _ModelMatrix;

    struct appdata {
        float4 vertex : position;
        float2 uv : TEXCOORD0;
    };

    struct v2g
    {
        float4 pos : SV_POSITION;
        float4 color : COLOR;
        float size : texcoord0;
    };

    struct g2f {
        float4 pos : SV_POSITION;
        float4 color : COLOR;
        float2 uv  : texcoord0;
    };

    v2g vert(appdata v, uint id : SV_VertexID)
    {
        v2g o;

        Particle p = _ParticleBuffer[id];
        o.pos = mul(_ModelMatrix, float4(p.position, 1));
        o.color = p.color;
        o.size = p.size;
        return o;
    }

    [maxvertexcount(4)]
    void geom(point v2g input[1], inout TriangleStream<g2f> outStream) {
        g2f o;
        float3 up = float3(0, 1, 0);
        float3 forward = _WorldSpaceCameraPos - input[0].pos;
        forward.y = 0;
        forward = normalize(forward);
        float3 right = cross(up, forward);

        float halfS = input[0].size * 0.5f;

        float4 v[4];
        v[0] = float4(input[0].pos + halfS * right - halfS * up, 1.0);
        v[1] = float4(input[0].pos + halfS * right + halfS * up, 1.0);
        v[2] = float4(input[0].pos - halfS * right - halfS * up, 1.0);
        v[3] = float4(input[0].pos - halfS * right + halfS * up, 1.0);

        o.pos = mul(UNITY_MATRIX_VP, v[0]);
        o.uv = float2(1.0, 0.0);
        o.color = input[0].color;
        outStream.Append(o);

        o.pos = mul(UNITY_MATRIX_VP, v[1]);
        o.uv = float2(1.0, 1.0);
        o.color = input[0].color;
        outStream.Append(o);

        o.pos = mul(UNITY_MATRIX_VP, v[2]);
        o.uv = float2(0.0, 0.0);
        o.color = input[0].color;
        outStream.Append(o);

        o.pos = mul(UNITY_MATRIX_VP, v[3]);
        o.uv = float2(0.0, 1.0);
        o.color = input[0].color;
        outStream.Append(o);
        
        outStream.RestartStrip();
    }

    fixed4 frag(g2f i) : SV_Target
    {
        fixed4 color = tex2D(_MainTex, i.uv) * i.color;
        return color;
    }
        ENDCG

    SubShader
    {

        Tags{ "Queue" = "Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off Lighting Off ZWrite Off

        Pass
        {
            CGPROGRAM
                #pragma vertex vert
                #pragma geometry geom
                #pragma fragment frag
                #pragma target 5.0
            ENDCG
        }
    }
}
