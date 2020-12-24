using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CellularGrowth {
    [StructLayout(LayoutKind.Sequential)]
    public struct Edge {
        public int a, b;        // particle index connecting together
        public Vector2 force;
        public uint alive;
    }
}