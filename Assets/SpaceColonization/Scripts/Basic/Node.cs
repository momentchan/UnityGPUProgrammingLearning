using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceColonization {
    public struct Node {
        public Vector3 position;
        public float t; // (0.0, 1.0)
        public float offset; // distance from root
        public float mass;
        public int from; // branch root index
        public uint active;
    }
}