Shader "Unlit/RainPostEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Size("Size", float) = 1
        _Distortion("Distortion", Range(-1, 1)) = 1
        _Blur("Blur", Range(0, 1)) = 0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" "RenderQueue" = "Transparent" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #define S(a, b, t) smoothstep(a, b, t)

                #include "UnityCG.cginc"
                #include "../../../Common/Libs/Random.cginc"
                #include "../../../Common/Libs/ConstantUtil.cginc"

                struct v2f {
                    float2 uv : TEXCOORD0;
                    float4 grabUv : TEXCOORD1;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float _Size;
                float _Distortion;
                float _Blur;

                float3 Layer(float2 UV, float t) {
                    float2 aspect = float2(2, 1);
                    float2 uv = UV * _Size * aspect;
                    uv.y += t * .25;
                    float2 gv = frac(uv) - 0.5;
                    float2 id = floor(uv);

                    float n = nrand(id);
                    t += n * PI2;
                    float w = UV.y * 10;
                    float x = (n - .5) * .8; // (-0.4, 0.4)
                    x += (.4 - abs(x)) * sin(3 * w) * pow(sin(w), 6) * .45;
                    float y = -sin(t + sin(t + sin(t) * .5)) * .45;
                    y -= (gv.x - x) * (gv.x - x);

                    float2 dropPos = (gv - float2(x, y)) / aspect;
                    float drop = S(.05, .03, length(dropPos));

                    float2 trailPos = (gv - float2(x, t * .25)) / aspect;
                    trailPos.y = (frac(trailPos.y * 8) - .5) / 8;
                    float trail = S(.03, .01, length(trailPos));
                    float fogTrail = S(-0.05, 0.05, dropPos.y);
                    fogTrail *= S(0.5, y, gv.y);
                    trail *= fogTrail;
                    fogTrail *= S(0.05, 0.04, abs(dropPos.x));

                    float2 offs = drop * dropPos + trail * trailPos;
                    return float3(offs, fogTrail);
                }

                v2f vert(appdata_full v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.grabUv = UNITY_PROJ_COORD(ComputeGrabScreenPos(o.vertex));
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float t = fmod(_Time.y, 7200);
                    fixed4 col = 0;

                    float3 drops = Layer(i.uv, t);

                    drops += Layer(i.uv * 1.23 + 6.5, t);
                    drops += Layer(i.uv * 1.56 + 1.5, t);

                    float fade = 1 - saturate(fwidth(i.uv) * 50);
                    float blur = _Blur * (1 - drops.z * fade) * 0.1;

                    col = tex2D(_MainTex, i.uv + drops.xy * _Distortion * fade);
                    return col;
                }
                ENDCG
            }
        }
}
