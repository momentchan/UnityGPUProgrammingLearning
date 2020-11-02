Shader "Unlit/SSRMainCamera"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    float4x4 _ViewProj;
    float4x4 _InvViewProj;

    sampler2D _MainTex;

    sampler2D _CameraDepthTexture;
    sampler2D _SubCameraDepthTex;
    sampler2D _SubCameraMainTex;

    sampler2D _CameraGBufferTexture1; // rgb: specular, a: smoothness
    sampler2D _CameraGBufferTexture2; // rgb: normal,   a: unused


    fixed4 frag_reflection(v2f_img i) : SV_Target
    {
        float2 uv = i.uv;
        float2 uvCenter = 2.0 * uv - 1.0; // (-1 ~ 1)
        float smoothness = tex2D(_CameraGBufferTexture1, uv).w;

        float4 originCol = tex2D(_MainTex, uv);
        float4 refCol = originCol;
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
        if (depth <= 0) return originCol;

        float4 worldPos = mul(_InvViewProj, float4(uvCenter, depth, 1.0));
        worldPos /= worldPos.w;

        float3 toCam = normalize(worldPos - _WorldSpaceCameraPos);
        float3 normal = tex2D(_CameraGBufferTexture2, uv).rgb * 2.0 - 1.0;


        // Ray Tracing
        float3 ray = worldPos;
        int maxLoop = 100;
        float rayLenCoef = 0.05f;
        float thickness = 0.003f;
        float reflectionRate = 0.5f;
        float3 step = normal * rayLenCoef;

        [loop]
        for (int n = 1; n <= maxLoop; n++) {
            ray += step;
            float4 rayScreen = mul(_ViewProj, float4(ray, 1.0));        //[-1,1]
            float2 rayUV = rayScreen.xy / rayScreen.w * 0.5f + 0.5f;    //[0 ,1]
            float rayDepth = rayScreen.z / rayScreen.w;
            float subCameraDepth = SAMPLE_DEPTH_TEXTURE(_SubCameraDepthTex, rayUV);

            if (rayDepth < subCameraDepth && rayDepth + thickness > subCameraDepth) {
                float sign = -1.0;
                for (int m = 1; m <= 8; ++m) {
                    ray += sign * pow(0.5, m) * step;
                    rayScreen = mul(_ViewProj, float4(ray, 1.0));
                    rayUV = rayScreen.xy / rayScreen.w * 0.5f + 0.5f;
                    rayDepth = rayScreen.z / rayScreen.w;
                    subCameraDepth = SAMPLE_DEPTH_TEXTURE(_SubCameraDepthTex, rayUV);
                    sign = (rayDepth < subCameraDepth) ? -1 : 1;
                }
                refCol = tex2D(_SubCameraMainTex, rayUV);
            }
        }

        return originCol * (1 - smoothness) + refCol * smoothness;
    }
    
    ENDCG


    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_reflection
            ENDCG
        }
    }
}
