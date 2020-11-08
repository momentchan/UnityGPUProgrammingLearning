Shader "Hidden/ScreenSpaceFluidRendering/RenderGBuffer"
{
    CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata {
        float4 vertex : POSItION;
    };
    struct v2f {
        float4 vertex    : SV_POSITION;
        float4 screenPos : TEXCOORD;
    };

    struct gbufferOut {
        half4 diffuse  : SV_Target0;
        half4 specular : SV_Target1;
        half4 normal   : SV_Target2;
        half4 emission : SV_Target3;
        float depth    : SV_Depth;
    };

    sampler2D _ColorBuffer;
    sampler2D _DepthBuffer;
    sampler2D _NormalBuffer;

    fixed4 _Diffuse;
    fixed4 _Specular;
    float4 _Emission;

    v2f vert(appdata v) {
        v2f o = (v2f)0;
        o.vertex = o.screenPos = v.vertex;
        o.screenPos.y *= _ProjectionParams.x;
        return o;
    }

    void frag(v2f i, out gbufferOut o)
    {
        float2 uv = i.screenPos.xy * 0.5 + 0.5f;

        float  d = tex2D(_DepthBuffer, uv).r;
        float3 n = tex2D(_NormalBuffer, uv).xyz;
        float4 c = tex2D(_ColorBuffer, uv);

#if UNITY_REVERSED_Z
        if (Linear01Depth(d) > 1.0 - 1e-3)
            discard;
#else
        if (Linear01Depth(d) < -1e-3)
            discard;
#endif
        o.diffuse = _Diffuse;
        o.specular = _Specular;
        o.normal = float4(n, 1.0);
        o.emission = _Emission;
#ifndef UNITY_HRD_ON
        o.emission = exp2(-o.emission);
#endif
        o.depth = d;
    }

    ENDCG

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "DisableBatching" = "True" "Queue" = "Geometry+10" }
        Cull Off 
        ZWrite On

        Pass
        {
            Tags{ "LightMode" = "Deffered" }

            Stencil
            {
                Comp Always
                Pass Replace
                Ref 255
            }

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile ___ UNITY_HDR_ON
            ENDCG
        }
    }
}
