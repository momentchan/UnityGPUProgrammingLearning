Shader "Unlit/Rain"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size("Size", float) = 1
        _Distortion("Distortion", Range(-1, 1)) = 1
        _Blur("Blur", Range(0, 1)) = 0

            _TimeScale("Time scale", Float) = 1
        _FracScale("Fractal Scale", Vector) = (1, 1, 1, 1)
        _Velocity("Velocity XY", Vector) = (-0.2, -0.2, 1, 1)
        _MaxStrength("Max Sstrength", Float) = 1
        _Threshold("Threshold", Float) = 1


        _RotateAngle("Rotate Angle", float) = 0
        _RotatePower("Rotate Power", float) =0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderQueue" = "Transparent" }
        LOD 100

        GrabPass{"_GrabTexture"}
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
            sampler2D _GrabTexture;
            float4 _MainTex_ST;
            float _Size;
            float _Distortion;
            float _Blur;
            float3 _FracScale;
            float _TimeScale;
            float3 _Velocity;
            float _MaxStrength;
            float _Threshold;

            float _RotateAngle;
            float _RotatePower;
            

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
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }
            float noise(float3 x)
            {
                // The noise function returns a value in the range -1.0f -> 1.0f

                float3 p = floor(x);
                float3 f = frac(x);

                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;

                return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                    lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                    lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                        lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }
            float2 getRotationUV(float2 uv, float angle, float power) {
                float2 v = (float2)0;
                float rad = angle * 3.14159265359;
                v.x = uv.x + cos(rad) * power;
                v.y = uv.y + sin(rad) * power;
                return v;
            }
            float CalculateFractalFast(float2 uv)
            {
                float2 uvFractal = uv * _FracScale.xy;
                uvFractal.y *= 2;
                float t = _TimeScale * _Time.y;
                float3 p = (float3(uvFractal, 0) + t * float3(-1.0 * _Velocity.x, -1.0 * _Velocity.y, -1.0 * _Velocity.z));

                float3 q = p;
                float f;
                f = 0.50000 * noise(q); q = q * 2.02;
                f += 0.25000 * noise(q); q = q * 2.03;
                f += 0.12500 * noise(q); q = q * 2.01;
                f += 0.06250 * noise(q); q = q * 2.02;
                f += 0.03125 * noise(q);
                f = pow(saturate((f - _Threshold)) * _MaxStrength, _FracScale.z);
                return saturate(f);
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

                float fade = 1-saturate(fwidth(i.uv) * 50);
                float fractal = CalculateFractalFast(i.grabUv.xy / i.grabUv.w);
                float blur = _Blur * (1 - drops.z * fade * 0.2) *(1 + fractal * 5);

                float2 projUv = i.grabUv.xy / i.grabUv.w;
                projUv = getRotationUV(projUv, fractal * _RotateAngle, fractal * _RotatePower);
                projUv += drops.xy * _Distortion * fade;

                float a = nrand(i.uv) * PI2;
                const float numSamples = 32;
                for (float i = 0; i < numSamples; i++) {
                    float2 offs = float2(sin(a), cos(a)) * blur;
                    float d = frac(sin((i + 1) * 534.2) * 533);
                    d = sqrt(d);
                    col += tex2D(_GrabTexture, projUv + offs);
                    a++;
                }
                col /= numSamples * 0.9;

                return col;
            }
            ENDCG
        }
    }
}
