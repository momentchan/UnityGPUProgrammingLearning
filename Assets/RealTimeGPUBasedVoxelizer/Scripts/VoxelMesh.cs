using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Voxelizer {
    public class VoxelMesh {
        internal static Mesh Build(Voxel_t[] voxels, float size) {
            var hsize = size * 0.5f;
            var forward = Vector3.forward * hsize;
            var back = -forward;
            var right = Vector3.right * hsize;
            var left = -right;
            var up = Vector3.up * hsize;
            var down = -up;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            for (var i = 0; i < voxels.Length; i++) {
                if (voxels[i].fill == 0) continue;

                var p = voxels[i].position;
                var corners = new Vector3[8] {
                    p + forward + left  + up,
                    p + back    + left  + up,
                    p + back    + right + up,
                    p + forward + right + up,

                    p + forward + left  + down,
                    p + back    + left  + down,
                    p + back    + right + down,
                    p + forward + right + down
                };
                // Up
                AddTriangle(corners[0], corners[3], corners[1], up, vertices, normals, triangles);
                AddTriangle(corners[1], corners[3], corners[2], up, vertices, normals, triangles);

                // Down
                AddTriangle(corners[4], corners[5], corners[7], down, vertices, normals, triangles);
                AddTriangle(corners[5], corners[6], corners[7], down, vertices, normals, triangles);

                // Right
                AddTriangle(corners[2], corners[3], corners[6], right, vertices, normals, triangles);
                AddTriangle(corners[6], corners[3], corners[7], right, vertices, normals, triangles);

                // Left
                AddTriangle(corners[0], corners[1], corners[5], left, vertices, normals, triangles);
                AddTriangle(corners[0], corners[5], corners[4], left, vertices, normals, triangles);

                // Forward
                AddTriangle(corners[0], corners[4], corners[3], forward, vertices, normals, triangles);
                AddTriangle(corners[3], corners[4], corners[7], forward, vertices, normals, triangles);

                // Back
                AddTriangle(corners[1], corners[2], corners[5], back, vertices, normals, triangles);
                AddTriangle(corners[5], corners[2], corners[6], back, vertices, normals, triangles);
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.indexFormat = (vertices.Count <= 65535) ? IndexFormat.UInt16 : IndexFormat.UInt32;
            mesh.SetNormals(normals);
            mesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh BuildSingleCube(float size) {
            var hsize = size * 0.5f;
            var forward = Vector3.forward * hsize;
            var back = -forward;
            var right = Vector3.right * hsize;
            var left = -right;
            var up = Vector3.up * hsize;
            var down = -up;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            var corners = new Vector3[8] {
                forward + left  + up,
                back    + left  + up,
                back    + right + up,
                forward + right + up,

                forward + left  + down,
                back    + left  + down,
                back    + right + down,
                forward + right + down
            };
            // Up
            AddTriangle(corners[0], corners[3], corners[1], up, vertices, normals, triangles);
            AddTriangle(corners[1], corners[3], corners[2], up, vertices, normals, triangles);

            // Down
            AddTriangle(corners[4], corners[5], corners[7], down, vertices, normals, triangles);
            AddTriangle(corners[5], corners[6], corners[7], down, vertices, normals, triangles);

            // Right
            AddTriangle(corners[2], corners[3], corners[6], right, vertices, normals, triangles);
            AddTriangle(corners[6], corners[3], corners[7], right, vertices, normals, triangles);

            // Left
            AddTriangle(corners[0], corners[1], corners[5], left, vertices, normals, triangles);
            AddTriangle(corners[0], corners[5], corners[4], left, vertices, normals, triangles);

            // Forward
            AddTriangle(corners[0], corners[4], corners[3], forward, vertices, normals, triangles);
            AddTriangle(corners[3], corners[4], corners[7], forward, vertices, normals, triangles);

            // Back
            AddTriangle(corners[1], corners[2], corners[5], back, vertices, normals, triangles);
            AddTriangle(corners[5], corners[2], corners[6], back, vertices, normals, triangles);

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal, List<Vector3> vertices, List<Vector3> normals, List<int> triangles) {
            int i = vertices.Count;
            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2);
            normals.Add(normal); normals.Add(normal); normals.Add(normal);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        }
    }
}