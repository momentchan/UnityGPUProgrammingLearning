using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CellularGrowth {
    public class CellularGrowth : MonoBehaviour, ComputeShaderUser {

        [SerializeField] protected ComputeShader compute;
        [SerializeField] protected Material particleMat, edgeMat;
        [SerializeField] protected Gradient gradient;
        [SerializeField] protected int count = 8192;

        [SerializeField, Range(0.1f, 1.5f)] protected float size = 0.9f;
        [SerializeField, Range(0.01f, 1f)] protected float grow = 0.25f;

        [SerializeField] DividePattern pattern = DividePattern.Branch;
        [SerializeField, Range(1, 4)] protected int iterations = 2;
        [SerializeField, Range(0.02f, 3f)] protected float divideInterval = 0.5f;
        [SerializeField, Range(1, 128)] protected int maxDivideCount = 16;
        [SerializeField, Range(2, 6)] protected int maxLink = 4;

        [SerializeField, Range(0.1f, 1f)] protected float drag = 0.995f;
        [SerializeField, Range(0.1f, 5f)] protected float limit = 3f;

        [SerializeField, Range(1f, 5f)] protected float repulsion = 1f;
        [SerializeField, Range(1f, 10f)] protected float spring = 5f;

        private Mesh particleMesh, edgeMesh;
        private Texture2D pallete;
        private GPUObjectPingPongPool particlePool;
        private GPUObjectPool         edgePool;
        private GPUPool               dividablePool;
        private GPUArgsBuffer         particleArgsBuffer, edgeArgsBuffer;

        private enum DividePattern {
            Closed, Branch
        }

        private enum ComputeKernel {
            InitParticles, 
            InitEdges, 
            EmitParticles, 
            UpdateParticles, 
            UpdateEdges, 
            SpringEdges, 
            GetDividableEdges, 
            DivideUnconnectedParticles,
            DivideEdgesClosed,
            DivideEdgesBranch
        }

        private Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        private GPUThreads gpuThreads;

        #region Shader Variable
        private int particlesProp            = Shader.PropertyToID("_Particles");
        private int particlesReadProp        = Shader.PropertyToID("_ParticlesRead");
        private int particlePoolAppendProp   = Shader.PropertyToID("_ParticlePoolAppend");
        private int particlePoolConsumeProp  = Shader.PropertyToID("_ParticlePoolConsume");

        private int edgesProp                = Shader.PropertyToID("_Edges");
        private int edgePoolAppendProp       = Shader.PropertyToID("_EdgePoolAppend");
        private int edgePoolConsumeProp      = Shader.PropertyToID("_EdgePoolConsume");

        private int dividablePoolAppendProp  = Shader.PropertyToID("_DividablePoolAppend");
        private int dividablePoolConsumeProp = Shader.PropertyToID("_DividablePoolConsume");
        private int divideCountProp          = Shader.PropertyToID("_DivideCount");
        private int maxLinkProp              = Shader.PropertyToID("_MaxLink");

        private int palleteProp              = Shader.PropertyToID("_Palette");

        private int timeProp                 = Shader.PropertyToID("_Time");
        private int deltaTimeProp            = Shader.PropertyToID("_DT");

        private int emitPointProp            = Shader.PropertyToID("_EmitPoint");
        private int emitCountProp            = Shader.PropertyToID("_EmitCount");

        private int sizeProp                 = Shader.PropertyToID("_Size");
        private int local2WorldProp          = Shader.PropertyToID("_Local2World");

        private int growProp                 = Shader.PropertyToID("_Grow");
        private int dragProp                 = Shader.PropertyToID("_Drag");
        private int limitProp                = Shader.PropertyToID("_Limit");
        private int repulsionProp            = Shader.PropertyToID("_Repulsion");
        private int springProp               = Shader.PropertyToID("_Spring");
        #endregion

        void Start() {
            pallete = Texture2DUtil.CreateTexureFromGradient(gradient, 128);

            InitBuffers();
            InitKernels();

            InitParticlesKernel();
            InitEdgesKernel();

            EmitParticlesKernel(Vector2.zero, 1);

            StartCoroutine(IDivder());
        }

        #region Initialize
        public void InitBuffers() {
            particlePool  = new GPUObjectPingPongPool(count, typeof(Particle));
            edgePool      = new GPUObjectPool(count, typeof(Edge));
            dividablePool = new GPUPool(count, typeof(uint));

            particleMesh = MeshUtil.CreateQuad();
            particleArgsBuffer = new GPUArgsBuffer(particleMesh.GetIndexCount(0), (uint)count);

            edgeMesh = MeshUtil.CreateLine();
            edgeArgsBuffer = new GPUArgsBuffer(edgeMesh.GetIndexCount(0), (uint)count);
        }

        public void InitKernels() {
            kernelMap = Enum.GetValues(typeof(ComputeKernel))
                .Cast<ComputeKernel>()
                .ToDictionary(t => t, t => compute.FindKernel(t.ToString()));

            gpuThreads = ComputeShaderUtil.GetThreadGroupSize(compute, kernelMap[ComputeKernel.InitParticles]);
            ComputeShaderUtil.InitialCheck(count, gpuThreads);
        }

        private void InitParticlesKernel() {
            var kernel = kernelMap[ComputeKernel.InitParticles];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, particlePoolAppendProp, particlePool.PoolBuffer);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        private void InitEdgesKernel() {
            var kernel = kernelMap[ComputeKernel.InitEdges];
            compute.SetBuffer(kernel, edgesProp, edgePool.ObjectBuffer);
            compute.SetBuffer(kernel, edgePoolAppendProp, edgePool.PoolBuffer);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        #endregion

        #region Emit
        private void EmitParticlesKernel(Vector2 emitPoint, int emitCount = 8) {

            // make sure not exceed the pool buffer size
            emitCount = Mathf.Min(emitCount, particlePool.CopyPoolSize());
            if (emitCount <= 0) return;

            var kernel = kernelMap[ComputeKernel.EmitParticles];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, particlePoolConsumeProp, particlePool.PoolBuffer);
            compute.SetVector(emitPointProp, emitPoint);
            compute.SetInt(emitCountProp, emitCount);

            compute.Dispatch(kernel, Mathf.CeilToInt(1f * emitCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        #endregion

        void Update() {
            compute.SetFloat(timeProp, Time.timeSinceLevelLoad);
            compute.SetFloat(deltaTimeProp, Time.deltaTime);

            for (var i = 0; i < iterations; i++) {
                UpdateEdgesKernel();
                SpringEdgesKernel();
                UpdateParticlesKernel();
            }
            Render();
        }
        
        #region Update
        private void UpdateParticlesKernel() {
            var kernel = kernelMap[ComputeKernel.UpdateParticles];
            compute.SetBuffer(kernel, particlesReadProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Write);
            compute.SetFloat(growProp, grow);
            compute.SetFloat(dragProp, drag);
            compute.SetFloat(limitProp, limit);
            compute.SetFloat(repulsionProp, repulsion);

            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);

            particlePool.ObjectPingPong.Swap();
        }

        private void UpdateEdgesKernel() {
            var kernel = kernelMap[ComputeKernel.UpdateEdges];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, edgesProp, edgePool.ObjectBuffer);
            compute.SetFloat(springProp, spring);

            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        private void SpringEdgesKernel() {
            var kernel = kernelMap[ComputeKernel.SpringEdges];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, edgesProp, edgePool.ObjectBuffer);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        #endregion

        #region Render
        private void Render() {
            // render particles
            particleMat.SetPass(0);
            particleMat.SetBuffer(particlesProp, particlePool.ObjectPingPong.Read);
            particleMat.SetMatrix(local2WorldProp, transform.localToWorldMatrix);
            particleMat.SetFloat(sizeProp, size);
            particleMat.SetTexture(palleteProp, pallete);
            Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMat, new Bounds(Vector3.zero, Vector3.one * 100f), particleArgsBuffer.Buffer);

            if (edgeMat == null) return;
            // render edges
            edgeMat.SetPass(0);
            edgeMat.SetBuffer(particlesProp, particlePool.ObjectPingPong.Read);
            edgeMat.SetBuffer(edgesProp, edgePool.ObjectBuffer);
            edgeMat.SetMatrix(local2WorldProp, transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(edgeMesh, 0, edgeMat, new Bounds(Vector3.zero, Vector3.one * 100f), edgeArgsBuffer.Buffer);
        }
        #endregion

        #region Divide
        IEnumerator IDivder() {
            yield return null;
            while (true) {
                yield return new WaitForSeconds(divideInterval);
                Divide();
            }
        }

        private void Divide() {
            GetDividableEdgesKernel();

            int dividableEdgesCount = dividablePool.CopyPoolSize();

            if (dividableEdgesCount == 0) {
                // divide particles without links
                DivideUnconnectedParticles(); 
            } else {
                DivideEdgesKernel(dividableEdgesCount, maxDivideCount);
            }
        }

        private void GetDividableEdgesKernel() {
            dividablePool.ResetPoolCounter();

            var kernel = kernelMap[ComputeKernel.GetDividableEdges];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, edgesProp, edgePool.ObjectBuffer);
            compute.SetBuffer(kernel, dividablePoolAppendProp, dividablePool.PoolBuffer);
            compute.SetInt(maxLinkProp, maxLink);

            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        private void DivideUnconnectedParticles() {
            var kernel = kernelMap[ComputeKernel.DivideUnconnectedParticles];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, particlePoolConsumeProp, particlePool.PoolBuffer);
            compute.SetBuffer(kernel, edgesProp, edgePool.ObjectBuffer);
            compute.SetBuffer(kernel, edgePoolConsumeProp, edgePool.PoolBuffer);

            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        private void DivideEdgesKernel(int dividableEdgesCount, int divideCount) {
            divideCount = Mathf.Min(dividableEdgesCount, divideCount);
            divideCount = Mathf.Min(particlePool.CopyPoolSize(), divideCount);
            divideCount = Mathf.Min(edgePool.CopyPoolSize(), divideCount);
            if (divideCount <= 0) return;

            var kernel = pattern == DividePattern.Closed ? 
                         kernelMap[ComputeKernel.DivideEdgesClosed] :
                         kernelMap[ComputeKernel.DivideEdgesBranch];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, particlePoolConsumeProp, particlePool.PoolBuffer);
            compute.SetBuffer(kernel, edgesProp, edgePool.ObjectBuffer);
            compute.SetBuffer(kernel, edgePoolConsumeProp, edgePool.PoolBuffer);
            compute.SetBuffer(kernel, dividablePoolConsumeProp, dividablePool.PoolBuffer);
            compute.SetInt(divideCountProp, divideCount);

            compute.Dispatch(kernel, Mathf.CeilToInt(1f * divideCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        #endregion

        private void OnDestroy() {
            particlePool.Dispose();
            edgePool.Dispose();
            dividablePool.Dispose();
            particleArgsBuffer.Dispose();
            edgeArgsBuffer.Dispose();
            Destroy(pallete);
        }
    }
}