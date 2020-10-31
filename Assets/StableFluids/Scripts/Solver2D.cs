using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Render;


namespace StableFluid {

    public class Solver2D : SolverBase {
        protected override void InitializeComputeShader() {
            width       = Screen.width;
            height      = Screen.height;
            solverTex   = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.ARGBFloat, TextureWrapMode.Clamp, FilterMode.Point, solverTex);
            densityTex  = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, TextureWrapMode.Clamp, FilterMode.Point, densityTex);
            velocityTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RGHalf, TextureWrapMode.Clamp, FilterMode.Point, velocityTex);
            prevTex     = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.ARGBFloat, TextureWrapMode.Clamp, FilterMode.Point, prevTex);

            Shader.SetGlobalTexture(solverTexId, solverTex);
        }

        protected override void DensityStep() {
            // Add source
            if (SourceTex != null) {
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceDensity], sourceId, SourceTex);
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceDensity], densityId, densityTex);
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceDensity], prevId, prevTex);
                computeShader.Dispatch(kernelMap[ComputeKernels.AddSourceDensity], Mathf.CeilToInt(densityTex.width / gpuThreads.x), Mathf.CeilToInt(densityTex.height / gpuThreads.y), 1);
            }

            // Diffuse density
            computeShader.SetTexture(kernelMap[ComputeKernels.DiffuseDensity], densityId, densityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.DiffuseDensity], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.DiffuseDensity], Mathf.CeilToInt(densityTex.width / gpuThreads.x), Mathf.CeilToInt(densityTex.height / gpuThreads.y), 1);

            // Swap density
            computeShader.SetTexture(kernelMap[ComputeKernels.SwapDensity], densityId, densityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.SwapDensity], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.SwapDensity], Mathf.CeilToInt(densityTex.width / gpuThreads.x), Mathf.CeilToInt(densityTex.height / gpuThreads.y), 1);

            // Advection using velocity solver
            computeShader.SetTexture(kernelMap[ComputeKernels.AdvectDensity], densityId, densityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.AdvectDensity], prevId, prevTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.AdvectDensity], velocityId, velocityTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.AdvectDensity], Mathf.CeilToInt(densityTex.width / gpuThreads.x), Mathf.CeilToInt(densityTex.height / gpuThreads.y), 1);
        }

        protected override void VelocityStep() {
            // Add source
            if (SourceTex != null) {
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceVelocity], sourceId, SourceTex);
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceVelocity], velocityId, velocityTex);
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceVelocity], prevId, prevTex);
                computeShader.Dispatch(kernelMap[ComputeKernels.AddSourceVelocity], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);
            }

            // Diffuse velocity
            computeShader.SetTexture(kernelMap[ComputeKernels.DiffuseVelocity], velocityId, velocityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.DiffuseVelocity], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.DiffuseVelocity], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Project
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep1], velocityId, velocityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep1], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.ProjectStep1], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Project
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep2], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.ProjectStep2], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Project
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep3], velocityId, velocityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep3], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.ProjectStep3], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Swap velocity
            computeShader.SetTexture(kernelMap[ComputeKernels.SwapVelocity], velocityId, velocityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.SwapVelocity], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.SwapVelocity], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Advection
            computeShader.SetTexture(kernelMap[ComputeKernels.AdvectVelocity], velocityId, velocityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.AdvectVelocity], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.AdvectVelocity], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Project
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep1], velocityId, velocityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep1], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.ProjectStep1], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Project
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep2], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.ProjectStep2], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);

            // Project
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep3], velocityId, velocityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.ProjectStep3], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.ProjectStep3], Mathf.CeilToInt(velocityTex.width / gpuThreads.x), Mathf.CeilToInt(velocityTex.height / gpuThreads.y), 1);
        }
    }
}