using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoidsSimulationOnGPU;

namespace Goldfish {
    public class GoldfishRenderer : MonoBehaviour {
        public Vector3 ObjectScale = new Vector3(0.1f, 0.2f, 0.5f);

        public GPUBoids GPUBoids;
        public Mesh InstancedMesh;
        public Material[] materials;

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // index per instance
        // number of instances
        // start of index
        // base position
        // start of instance

        ComputeBuffer [] argsBuffers;

        void Start() {
            argsBuffers = new ComputeBuffer[3] {
                new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments),
                new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments),
                new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments)
            };
        }

        void Update() {
            RenderInstancedMesh();
        }

        private void OnDestroy() {
            foreach (var buffer in argsBuffers) {
                if (buffer != null) {
                    buffer.Release();
                }
            }
            argsBuffers = null;
        }
        
        void RenderInstancedMesh() {
            if (GPUBoids == null)
                return;
            //var vertices = InstancedMesh.vertices;

            //float minX=Mathf.Infinity, maxX = Mathf.NegativeInfinity;
            //float minY=Mathf.Infinity, maxY = Mathf.NegativeInfinity;
            //float minZ=Mathf.Infinity, maxZ = Mathf.NegativeInfinity;

            //for (var i = 0; i < vertices.Length; i++) {
            //    minX = Mathf.Min(minX, vertices[i].x);
            //    minY = Mathf.Min(minY, vertices[i].y);
            //    minZ = Mathf.Min(minZ, vertices[i].z);

            //    maxX = Mathf.Max(maxX, vertices[i].x);
            //    maxY = Mathf.Max(maxY, vertices[i].y);
            //    maxZ = Mathf.Max(maxZ, vertices[i].z);
            //}


            //return;
            for(var i = 0; i < InstancedMesh.subMeshCount; i++){

                uint[] args = new uint[5]
                {
                     InstancedMesh.GetIndexCount(i), // index count per instance
                     (uint)GPUBoids.GetMaxObjectNum(),
                     InstancedMesh.GetIndexStart(i),   // start index location
                     InstancedMesh.GetBaseVertex(i), // base vertex location
                     0  // start instance location ???
                };

                argsBuffers[i].SetData(args);

                var bound = new Bounds
                (
                    GPUBoids.GetSimulationAreaCenter(),
                    GPUBoids.GetSimulationAreaSize()
                );

                materials[i].SetVector("_ObjectScale", ObjectScale);
                materials[i].SetBuffer("_BoidDataBuffer", GPUBoids.GetBoidDataBuffer());

                Graphics.DrawMeshInstancedIndirect(
                    InstancedMesh,
                    i,
                    materials[i],
                    bound,
                    argsBuffers[i]
                );
            }
        }
    }
}