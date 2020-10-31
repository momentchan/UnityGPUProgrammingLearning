using UnityEngine;

namespace ScreenSpaceReflection {
    public class MipMap : MonoBehaviour {

        [SerializeField] Shader shader;
        [SerializeField] int lod;

        Material mat;
        RenderTexture rt;

        void Start() {
            mat = new Material(shader);
            rt = new RenderTexture(Screen.width, Screen.height, 24);
            rt.useMipMap = true;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst) {
            mat.SetInt("_LOD", lod);
            Graphics.Blit(src, rt);
            Graphics.Blit(rt, dst, mat);
        }
    }
}