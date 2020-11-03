using UnityEngine;

namespace ScreenSpaceFluidRendering {
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class GBufferTest : MonoBehaviour {

        [SerializeField] GBufferType bufferType;
        [SerializeField] Material mat;

        void Start() {
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }

        [ImageEffectOpaque]
        void OnRenderImage(RenderTexture src, RenderTexture dst) {

            foreach (var keyword in mat.shaderKeywords)
                mat.DisableKeyword(keyword);

            switch (bufferType) {
                case GBufferType.Diffuse:
                    mat.EnableKeyword("_GBUFFERTYPE_DIFFUSE");
                    break;
                case GBufferType.Specular:
                    mat.EnableKeyword("_GBUFFERTYPE_SPECULAR");
                    break;
                case GBufferType.Normal:
                    mat.EnableKeyword("_GBUFFERTYPE_NORMAL");
                    break;
                case GBufferType.Emission:
                    mat.EnableKeyword("_GBUFFERTYPE_EMISSION");
                    break;
                case GBufferType.Depth:
                    mat.EnableKeyword("_GBUFFERTYPE_DEPTH");
                    break;
                default:
                    mat.EnableKeyword("_GBUFFERTYPE_SOURCE");
                    break;
            }
            Graphics.Blit(src, dst, mat);
        }

        void OnGUI() {
            var fontSizeStore = GUI.skin.label.fontSize;
            var fontSize = 72;
            GUI.skin.label.fontSize = fontSize;
            GUI.color = Color.grey;
            GUI.Label(new Rect(fontSize * 0.4f, fontSize * 0.2f, Screen.width, fontSize * 1.25f), $"G-Buffer Type: {bufferType.ToString()}");
            GUI.color = Color.white;
        }

        enum GBufferType {
            Diffuse,
            Specular,
            Normal,
            Emission,
            Depth,
            Source
        }
    }
}