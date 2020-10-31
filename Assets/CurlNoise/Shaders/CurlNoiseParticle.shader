Shader "CurlNoise/CurlNoiseParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

    struct v2f
    {
        float4 pos     : SV_POSITION;
        float2 uv      : TEXCOORD0;
        float3 ambient : TEXCOORD1;
        float3 diffuse : TEXCOORD2;
        float3 color   : TEXCOORD3;
        SHADOW_COORDS(4)
    };

    v2f vert(appdata_full v, uint id : SV_InstanceID)
    {
        Particle p = _ParticleBuffer[id];

        float3 localPos = v.vertex.xyz * p.size.x + p.position;
        float3 worldPos = mul(_ModelMatrix, float4(localPos, 1.0));
        float3 worldNormal = v.normal;

        float ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
        float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
        float3 diffuse = (ndotl * _LightColor0.rgb);
        float3 color = p.color;

        v2f o;
        o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
        o.uv = v.texcoord;
        o.ambient = ambient;
        o.diffuse = diffuse;
        o.color = color;
        TRANSFER_SHADOW(o)
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        // sample the texture
        fixed shadow = SHADOW_ATTENUATION(i);
        fixed4 albedo = tex2D(_MainTex, i.uv);
        float3 lighting = i.diffuse * shadow + i.ambient;
        fixed4 col = fixed4(albedo * lighting * i.color, albedo.w);
        UNITY_APPLY_FOG(i.fogCoord, col)
        return col;
    }
    ENDCG

    SubShader
    {

        Tags { "LightMode"="ForwardBase" }
        LOD 100

        Pass
        {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
                #pragma target 5.0
            ENDCG
        }
    }
}
