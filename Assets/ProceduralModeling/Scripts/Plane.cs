using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralModeling {
    public class Plane : ProceduralModelingBase {

        [SerializeField, Range(2, 30)] protected int widthSegments = 8; 
        [SerializeField, Range(2, 30)] protected int heightSegments = 8;

        [SerializeField, Range(0.1f, 10f)] protected float width = 1f;
        [SerializeField, Range(0.1f, 10f)] protected float height = 1f;

        protected override Mesh Build() {
            var mesh = new Mesh();

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var normals = new List<Vector3>();

            float hinv = 1.0f / (heightSegments - 1);
            float winv = 1.0f / (widthSegments - 1);

            for(var y=0; y< heightSegments; y++) {
                var ry = y * hinv;

                for(var x =0; x < widthSegments; x++) {
                    var rx = x * winv;

                    vertices.Add(new Vector3(
                        (rx-0.5f) * width, 
                        0f, 
                        (0.5f-ry) * height
                    ));
                    uv.Add(new Vector2(rx, ry));
                    normals.Add(Vector3.up);
                }
            }

            var triangles = new List<int>();

            for (var y = 0; y < heightSegments-1; y++) {
                for (var x = 0; x < widthSegments-1; x++) {
                    int index = y * widthSegments + x;
                    var a = index;
                    var b = index + 1;
                    var c = index + 1 + widthSegments;
                    var d = index + widthSegments;

                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);


                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}

