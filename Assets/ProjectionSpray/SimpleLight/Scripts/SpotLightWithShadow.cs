using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    [ExecuteInEditMode]
    public class SpotLightWithShadow : LightComponent {

        Shader DepthRenderShader {
            get { return Shader.Find("ProjectionSpray/SimpleLight/DepthRender"); }
        }

        protected Camera camera {
            get {
                if (c == null) {
                    c = GetComponent<Camera>();
                    if(c == null)
                        c = gameObject.AddComponent<Camera>();
                    depthTexture = new RenderTexture(depthTextureResolution, depthTextureResolution, 16, RenderTextureFormat.RFloat);
                    depthTexture.wrapMode = TextureWrapMode.Clamp;
                    depthTexture.Create();
                    c.targetTexture = depthTexture;
                    c.SetReplacementShader(DepthRenderShader, "RenderType");
                    c.clearFlags = CameraClearFlags.Nothing;
                    c.nearClipPlane = 0.01f;
                    c.enabled = false;
                }
                return c;
            }
        }
        protected Camera c;

        [SerializeField] protected int depthTextureResolution = 1024;
        protected RenderTexture depthTexture;

        [SerializeField, Range(0, 90)] protected float angle = 30f;
        [SerializeField] protected float range = 10f;
        [SerializeField] Texture cookie;

        protected override void Update() {
            base.Update();

            var currentRt = RenderTexture.active;
            RenderTexture.active = depthTexture;
            GL.Clear(true, true, Color.white * camera.farClipPlane);
            camera.fieldOfView = angle;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = range;
            camera.Render();
            RenderTexture.active = currentRt;

            var projMatrix = camera.projectionMatrix;
            var worldToLitMatrix = transform.worldToLocalMatrix;

            foreach (var t in targets) {
                t.GetPropertyBlock(mpb);
                mpb.SetVector("_LitPos", transform.position);
                mpb.SetFloat("_Intensity", intensity);
                mpb.SetColor("_LitColor", color);
                mpb.SetMatrix("_ProjMatrix", projMatrix);
                mpb.SetMatrix("_WorldToLitMatrix", worldToLitMatrix);
                mpb.SetTexture("_Cookie", cookie);
                mpb.SetTexture("_LitDepth", depthTexture);
                t.SetPropertyBlock(mpb);
            }
        }
        private void OnDestroy() {
            if (depthTexture != null) {
                depthTexture.Release();
                depthTexture = null;
            }
        }

        private void OnDrawGizmos() {
            Gizmos.color = color;
            Gizmos.DrawRay(transform.position, transform.forward);
        }
    }
}