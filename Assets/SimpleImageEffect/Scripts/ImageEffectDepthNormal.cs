using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleImageEffect {
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class ImageEffectDepthNormal : ImageEffectBase {

        protected new Camera camera;
        [SerializeField]
        protected DepthTextureMode depthTextureMode;

        protected override void Start() {
            base.Start();

            camera = GetComponent<Camera>();
            camera.depthTextureMode = depthTextureMode;
        }

        protected virtual void OnValidate() {
            if (camera != null) {
                camera.depthTextureMode = depthTextureMode;
            }
        }
    }
}