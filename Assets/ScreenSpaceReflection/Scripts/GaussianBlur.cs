using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ScreenSpaceReflection {
    public class GaussianBlur : MonoBehaviour {

        [SerializeField] protected Shader shader;
        [SerializeField] protected int blurTimes = 2;
        [SerializeField] protected float blurSize = 1;


        protected Material material;

        private void Start() {
            material = new Material(shader);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst) {

            material.SetFloat("_BlurSize", blurSize);

            var tmp = RenderTexture.GetTemporary(src.width, src.height, src.depth);

            for(var i=0; i<blurTimes; i++) {
                Graphics.Blit(src, tmp, material, 0);
                Graphics.Blit(tmp, src, material, 1);
            }
            Graphics.Blit(src, dst);

            RenderTexture.ReleaseTemporary(tmp);
        }
    }
}