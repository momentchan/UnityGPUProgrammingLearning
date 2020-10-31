using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxelizer {
    public class GPUVoxelData : System.IDisposable {

        public int Width => width;
        public int Height => height;
        public int Depth => depth;
        public float UnitLength => unitLength;
        public ComputeBuffer Buffer => buffer;

        int width, height, depth;
        float unitLength;
        ComputeBuffer buffer;
        Voxel_t[] voxels;

        public GPUVoxelData(ComputeBuffer buf, int w, int h, int d, float u) {
            buffer = buf;
            width = w;
            height = h;
            depth = d;
            unitLength = u;
        }

        public Voxel_t[] GetData() {
            if (voxels == null) {
                voxels = new Voxel_t[Buffer.count];
                buffer.GetData(voxels);
            }
            return voxels;
        }

        public void Dispose() {
            if (buffer != null) {
                buffer.Release();
                buffer = null;
            }
        }
    }
}