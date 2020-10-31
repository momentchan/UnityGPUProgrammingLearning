using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralModeling {
    public class Sphere : ProceduralModelingBase {
        [SerializeField, Range(10, 100)] protected int thetaSegments = 15, phiSegments = 15;
        [SerializeField, Range(0.5f, 5f)] protected float radius = 1f;
        const float PI2 = Mathf.PI * 2f;

        protected override Mesh Build() {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            for (var i = 0; i <= thetaSegments; i++) {
                for (var j = 0; j <= phiSegments; j++) {
                    var u = 1f * j / phiSegments;
                    var v = 1f * i / thetaSegments;

                    var phi = u * PI2 + PI2 * 0.5f;
                    var theta = v * PI2 * 0.5f;
                    float sinTheta = Mathf.Sin(theta), cosTheta = Mathf.Cos(theta);
                    float sinPhi = Mathf.Sin(phi), cosPhi = Mathf.Cos(phi);

                    var x = radius * sinTheta * cosPhi;
                    var y = radius * cosTheta;
                    var z = radius * sinTheta * sinPhi;

                    vertices.Add(new Vector3(x, y, z));
                    normals.Add(new Vector3(x, y, z).normalized);
                    uv.Add(new Vector2(u, 1 - v));
                }
            }

            for (var i = 0; i < thetaSegments; i++) {
                for (var j = 0; j < phiSegments; j++) {
                    var a = (phiSegments + 1) * i + j;
                    var b = (phiSegments + 1) * i + j + 1;
                    var c = (phiSegments + 1) * (i + 1) + j;
                    var d = (phiSegments + 1) * (i + 1) + j + 1;

                    triangles.Add(a); triangles.Add(b); triangles.Add(d);
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);   
                }
            }

            var mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = triangles.ToArray();

            return mesh;
        }
    }
}