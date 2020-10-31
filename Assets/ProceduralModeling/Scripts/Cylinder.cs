using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralModeling {

    // Reminder!!!!! : triangle is clockwise

    public class Cylinder : ProceduralModelingBase {

        [SerializeField, Range(0.1f, 10f)] protected float radius = 1f, height = 3f;
        [SerializeField, Range(3, 32)] protected int segments = 16;
        [SerializeField] bool hasCap = true;

        const float PI2 = Mathf.PI * 2f;

        protected override Mesh Build() {
            var mesh = new Mesh();

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            var top = 0.5f * height;
            var bottom = -0.5f * height;

            GenerateCap(segments, radius, top, bottom, vertices, uv, normals, true);

            var len = segments * 2;

            for(var i=0; i < segments; i++) {
                var idx = i * 2;
                var a = idx;
                var b = (idx + 1) % len;
                var c = (idx + 2) % len;
                var d = (idx + 3) % len;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(d);
                triangles.Add(b);
                triangles.Add(c);
            }

            if (hasCap) {
                GenerateCap(segments, radius, top, bottom, vertices, uv, normals, false);

                // center of top cap
                vertices.Add(new Vector3(0, top, 0));
                uv.Add(new Vector2(0.5f, 1f));
                normals.Add(Vector3.up);

                // center of bottom cap
                vertices.Add(new Vector3(0, bottom, 0));
                uv.Add(new Vector3(0.5f, 0f));
                normals.Add(Vector3.down);

                var it = vertices.Count - 2;
                var ib = vertices.Count - 1;
                var offset = len;

                for(var i=0; i < len; i += 2) {
                    triangles.Add(it);
                    triangles.Add((i + 2) % len + offset);
                    triangles.Add(i + offset);
                }
                
                for (var i = 1; i < len; i += 2) {
                    triangles.Add(ib);
                    triangles.Add(i + offset);
                    triangles.Add((i + 2) % len + offset);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }

        void GenerateCap(int segments, float radius, float top, float bottom, List<Vector3> vertices, List<Vector2> uv, List<Vector3> normals, bool side) {
            for(var i=0; i < segments; i++) {
                var ratio = (float) i / (segments);
                var rad = ratio * PI2;

                var x = Mathf.Cos(rad) * radius;
                var z = Mathf.Sin(rad) * radius;
                var tp = new Vector3(x, top, z);
                var bp = new Vector3(x, bottom, z);

                // top
                vertices.Add(tp);
                uv.Add(new Vector2(ratio, 1));

                // bottom
                vertices.Add(bp);
                uv.Add(new Vector2(ratio, 0));

                if (side) {
                    var normal = new Vector3(x, 0, z).normalized;
                    normals.Add(normal);
                    normals.Add(normal);
                } else {
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.down);
                }
            }
        }
    }
}