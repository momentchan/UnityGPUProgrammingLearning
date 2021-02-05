using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceColonization {
    [StructLayout(LayoutKind.Sequential)]
    public struct SkinnedAttraction {
        public Vector3 position;
        public int bone;
        public int nearestIndex;
        public uint found;
        public uint active;
    }
}