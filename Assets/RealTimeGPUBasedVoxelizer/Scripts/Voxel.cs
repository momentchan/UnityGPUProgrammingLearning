using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Voxelizer {

    [StructLayout(LayoutKind.Sequential)]
    public struct Voxel_t {
        public Vector3 position;
        public uint fill;
        public uint front;
        public bool IsEmpty() => fill < 1;
        public bool IsFrontFace() => fill > 0 && front > 0;
        public bool IsBackFace() => fill > 0 && front < 1;

    }
}