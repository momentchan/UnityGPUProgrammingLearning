using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CellularGrowth {
    public class CellularGrowthParticleOnly : MonoBehaviour, IComputeShader {

        [SerializeField] protected ComputeShader compute;
        [SerializeField] protected Mesh mesh;
        [SerializeField] protected Material material;
        [SerializeField] protected Gradient gradient;
        [SerializeField] protected int count = 8192;

        [SerializeField] protected float size = 0.9f;
        [SerializeField] protected float grow = 0.25f;

        [SerializeField] protected float divideInterval = 0.5f;
        [SerializeField] protected int maxDivideCount = 16;

        [SerializeField] protected float drag = 0.995f;
        [SerializeField] protected float limit = 3f;

        [SerializeField] protected float repulsion = 1f;

        private Texture2D pallete;
        private GPUObjectPingPongPool particlePool;
        private GPUPool dividablePoolBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] drawArgs = new uint[5] { 0, 0, 0, 0, 0 };

        private enum ComputeKernel {
            Init, Emit, Update, GetDividable, Divide
        }
        private Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        private GPUThreads gpuThreads;

        #region Shader Variable
        private int particleBufferPropId       = Shader.PropertyToID("_Particles");
        private int particleBufferReadPropId   = Shader.PropertyToID("_ParticlesRead");
        private int poolAppendPropId           = Shader.PropertyToID("_ParticlePoolAppend");
        private int poolConsumePropId          = Shader.PropertyToID("_ParticlePoolConsume");
                                               
        private int dividablePoolAppendPropId  = Shader.PropertyToID("_DividablePoolAppend");
        private int dividablePoolConsumePropId = Shader.PropertyToID("_DividablePoolConsume");
        private int divideCountPropId          = Shader.PropertyToID("_DivideCount");                                      
                                               
        private int palletePropId              = Shader.PropertyToID("_Palette");
                                               
        private int timePropId                 = Shader.PropertyToID("_Time");
        private int deltaTimePropId            = Shader.PropertyToID("_DT");
                                              
        private int emitPointPropId            = Shader.PropertyToID("_EmitPoint");
        private int emitCountPropId            = Shader.PropertyToID("_EmitCount");
                                               
        private int sizePropId                 = Shader.PropertyToID("_Size");
        private int local2WorldPropId          = Shader.PropertyToID("_Local2World");
                                               
        private int growPropId                 = Shader.PropertyToID("_Grow");
        private int dragPropId                 = Shader.PropertyToID("_Drag");
        private int limitPropId                = Shader.PropertyToID("_Limit");
        private int repulsionPropId            = Shader.PropertyToID("_Repulsion");
        #endregion

        void Start() {
            pallete = Texture2DUtil.CreateTexureFromGradient(gradient, 128);

            InitBuffers();
            InitKernels();

            InitParticlesKernel();

            StartCoroutine(IDivder());
        }

        #region Initialize
        public void InitBuffers() {
            particlePool = new GPUObjectPingPongPool(count, typeof(Particle));

            dividablePoolBuffer = new GPUPool(count, typeof(uint));

            drawArgs[0] = mesh.GetIndexCount(0);
            drawArgs[1] = (uint)count;
            argsBuffer = new ComputeBuffer(1, drawArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(drawArgs);
        }

        public void InitKernels() {
            kernelMap = Enum.GetValues(typeof(ComputeKernel))
                .Cast<ComputeKernel>()
                .ToDictionary(t => t, t => compute.FindKernel(t.ToString()));

            gpuThreads = ComputeShaderUtil.GetThreadGroupSize(compute, kernelMap[ComputeKernel.Init]);
            ComputeShaderUtil.InitialCheck(count, gpuThreads);
        }

        private void InitParticlesKernel() {
            var kernel = kernelMap[ComputeKernel.Init];
            compute.SetBuffer(kernel, particleBufferPropId, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, poolAppendPropId, particlePool.PoolBuffer);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        #endregion

        void Update() {
            compute.SetFloat(timePropId, Time.timeSinceLevelLoad);
            compute.SetFloat(deltaTimePropId, Time.deltaTime);

            if (Input.GetMouseButton(0)) {
                EmitParticlesKernel(GetMousePoint());
            }

            UpdateParticlesKernel();
            RenderParticles();
        }

        #region Emit
        private void EmitParticlesKernel(Vector2 emitPoint, int emitCount = 8) {

            // make sure not exceed the pool buffer size
            emitCount = Mathf.Min(emitCount, particlePool.CopyPoolSize());
            if (emitCount <= 0) return;

            compute.SetVector(emitPointPropId, emitPoint);
            compute.SetInt(emitCountPropId, emitCount);

            var kernel = kernelMap[ComputeKernel.Emit];
            compute.SetBuffer(kernel, particleBufferPropId, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, poolConsumePropId, particlePool.PoolBuffer);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * emitCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        #endregion

        #region Update
        private void UpdateParticlesKernel() {

            compute.SetFloat(growPropId, grow);
            compute.SetFloat(dragPropId, drag);
            compute.SetFloat(limitPropId, limit);
            compute.SetFloat(repulsionPropId, repulsion);

            var kernel = kernelMap[ComputeKernel.Update];
            compute.SetBuffer(kernel, particleBufferReadPropId, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, particleBufferPropId, particlePool.ObjectPingPong.Write);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
            particlePool.ObjectPingPong.Swap();
        }
        #endregion

        #region Render
        private void RenderParticles() {
            material.SetPass(0);
            material.SetBuffer(particleBufferPropId, particlePool.ObjectPingPong.Read);
            material.SetMatrix(local2WorldPropId, transform.localToWorldMatrix);
            material.SetFloat(sizePropId, size);
            material.SetTexture(palletePropId, pallete);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one * 100f), argsBuffer);
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
            GetDividableParticlesKernel();
            DivideParticlesKernel(maxDivideCount);
        }

        private void GetDividableParticlesKernel() {
            dividablePoolBuffer.ResetPoolCounter();

            var kernel = kernelMap[ComputeKernel.GetDividable];
            compute.SetBuffer(kernel, particleBufferPropId, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, dividablePoolAppendPropId, dividablePoolBuffer.PoolBuffer);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        private void DivideParticlesKernel(int maxDivideCount = 16) {
            maxDivideCount = Mathf.Min(Mathf.Min(maxDivideCount, dividablePoolBuffer.CopyPoolSize()), particlePool.CopyPoolSize());
            if (maxDivideCount <= 0) return;

            var kernel = kernelMap[ComputeKernel.Divide];
            compute.SetBuffer(kernel, particleBufferPropId, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, poolConsumePropId, particlePool.PoolBuffer);
            compute.SetBuffer(kernel, dividablePoolConsumePropId, dividablePoolBuffer.PoolBuffer);
            compute.SetInt(divideCountPropId, maxDivideCount);
            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        #endregion

        private Vector2 GetMousePoint() {
            var p = Input.mousePosition;
            var world = Camera.main.ScreenToWorldPoint(new Vector3(p.x, p.y, Camera.main.nearClipPlane));
            var local = transform.InverseTransformPoint(world);
            return local;
        }

        private void OnDestroy() {
            particlePool.Dispose();
            dividablePoolBuffer.Dispose();
            argsBuffer.Dispose();
            Destroy(pallete);
        }
    }
}