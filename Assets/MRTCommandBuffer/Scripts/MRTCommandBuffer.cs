using UnityEngine;
using UnityEngine.Rendering;

namespace MRTCommandBuffer {
    public class MRTCommandBuffer : MonoBehaviour {
        public Transform target; 

        [SerializeField] protected RenderTexture[] rtGBuffers = new RenderTexture[3]; 
        [SerializeField] protected RenderTexture depthBuffer = null;
        [SerializeField] protected Material gBufferMaterial;
        protected Renderer targetRender; 

        void Start() {
            var cam = GetComponent<Camera>();
            cam.depthTextureMode |= DepthTextureMode.Depth;

            targetRender = target.GetComponentInChildren<MeshRenderer>();

            var cmd = new CommandBuffer();
            cmd.name = "TestGBufferCMD";

            var rtGBuffersID = new RenderTargetIdentifier[rtGBuffers.Length];

            for (var i = 0; i < rtGBuffers.Length; ++i) {
                rtGBuffers[i] = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
                rtGBuffersID[i] = rtGBuffers[i];
            }
            depthBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);

            cmd.SetRenderTarget(rtGBuffersID, depthBuffer); 
            cmd.ClearRenderTarget(true, true, Color.clear, 1);
            cmd.DrawRenderer(targetRender, gBufferMaterial);

            cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cmd);
        }
    }
}