using UnityEngine;
using mj.gist;
using System.Runtime.InteropServices;
using System.Linq;

namespace ScreenSpaceFluidRendering {
    public class SPHFluidSimulation : MonoBehaviour {

        #region Definition
        struct Particle {
            public Vector3 position;
            public Vector3 velocity;
        };

        struct ParticleDensity {
            public float density;
        };

        struct ParticleForce {
            public Vector3 acceleration;
        };

        public enum NumParticlesSet {
            NUM_8K,
            NUM_16K,
            NUM_32K
        }
        const int SIMULATION_BLOCK_SIZE = 256;
        const int NUM_PARTICLES_8K = 8 * 1024;
        const int NUM_PARTICLES_16K = 16 * 1024;
        const int NUM_PARTICLES_32K = 32 * 1024;
        #endregion

        public ComputeBuffer GetParticlesBuffer() => particlesBufferRead;
        public int GetParticleNum() => numParticles;

        [SerializeField] ComputeShader cs;
        [SerializeField] NumParticlesSet particleNum = NumParticlesSet.NUM_16K;
        [SerializeField] bool needReset              = true;
        [SerializeField] bool enableSimulation       = true;

        [SerializeField] float smoothlen             = 0.012f;
        [SerializeField] float pressureStiffness     = 200.0f;
        [SerializeField] float restDensity          = 1000.0f;
        [SerializeField] float mass                  = 0.0002f;
        [SerializeField] float viscosity             = 0.1f;
        [SerializeField] float maxAllowableTimeStep  = 0.005f;
        
        
        [SerializeField] Vector3 domainCenter        = Vector3.zero;
        [SerializeField] float domainSphereRadius    = 1.0f;
        [SerializeField] float restitution           = 1.0f;
        [SerializeField] float maxVelocity           = 0.5f;

        // Gravity
        public Vector3 Gravity { get; set; } = Vector3.down * 0.5f;
        public float GravityToCenter { get; set; }

        // Interaction
        [SerializeField] float mouseRadius = 0.5f;
        [SerializeField] float interactionForce = 10f;
        public bool MouseDown { get; set; }
        public Vector3 MousePosition { get; set; }

        ComputeBuffer particlesBufferRead;
        ComputeBuffer particlesBufferWrite;
        ComputeBuffer particlesDensityBuffer;
        ComputeBuffer particlesForceBuffer;

        int numParticles;
        float timeStep;
        float densityCoef;
        float pressureCoef;
        float viscosityCoef;

        void Start() {

            if (needReset) {
                ReleaseResources();
                Initialize();
                needReset = false;
            }
        }

        void Update() {
            if (!enableSimulation) return;
            Simulation();
        }

        private void ReleaseResources() {
            ComputeShaderUtil.ReleaseBuffer(particlesBufferRead);
            ComputeShaderUtil.ReleaseBuffer(particlesBufferWrite);
            ComputeShaderUtil.ReleaseBuffer(particlesDensityBuffer);
            ComputeShaderUtil.ReleaseBuffer(particlesForceBuffer);
        }

        private void Initialize() 
            {
            switch (particleNum) {
                case NumParticlesSet.NUM_8K:
                    numParticles = NUM_PARTICLES_8K;
                    break;
                case NumParticlesSet.NUM_16K:
                    numParticles = NUM_PARTICLES_16K;
                    break;
                case NumParticlesSet.NUM_32K:
                    numParticles = NUM_PARTICLES_32K;
                    break;
            }

            particlesBufferRead = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(Particle)));
            particlesBufferWrite = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(Particle)));
            particlesDensityBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(ParticleDensity)));
            particlesForceBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(ParticleForce)));

            var particles = Enumerable.Range(0, numParticles).Select(_ =>
                new Particle() {
                    position = Random.insideUnitSphere * 0.01f,
                    velocity = Vector3.zero
                }).ToArray();

            particlesBufferRead.SetData(particles);
            particlesBufferWrite.SetData(particles);

            particles = null;
        }
        
        private void Simulation() {

            timeStep = Mathf.Min(maxAllowableTimeStep, Time.deltaTime);
            densityCoef = mass * 315.0f / (64.0f * Mathf.PI * Mathf.Pow(smoothlen, 9.0f));
            pressureCoef = mass * -45f / (Mathf.PI * Mathf.Pow(smoothlen, 6.0f));
            viscosityCoef = mass * viscosity * 45.0f / (Mathf.PI * Mathf.Pow(smoothlen, 6.0f));

            var kernelId = 0;

            cs.SetInt("_NumParticles", numParticles);
            cs.SetFloat("_TimeStep", timeStep);
            cs.SetFloat("_Smoothlen", smoothlen);
            cs.SetFloat("_PressureStiffness", pressureStiffness);
            cs.SetFloat("_RestDensity", restDensity);
            cs.SetFloat("_DensityCoef", densityCoef);
            cs.SetFloat("_PressureCoef", pressureCoef);
            cs.SetFloat("_ViscosityCoef", viscosityCoef);
            cs.SetVector("_Gravity", new Vector4(Gravity.x, Gravity.y, Gravity.z, GravityToCenter));
            cs.SetVector("_DomainCenter", new Vector4(domainCenter.x, domainCenter.y, domainCenter.z, 0.0f));
            cs.SetFloat("_DomainSphereRadius", domainSphereRadius);
            cs.SetFloat("_Restitution", restitution);
            cs.SetFloat("_MaxVelocity", maxVelocity);

            // Interaction
            cs.SetBool("_MouseDown", MouseDown);
            cs.SetVector("_MousePosition", MousePosition);
            cs.SetFloat("_MouseRadius", mouseRadius);
            cs.SetFloat("_InteractionForce", interactionForce);

            // Density
            kernelId = cs.FindKernel("DensityCS_Shared");
            cs.SetBuffer(kernelId, "_ParticlesRead", particlesBufferRead);
            cs.SetBuffer(kernelId, "_ParticlesDensityWrite", particlesDensityBuffer);
            cs.Dispatch(kernelId, numParticles / SIMULATION_BLOCK_SIZE, 1, 1);

            // Force
            kernelId = cs.FindKernel("ForceCS_Shared");
            cs.SetBuffer(kernelId, "_ParticlesRead", particlesBufferRead);
            cs.SetBuffer(kernelId, "_ParticlesDensityRead", particlesDensityBuffer);
            cs.SetBuffer(kernelId, "_ParticlesForceWrite", particlesForceBuffer);
            cs.Dispatch(kernelId, numParticles / SIMULATION_BLOCK_SIZE, 1, 1);

            // Integrate
            kernelId = cs.FindKernel("IntegrateCS");
            cs.SetBuffer(kernelId, "_ParticlesRead", particlesBufferRead);
            cs.SetBuffer(kernelId, "_ParticlesForceRead", particlesForceBuffer);
            cs.SetBuffer(kernelId, "_ParticlesWrite", particlesBufferWrite);
            cs.Dispatch(kernelId, numParticles / SIMULATION_BLOCK_SIZE, 1, 1);

            ComputeShaderUtil.SwapBuffer(ref particlesBufferRead, ref particlesBufferWrite);
        }
        private void OnDestroy() {
            ReleaseResources();
        }
    }
}