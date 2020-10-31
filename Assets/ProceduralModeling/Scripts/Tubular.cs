using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralModeling {
    public class Tubular : ProceduralModelingBase {

        [SerializeField] protected CatmullRomCurve curve;

        [SerializeField, Range(2, 50)] protected int tubularSegments = 20, radialSegments = 8;
        [SerializeField, Range(0.1f, 5f)] protected float radius = 0.5f;
        [SerializeField] protected bool closed = false;

        const float PI2 = Mathf.PI * 2f;

        protected override Mesh Build() {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            var frames = curve.ComputeFrenetFrames(tubularSegments, closed);

            for(var i=0; i<tubularSegments; i++) {
                GenerateSegments(curve, frames, vertices, normals, tangents, i);
            }

            // last segment
            GenerateSegments(curve, frames, vertices, normals, tangents, closed ? 0 : tubularSegments);

            for (var i = 0; i <= tubularSegments; i++) {
                for (var j = 0; j <= radialSegments; j++) {
                    var u = 1f * j / radialSegments;
                    var v = 1f * i / tubularSegments;
                    uv.Add(new Vector2(u, v));
                }
            }

            for (var i = 0; i < tubularSegments; i++) {
                for (var j = 0; j < radialSegments; j++) {
                    var a = (radialSegments+1) * i + j;
                    var b = (radialSegments + 1) * i + j + 1;
                    var c = (radialSegments + 1) * (i + 1) + j;
                    var d = (radialSegments + 1) * (i + 1) + j + 1;

                    triangles.Add(a); triangles.Add(b); triangles.Add(d);
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);
                }
            }

            var mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.tangents = tangents.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = triangles.ToArray();

            return mesh;
        }

        private void GenerateSegments(CatmullRomCurve curve, List<FrenetFrame> frames, List<Vector3> vertices, List<Vector3> normals, List<Vector4> tangents, int index) {
            var u = 1f * index / tubularSegments;
            var p = curve.GetPointAt(u);
            var frame = frames[index];

            var N = frame.Normal;
            var B = frame.Binormal;
            var T = frame.Tangent;

            for(var i = 0; i <= radialSegments; i++) {
                var rad = 1f * i / radialSegments * PI2;

                float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                var v = (cos * N + sin * B).normalized;

                vertices.Add(p + v * radius);
                normals.Add(v);
                tangents.Add(T);
            }
        }
        

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            DrawCurve();
            DrawFrenetFrames();
        }
        private void DrawCurve() {
            const float size = 0.025f;

            Gizmos.matrix = transform.localToWorldMatrix;
            var frames = curve.ComputeFrenetFrames(tubularSegments, closed);
            for (int i = 0, n = tubularSegments; i < n; i++) {
                var u0 = 1f * i / tubularSegments;
                var p0 = curve.GetPointAt(u0);

                if (i < n - 1) {
                    var u1 = 1f * (i + 1) / tubularSegments;
                    var p1 = curve.GetPointAt(u1);
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(p0, p1);
                }

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(p0, size);

                var frame = frames[i];
                var N = frame.Normal;
                var B = frame.Binormal;

                Gizmos.color = Color.yellow;
                var radius = size * 4;
                for (int j = 0; j <= radialSegments; j++) {
                    // 0 ~ 2pi
                    float rad0 = 1f * j / radialSegments * PI2;
                    float rad1 = 1f * (j + 1) / radialSegments * PI2;

                    float cos0 = Mathf.Cos(rad0), sin0 = Mathf.Sin(rad0);
                    float cos1 = Mathf.Cos(rad1), sin1 = Mathf.Sin(rad1);

                    var normal0 = (cos0 * N + sin0 * B).normalized;
                    var normal1 = (cos1 * N + sin1 * B).normalized;
                    var v0 = (p0 + radius * normal0);
                    var v1 = (p0 + radius * normal1);
                    Gizmos.DrawLine(v0, v1);
                }

            }

        }

        private void DrawFrenetFrames() {
            const float size = 0.05f;

            Handles.matrix = transform.localToWorldMatrix;
            var frames = curve.ComputeFrenetFrames(tubularSegments, closed);
            for (int i = 0, n = tubularSegments; i < n; i++) {
                var u = 1f * i / tubularSegments;
                var p = curve.GetPointAt(u);
                var frame = frames[i];

                Handles.color = Color.white;
                Handles.RectangleHandleCap(0, p, Quaternion.LookRotation(frame.Tangent), size * 2f, EventType.Repaint);

                Handles.color = Color.red;
                Handles.ArrowHandleCap(0, p, Quaternion.LookRotation(frame.Tangent), size, EventType.Repaint);

                Handles.color = Color.green;
                Handles.ArrowHandleCap(0, p, Quaternion.LookRotation(frame.Normal), size, EventType.Repaint);

                Handles.color = Color.blue;
                Handles.ArrowHandleCap(0, p, Quaternion.LookRotation(frame.Binormal), size, EventType.Repaint);

            }
        }
#endif
    }
}