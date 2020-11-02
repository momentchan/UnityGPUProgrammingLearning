using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common;
using System.Linq;

namespace ScreenSpaceReflection {
    [ExecuteInEditMode]
    public class ScreenSpaceReflection : MonoBehaviour {

        enum Pass { Depth, Reflection, xBlur, yBlur, Accumulation, Composition }
        enum ViewMode { SSR, Normal, Reflection, Calculation, MipMap, Diffuse, Specular, Occusion, Smoothness }
        [SerializeField] Shader shader;
        [SerializeField] ViewMode viewMode;

        [Header("Reflection")]
        [SerializeField, Range(0, 5)] int maxLOD = 3;
        [SerializeField, Range(0, 150)] int maxLoop = 150;
        [SerializeField, Range(0.001f, 0.01f)] float thickness = 0.003f;
        [SerializeField, Range(0.01f, 0.1f)] float rayLenCoef = 0.05f;
        [SerializeField, Range(0f, 1f)] float reflectionRate = 0.5f;
        [SerializeField, Range(0.001f, 0.01f)] float baseRaise = 0.002f;

        [Header("Blur")]
        [SerializeField] int blurIter = 3;
        [SerializeField, Range(0f, 0.1f)] float blurThreshold = 0.01f;

        Material mat;
        RenderTexture dpt;
        RenderTexture[] rts = new RenderTexture[2];
        Camera cam;
        Mesh quad;

        int width => cam.pixelWidth;
        int height => cam.pixelHeight;

        void OnDisable() {
            Destroy(mat);
            dpt.Release();
        }

        void Start() {
            mat = new Material(shader);
            cam = GetComponent<Camera>();
            dpt = RenderTextureUtil.CreateRenderTexture(width, height, 24, RenderTextureFormat.Default, FilterMode.Bilinear, true, true, true);
            quad = CreateQuad();
        }

        void Update() {

            if (dpt != null) {
                if (RenderTextureUtil.IsResolutionChanged(dpt, width, height)) {
                    dpt.Release();
                    dpt = RenderTextureUtil.CreateRenderTexture(width, height, 24, RenderTextureFormat.Default, FilterMode.Bilinear, true, true, true);
                }
            }

            for (var i = 0; i < rts.Length; i++) {

                if (rts[i] != null) {
                    if (RenderTextureUtil.IsResolutionChanged(rts[i], width, height)) {
                        rts[i].Release();
                        rts[i] = RenderTextureUtil.CreateRenderTexture(width, height, 0, RenderTextureFormat.ARGB32, FilterMode.Bilinear, false, false, true);
                    }
                } else {
                    rts[i] = RenderTextureUtil.CreateRenderTexture(width, height, 0, RenderTextureFormat.ARGB32, FilterMode.Bilinear, false, false, true);
                    Graphics.SetRenderTarget(rts[i]);
                    GL.Clear(false, true, Color.clear);
                }
            }
        }
        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            Graphics.Blit(src, dpt, mat, (int)Pass.Depth);

            var view = cam.worldToCameraMatrix;                                 // world  -> camera
            var proj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);  // camera -> screen
            var viewProj = proj * view;                                         // world  -> screen
            mat.SetMatrix("_ViewProj", viewProj);
            mat.SetMatrix("_InvViewProj", viewProj.inverse);

            mat.SetInt("_ViewMode", (int)viewMode);
            mat.SetInt("_MaxLoop", maxLoop);
            mat.SetInt("_MaxLOD", maxLOD);

            mat.SetFloat("_BaseRaise", baseRaise);
            mat.SetFloat("_Thickness", thickness);
            mat.SetFloat("_RayLenCoeff", rayLenCoef);
            mat.SetFloat("_ReflectionRate", reflectionRate);

            mat.SetTexture("_CameraDepthMipmap", dpt);

            var reflectionRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var xBlurRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var yBlurRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            reflectionRT.filterMode = FilterMode.Bilinear;
            xBlurRT.filterMode = FilterMode.Bilinear;
            yBlurRT.filterMode = FilterMode.Bilinear;

            Graphics.Blit(src, reflectionRT, mat, (int)Pass.Reflection);
            mat.SetTexture("_ReflectionTexture", reflectionRT);

            mat.SetFloat("_BlurThreshold", blurThreshold);

            if (viewMode == ViewMode.SSR) {

                for (var i = 0; i < blurIter; i++) {
                    Graphics.SetRenderTarget(xBlurRT);
                    mat.SetPass((int)Pass.xBlur);
                    Graphics.DrawMeshNow(quad, Matrix4x4.identity);
                    mat.SetTexture("_ReflectionTexture", xBlurRT);

                    Graphics.SetRenderTarget(yBlurRT);
                    mat.SetPass((int)Pass.yBlur);
                    Graphics.DrawMeshNow(quad, Matrix4x4.identity);
                    mat.SetTexture("_ReflectionTexture", yBlurRT);
                }
                mat.SetTexture("_PreAccumulationTexture", rts[1]);
                Graphics.SetRenderTarget(rts[0]);
                mat.SetPass((int)Pass.Accumulation);
                Graphics.DrawMeshNow(quad, Matrix4x4.identity);

                mat.SetTexture("_AccumulationTexture", rts[0]);
                Graphics.SetRenderTarget(dst);
                Graphics.Blit(src, dst, mat, (int)Pass.Composition);

            } else {
                Graphics.Blit(reflectionRT, dst);
            }

            RenderTexture.ReleaseTemporary(reflectionRT);
            RenderTexture.ReleaseTemporary(xBlurRT);
            RenderTexture.ReleaseTemporary(yBlurRT);

            var tmp = rts[1];
            rts[1] = rts[0];
            rts[0] = tmp;
        }

        Mesh CreateQuad() {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[] {
                new Vector3(-1f, -1f, 0f),
                new Vector3(1f, -1f, 0f),
                new Vector3(-1f, 1f, 0f),
                new Vector3(1f, 1f, 0f)
            };

            mesh.triangles = new int[] {
                0, 3, 1,
                0, 2, 3
            };
            return mesh;
        }
    }
}