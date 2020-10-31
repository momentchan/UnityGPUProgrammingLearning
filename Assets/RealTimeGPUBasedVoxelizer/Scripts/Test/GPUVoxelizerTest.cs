using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxelizer.Test {
    [RequireComponent(typeof(MeshFilter))]
    public class GPUVoxelizerTest : MonoBehaviour {

        public Mesh Source { get { return source; } }
        public int Resolution { get { return resolution; } }

        [SerializeField] ComputeShader voxelizer;
        [SerializeField] protected Mesh source;
        [SerializeField, Range(16, 256)] protected int resolution = 32;
        [SerializeField] protected bool surfaceOnly = false;

        void Start() {
            var filter = GetComponent<MeshFilter>();

            var data = GPUVoxelizer.Voxelize(voxelizer, source, resolution);
            var voxels = data.GetData();
            filter.sharedMesh = VoxelMesh.Build(voxels, data.UnitLength);
            data.Dispose();
        }
    }
}