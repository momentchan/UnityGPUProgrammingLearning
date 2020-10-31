Shader "Voxelizer/GPUVoxelParticleSystem"
{
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _Scale("Scale", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow
        #pragma instancing_options procedural:setup

        #include "UnityCG.cginc"
        #include "Utils/Quaternion.cginc"
        #include "Utils/Matrix.cginc"
        #include "Structure/VoxelParticle.cginc"

        struct Input {
            float4 color;
        };

        struct BoidData {
            float3 velocity;
            float3 position;
        };

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<VoxelParticle> _ParticleBuffer;
#endif

        half _Glossiness;
        half _Metallic;
        float4 _Color;
        float4x4 _WorldToLocal, _LocalToWorld;
        float _Scale;

        void setup() {
            unity_ObjectToWorld = _LocalToWorld;
            unity_WorldToObject = _WorldToLocal;
        }

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.color = _Color;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

            uint id = unity_InstanceID;
            VoxelParticle particle = _ParticleBuffer[id];
            float4x4 m = compose(particle.position.xyz, particle.rotation.xyzw, particle.scale.xyz * _Scale);
            v.vertex.xyz = mul(m, v.vertex.xyzw).xyz;
            v.normal.xyz = normalize(mul(m, v.normal));
#endif
        }

        void surf(Input IN, inout SurfaceOutputStandard o) {
            o.Albedo = IN.color;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }

        ENDCG
    }
    FallBack "Diffuse"
}

