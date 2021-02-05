using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceColonization {
    [StructLayout(LayoutKind.Sequential)]
    public struct Attraction {
        public Vector3 position;
        public int nearestIndex;
        public uint found;
        public uint active;
    }
}