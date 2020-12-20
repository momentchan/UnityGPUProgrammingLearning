Shader "Unlit/ParticleRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    #include "Body.cginc"
    #include "../../Common/Libs/Definition.cginc"

    StructuredBuffer<Body> _Particles;
    float _Scale;
    float4 _Color;
    sampler2D _MainTex;
    float4 _MainTex_ST;

	struct v2g {
        float4 pos: SV_POSITION;
	};

    struct g2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD1;
    };

    float3 hsv2rgb_smooth(float3 c)
    {
        float3 rgb = clamp(abs(fmod(c.x * 6.0 + float3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);

        rgb = rgb * rgb * (3.0 - 2.0 * rgb); // cubic smoothing	

        return c.z * lerp((float3)1, rgb, (float3)c.y);
    }

    v2g vert(uint id : SV_VertexID) {
        v2g o;
        o.pos = float4(_Particles[id].position, 1);
        return o;
    }

    [maxvertexcount(4)]
    void geom(point v2g IN[1], inout TriangleStream<g2f> stream) {
        g2f o;

        float4 vertPos = IN[0].pos;

        [unroll]
        for (int i = 0; i < 4; i++) {
            float3 displace = g_positions[i] * _Scale;
            float3 pos = vertPos + mul(unity_CameraToWorld, displace);
            o.pos = UnityObjectToClipPos(float4(pos, 1.0));
            o.uv = g_texcoords[i];
            stream.Append(o);
        }
        stream.RestartStrip();
    }

    fixed4 frag(g2f i) : SV_Target{
        float uv = i.uv;
        fixed4 col = tex2D(_MainTex, i.uv) * _Color;
        //col.rgb *= hsv2rgb_smooth(float3(sin(_Time.x) * 0.5 + 0.5, 0.8, 1));
        return col;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderQueue"="Transparent" }
        ZWrite Off
        Blend One One
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDCG
        }
    }
}
