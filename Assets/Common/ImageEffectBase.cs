using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    [RequireComponent(typeof(Camera))]
    public class ImageEffectBase : MonoBehaviour {

        [SerializeField] protected Material material;
        [SerializeField] protected bool enable;

        protected virtual void Start() {
        }

        protected virtual void OnRenderImage(RenderTexture src, RenderTexture dst) {
            if (!IsSupportAndEnable())
                Graphics.Blit(src, dst, material);
            else
                Graphics.Blit(src, dst);
        }

        protected bool IsSupportAndEnable () {
            return material != null && material.shader.isSupported && enable;
        }
    }
}