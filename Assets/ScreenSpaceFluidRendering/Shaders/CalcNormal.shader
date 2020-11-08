Shader "Hidden/ScreenSpaceFluidRendering/CalcNormal"
{
    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _DepthBuffer;
    float4 _DepthBuffer_TexelSize;
    float4x4 _ViewMatrix;


    float3 uvToView(float2 uv, float z) {
        float2 xyPos = uv * 2.0 - 1.0;
        float4 clipPos = float4(xyPos, z, 1.0);
        float4 viewPos = mul(unity_CameraInvProjection, clipPos);
        viewPos.xyz = viewPos.xyz / viewPos.w;
        return viewPos.xyz;
    }

    float sampleDepth(float2 uv) {
#if UNITY_REVERSED_Z
        return 1.0 - tex2D(_DepthBuffer, uv).r;
#else
        return tex2D(_DepthBuffer, uv).r;
#endif
    }

    float3 getViewPos(float2 uv) {
        return uvToView(uv, sampleDepth(uv));
    }

    fixed4 frag(v2f_img i) : SV_Target
    {
        float2 uv = i.uv;
        float depth = tex2D(_DepthBuffer, i.uv);

#if UNITY_REVERSED_Z
        if (Linear01Depth(depth) > 1.0 - 1e-3)
            discard;
#else
        if (Linear01Depth(depth) < - 1e-3)
            discard;
#endif
        float2 ts = _DepthBuffer_TexelSize;
        float3 posView = getViewPos(uv);

        // derivative x
        float3 ddx = getViewPos(uv + float2(ts.x, 0)) - posView;
        float3 ddx2 = posView - getViewPos(uv - float2(ts.x, 0));
        ddx = abs(ddx.z) < abs(ddx2.z) ? ddx : ddx2;

        // derivative y
        float3 ddy = getViewPos(uv + float2(0, ts.x)) - posView;
        float3 ddy2 = posView - getViewPos(uv - float2(0, ts.x));
        ddy = abs(ddy.z) < abs(ddy2.z) ? ddy : ddy2;

        // Compute normal
        float3 N = normalize(cross(ddx, ddy));
        float4x4 vm = _ViewMatrix;
        N = normalize(mul(vm, float4(N, 0.0)));

        // (-1.0~1.0) -> (0~1.0)
        float4 col = float4(N * 0.5f + 0.5f, 1.0);

        return col;
    }

    ENDCG

    SubShader
    {
        Cull Off ZTest Always ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}
