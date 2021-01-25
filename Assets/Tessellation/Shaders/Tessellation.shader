Shader "Tessellation/Tessellation"
{
    Properties
    {
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "white" {}
        _DispTex("Displacement", 2D) = "black" {}

        _Color("Color", Color) = (1,1,1,1)
        _SpecColor("Spec color", color) = (0.5,0.5,0.5,0.5)
        _Specular("Specular", Range(0,1)) = 0.0
        _Glossiness("Glossiness", Range(0,1)) = 0.5

        _EdgeLength("Edge length", Range(2,50)) = 15
        _Displacement("Displacement", Range(0, 40.0)) = 0.3

        [Toggle] _Negative("Negative Displacement", Float) = 0
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma fragment frag
            #pragma hull hull_shader
            #pragma domain domain_shader
            #pragma shader_feature _NEGATIVE_ON

            #include "UnityCG.cginc"
            #include "Tessellation.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct InternalTessInterp_appdata {
                float4 vertex : INTERNALTESSPOS;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct TessllationFactors {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            struct v2f
            {
                UNITY_POSITION(pos);
                float2 uv_MainTex : TEXCOORD0;
                float4 tSpace0 : TEXCOORD1;
                float4 tSpace1 : TEXCOORD2;
                float4 tSpace2 : TEXCOORD3;
            };

            sampler2D _MainTex;
            sampler2D _NormalMap;
            sampler2D _DispTex;
            float4 _MainTex_ST;
            float4 _DispTex_ST;
            float _Displacement;
            float _EdgeLength;
            float _Specular;
            float _Glossiness;
            fixed4 _SpecColor;
            fixed4 _Color;

            InternalTessInterp_appdata vert(appdata v)
            {
                InternalTessInterp_appdata o;
                o.vertex = v.vertex;
                o.tangent = v.tangent;
                o.normal = v.normal;
                o.texcoord = v.texcoord;
                return o;
            }

            // tessellation constant shader
            TessllationFactors hull_const(InputPatch<InternalTessInterp_appdata, 3> v) {
                TessllationFactors o;
                float4 tf;

                tf = UnityEdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, _EdgeLength, _Displacement * 1.5f);

                o.edge[0] = tf.x;
                o.edge[1] = tf.y;
                o.edge[2] = tf.z;
                o.inside = tf.w;
                return o;
            }

            // tessellation hull shader
            [UNITY_domain("tri")]
            [UNITY_partitioning("fractional_odd")]
            [UNITY_outputtopology("triangle_cw")]
            [UNITY_patchconstantfunc("hull_const")]
            [UNITY_outputcontrolpoints(3)]
            InternalTessInterp_appdata hull_shader(InputPatch <InternalTessInterp_appdata, 3> v, uint id : SV_OutputControlPointID){
                return v[id];
            }

            // vertex shader
            v2f vert_to_frag_process(appdata v) {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv_MainTex.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
                o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
                o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
                o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
                return o;
            }

            void disp(inout appdata v) {
                float d = tex2Dlod(_DispTex, float4(v.texcoord, 0, 0)).r * _Displacement;
#ifdef _NEGATIVE_ON
                v.vertex.xyz += v.normal * d;
#else
                v.vertex.xyz -= v.normal * d;
#endif
            }

            // tessellation 
            [UNITY_domain("tri")]
            v2f domain_shader(TessllationFactors tessFactors, const OutputPatch<InternalTessInterp_appdata, 3> vi, float3 bary : SV_DomainLocation) {
                appdata v;
                UNITY_INITIALIZE_OUTPUT(appdata, v);
                v.vertex = vi[0].vertex * bary.x + vi[1].vertex * bary.y + vi[2].vertex * bary.z;
                v.tangent = vi[0].tangent * bary.x + vi[1].tangent * bary.y + vi[2].tangent * bary.z;
                v.normal = vi[0].normal * bary.x + vi[1].normal * bary.y + vi[2].normal * bary.z;
                v.texcoord = vi[0].texcoord * bary.x + vi[1].texcoord * bary.y + vi[2].texcoord * bary.z;

                disp(v);

                v2f o = vert_to_frag_process(v);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldPos = float3(i.tSpace0.w, i.tSpace1.w, i.tSpace2.w);
#ifndef USING_DIRECTIONAL_LIGHT
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
                fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

                fixed3 albedo = 0;
                half emission = 0;
                half specular = 0;
                fixed alpha = 0;
                fixed gloss = 0;
                fixed3 normal = fixed3(0, 0, 1);

                fixed4 col = tex2D(_MainTex, i.uv_MainTex) * _Color;
                albedo = col.rgb;
                specular = _Specular;
                gloss = _Glossiness;
                normal = UnpackNormal(tex2D(_NormalMap, i.uv_MainTex));

                float3 worldN; 
                worldN.x = dot(i.tSpace0.xyz, normal);
                worldN.y = dot(i.tSpace1.xyz, normal);
                worldN.z = dot(i.tSpace2.xyz, normal);
                worldN = normalize(worldN);
                normal = worldN;

                half3 h = normalize(lightDir + worldViewDir);
                fixed diff = max(0, dot(normal, lightDir));
                float nh = max(0, dot(normal, h));
                float spec = pow(nh, specular * 128) * _Glossiness;

                fixed4 c;
                c.rgb = albedo * diff + _SpecColor.rgb * spec;
                c.a = col.a;

                return c;
            }
            ENDCG
        }
    }
}
