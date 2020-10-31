using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryWireframe {
    public abstract class WireframeBase : MonoBehaviour {
        [SerializeField] protected Material material;

        protected abstract void OnRenderObject();
        public static class CSPARAM {

            // Basic
            public const string VERTEX_NUM = "_VertexNum";
            public const string LEVEL = "_Level";
            public const string LOCAL_TO_WOLRD_MATRIX = "_LocalToWorldMatrix";

            // Instancing
            public const string UPDATE = "Update";
            public const string TIME = "_Time";
            public const string NOISE_SCALE = "_NoiseScale";
            public const string NOISE_SPEED = "_NoiseSpeed";
            public const string SUB_DIVISION_NUM = "_SubDivisionNum";
            public const string PARTICLE_BUFFER = "_ParticleBuffer";
        }
    }
}