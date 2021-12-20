using System.Collections.Generic;
using UnityEngine;
using mj.gist;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;

namespace CurlNoise {
    public class CurlNoiseParticle : MonoBehaviour {

        [SerializeField] protected ComputeShader compute;
        [SerializeField] protected Mesh instanceMesh;
        [SerializeField] protected Material instanceMaterial;
        [SerializeField] protected int instanceCount = 100000;
        [SerializeField] protected Vector3 externalForce = Vector3.back;
        [SerializeField] protected float emitterSize = 1f;
        [SerializeField] protected float viscosity = 2f;
        [SerializeField] protected float convergency = 12f;

        [SerializeField] protected List<ColorGradient> colors = new List<ColorGradient>();
        [SerializeField] protected List<Life> lives = new List<Life>();
        [SerializeField] protected List<Size> sizes = new List<Size>();
        
        private const float IDLE_TIME = 3f;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private int cachedInstanceCount = -1;
        private float timer = 0f;   

        private Kernel emitKernel, updateKernel;
        private ComputeBuffer argsBuffer, particleBuffer;

        void Start() {
            Initialize();
        }

        private void Initialize() {
            emitKernel = new Kernel(compute, CSPARAM.EMIT);
            updateKernel = new Kernel(compute, CSPARAM.UPDATE);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            InitialCheck();
            UpdateBuffers();
        }

        void InitialCheck() {
            Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50, "Under the DirectCompute5.0 (DX11 GPU) doesn't work");
            Assert.IsTrue(emitKernel.ThreadX * emitKernel.ThreadY * emitKernel.ThreadZ <= DirectCompute5_0.MAX_PROCESS, "Resolution is too heigh");
            Assert.IsTrue(emitKernel.ThreadX <= DirectCompute5_0.MAX_X, "THREAD_X is too large");
            Assert.IsTrue(emitKernel.ThreadY <= DirectCompute5_0.MAX_Y, "THREAD_Y is too large");
            Assert.IsTrue(emitKernel.ThreadZ <= DirectCompute5_0.MAX_Z, "THREAD_Z is too large");
            Assert.IsTrue(instanceCount <= DirectCompute5_0.MAX_PROCESS, "particleNumber is too large");
        }

        private void UpdateBuffers() {

            if (particleBuffer != null)
                particleBuffer.Release();

            particleBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(CurlParticle)));

            var particles = new CurlParticle[instanceCount];

            for(var i = 0; i < instanceCount; i++) {
                var emitPos = Random.insideUnitSphere * emitterSize;
                var life = lives[Random.Range(0, lives.Count)];
                var size = sizes[Random.Range(0, sizes.Count)];
                var color = colors[Random.Range(0, colors.Count)];
                particles[i] = new CurlParticle(emitPos, Random.Range(life.minLife, life.maxLife), size.startSize, size.endSize, color.startColor, color.endColor);
            }
            particleBuffer.SetData(particles);

            var numIndices = instanceMesh != null ? instanceMesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint)instanceCount;
            argsBuffer.SetData(args);
            cachedInstanceCount = instanceCount;
        }

        void Update() {
            timer += Time.deltaTime;

            if (timer <= IDLE_TIME) return;

            if (cachedInstanceCount != instanceCount)
                UpdateBuffers();

            instanceMaterial.SetPass(0);
            instanceMaterial.SetBuffer(CSPARAM.PARTICLE_BUFFER, particleBuffer);
            instanceMaterial.SetMatrix(CSPARAM.MODEL_MATRIX, transform.localToWorldMatrix);

            compute.SetVector(CSPARAM.TIME, new Vector2(Time.deltaTime, timer));
            compute.SetVector(CSPARAM.EXTERNAL_FORCE, externalForce);
            compute.SetFloat(CSPARAM.CONVERGENCY, convergency);
            compute.SetFloat(CSPARAM.VISCOSITY, viscosity);

            compute.SetBuffer(emitKernel.Index, CSPARAM.PARTICLE_BUFFER, particleBuffer);
            compute.Dispatch(emitKernel.Index, Mathf.CeilToInt((float)instanceCount / emitKernel.ThreadX), (int)emitKernel.ThreadY, (int)emitKernel.ThreadZ);

            compute.SetBuffer(updateKernel.Index, CSPARAM.PARTICLE_BUFFER, particleBuffer);
            compute.Dispatch(updateKernel.Index, Mathf.CeilToInt((float)instanceCount / updateKernel.ThreadX), (int)updateKernel.ThreadY, (int)updateKernel.ThreadZ);

            Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, Vector3.one * 100f), argsBuffer);
        }

        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(particleBuffer);
            ComputeShaderUtil.ReleaseBuffer(argsBuffer);
        }

        #region definition
        public static class CSPARAM{
            public const string EMIT             = "Emit";
            public const string UPDATE           = "Update";
            public const string PARTICLE_BUFFER  = "_ParticleBuffer";
            public const string MODEL_MATRIX     = "_ModelMatrix";
            public const string TIME             = "_Time";
            public const string EXTERNAL_FORCE   = "_ExternalForce";
            public const string VISCOSITY        = "_Viscosity";
            public const string CONVERGENCY      = "_Convergency";
        }

        private struct CurlParticle {
            Vector3 emitPos;
            Vector3 position;
            Vector4 velocity;   // xyz = velocity, w = velocity coef
            Vector3 life;       // x = time elapsed, y = life time, z = isActive (1 or -1)
            Vector3 size;       // x = current size, y = start size, z = target size
            Vector4 color;
            Vector4 startColor;
            Vector4 endColor;
             
            public CurlParticle(Vector3 emitPos, float life, float startSize, float endSize, Color startColor, Color endColor) {
                this.emitPos = emitPos;
                this.position = Vector3.zero;
                this.velocity = Vector3.zero;
                this.life = new Vector3(0, life, -1);
                this.size = new Vector3(0, startSize, endSize);
                this.color = Color.white;
                this.startColor = startColor;
                this.endColor = endColor;
            }
        }

        [System.Serializable]
        public struct Size {
            public float startSize;
            public float endSize;
        }

        [System.Serializable]
        public struct ColorGradient {
            public Color startColor;
            public Color endColor;
        }

        [System.Serializable]
        public struct Life {
            [Range(0f, 60f)]
            public float minLife;
            [Range(0f, 60f)]
            public float maxLife;
        }
        #endregion
    }
}