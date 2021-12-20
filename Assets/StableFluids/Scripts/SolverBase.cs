using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Render;
using mj.gist;

namespace StableFluid {

    using mj.gist;

    // Note:
    // 1. Update velocity
    //    - Add external force to velocity
    //    - Diffuse velocity
    //    - Mass conservation (projectStep 1, 2, 3)
    //    - Advect velocity
    //    - Mass conservation
    // 2. Update density
    //    - Add external force to density 
    //    - Diffuse density
    //    - Advect density

    public struct GPUThreads {
        public int x, y, z;
        public GPUThreads(uint x, uint y, uint z) {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }
    }

    public abstract class SolverBase : MonoBehaviour {

        protected enum ComputeKernels {
            AddSourceDensity,
            DiffuseDensity,
            AdvectDensity,
            SwapDensity,

            AddSourceVelocity,
            DiffuseVelocity,
            AdvectVelocity,
            ProjectStep1,
            ProjectStep2,
            ProjectStep3,
            SwapVelocity,

            Draw
        };

        protected Dictionary<ComputeKernels, int> kernelMap = new Dictionary<ComputeKernels, int>();
        protected GPUThreads gpuThreads;
        protected RenderTexture solverTex;
        protected RenderTexture densityTex;
        protected RenderTexture velocityTex;
        protected RenderTexture prevTex;
        protected string solverProp = "solver";
        protected string densityProp = "density";
        protected string velocityProp = "velocity";
        protected string prevProp = "prev";
        protected string sourceProp = "source";
        protected string diffProp = "diff";
        protected string viscProp = "visc";
        protected string dtProp = "dt";
        protected string velocityCoefProp = "velocityCoef";
        protected string densityCoefProp = "densityCoef";
        protected string solverTexProp = "_SolverTex";
        protected int solverId, densityId, velocityId, prevId, sourceId, diffId, viscId, dtId, velocityCoefId, densityCoefId, solverTexId;
        protected int width, height;

        [SerializeField]
        protected ComputeShader computeShader;


        [SerializeField]
        protected float visc;

        [SerializeField]
        protected float diff;

        [SerializeField]
        protected float velocityCoef;

        [SerializeField]
        protected float densityCoef;

        [SerializeField]
        protected bool isDensityOnly;

        [SerializeField]
        protected int lod;

        [SerializeField]
        protected bool debug;

        [SerializeField]
        protected Material debugMat;

        [SerializeField]
        protected RenderTexture sourceTex;

        public RenderTexture SourceTex { get { return sourceTex; } set { sourceTex = value; } }

        public TextureEvent textureBinding;

        protected virtual void Start() {
            Initialize();
        }

        protected virtual void Update() {
            if (width != Screen.width || height != Screen.height) InitializeComputeShader();
            computeShader.SetFloat(diffId, diff);
            computeShader.SetFloat(viscId, visc);
            computeShader.SetFloat(dtId, Time.deltaTime);
            computeShader.SetFloat(velocityCoefId, velocityCoef);
            computeShader.SetFloat(densityCoefId, densityCoef);

            if (!isDensityOnly) VelocityStep();
            DensityStep();

            computeShader.SetTexture(kernelMap[ComputeKernels.Draw], densityId, densityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.Draw], velocityId, velocityTex);
            computeShader.SetTextureFromGlobal(kernelMap[ComputeKernels.Draw], solverId, solverTexId);
            computeShader.Dispatch(kernelMap[ComputeKernels.Draw], Mathf.CeilToInt(solverTex.width / gpuThreads.x), Mathf.CeilToInt(solverTex.height / gpuThreads.y), 1);
            Shader.SetGlobalTexture(solverTexId, solverTex);

            textureBinding.Invoke(solverTex);
        }

        private void OnDestroy() {
            RenderUtility.ReleaseRenderTexture(solverTex);
            RenderUtility.ReleaseRenderTexture(densityTex);
            RenderUtility.ReleaseRenderTexture(velocityTex);
            RenderUtility.ReleaseRenderTexture(prevTex);
#if UNITY_EDITOR
            Debug.Log("Buffer Released");
#endif
        }
        protected virtual void Initialize() {
            kernelMap = Enum.GetValues(typeof(ComputeKernels)).Cast<ComputeKernels>().ToDictionary(k=>k, k=>computeShader.FindKernel(k.ToString()));

            uint threadX, threadY, threadZ;
            computeShader.GetKernelThreadGroupSizes(kernelMap[ComputeKernels.Draw], out threadX, out threadY, out threadZ);
            gpuThreads = new GPUThreads(threadX, threadY, threadZ);

            InitialCheck();

            solverId = Shader.PropertyToID(solverProp);
            densityId = Shader.PropertyToID(densityProp);
            velocityId = Shader.PropertyToID(velocityProp);
            prevId = Shader.PropertyToID(prevProp);
            sourceId = Shader.PropertyToID(sourceProp);
            diffId = Shader.PropertyToID(diffProp);
            viscId = Shader.PropertyToID(viscProp);
            dtId = Shader.PropertyToID(dtProp);
            velocityCoefId = Shader.PropertyToID(velocityCoefProp);
            densityCoefId = Shader.PropertyToID(densityCoefProp);
            solverTexId = Shader.PropertyToID(solverTexProp);

            InitializeComputeShader();

            if (debug) {
                if (debugMat == null) return;
                debugMat.mainTexture = solverTex;
            }
        }

        protected virtual void InitialCheck() {
            Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50, "Under the DirectCompute5.0 (DX11 GPU) doesn't work : StableFluid");
            Assert.IsTrue(gpuThreads.x * gpuThreads.y * gpuThreads.z <= DirectCompute5_0.MAX_PROCESS, "Resolution is too heigh : Stablefluid");
            Assert.IsTrue(gpuThreads.x <= DirectCompute5_0.MAX_X, "THREAD_X is too large : StableFluid");
            Assert.IsTrue(gpuThreads.y <= DirectCompute5_0.MAX_Y, "THREAD_Y is too large : StableFluid");
            Assert.IsTrue(gpuThreads.z <= DirectCompute5_0.MAX_Z, "THREAD_Z is too large : StableFluid");
        }

        protected abstract void InitializeComputeShader();


        protected abstract void DensityStep();
        protected abstract void VelocityStep();

    }
}