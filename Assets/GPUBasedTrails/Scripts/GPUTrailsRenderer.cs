using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUBasedTrails {
    [RequireComponent(typeof(GPUTrails))]
    public class GPUTrailsRenderer : MonoBehaviour {
        [SerializeField] protected Material material;
        GPUTrails trails;

        void Start() {
            trails = GetComponent<GPUTrails>();
        }

        void OnRenderObject() {
            material.SetInt(GPUTrails.CSPARAM.NODE_NUM_PER_TRAIL, trails.NodeNum);
            material.SetFloat(GPUTrails.CSPARAM.LIFE, trails.Life);
            material.SetBuffer(GPUTrails.CSPARAM.TRAIL_BUFFER, trails.TrailBuffer);
            material.SetBuffer(GPUTrails.CSPARAM.NODE_BUFFER, trails.NodeBuffer);
            material.SetPass(0);

            var nodeNum = trails.NodeNum;
            var trailNum = trails.TrailNum;
            Graphics.DrawProceduralNow(MeshTopology.Points, nodeNum, trailNum);
        }
    }
}