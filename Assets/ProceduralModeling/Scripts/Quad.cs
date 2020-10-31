using UnityEngine;

namespace ProceduralModeling {
    public class Quad : ProceduralModelingBase {

        [SerializeField, Range(0.1f, 10f)] protected float size = 1f;

        protected override Mesh Build() {
            var mesh = new Mesh();

            var hsize = size * 0.5f;

            var vertices = new Vector3[] {
                new Vector3(-hsize,  hsize, 0f),
                new Vector3( hsize,  hsize, 0f),
                new Vector3( hsize, -hsize, 0f),
                new Vector3(-hsize, -hsize, 0f)
            };

            var uv = new Vector2[] {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f)
            };

            var normals = new Vector3[] {
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f)
            };

            var triangles = new int[] {
                0,1,2,
                2,3,0
            };

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}