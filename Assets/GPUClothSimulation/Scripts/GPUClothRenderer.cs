using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUClothSimulation {
    public class GPUClothRenderer : MonoBehaviour {
        [SerializeField] private Material material;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private GPUClothSimulator simulator;
        private bool IsInit = false;

        void Update() {
            if (!IsInit) {
                simulator = GetComponent<GPUClothSimulator>();

                if (simulator && simulator.IsInit) {
                    meshRenderer = PrepareMeshRenderer();
                    meshRenderer.material = material;
                    material.SetTexture("_PositionTex", simulator.GetPositionBuffer());
                    material.SetTexture("_NormalTex", simulator.GetNormalBuffer());

                    meshFilter = PrepareMeshFilter();
                    meshFilter.mesh = CreateClothMesh();

                    IsInit = true;
                }
            }
        }

        private Mesh CreateClothMesh() {
            var clothResolution = simulator.GetClothResolution();
            var gridWidth = clothResolution.x - 1;
            var gridHeight = clothResolution.y - 1;

            var mesh = new Mesh();

            var tileSizeX = 1.0f / gridWidth;
            var tileSizeY = 1.0f / gridHeight;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();

            var index = 0;
            for (var x = 0; x < gridWidth; x++) {
                for (var y = 0; y < gridHeight; y++) {
                    vertices.Add(new Vector3((x + 0) * tileSizeX, (y + 0) * tileSizeY, 0));
                    vertices.Add(new Vector3((x + 1) * tileSizeX, (y + 0) * tileSizeY, 0));
                    vertices.Add(new Vector3((x + 1) * tileSizeX, (y + 1) * tileSizeY, 0));
                    vertices.Add(new Vector3((x + 0) * tileSizeX, (y + 1) * tileSizeY, 0));

                    triangles.Add(index + 2);
                    triangles.Add(index + 1);
                    triangles.Add(index);
                    triangles.Add(index);
                    triangles.Add(index + 3);
                    triangles.Add(index + 2);
                    index += 4;

                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);

                    uv.Add(new Vector2((x + 0) * tileSizeX, (y + 0) * tileSizeY));
                    uv.Add(new Vector2((x + 1) * tileSizeX, (y + 0) * tileSizeY));
                    uv.Add(new Vector2((x + 1) * tileSizeX, (y + 1) * tileSizeY));
                    uv.Add(new Vector2((x + 0) * tileSizeX, (y + 1) * tileSizeY));
                }
            }
            // make the count of vertices be able to be more than 65535
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();

            mesh.bounds = new Bounds(transform.position, Vector3.one * 1000f);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.MarkDynamic();
            mesh.name = $"Grid_{clothResolution.x}_{clothResolution.y}";

            return mesh;
        }
        private MeshRenderer PrepareMeshRenderer() {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (!meshRenderer)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            return meshRenderer;
        }

        private MeshFilter PrepareMeshFilter() {
            var meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            return meshFilter;
        }
    }
}