using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Common;
using System.Linq;
using UnityEngine.Assertions;

namespace StrangeAttractor {
    public abstract class StrangeAttractor : MonoBehaviour {

        [SerializeField] protected KeyCode reEmitKey = KeyCode.A;
        [SerializeField] protected ComputeShader computeShader;
        [SerializeField] protected Mesh instanceMesh;
        [SerializeField] protected Material mat;
        [SerializeField] protected int instanceCount;
        [SerializeField] protected float emitterSize = 10f;
        [SerializeField] protected float particleSize = 0.3f;
        [SerializeField] protected Gradient gradient;

        protected ComputeShader computeShaderInstance;
        protected ComputeBuffer computeBuffer;
        protected ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private int cachedInstanceCount = -1;
        private float timer;

        private int bufferPropId;
        private int timesPropId;
        private int modelMatrixPropId;

        private Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        private GPUThreads gpuThreads;

        private enum ComputeKernel {
            Emit, Update
        }

        protected abstract void InitializeShaderUniforms();
        protected abstract void InitializeComputeBuffer();
        protected abstract void UpdateShaderUniforms();

        private void Start() {
            Initialize();
        }

        protected virtual void Update() {
            timer += Time.deltaTime;

            if (cachedInstanceCount != instanceCount)
                InitializeBuffers();

            mat.SetPass(0);
            mat.SetBuffer(bufferPropId, computeBuffer);
            mat.SetMatrix(modelMatrixPropId, transform.localToWorldMatrix);

            UpdateShaderUniforms();

            computeShaderInstance.SetVector(timesPropId, new Vector2(Time.deltaTime, timer));

            computeShaderInstance.SetBuffer(kernelMap[ComputeKernel.Update], bufferPropId, computeBuffer);
            computeShaderInstance.Dispatch(kernelMap[ComputeKernel.Update], Mathf.CeilToInt(1f * instanceCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);

            // Render
            Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, mat, new Bounds(Vector3.zero, new Vector3(100f, 100f, 100f)), argsBuffer);

            if (Input.GetKeyDown(reEmitKey)) {
                computeShaderInstance.SetBuffer(kernelMap[ComputeKernel.Emit], bufferPropId, computeBuffer);
                computeShaderInstance.Dispatch(kernelMap[ComputeKernel.Emit], Mathf.CeilToInt(1f * instanceCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);
            }
        }

        protected virtual void Initialize() {
            computeShaderInstance = computeShader;
            kernelMap = System.Enum.GetValues(typeof(ComputeKernel))
                .Cast<ComputeKernel>()
                .ToDictionary(t => t, t => computeShaderInstance.FindKernel(t.ToString()));
            uint threadX, threadY, threadZ;
            computeShaderInstance.GetKernelThreadGroupSizes(kernelMap[ComputeKernel.Emit], out threadX, out threadY, out threadZ);
            gpuThreads = new GPUThreads(threadX, threadY, threadZ);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            bufferPropId = Shader.PropertyToID("_Particles");
            timesPropId = Shader.PropertyToID("_Times");
            modelMatrixPropId = Shader.PropertyToID("_ModelMatrix");

            InitializeShaderUniforms();

            InitialCheck();
            InitializeBuffers();
        }

        private void InitialCheck() {
            Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50, "Under the DirectCompute5.0 (DX11 GPU) doesn't work");
            Assert.IsTrue(gpuThreads.x * gpuThreads.y * gpuThreads.z <= DirectCompute5_0.MAX_PROCESS, "Resolution is too heigh");
            Assert.IsTrue(gpuThreads.x <= DirectCompute5_0.MAX_X, "THREAD_X is too large");
            Assert.IsTrue(gpuThreads.y <= DirectCompute5_0.MAX_Y, "THREAD_Y is too large");
            Assert.IsTrue(gpuThreads.z <= DirectCompute5_0.MAX_Z, "THREAD_Z is too large");
            Assert.IsTrue(instanceCount <= DirectCompute5_0.MAX_PROCESS, "particleNumber is too large");
        }

        private void InitializeBuffers() {
            InitializeComputeBuffer();

            uint numIndices = (instanceMesh != null) ? instanceMesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint)instanceCount;
            argsBuffer.SetData(args);

            cachedInstanceCount = instanceCount;

            computeShaderInstance.SetBuffer(kernelMap[ComputeKernel.Emit], bufferPropId, computeBuffer);
            computeShaderInstance.Dispatch(kernelMap[ComputeKernel.Emit], Mathf.CeilToInt(1f * instanceCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(computeBuffer);
            ComputeShaderUtil.ReleaseBuffer(argsBuffer);
        }

        protected struct Particle {
            Vector3 emitPos;
            Vector3 position;
            Vector3 velocity;
            float life;
            Vector2 size;       // x = current size, y = target size
            Vector4 color;

            public Particle(Vector3 emitPos, float size, Color color) {
                this.emitPos = emitPos;
                this.position = Vector3.zero;
                this.velocity = Vector3.zero;
                this.life = 0;
                this.size = new Vector2(0, size);
                this.color = color;
            }
        }

    }
}