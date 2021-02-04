using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    public class MeshUtil {
        public static Mesh CreateQuad() {
            var mesh = new Mesh();

            mesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f), new Vector3( 0.5f,  0.5f, 0f)
            };
            mesh.uv = new Vector2[] {
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(1f, 0f), new Vector2(1f, 1f)
            };
            mesh.SetIndices(
                new int[] { 0, 1, 2, 1, 3, 2 },
                MeshTopology.Triangles,
                0
            );
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateLine() {
            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = new Vector3[] { Vector3.zero, Vector3.up };
            mesh.uv = new Vector2[] { new Vector2(0f, 0f), new Vector2(0f, 1f) };
            mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
            return mesh;
        }
    }
}