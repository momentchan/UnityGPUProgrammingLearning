using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    public class TextureProvider : MonoBehaviour {

        [SerializeField] private Renderer renderer;
        [SerializeField] private string propName = "_Prop";

        public Texture ProvideTexture { get; set; }

        private int propertyID;
        private MaterialPropertyBlock propertyBlock;

        void Start() {
            propertyID = Shader.PropertyToID(propName);
            propertyBlock = new MaterialPropertyBlock();
            if (!renderer)
                renderer = GetComponent<Renderer>();
        }
        void Update() {
            if (!ProvideTexture || !renderer) return;
            propertyBlock.SetTexture(propertyID, ProvideTexture);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}