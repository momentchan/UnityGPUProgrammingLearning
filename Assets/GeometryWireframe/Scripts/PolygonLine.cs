using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryWireframe {
    [ExecuteInEditMode]
    public class PolygonLine : WireframeBase {
        [SerializeField, Range(2, 64)] protected int vertexNum = 10;
        protected override void OnRenderObject() {
            material.SetInt(CSPARAM.VERTEX_NUM, vertexNum);
            material.SetMatrix(CSPARAM.LOCAL_TO_WOLRD_MATRIX, transform.localToWorldMatrix);
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1);
        }
    }
}