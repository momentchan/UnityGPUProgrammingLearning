using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceColonization {
    public struct SkinnedNode {
        public Vector3 position;
        public Vector3 animated; // position after skinned
        public int index0; // bone index
        public float t; // (0.0, 1.0)
        public float offset; // distance from root
        public float mass;
        public int from; // branch root index
        public uint active;
    }
}