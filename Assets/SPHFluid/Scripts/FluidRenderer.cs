using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SPHFluid {
    [RequireComponent(typeof(SPHFluid2D))]
    public class FluidRenderer : MonoBehaviour {

        public SPHFluid2D solver;
        public Material renderMat;
        public Color color;

        private void OnRenderObject() {
            renderMat.SetPass(0);
            renderMat.SetColor("_Color", color);
            renderMat.SetBuffer("_ParticleBuffer", solver.ParticleBufferRead);
            Graphics.DrawProceduralNow(MeshTopology.Points, solver.ParticleNums);
        }
    }
}