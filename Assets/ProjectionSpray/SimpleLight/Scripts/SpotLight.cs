using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    [ExecuteInEditMode]
    public class SpotLight : LightComponent {
        [SerializeField, Range(0, 90)] protected float angle = 30f;
        [SerializeField] protected float range = 10f;
        [SerializeField] Texture cookie;

        protected override void Update() {
            base.Update();
            var projMatrix = Matrix4x4.Perspective(angle, 1f, 0f, range);
            var worldToLitMatrix = transform.worldToLocalMatrix;

            foreach (var t in targets) {
                t.GetPropertyBlock(mpb);
                mpb.SetVector("_LitPos", transform.position);
                mpb.SetFloat("_Intensity", intensity);
                mpb.SetColor("_LitColor", color);
                mpb.SetMatrix("_ProjMatrix", projMatrix);
                mpb.SetMatrix("_WorldToLitMatrix", worldToLitMatrix);
                mpb.SetTexture("_Cookie", cookie);
                t.SetPropertyBlock(mpb);
            }
        }
        private void OnDrawGizmos() {
            Gizmos.color = color;
            Gizmos.DrawRay(transform.position, transform.forward);
        }
    }
}