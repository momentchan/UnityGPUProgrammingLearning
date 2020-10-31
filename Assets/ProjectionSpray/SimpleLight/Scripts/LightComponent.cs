using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    public abstract class LightComponent : MonoBehaviour {
        [SerializeField] protected Renderer [] targets;
        [SerializeField] protected float intensity = 1f;
        [SerializeField] protected Color color;

        protected static MaterialPropertyBlock mpb;

        protected virtual void Update() {
            if (targets == null) return;
            if (mpb == null)
                mpb = new MaterialPropertyBlock();
        }
    }
}