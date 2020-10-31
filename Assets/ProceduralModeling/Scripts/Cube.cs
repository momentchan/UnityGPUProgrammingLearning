using System.Collections.Generic;
using UnityEngine;

namespace ProceduralModeling {
    public class Cube : ProceduralModelingBase {
        [SerializeField, Range(0.5f, 5f)] protected float width = 1f, height = 1f, depth = 1f;

        protected override Mesh Build() {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            float hw = width * 0.5f,
                  hh = height * 0.5f,
                  hd = depth * 0.5f;

            // Up
            vertices.Add(new Vector3(-hw, hh, -hd));
            vertices.Add(new Vector3(-hw, hh, hd));
            vertices.Add(new Vector3(hw, hh, hd));
            vertices.Add(new Vector3(hw, hh, -hd));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.up);

            // Down
            vertices.Add(new Vector3(-hw, -hh, hd));
            vertices.Add(new Vector3(-hw, -hh, -hd));
            vertices.Add(new Vector3(hw, -hh, -hd));
            vertices.Add(new Vector3(hw, -hh, hd));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.down);

            // Left
            vertices.Add(new Vector3(-hw, -hh, hd));
            vertices.Add(new Vector3(-hw, hh, hd));
            vertices.Add(new Vector3(-hw, hh, -hd));
            vertices.Add(new Vector3(-hw, -hh, -hd));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.left);

            // Right
            vertices.Add(new Vector3(hw, -hh, -hd));
            vertices.Add(new Vector3(hw, hh, -hd));
            vertices.Add(new Vector3(hw, hh, hd));
            vertices.Add(new Vector3(hw, -hh, hd));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.right);

            // Forward
            vertices.Add(new Vector3(hw, -hh, hd));
            vertices.Add(new Vector3(hw, hh, hd));
            vertices.Add(new Vector3(-hw, hh, hd));
            vertices.Add(new Vector3(-hw, -hh, hd));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.forward);

            // Back
            vertices.Add(new Vector3(-hw, -hh, -hd));
            vertices.Add(new Vector3(-hw, hh, -hd));
            vertices.Add(new Vector3(hw, hh, -hd));
            vertices.Add(new Vector3(hw, -hh, -hd));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.back);

            for(var i = 0; i < 6; i++) {
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(0, 1));
                uv.Add(new Vector2(1, 1));
                uv.Add(new Vector2(1, 0));

                var a = i * 4 + 0;
                var b = i * 4 + 1;
                var c = i * 4 + 2;
                var d = i * 4 + 3;

                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
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