using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryWireframe {
    [ExecuteInEditMode]
    public class WaveLine : WireframeBase {
        [SerializeField, Range(2, 64)] protected int vertexNum = 10;
        protected override void OnRenderObject() {
            material.SetInt(CSPARAM.VERTEX_NUM, vertexNum);
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.LineStrip, vertexNum);
        }
    }
}