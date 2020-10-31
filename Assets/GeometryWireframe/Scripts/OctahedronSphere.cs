using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryWireframe {
    [ExecuteInEditMode]
    public class OctahedronSphere : WireframeBase {
        [SerializeField, Range(1, 9)] protected int level = 1;

        protected override void OnRenderObject() {
            material.SetInt(CSPARAM.LEVEL, level);
            material.SetMatrix(CSPARAM.LOCAL_TO_WOLRD_MATRIX, transform.localToWorldMatrix);
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 8);
        }
    }
}