using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravitationalNBodySimulation {
    [RequireComponent(typeof(SimpleNBodySimulation))]
    public class ParticleRenderer : MonoBehaviour {

        [SerializeField] protected Material material;
        [SerializeField] protected float scale = 1f;
        [SerializeField] protected Color color = Color.white;

        protected SimpleNBodySimulation simulation;

        void Start() {
            simulation = GetComponent<SimpleNBodySimulation>();
        }

        void OnRenderObject() {
            material.SetPass(0);
            material.SetFloat("_Scale", scale);
            material.SetColor("_Color", color);
            material.SetBuffer("_Particles", simulation.GetParticleBuffer());

            Graphics.DrawProceduralNow(MeshTopology.Points, simulation.GetParticleNumbers());
        }
    }
}