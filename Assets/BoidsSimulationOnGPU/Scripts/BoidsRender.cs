using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsSimulationOnGPU {
    [RequireComponent(typeof(GPUBoids))]
    public class BoidsRender : MonoBehaviour {
        public Vector3 ObjectScale = new Vector3(0.1f, 0.2f, 0.5f);

        public GPUBoids GPUBoids;
        public Mesh InstancedMesh;
        public Material InstancedRenderMaterial;

        uint[] args = new uint [5] { 0, 0, 0, 0, 0 };
        // index per instance
        // number of instances
        // start of index
        // base position
        // start of instance

        ComputeBuffer argsBuffer;

        void Start() {
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        void Update() {
            RenderInstancedMesh();
        }

        private void OnDestroy() {
            if (argsBuffer != null) {
                argsBuffer.Release();
                argsBuffer = null;
            }
        }

        void RenderInstancedMesh() {
            if (GPUBoids == null || InstancedRenderMaterial == null || !SystemInfo.supportsInstancing)
                return;

            uint numIndices = InstancedMesh != null ? InstancedMesh.GetIndexCount(0) : 0;
            uint maxOjbectNum = (uint)GPUBoids.GetMaxObjectNum();
            args[0] = numIndices;
            args[1] = maxOjbectNum;
            argsBuffer.SetData(args);

            var bound = new Bounds
            (
                GPUBoids.GetSimulationAreaCenter(), 
                GPUBoids.GetSimulationAreaSize()
            );
            InstancedRenderMaterial.SetVector("_ObjectScale", ObjectScale);
            InstancedRenderMaterial.SetBuffer("_BoidDataBuffer", GPUBoids.GetBoidDataBuffer());

            Graphics.DrawMeshInstancedIndirect(
                InstancedMesh, 
                0, 
                InstancedRenderMaterial,
                bound,
                argsBuffer
            );
        }

    }
}