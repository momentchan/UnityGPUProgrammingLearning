using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenSpaceReflection {
    public class SSRSubCamera : MonoBehaviour {

        [SerializeField] Shader shader;

        Camera cam;
        Material mat;
        RenderTexture rt;
        const string depthTex = "_SubCameraDepthTex";
        const string mainTex = "_SubCameraMainTex";

        int width => cam.pixelWidth;
        int height => cam.pixelHeight;


        void Start() {
            cam = GetComponent<Camera>();
            mat = new Material(shader);
            rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        }
        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            Graphics.Blit(src, rt, mat, 0);
            Graphics.Blit(src, dst, mat, 1);
            Shader.SetGlobalTexture(mainTex, rt);
            Shader.SetGlobalTexture(depthTex, dst);
        }

        private void OnDisable() {
            Destroy(mat);
            rt.Release();
        }
    }
}