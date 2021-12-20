using System.Collections.Generic;
using UnityEngine;

using mj.gist;
using System.Linq;
using UnityEngine.Assertions;
using mj.gist;

namespace StrangeAttractor {
    public abstract class StrangeAttractor : MonoBehaviour {

        [SerializeField] protected KeyCode reEmitKey = KeyCode.A;
        [SerializeField] protected ComputeShader computeShader;
        [SerializeField] protected int instanceCount;
        [SerializeField] protected float emitterSize = 0.2f;
        [SerializeField] protected float particleSize = 0.3f;
        [SerializeField] protected Gradient gradient;

        protected ComputeShader computeShaderInstance;
        protected ComputeBuffer computeBuffer;
        private int cachedInstanceCount = -1;
        private float timer;

        private int bufferPropId;
        private int timesPropId;

        private Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        private GPUThreads gpuThreads;

        public ComputeBuffer GetParticleBuffer() => computeBuffer;
        public int GetParticleNumbers() => instanceCount;

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

            UpdateShaderUniforms();

            computeShaderInstance.SetVector(timesPropId, new Vector2(Time.deltaTime, timer));

            computeShaderInstance.SetBuffer(kernelMap[ComputeKernel.Update], bufferPropId, computeBuffer);
            computeShaderInstance.Dispatch(kernelMap[ComputeKernel.Update], Mathf.CeilToInt(1f * instanceCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);

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
            
            gpuThreads = ComputeShaderUtil.GetThreadGroupSize(computeShaderInstance, kernelMap[ComputeKernel.Emit]);
            ComputeShaderUtil.InitialCheck(instanceCount, gpuThreads);

            bufferPropId = Shader.PropertyToID("_Particles");
            timesPropId = Shader.PropertyToID("_Times");

            InitializeShaderUniforms();
            InitializeBuffers();
        }

        private void InitializeBuffers() {
            InitializeComputeBuffer();

            cachedInstanceCount = instanceCount;

            computeShaderInstance.SetBuffer(kernelMap[ComputeKernel.Emit], bufferPropId, computeBuffer);
            computeShaderInstance.Dispatch(kernelMap[ComputeKernel.Emit], Mathf.CeilToInt(1f * instanceCount / gpuThreads.x), gpuThreads.y, gpuThreads.z);
        }

        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(computeBuffer);
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