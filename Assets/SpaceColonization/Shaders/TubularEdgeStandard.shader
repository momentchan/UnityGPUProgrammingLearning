Shader "SpaceColonization/TubularEdge"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "white"{}
        _Thickness("Thickness", Range(0.01, 0.1)) = 0.1
        _Color("Color", Color) = (1, 1, 1, 1)
        [HDR] _Emission("Emission", Color) = (1, 1, 1, 1)

        [Space]
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0, 1)) = 0
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        CGINCLUDE

        #include "Includes/Edge.cginc"
        #include "Includes/Node.cginc"
        #include "UnityCG.cginc"
        #include "UnityLightingCommon.cginc"
        #include "UnityGBuffer.cginc"
        #include "UnityStandardUtils.cginc"
        #include "../../Common/Libs/Random.cginc"
        #include "../../Common/Libs/PhotoshopMath.cginc"


        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
            uint vid : SV_VertexID;
        };

        struct v2g
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;
            float2 uv2 : TEXCOORD1;
            float3 viewDir : TEXCOORD2;
            float alpha : COLOR;
            float emission : TEXCOORD3;
            uint seed : TEXCOORD4;
        };

        struct g2f {
            float4 position : SV_POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
            half3 ambient : TEXCOORD2;
            float3 wpos : TEXCOORD3;
            float emission : TEXCOORD4;
            uint seed : TEXCOORD5;
        };

        StructuredBuffer<Node> _Nodes;
        StructuredBuffer<Edge> _Edges;
        sampler2D _NoiseTex;
        uint _EdgesCount;

        float4x4 _Local2World;
        float4 _Color;
        float _Metallic;
        float _Glossiness;
        float4 _Emission;
        half _Thickness;

        v2g vert(appdata v, uint iid : SV_InstanceID)
        {
            Edge e = _Edges[iid];
            Node na = _Nodes[e.a];
            Node nb = _Nodes[e.b];

            float3 ap = na.position;
            float3 bp = nb.position;
            float3 dir = bp - ap;
            bp = ap + dir * nb.t;
            float3 localPos = lerp(ap, bp, v.vid);
            float3 worldPos = mul(_Local2World, float4(localPos, 1));

            v2g o;
            o.position = float4(worldPos, 1);
            o.viewDir = WorldSpaceViewDir(float4(worldPos, 1));
            o.uv = v.uv;
            o.uv2 = float2(lerp(na.offset, nb.offset, v.vid), 0);
            o.alpha = (na.active && nb.active) && (iid < _EdgesCount);
            o.emission = smoothstep(1, 0, pow(lerp(na.t, nb.t, v.vid),2));
            o.seed = iid;
            return o;
        }


        g2f Create(float3 wpos, float3 wnorm, float2 uv, float emission, uint seed) {
            g2f o;
            o.wpos = wpos;
            o.position = UnityWorldToClipPos(wpos);
            o.uv = uv;
            o.normal = wnorm;
            o.emission = emission;
            o.ambient = ShadeSHPerVertex(wnorm, 0);
            o.seed = seed;
            return o;
        }


        [maxvertexcount(60)]
        void geom(line v2g IN[2], inout TriangleStream<g2f> outStream) {
            v2g p0 = IN[0];
            v2g p1 = IN[1];
            
            uint seed = IN[0].seed;
            float alpha = p0.alpha;
            float thickness = alpha * _Thickness;
            float3 tp = lerp(p0.position, p1.position, alpha);

            float3 t = normalize(p1.position - p0.position);
            float3 n = normalize(p0.viewDir);
            float3 bn = cross(t, n);
            n = cross(t, bn);

            static const uint rows = 6, cols = 6;
            static const float rows_inv = 1.0 / rows, cols_inv = 1.0 / (cols - 1);


            for (uint col = 0; col < cols; col++) {
                float r_col = (col * cols_inv) * UNITY_TWO_PI;

                float s, c;
                sincos(r_col, s, c);

                float3 normal = normalize(n * c + bn * s);

                float3 w0 = p0.position + normal * thickness;
                float3 w1 = p1.position + normal * thickness;

                g2f o0 = Create(w0, normal, p0.uv2,  p1.emission, seed);
                outStream.Append(o0);

                g2f o1 = Create(w1, normal, p1.uv2, p1.emission, seed);
                outStream.Append(o1);
            }
            outStream.RestartStrip();

            // half circle
            for (uint row = 0; row < rows; row++) {
                float r_row_0 = (row * rows_inv) * UNITY_HALF_PI;
                float r_row_1 = ((row + 1) * rows_inv) * UNITY_HALF_PI;

                for (uint col = 0; col < cols; col++) {
                    float r_col = (col * cols_inv) * UNITY_TWO_PI;

                    float s, c;
                    sincos(r_col, s, c);

                    float3 n0 = normalize(n * c * (1 - sin(r_row_0)) + bn * s * (1 - sin(r_row_0)) + t * sin(r_row_0));
                    float3 n1 = normalize(n * c * (1 - sin(r_row_1)) + bn * s * (1 - sin(r_row_1)) + t * sin(r_row_1));

                    float3 w0 = tp + n0 * thickness;
                    float3 w1 = tp + n1 * thickness;


                    g2f o0 = Create(w0, n0, p0.uv2, p1.emission, seed);
                    outStream.Append(o0);

                    g2f o1 = Create(w1, n1, p1.uv2, p1.emission, seed);
                    outStream.Append(o1);
                }
                outStream.RestartStrip();
            }
        }


        void frag(g2f IN,
            out half4 outGBuffer0 : SV_Target0,
            out half4 outGBuffer1 : SV_Target1,
            out half4 outGBuffer2 : SV_Target2,
            out half4 outEmission : SV_Target3)
        {
            half3 albedo = _Color.rgb * tex2D(_NoiseTex, nrand3(IN.wpos));

            half3 c_diff, c_spec;
            half refl10;
            c_diff = DiffuseAndSpecularFromMetallic(
                albedo, _Metallic, // input
                c_spec, refl10 // output
            );

            UnityStandardData data;
            data.diffuseColor = c_diff;
            data.occlusion = 1.0;
            data.specularColor = c_spec;
            data.smoothness = _Glossiness;
            data.normalWorld = IN.normal;
            UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

            half3 sh = ShadeSHPerPixel(data.normalWorld, IN.ambient, IN.wpos);
            float3 emission = _Emission * IN.emission + half4(sh * c_diff, 1);
            emission = HSLShift(emission, float3(nrand(IN.seed.xx),0,0));
            outEmission = float4(emission,1);
        }

        ENDCG

        Pass
        {
            Tags{ "LightMode" = "Deferred" }

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_shadowcaster noshadowmask nodynlightmap nodirlightmap nolightmap
            ENDCG
        }
    }
}
