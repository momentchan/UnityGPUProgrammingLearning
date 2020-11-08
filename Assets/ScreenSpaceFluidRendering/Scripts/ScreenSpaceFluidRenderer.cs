using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScreenSpaceFluidRendering {
    public class ScreenSpaceFluidRenderer : MonoBehaviour {
        [SerializeField] SPHFluidSimulation simulation;

        [SerializeField] Shader renderDepthShader;
        [SerializeField] Shader bilateralFilterBlurShader;
        [SerializeField] Shader calcNormalShader;
        [SerializeField] Shader renderGBufferShader;

        Material renderDepthMat;
        Material bilateralFilterBlurMat;
        Material calcNormalMat;
        Material renderGBufferMat;

        [SerializeField] float particleSize;
        [SerializeField, Range(1f, 16f)] float blurRadius = 2;
        [SerializeField] float blurScale = 1.0f;
        [SerializeField] float blurDepthFallOff = 50.0f;

        public Color diffuse = new Color(0.0f, 0.2f, 1.0f, 1.0f);
        public Color specular = new Color(0.25f, 0.25f, 0.25f, 0.25f);
        public float smoothness = 0.25f;
        public Color emission = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        public float emissionIntensity = 1.0f;

        private struct CommanddBufferInfo {
            public CameraEvent pass;
            public CommandBuffer buffer;
        };

        Dictionary<Camera, CommanddBufferInfo> cameras = new Dictionary<Camera, CommanddBufferInfo>();

        private Mesh _quad;
        private Mesh quad { get { return _quad ?? (_quad = GenerateQuad()); } }
        Mesh GenerateQuad() {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[4]
            {
                new Vector3( 1.0f,  1.0f, 0.0f),
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(-1.0f, -1.0f, 0.0f),
                new Vector3( 1.0f, -1.0f, 0.0f),
            };
            mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
            return mesh;
        }
        private void OnWillRenderObject() {
            var active = gameObject.activeInHierarchy && enabled;
            if (!active) {
                CleanUp();
                return;
            }

            var camera = Camera.current;
            if (!camera)
                return;

            if (!cameras.ContainsKey(camera)) {
                var info = new CommanddBufferInfo() {
                    pass = CameraEvent.BeforeGBuffer,
                    buffer = new CommandBuffer() { name = "Screen Space Fluid Renderer" }
                };

                camera.AddCommandBuffer(info.pass, info.buffer);
                cameras.Add(camera, info);
            } else {
                var buf = cameras[camera].buffer;
                buf.Clear();

                // Step1: Depth 
                #region Depth
                int depthBufferId = Shader.PropertyToID("_DepthBuffer");
                buf.GetTemporaryRT(depthBufferId, -1, -1, 24, FilterMode.Point, RenderTextureFormat.RFloat);
                buf.SetRenderTarget
                (
                    new RenderTargetIdentifier(depthBufferId), // depth
                    new RenderTargetIdentifier(depthBufferId)  // write to depth
                );
                buf.ClearRenderTarget(true, true, Color.clear);

                renderDepthMat.SetFloat("_ParticleSize", particleSize);
                renderDepthMat.SetBuffer("_ParticleDataBuffer", simulation.GetParticlesBuffer());

                buf.DrawProcedural(
                    Matrix4x4.identity,
                    renderDepthMat,
                    0,
                    MeshTopology.Points,
                    simulation.GetParticleNum()
                );
                #endregion

                // Step2: Blur
                #region
                var tempDepthBufferId = Shader.PropertyToID("_TempDepthBufferId");
                buf.GetTemporaryRT(tempDepthBufferId, -1, -1, 0, FilterMode.Trilinear, RenderTextureFormat.RFloat);
                bilateralFilterBlurMat.SetFloat("_BlurRadius", blurRadius);
                bilateralFilterBlurMat.SetFloat("_BlurScale", blurScale);
                bilateralFilterBlurMat.SetFloat("_BlurDepthFallOff", blurDepthFallOff);

                buf.SetGlobalVector("_BlurDir", new Vector2(1.0f, 0.0f));
                buf.Blit(depthBufferId, tempDepthBufferId, bilateralFilterBlurMat);

                buf.SetGlobalVector("_BlurDir", new Vector2(0.0f, 1.0f));
                buf.Blit(tempDepthBufferId, depthBufferId, bilateralFilterBlurMat);
                #endregion

                // Step3: Normal
                #region
                var normalBufferId = Shader.PropertyToID("_NormalBuffer");
                buf.GetTemporaryRT(normalBufferId, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat);
                buf.SetRenderTarget(normalBufferId);
                buf.ClearRenderTarget(false, true, Color.clear);
                var view = camera.worldToCameraMatrix;
                calcNormalMat.SetMatrix("_ViewMatrix", view);
                buf.SetGlobalTexture("_DepthBuffer", depthBufferId);
                buf.Blit(null, normalBufferId, calcNormalMat);
                #endregion

                // Step4: GBuffer
                #region GBuffer
                buf.SetGlobalTexture("_DepthBuffer", depthBufferId);
                buf.SetGlobalTexture("_NormalBuffer", normalBufferId);
                renderGBufferMat.SetColor("_Diffuse", diffuse);
                renderGBufferMat.SetColor("_Specular", new Vector4(specular.r, specular.g, specular.b, smoothness));
                renderGBufferMat.SetColor("_Emission", emission);

                buf.SetRenderTarget(
                    new RenderTargetIdentifier[4] {
                        BuiltinRenderTextureType.GBuffer0, // Diffuse
                        BuiltinRenderTextureType.GBuffer1, // Specular + Smoothness
                        BuiltinRenderTextureType.GBuffer2, // World Noraml
                        BuiltinRenderTextureType.GBuffer3  // Emission
                    },
                    BuiltinRenderTextureType.CameraTarget
                );
                buf.DrawMesh(quad, Matrix4x4.identity, renderGBufferMat);
                #endregion

                buf.ReleaseTemporaryRT(depthBufferId);
                buf.ReleaseTemporaryRT(tempDepthBufferId);
                buf.ReleaseTemporaryRT(normalBufferId);
            }
        }

        private void CleanUp() {
            foreach (var pair in cameras) {
                var cam = pair.Key;
                var info = pair.Value;
                if (cam)
                    cam.RemoveCommandBuffer(info.pass, info.buffer);
            }
            cameras.Clear();
            DestroyMaterial(ref renderDepthMat);
            DestroyMaterial(ref bilateralFilterBlurMat);
            DestroyMaterial(ref calcNormalMat);
            DestroyMaterial(ref renderGBufferMat);
        }
        private void CreateMaterial(ref Material mat, Shader shader) {
            if(mat==null && shader != null) {
                mat = new Material(shader) { hideFlags = HideFlags.DontSave };
            }
        }
        private void DestroyMaterial(ref Material mat) {
            if (mat != null) {
                if (Application.isPlaying)
                    Destroy(mat);
                else
                    DestroyImmediate(mat);
            }
            mat = null;
        }
        private void OnEnable() {
            CleanUp();
            CreateMaterial(ref renderDepthMat, renderDepthShader);
            CreateMaterial(ref bilateralFilterBlurMat, bilateralFilterBlurShader);
            CreateMaterial(ref calcNormalMat, calcNormalShader);
            CreateMaterial(ref renderGBufferMat, renderGBufferShader);
        }
        private void OnDisable() {
            CleanUp();
        }
    }
}