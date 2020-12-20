using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrangeAttractor {
    public class ParticleRenderer : MonoBehaviour {

        [SerializeField] protected Material material;

        private StrangeAttractor attractor;

        void Start() {
            attractor = GetComponent<StrangeAttractor>();
        }

        void OnRenderObject() {
            material.SetPass(0);
            material.SetBuffer("_Particles", attractor.GetParticleBuffer());
            material.SetMatrix("_ModelMatrix", transform.localToWorldMatrix);

            Graphics.DrawProceduralNow(MeshTopology.Points, attractor.GetParticleNumbers());
        }
    }
}