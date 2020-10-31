Shader "GPUMarchingCubes/MarchingCubes"
{
    Properties
    {
        _SegmentNum("SegmentNum", int) = 32

        _Scale("Scale", float) = 1
        _Threshold("Threashold", float) = 0.5

        _DiffuseColor("Diffuse", Color) = (0,0,0,1)

        _EmissionIntensity("Emission Intensity", Range(0,1)) = 1
        _EmissionColor("Emission", Color) = (0,0,0,1)

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGINCLUDE
        #define UNITY_PASS_DEFERRED
        #include "HLSLSupport.cginc"
        #include "UnityShaderVariables.cginc"
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "UnityPBSLighting.cginc"

        #include "Libs/Primitives.cginc"
        #include "Libs/Utils.cginc"

        struct appdata {
            float4 vertex : POSITION;
        };

        struct v2g {
            float4 pos : SV_POSITION;
        };

        struct g2f_light {
            float4 pos      : SV_POSITION;
            float3 normal   : NORMAL;
            float4 worldPos : TEXCOORD0;
            half3 sh        : TEXCOORD3;				
        };

        struct g2f_shadow {
            float4 pos			: SV_POSITION;	
            float4 hpos			: TEXCOORD1;
        };

        int _SegmentNum;

        float _Scale;
        float _Threshold;

        float4 _DiffuseColor;
        float3 _HalfSize;
        float4x4 _Matrix;

        float _EmissionIntensity;
        half3 _EmissionColor;

        half _Glossiness;
        half _Metallic;

        StructuredBuffer<float3> vertexOffset;
        StructuredBuffer<int> cubeEdgeFlags;
        StructuredBuffer<int2> edgeConnection;
        StructuredBuffer<float3> edgeDirection;
        StructuredBuffer<int> triangleConnectionTable;

        float DistanceFuncKaiware(float3 pos, float scale)
        {
            float3 p = pos;

            // スケール
            p = p / scale;

            // 頭部
            float d1 = roundBox(p, float3(1, 0.8, 1), 0.1);

            // くちばし
            float d2_0 = roundBox(p - float3(0, -0.2, 0.7), float3(0.8, 0.25, 0.3), 0.1);
            float d2_1 = box(p - float3(0, -0.0, 0.7), float3(1.1, 0.35, 1.1));	// 上半分
            float d2_2 = box(p - float3(0, -0.4, 0.7), float3(1.1, 0.35, 1.1));	// 下半分
            float d2_3 = roundBox(p - float3(0, -0.2, 0.7), float3(0.75, 0.1, 0.25), 0.1);	// 溝

            float d2_top = max(d2_0, d2_1);
            float d2_bottom = max(d2_0, d2_2);
            float d2 = min(min(d2_top, d2_bottom), d2_3);

            // はっぱの茎
            float d3_0 = Capsule(p, float3(0, 0.5, 0), float3(0, 0.75, 0), 0.05);
            // 葉っぱ
            float d3_1 = ellipsoid(p - float3(0.2, 0.75, 0), float3(0.25, 0.025, 0.1));
            float d3_2 = ellipsoid(p - float3(-0.2, 0.75, 0), float3(0.25, 0.025, 0.1));
            float d3 = min(d3_0, min(d3_1, d3_2));

            // 目
            float d4_0 = Capsule(p, float3(0.2, 0.25, 0.6), float3(0.4, 0.2, 0.6), 0.03);
            float d4_1 = Capsule(p, float3(-0.2, 0.25, 0.6), float3(-0.4, 0.2, 0.6), 0.03);
            float d4 = min(d4_0, d4_1);

            // 合成
            float sum = max(min(min(d1, d2), d3), -d4);

            sum *= scale;

            return sum;
        }
        float sdCone(float3 p, float2 c, float h)
        {
            // c is the sin/cos of the angle, h is height
            // Alternatively pass q instead of (c,h),
            // which is the point at the base in 2D
            float2 q = h * float2(c.x / c.y, -1.0);

            float2 w = float2(length(p.xz), p.y);
            float2 a = w - q * clamp(dot(w, q) / dot(q, q), 0.0, 1.0);
            float2 b = w - q * float2(clamp(w.x / q.x, 0.0, 1.0), 1.0);
            float k = sign(q.y);
            float d = min(dot(a, a), dot(b, b));
            float s = max(k * (w.x * q.y - w.y * q.x), k * (w.y - q.y));
            return sqrt(d) * sign(s);
        }

        float Sample(float x, float y, float z) {

            // out of bound
            if ((x <= 1) || (y <= 1) || (z <= 1) || (x >= _SegmentNum - 1) || (y >= _SegmentNum - 1) || (z >= _SegmentNum - 1))
                return 0;

            float3 size = float3(_SegmentNum, _SegmentNum, _SegmentNum);

            float3 pos = float3(x, y, z) / size;

            float result = 0;

#if 1
            // three sphere
            for (int i = 0; i < 3; i++) {
                float3 center = float3(0.5, 0.25 + 0.25 * i, 0.5);
                float r = 0.005 + (sin(_Time.y * 8.0 + i * 23.365) * 0.5 + 0.5) * 0.125;
                float sp = -sphere(pos - center, r) + 0.5; // sphere will return negative if point is inside sphere
                result = smoothMax(result, sp, 14); // blending three sphere
            }
#else
            result = -DistanceFuncKaiware(twistY(pos - float3(0.5, 0.5, 0.5), _SinTime.z * 10.0), 0.5) + 0.5;
#endif

            return result;
        }


        float getOffset(float v1, float v2, float desired) {
            float delta = v2 - v1;
            if (delta == 0.0)
                return 0.5f;
            return (desired - v1) / delta;
        }

        float3 getNormal(float fX, float fY, float fZ) {
            float3 normal;
            float offset = 1; // neighborhood point

            normal.x = Sample(fX - offset, fY, fZ) - Sample(fX + offset, fY, fZ);
            normal.y = Sample(fX, fY - offset, fZ) - Sample(fX, fY + offset, fZ);
            normal.z = Sample(fX, fY, fZ - offset) - Sample(fX, fY, fZ + offset);

            return normal;
        }

        v2g vert (appdata v)
        {
            v2g o = (v2g)0;
            o.pos = v.vertex;
            return o;
        }
        
        [maxvertexcount(15)]
        void geom_light(point v2g input[1], inout TriangleStream<g2f_light> stream) {

            g2f_light o = (g2f_light)0;
            int i, j;
            float cubeValues[8];

            float3 edgeVertices[12] = {
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0)
            };
            float3 edgeNormals[12] = {
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0)
            };

            float3 pos = input[0].pos.xyz;
            float3 defpos = pos;

            for (i = 0; i < 8; i++) {
                cubeValues[i] = Sample(
                    pos.x + vertexOffset[i].x,
                    pos.y + vertexOffset[i].y,
                    pos.z + vertexOffset[i].z
                );
            }

            pos *= _Scale;
            pos -= _HalfSize;

            // Store contain info
            int flagIndex = 0; 
            for (i = 0; i < 8; i++) {
                if (cubeValues[i] <= _Threshold) {
                    flagIndex |= (1 << i);
                }
            }

            int edgeFlags = cubeEdgeFlags[flagIndex];
            if (edgeFlags == 0 || edgeFlags == 255)
                return;

            // Create vertices and normals
            float offset = 0.5f;
            float3 vertex; 
            for (i = 0; i < 12; i++) {
                if ((edgeFlags & (1 << i)) != 0) {
                    offset = getOffset(cubeValues[edgeConnection[i].x], cubeValues[edgeConnection[i].y], _Threshold);

                    vertex = vertexOffset[edgeConnection[i].x] + offset * edgeDirection[i];
                    edgeVertices[i] = pos + vertex * _Scale;
                    edgeNormals[i] = getNormal(defpos.x + vertex.x, defpos.y + vertex.y, defpos.z + vertex.z);
                }
            }

            int vindex = 0;
            int findex = 0;
            for (i = 0; i < 5; i++) {
                findex = flagIndex * 16 + 3 * i;
                if (triangleConnectionTable[findex] < 0)
                    return;

                // Creat triangles
                for (j = 0; j < 3; j++) {
                    vindex = triangleConnectionTable[findex + j];
                    float4 ppos = mul(_Matrix, float4(edgeVertices[vindex], 1));
                    o.pos = UnityObjectToClipPos(ppos);

                    float3 norm = UnityObjectToWorldNormal(normalize(edgeNormals[vindex]));
                    o.normal = normalize(mul(_Matrix, float4(norm, 0)));
                    stream.Append(o);
                }
                stream.RestartStrip();
            }
        }


        void frag_light(g2f_light IN,
            out half4 outDiffuse		: SV_Target0,
            out half4 outSpecSmoothness : SV_Target1,
            out half4 outNormal : SV_Target2,
            out half4 outEmission : SV_Target3)
        {
            fixed3 normal = IN.normal;

            float3 worldPos = IN.worldPos;

            fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

#ifdef UNITY_COMPILER_HLSL
            SurfaceOutputStandard o = (SurfaceOutputStandard)0;
#else
            SurfaceOutputStandard o;
#endif
            o.Albedo = _DiffuseColor.rgb;
            o.Emission = _EmissionColor * _EmissionIntensity;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
            o.Occlusion = 1.0;
            o.Normal = normal;

            // Setup lighting environment
            UnityGI gi;
            UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
            //gi.indirect.diffuse = 0;
            //gi.indirect.specular = 0;
            //gi.light.color = 0;
            //gi.light.dir = half3(0, 1, 0);
            //gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);

            // Call GI (lightmaps/SH/reflections) lighting function
            UnityGIInput giInput;
            UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
            giInput.light = gi.light;
            giInput.worldPos = worldPos;
            giInput.worldViewDir = worldViewDir;
            giInput.atten = 1.0;

            giInput.ambient = IN.sh;

            giInput.probeHDR[0] = unity_SpecCube0_HDR;
            giInput.probeHDR[1] = unity_SpecCube1_HDR;

#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
            giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif

#if UNITY_SPECCUBE_BOX_PROJECTION
            giInput.boxMax[0] = unity_SpecCube0_BoxMax;
            giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
            giInput.boxMax[1] = unity_SpecCube1_BoxMax;
            giInput.boxMin[1] = unity_SpecCube1_BoxMin;
            giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif

            LightingStandard_GI(o, giInput, gi);

            // call lighting function to output g-buffer
            outEmission = LightingStandard_Deferred(o, worldViewDir, gi, outDiffuse, outSpecSmoothness, outNormal);
            outDiffuse.a = 1.0;

#ifndef UNITY_HDR_ON
            outEmission.rgb = exp2(-outEmission.rgb);
#endif
        }


        [maxvertexcount(15)]
        void geom_shadow(point v2g input[1], inout TriangleStream<g2f_shadow> stream) {

            g2f_shadow o = (g2f_shadow)0;
            int i, j;
            float cubeValues[8];

            float3 edgeVertices[12] = {
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0)
            };
            float3 edgeNormals[12] = {
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0),
                float3(0,0,0)
            };

            float3 pos = input[0].pos.xyz;
            float3 defpos = pos;

            for (i = 0; i < 8; i++) {
                cubeValues[i] = Sample(
                    pos.x + vertexOffset[i].x,
                    pos.y + vertexOffset[i].y,
                    pos.z + vertexOffset[i].z
                );
            }

            pos *= _Scale;
            pos -= _HalfSize;

            // Store contain info
            int flagIndex = 0;
            for (i = 0; i < 8; i++) {
                if (cubeValues[i] <= _Threshold) {
                    flagIndex |= (1 << i);
                }
            }

            int edgeFlags = cubeEdgeFlags[flagIndex];
            if (edgeFlags == 0 || edgeFlags == 255)
                return;

            // Create vertices and normals
            float offset = 0.5f;
            float3 vertex;
            for (i = 0; i < 12; i++) {
                if ((edgeFlags & (1 << i)) != 0) {
                    offset = getOffset(cubeValues[edgeConnection[i].x], cubeValues[edgeConnection[i].y], _Threshold);

                    vertex = vertexOffset[edgeConnection[i].x] + offset * edgeDirection[i];
                    edgeVertices[i] = pos + vertex * _Scale;
                    edgeNormals[i] = getNormal(defpos.x + vertex.x, defpos.y + vertex.y, defpos.z + vertex.z);
                }
            }

            int vindex = 0;
            int findex = 0;
            for (i = 0; i < 5; i++) {
                findex = flagIndex * 16 + 3 * i;
                if (triangleConnectionTable[findex] < 0)
                    return;

                // Creat triangles
                for (j = 0; j < 3; j++) {
                    vindex = triangleConnectionTable[findex + j];
                    float4 ppos = mul(_Matrix, float4(edgeVertices[vindex], 1));

                    float3 norm = UnityObjectToWorldNormal(normalize(edgeNormals[vindex]));

                    float4 lpos = mul(unity_WorldToObject, ppos);
                    o.pos = UnityClipSpaceShadowCasterPos(lpos, normalize(mul(_Matrix, float4(norm, 0))));
                    o.pos = UnityApplyLinearShadowBias(o.pos);
                    o.hpos = o.pos;
                    stream.Append(o);
                }
                stream.RestartStrip();
            }
        }

        fixed4 frag_shadow(g2f_shadow i) : SV_Target {
            return i.hpos.z / i.hpos.w;
        }
        ENDCG

        Pass
        {
            Tags{ "LightMode" = "Deferred" }

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom_light
            #pragma fragment frag_light
            #pragma exclude_renderers nomrt
            #pragma multi_compile_prepassfinal noshadow
            ENDCG
        }

        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster
            ENDCG
        }
    }
    Fallback "Diffuse"
}
