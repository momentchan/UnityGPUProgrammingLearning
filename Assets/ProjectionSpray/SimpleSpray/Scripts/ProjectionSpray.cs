using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    public class ProjectionSpray : MonoBehaviour {
        public Color Color { get { return color; } set { color = value; } }


        [SerializeField] protected Material drawingMat;
        [SerializeField] protected float intensity = 1f;
        [SerializeField] protected Color color = Color.white;
        [SerializeField] protected float range = 10f;
        [SerializeField, Range(0, 90)] protected float angle = 30f;
        [SerializeField] protected int depthTextureResolution = 1024;
        [SerializeField] Texture cookie;
        protected RenderTexture depthTexture;
        
        Shader DepthRenderShader {
            get { return Shader.Find("ProjectionSpray/SimpleLight/DepthRender"); }
        }

        protected Camera camera {
            get {
                if (c == null) {
                    c = GetComponent<Camera>();
                    if (c == null)
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


        public void UpdateDrawingMaterial() {
            var currentRt = RenderTexture.active;
            RenderTexture.active = depthTexture;
            GL.Clear(true, true, Color.white * camera.farClipPlane);
            camera.fieldOfView = angle;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = range;
            camera.Render();
            RenderTexture.active = currentRt;

            var projMatrix = camera.projectionMatrix;
            var worldToDrawerMatrix = transform.worldToLocalMatrix;

            drawingMat.SetVector("_DrawerPos", transform.position);
            drawingMat.SetFloat("_Emission", intensity * Time.smoothDeltaTime);
            drawingMat.SetColor("_Color", color);
            drawingMat.SetMatrix("_WorldToDrawerMatrix", worldToDrawerMatrix);
            drawingMat.SetMatrix("_ProjMatrix", projMatrix);
            drawingMat.SetTexture("_Cookie", cookie);
            drawingMat.SetTexture("_DrawerDepth", depthTexture);
        }

        public void Draw(Drawable drawable) {
            drawable.Draw(drawingMat);
        }
    }
}