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

        [SerializeField] protected float drag = 0.995f;
        [SerializeField] protected float limit = 3f;

        [SerializeField] protected float repulsion = 1f;

        private Texture2D pallete;
        private PingPongBuffer particleBuffer;
        private ComputeBuffer poolBuffer, countBuffer;
        private ComputeBuffer dividablePoolBuffer;
        private ComputeBuffer argsBuffer;
        private int[] countArgs = new int[4] { 0, 1, 0, 0 };
        private uint[] drawArgs = new uint[5] { 0, 0, 0, 0, 0 };

        private enum ComputeKernel {
            Init, Emit, Update
        }
        private Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        private GPUThreads gpuThreads;

        #region Shader Variable
        private int particleBufferPropId     = Shader.PropertyToID("_Particles");
        private int particleBufferReadPropId = Shader.PropertyToID("_ParticlesRead");
        private int poolAppendPropId         = Shader.PropertyToID("_ParticlePoolAppend");
        private int poolConsumePropId        = Shader.PropertyToID("_ParticlePoolConsume");

        private int palletePropId            = Shader.PropertyToID("_Palette");

        private int timePropId               = Shader.PropertyToID("_Time");
        private int deltaTimePropId          = Shader.PropertyToID("_DT");

        private int emitPointPropId          = Shader.PropertyToID("_EmitPoint");
        private int emitCountPropId          = Shader.PropertyToID("_EmitCount");

        private int sizePropId               = Shader.PropertyToID("_Size");
        private int local2WorldPropId        = Shader.PropertyToID("_Local2World");

        private int growPropId               = Shader.PropertyToID("_Grow");
        private int dragPropId               = Shader.PropertyToID("_Drag");
        private int limitPropId              = Shader.PropertyToID("_Limit");
        private int repulsionPropId          = Shader.PropertyToID("_Repulsion");
        #endregion

        void Start() {
            Initialize();
        }

        void Update() {
            compute.SetFloat(timePropId, Time.timeSinceLevelLoad);
            compute.SetFloat(deltaTimePropId, Time.deltaTime);

            if (Input.GetMouseButton(0)) {
                EmitParticlesKernel(GetMousePoint());
            }

            UpdateParticlesKernel();
            RenderParticles();
        }

        private void EmitParticlesKernel(Vector2 emitPoint, int emitCount = 8) {

            // make sure not exceed the pool buffer size
            emitCount = Mathf.Min(emitCount, CopyPoolSize(poolBuffer));
            if (emitCount <= 0) return;

            compute.SetBuffer(kernelMap[ComputeKernel.Emit], particleBufferPropId, particleBuffer.Read);
            compute.SetBuffer(kernelMap[ComputeKernel.Emit], poolConsumePropId, poolBuffer);
            compute.SetVector(emitPointPropId, emitPoint);
            compute.SetInt(emitCountPropId, emitCount);

            compute.Dispatch(kernelMap[ComputeKernel.Emit], Mathf.CeilToInt(1f * emitCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }
        
        private void UpdateParticlesKernel() {
            compute.SetBuffer(kernelMap[ComputeKernel.Update], particleBufferReadPropId, particleBuffer.Read);
            compute.SetBuffer(kernelMap[ComputeKernel.Update], particleBufferPropId, particleBuffer.Write);

            compute.SetFloat(growPropId, grow);
            compute.SetFloat(dragPropId, drag);
            compute.SetFloat(limitPropId, limit);
            compute.SetFloat(repulsionPropId, repulsion);

            compute.Dispatch(kernelMap[ComputeKernel.Update], Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
            particleBuffer.Swap();
        }
        
        private void RenderParticles() {
            material.SetPass(0);
            material.SetBuffer(particleBufferPropId, particleBuffer.Read);
            material.SetMatrix(local2WorldPropId, transform.localToWorldMatrix);
            material.SetFloat(sizePropId, size);
            material.SetTexture(palletePropId, pallete);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one * 100f), argsBuffer);
        }
        IEnumerator IDivder() {
            yield return null;
        }

        #region Initialize
        private void Initialize() {
            pallete = Texture2DUtil.CreateTexureFromGradient(gradient, 128);

            InitBuffers();
            InitKernels();

            InitParticlesKernel();

            StartCoroutine(IDivder());
        }

        private void InitParticlesKernel() {
            compute.SetBuffer(kernelMap[ComputeKernel.Init], particleBufferPropId, particleBuffer.Read);
            compute.SetBuffer(kernelMap[ComputeKernel.Init], poolAppendPropId, poolBuffer);
            compute.Dispatch(kernelMap[ComputeKernel.Init], Mathf.CeilToInt(1f * count / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        public void InitBuffers() {
            particleBuffer = new PingPongBuffer(count, Marshal.SizeOf(typeof(Particle)));

            poolBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Append);
            poolBuffer.SetCounterValue(0);
            countBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
            countBuffer.SetData(countArgs);

            dividablePoolBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Append);
            dividablePoolBuffer.SetCounterValue(0);

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
        #endregion

        #region Utility
        private Vector2 GetMousePoint() {
            var p = Input.mousePosition;
            var world = Camera.main.ScreenToWorldPoint(new Vector3(p.x, p.y, Camera.main.nearClipPlane));
            var local = transform.InverseTransformPoint(world);
            return local;
        }

        private int CopyPoolSize(ComputeBuffer buffer) {
            countBuffer.SetData(countArgs);
            ComputeBuffer.CopyCount(buffer, countBuffer, 0);
            countBuffer.GetData(countArgs);
            return countArgs[0];
        }
        #endregion

        private void OnDestroy() {
            particleBuffer.Dispose();
            poolBuffer.Dispose();
            countBuffer.Dispose();
            dividablePoolBuffer.Dispose();
            argsBuffer.Dispose();
            Destroy(pallete);
        }
    }
}