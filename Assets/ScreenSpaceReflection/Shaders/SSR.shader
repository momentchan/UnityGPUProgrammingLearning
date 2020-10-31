Shader "ScreenSpaceReflection/SSR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };
    struct v2f
    {
        float4 vertex : SV_POSITION;
        float4 screen : TEXCOORD0;
    };

    float rand(float2 co)
    {
        return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
    }
    float ComputeDepth(float4 clipPos) {
        #if defined(SHADER_TARGET_GLSL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
            return (clipPos.z / clipPos.w) * 0.5 + 0.5;
        #else
            return clipPos.z / clipPos.w;
        #endif
    }

    int _ViewMode;
    int _MaxLoop;
    int _MaxLOD;

    float _RayLenCoeff;
    float _Thickness;
    float _ReflectionRate;
    float _BaseRaise;

    float4x4 _ViewProj;
    float4x4 _InvViewProj;

    sampler2D _MainTex;
    float4 _MainTex_ST;

    sampler2D _CameraGBufferTexture0; // rgb: diffuse,  a: occulusion
    sampler2D _CameraGBufferTexture1; // rgb: specular, a: smoothness
    sampler2D _CameraGBufferTexture2; // rgb: normal,   a: unused
    sampler2D _CameraGBufferTexture3; // rgb: emission, a: unused
    sampler2D _CameraDepthTexture;
    sampler2D _CameraDepthMipmap;

    v2f vert(appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.screen = ComputeScreenPos(o.vertex);
        return o;
    }

    fixed4 frag_depth(v2f i) : SV_Target
    {
        return tex2D(_CameraDepthTexture, i.screen);
    }

    fixed4 frag_reflection(v2f i) : SV_Target
    {
        float2 uv = i.screen.xy / i.screen.w;
        float4 col = tex2D(_MainTex, uv);
        float4 refCol = col;
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
        float smoothness = tex2D(_CameraGBufferTexture1, uv).w;
        if (depth <= 0) return tex2D(_MainTex, uv);

        float2 screenPos = 2.0 * uv - 1.0; // (-1 ~ 1)
        float4 worldPos = mul(_InvViewProj, float4(screenPos, depth, 1.0));
        worldPos /= worldPos.w;

        float3 toCam = normalize(worldPos - _WorldSpaceCameraPos);
        float3 normal = tex2D(_CameraGBufferTexture2, uv).rgb * 2.0 - 1.0;
        float3 ref = reflect(toCam, normal);

        // Ray Tracing
        int lod = 0;
        int calc = 0;
        float3 ray = worldPos;

        [loop]
        for (int n = 1; n <= _MaxLoop; n++) {
            float3 step = ref * _RayLenCoeff * (lod + 1);
            ray += step * (1 + rand(uv + _Time.x) * (1 - smoothness));

            float4 rayScreen = mul(_ViewProj, float4(ray, 1.0));
            float2 rayUV = rayScreen.xy / rayScreen.w * 0.5f + 0.5f;
            float rayDepth = ComputeDepth(rayScreen);
            float worldDepth = (lod ==0) ? SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, rayUV) : tex2Dlod(_CameraDepthTexture, float4(rayUV, 0, lod)) + _BaseRaise * lod;

            if (max(abs(rayUV.x - 0.5), abs(rayUV.y - 0.5)) > 0.5) break;


            if (rayDepth < worldDepth) {

                if (lod == 0) {
                    if (rayDepth + _Thickness > worldDepth) {
                        float sign = -1.0;
                        for (int m = 1; m <= 8; ++m) {
                            ray += sign * pow(0.5, m) * step;
                            rayScreen = mul(_ViewProj, float4(ray, 1.0));
                            rayUV = rayScreen.xy / rayScreen.w * 0.5f + 0.5f;
                            rayDepth = ComputeDepth(rayScreen);
                            worldDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, rayUV);
                            sign = (rayDepth < worldDepth) ? -1 : 1;
                        }
                        refCol = tex2D(_MainTex, rayUV);
                    }
                    break;
                }
                else {
                    ray -= step;
                    lod--;
                }
            }
            else if (n <= _MaxLOD) {
                lod++;
            }

            calc = n;

            if (length(ray - worldPos) > 15.0) break;
        }

        if (_ViewMode == 1) return float4(normal, 1);
        if (_ViewMode == 2) return float4(ref, 1);
        if (_ViewMode == 3) return float4(1, 1, 1, 1) * calc / _MaxLoop;
        if (_ViewMode == 4) return float4(1, 1, 1, 1) * tex2Dlod(_CameraDepthMipmap, float4(uv, 0, _MaxLOD));
        if (_ViewMode == 5) return float4(tex2D(_CameraGBufferTexture0, uv));
        if (_ViewMode == 6) return float4(tex2D(_CameraGBufferTexture1, uv));
        if (_ViewMode == 7) return float4(1, 1, 1, 1) * tex2D(_CameraGBufferTexture0, uv).w;
        if (_ViewMode == 8) return float4(1, 1, 1, 1) * tex2D(_CameraGBufferTexture1, uv).w;

        return (col * (1 - smoothness) + refCol * smoothness) * _ReflectionRate + col * (1 - _ReflectionRate);
    }
    ENDCG

    SubShader
    {
        Blend Off ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_depth
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_reflection
            ENDCG
        }
    }
}
