using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenSpaceReflection {
    public class SSRMainCamera : MonoBehaviour {

        [SerializeField] Shader shader;
        Camera cam;
        Material mat;

        void Start() {
            cam = GetComponent<Camera>();
            mat = new Material(shader);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            var view = cam.worldToCameraMatrix;
            var proj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            var viewProj = proj * view;
            mat.SetMatrix("_ViewProj", viewProj);
            mat.SetMatrix("_InvViewProj", viewProj.inverse);
            Graphics.Blit(src, dst, mat);
        }
        void OnDestroy() {
            Destroy(mat);
        }
    }
}