using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    [ExecuteInEditMode]
    public class PointLight : LightComponent {
        protected override void Update() {
            base.Update();

            foreach (var t in targets) {
                t.GetPropertyBlock(mpb);
                mpb.SetVector("_LitPos", transform.position);
                mpb.SetFloat("_Intensity", intensity);
                mpb.SetColor("_LitColor", color);
                t.SetPropertyBlock(mpb);
            }
        }
        private void OnDrawGizmos() {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, intensity);
        }
    }
}