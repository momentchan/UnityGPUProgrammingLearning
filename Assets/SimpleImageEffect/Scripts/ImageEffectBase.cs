using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleImageEffect {
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class ImageEffectBase : MonoBehaviour {

        [SerializeField]
        private Material material;

        protected virtual void Start() {
            if(!material
               || !material.shader.isSupported) {
                enabled = false;
            }
        }


        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            Graphics.Blit(source, destination, material);
        }
    }
}