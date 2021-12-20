using System.Collections.Generic;
using UnityEngine;
using mj.gist;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace ReactionDiffusion {
    public class ReactionDiffusion2DStandard : ReactionDiffusion2D {

        [SerializeField] protected Color bottomEmit;
        [SerializeField] protected Color topEmit;

        [SerializeField, Range(0f, 10f)] protected float bottomEmitIntensity;
        [SerializeField, Range(0f, 10f)] protected float topEmitIntensity;

        protected int heightTexProp           = Shader.PropertyToID("_MainTex");
        protected int normalTexProp           = Shader.PropertyToID("_NormalTex");

        protected int bottomEmitProp          = Shader.PropertyToID("_Emit0");
        protected int topEmitProp             = Shader.PropertyToID("_Emit1");
        protected int bottomEmitIntensityProp = Shader.PropertyToID("_EmitInt0");
        protected int topEmitIntensityProp    = Shader.PropertyToID("_EmitInt1");

        protected override void Initialize() {
            base.Initialize();
            normalMap = RenderTextureUtil.CreateRenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, true, false, false, TextureWrapMode.Repeat);
        }

        protected override void DrawKernel() {
            cs.SetInt(widthProp, width);
            cs.SetInt(heightProp, height);
            cs.SetBuffer(kernelMap[ComputeKernel.Draw], pixelBufferReadProp, pixelBuffer.Read);
            cs.SetTexture(kernelMap[ComputeKernel.Draw], heightMapProp, heightMap);
            cs.SetTexture(kernelMap[ComputeKernel.Draw], normalMapProp, normalMap);
            cs.Dispatch(kernelMap[ComputeKernel.Draw], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), 1);
        }

        protected override void UpdateMaterial() {
            mat.SetTexture(heightTexProp, heightMap);
            mat.SetTexture(normalTexProp, normalMap);

            mat.SetColor(bottomColProp, bottomColor);
            mat.SetColor(topColProp, topColor);
            mat.SetColor(bottomEmitProp, bottomEmit);
            mat.SetColor(topEmitProp, topEmit);
            mat.SetFloat(bottomEmitIntensityProp, bottomEmitIntensity);
            mat.SetFloat(topEmitIntensityProp, topEmitIntensity);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            Destroy(normalMap);
        }
    }
}