using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CellularGrowth {
    public class CellularGrowthParticleOnly : MonoBehaviour, ComputeShaderUser {

        [SerializeField] protected ComputeShader compute;
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

        private Mesh mesh;
        private Texture2D pallete;
        private GPUObjectPingPongPool particlePool;
        private GPUPool dividablePool;
        private GPUDrawArgsBuffer argsBuffer;

        private enum ComputeKernel {
            InitParticles, 
            EmitParticles, 
            UpdateParticles, 
            GetDividableParticles, 
            DivideParticles
        }
        private Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        private GPUThreads gpuThreads;

        #region Shader Variable
        private int particlesProp            = Shader.PropertyToID("_Particles");
        private int particlesReadProp        = Shader.PropertyToID("_ParticlesRead");
        private int particlePoolAppendProp   = Shader.PropertyToID("_ParticlePoolAppend");
        private int particlePoolConsumeProp  = Shader.PropertyToID("_ParticlePoolConsume");
                                             
        private int dividablePoolAppendProp  = Shader.PropertyToID("_DividablePoolAppend");
        private int dividablePoolConsumeProp = Shader.PropertyToID("_DividablePoolConsume");
        private int divideCountProp          = Shader.PropertyToID("_DivideCount");                                      
                                               
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

            dividablePool = new GPUPool(count, typeof(int));

            mesh = MeshUtil.CreateQuad();
            argsBuffer = new GPUDrawArgsBuffer(mesh.GetIndexCount(0), (uint)count);
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
        #endregion

        void Update() {
            compute.SetFloat(timeProp, Time.timeSinceLevelLoad);
            compute.SetFloat(deltaTimeProp, Time.deltaTime);

            if (Input.GetMouseButton(0)) {
                EmitParticlesKernel(GetMousePoint());
            }

            UpdateParticlesKernel();
            Render();
        }

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
        #endregion

        #region Render
        private void Render() {
            material.SetPass(0);
            material.SetBuffer(particlesProp, particlePool.ObjectPingPong.Read);
            material.SetMatrix(local2WorldProp, transform.localToWorldMatrix);
            material.SetFloat(sizeProp, size);
            material.SetTexture(palleteProp, pallete);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one * 100f), argsBuffer.Buffer);
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
            var kernel = kernelMap[ComputeKernel.GetDividableParticles];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, dividablePoolAppendProp, dividablePool.PoolBuffer);
            dividablePool.ResetPoolCounter();

            compute.Dispatch(kernel, Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        private void DivideParticlesKernel(int maxDivideCount = 16) {
            maxDivideCount = Mathf.Min(Mathf.Min(maxDivideCount, dividablePool.CopyPoolSize()), particlePool.CopyPoolSize());
            if (maxDivideCount <= 0) return;

            var kernel = kernelMap[ComputeKernel.DivideParticles];
            compute.SetBuffer(kernel, particlesProp, particlePool.ObjectPingPong.Read);
            compute.SetBuffer(kernel, particlePoolConsumeProp, particlePool.PoolBuffer);
            compute.SetBuffer(kernel, dividablePoolConsumeProp, dividablePool.PoolBuffer);
            compute.SetInt(divideCountProp, maxDivideCount);

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
            dividablePool.Dispose();
            argsBuffer.Dispose();
            Destroy(pallete);
        }
    }
}