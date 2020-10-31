using UnityEngine;

namespace Voxelizer {
    public class Triangle {
        public Vector3 a, b, c;
        public bool frontFacing;
        public Bounds bounds;

        public Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 dir) {
            this.a = a;
            this.b = b;
            this.c = c;

            var normal = Vector3.Cross(b - a, c - a);
            frontFacing = Vector3.Dot(normal, dir) <= 0;

            var min = Vector3.Min(a, Vector3.Min(b, c));
            var max = Vector3.Max(a, Vector3.Max(b, c));
            bounds.SetMinMax(min, max);
        }
    }
}