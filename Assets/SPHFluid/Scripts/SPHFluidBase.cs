using System.Runtime.InteropServices;
using UnityEngine;

namespace SPHFluid {
    // Note:
    // 1. Compute density
    // 2. Compute pressure using density
    // 3. Compute pressure Force + viscocity Force
    // 4. Combine with external force (like gravity or interaction force)
    // 5. Get acceleration, then update velocity and position 

    struct ParticleDensity {
        public float density;
    };
    struct ParticlePressure {
        public float presure;
    };
    struct ParticleForce {
        public Vector2 acceleration;
    };

    public abstract class SPHFluidBase<T> : MonoBehaviour where T : struct {

        [SerializeField] protected int particleNums;
        [SerializeField] protected float mass = 0.0002f;
        [SerializeField] protected float radius = 0.012f;

        [SerializeField] protected float pressureStiffness = 200.0f;
        [SerializeField] protected float wallStiffness = 3000.0f;
        [SerializeField] protected float restDensity = 1000.0f;
        [SerializeField] protected float viscosity = 0.1f;
        [SerializeField] protected float maxTimeStep = 0.005f;
        [SerializeField] protected int iterations = 4;
        [SerializeField] protected Vector2 gravity = new Vector2(0.0f, -0.5f);
        [SerializeField] protected Vector2 range = new Vector2(1, 1);

        [SerializeField] protected bool simulation;

        private float timeStep;
        private float viscocityCoef;
        private float pressureCoef;
        private float densityCoef;

        private static readonly int THREAD_SIZE_X = 1024;
        [SerializeField] private ComputeShader computeShader;
        private ComputeBuffer particleBufferRead;
        private ComputeBuffer particleBufferWrite;
        private ComputeBuffer particlePressureBuffer;
        private ComputeBuffer particleDensityBuffer;
        private ComputeBuffer particleForceBuffer;

        public int ParticleNums => particleNums;
        public ComputeBuffer ParticleBufferRead => particleBufferRead;

        protected virtual void Start() {
            InitBuffer();
        }

        protected virtual void Update() {
            if (!simulation) return;

            timeStep = Mathf.Min(maxTimeStep, Time.deltaTime);

            densityCoef = mass * 4f / (Mathf.PI * Mathf.Pow(radius, 8));
            pressureCoef = mass * -30.0f / (Mathf.PI * Mathf.Pow(radius, 5));
            viscocityCoef = mass * 20f / (3 * Mathf.PI * Mathf.Pow(radius, 5));

            computeShader.SetInt("_ParticleNums", particleNums);
            computeShader.SetFloat("_TimeStep", timeStep);
            computeShader.SetFloat("_Radius", radius);
            computeShader.SetFloat("_PressureStiffness", pressureStiffness);
            computeShader.SetFloat("_WallStiffness", wallStiffness);
            computeShader.SetFloat("_RestDensity", restDensity);
            computeShader.SetFloat("_Viscosity", viscosity);
            computeShader.SetFloat("_DensityCoef", densityCoef);
            computeShader.SetFloat("_PressureCoef", pressureCoef);
            computeShader.SetFloat("_ViscocityCoef", viscocityCoef);
            computeShader.SetVector("_Gravity", gravity);
            computeShader.SetVector("_Range", range);

            AdditionalCSParams(computeShader);

            for (var i = 0; i < iterations; i++)
                RunFluidSolver();
        }
        private void OnDestroy() {
            ReleaseBuffer(particleBufferRead);
            ReleaseBuffer(particleBufferWrite);
            ReleaseBuffer(particlePressureBuffer);
            ReleaseBuffer(particleDensityBuffer);
            ReleaseBuffer(particleForceBuffer);
        }

        private void InitBuffer() {
            particleBufferRead = new ComputeBuffer(particleNums, Marshal.SizeOf(typeof(T)));
            var particles = new T[particleNums];
            InitParticleData(particles);
            ParticleBufferRead.SetData(particles);
            particles = null;

            particleBufferWrite = new ComputeBuffer(particleNums, Marshal.SizeOf(typeof(T)));
            particleDensityBuffer = new ComputeBuffer(particleNums, Marshal.SizeOf(typeof(ParticleDensity)));
            particlePressureBuffer = new ComputeBuffer(particleNums, Marshal.SizeOf(typeof(ParticlePressure)));
            particleForceBuffer = new ComputeBuffer(particleNums, Marshal.SizeOf(typeof(ParticleForce)));
        }

        private void RunFluidSolver() {
            int kernelID = -1;
            int threadGroupX = Mathf.CeilToInt(particleNums / THREAD_SIZE_X);

            // Density
            kernelID = computeShader.FindKernel("DensityCS");
            computeShader.SetBuffer(kernelID, "_ParticleBufferRead", particleBufferRead);
            computeShader.SetBuffer(kernelID, "_ParticleDensityBufferWrite", particleDensityBuffer);
            computeShader.Dispatch(kernelID, threadGroupX, 1, 1);

            // Pressure
            kernelID = computeShader.FindKernel("PressureCS");
            computeShader.SetBuffer(kernelID, "_ParticleDensityBufferRead", particleDensityBuffer);
            computeShader.SetBuffer(kernelID, "_ParticlePressureBufferWrite", particlePressureBuffer);
            computeShader.Dispatch(kernelID, threadGroupX, 1, 1);

            // Force
            kernelID = computeShader.FindKernel("ForceCS");
            computeShader.SetBuffer(kernelID, "_ParticleBufferRead", particleBufferRead);
            computeShader.SetBuffer(kernelID, "_ParticleDensityBufferRead", particleDensityBuffer);
            computeShader.SetBuffer(kernelID, "_ParticlePressureBufferRead", particlePressureBuffer);
            computeShader.SetBuffer(kernelID, "_ParticleForceBufferWrite", particleForceBuffer);
            computeShader.Dispatch(kernelID, threadGroupX, 1, 1);

            // Integrate
            kernelID = computeShader.FindKernel("IntegrateCS");
            computeShader.SetBuffer(kernelID, "_ParticleBufferRead", particleBufferRead);
            computeShader.SetBuffer(kernelID, "_ParticleForceBufferRead", particleForceBuffer);
            computeShader.SetBuffer(kernelID, "_ParticleBufferWrite", particleBufferWrite);
            computeShader.Dispatch(kernelID, threadGroupX, 1, 1);

            SwapBuffer(ref particleBufferRead, ref particleBufferWrite);
        }

        private void SwapBuffer(ref ComputeBuffer ping, ref ComputeBuffer pong) {
            ComputeBuffer temp = ping;
            ping = pong;
            pong = temp;
        }

        protected abstract void AdditionalCSParams(ComputeShader computeShader);

        protected abstract void InitParticleData(T[] particles);
        
        private void ReleaseBuffer(ComputeBuffer buffer) {
            if (buffer != null) {
                buffer.Release();
                buffer = null;
            }
        }
    }
}