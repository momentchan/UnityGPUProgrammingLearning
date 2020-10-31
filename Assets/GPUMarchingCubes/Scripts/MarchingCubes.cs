using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUMarchingCubes {
    // Reminder: change camera to deferred
    public class MarchingCubes : MonoBehaviour {


        [SerializeField] protected int segmentNum = 32;
        [SerializeField, Range(0, 1)] protected float threshold = 0.5f;
        [SerializeField] protected Material mat;

        [SerializeField] protected Color diffuseColor = Color.green;
        [SerializeField] protected Color emissionColor = Color.black;
        [SerializeField] protected float emissionIntensity = 0;

        [SerializeField, Range(0, 1)] protected float metallic = 0;
        [SerializeField, Range(0, 1)] protected float glossiness = 0.5f;

        private int vertexNum = 0;
        private float renderScale = 0;
        private Mesh[] meshes;
        private Material[] materials;
        MarchingCubesDefines mcDefines;

        #region Unity Built-In
        void Start() {
            Initialize();
        }
        void Update() {
            RenderMesh();
        }

        void OnDestroy() {
            mcDefines.ReleaseBuffer();
        }

        private void OnDrawGizmos() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
        #endregion

        private void Initialize() {
            vertexNum = segmentNum * segmentNum * segmentNum;
            renderScale = 1f / segmentNum;
            CreateMesh();
            mcDefines = new MarchingCubesDefines();
        }

        private void CreateMesh() {
            int vertexMaxNum = 65535;
            int meshNum = Mathf.CeilToInt(vertexNum * 1.0f / vertexMaxNum);

            meshes = new Mesh[meshNum];
            materials = new Material[meshNum];

            Bounds bounds = 
                new Bounds(transform.position, 
                new Vector3(segmentNum, segmentNum, segmentNum) * renderScale);

            var id = 0;
            for(var i = 0; i < meshNum; i++) {
                Vector3[] vertices = new Vector3[vertexMaxNum];
                int[] indices = new int[vertexMaxNum];

                for(var j=0; j<vertexMaxNum; j++) {
                    vertices[j] = new Vector3(id % segmentNum,
                                             (id / segmentNum) % segmentNum,
                                             (id / (segmentNum * segmentNum)) % segmentNum);
                    indices[j] = j;
                    id++;
                }
                meshes[i] = new Mesh();
                meshes[i].vertices = vertices;
                meshes[i].SetIndices(indices, MeshTopology.Points, 0);
                meshes[i].bounds = bounds;
                materials[i] = new Material(mat);
            }
        }

        private void RenderMesh() {
            Vector3 halfSize = new Vector3(segmentNum, segmentNum, segmentNum) * renderScale * 0.5f;
            Matrix4x4 trs = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

            for(var i = 0; i < meshes.Length; i++) {
                materials[i].SetPass(0);

                materials[i].SetInt("_SegmentNum", segmentNum);
                materials[i].SetFloat("_Scale", renderScale);
                materials[i].SetFloat("_Threshold", threshold);
                materials[i].SetFloat("_Metallic", metallic);
                materials[i].SetFloat("_Glossiness", glossiness);
                materials[i].SetFloat("_EmissionIntensity", emissionIntensity);

                materials[i].SetVector("_HalfSize", halfSize);
                materials[i].SetColor("_DiffuseColor", diffuseColor);
                materials[i].SetColor("_EmissionColor", emissionColor);
                materials[i].SetMatrix("_Matrix", trs);

                // Set buffer
                materials[i].SetBuffer("vertexOffset", mcDefines.VertexOffsetBuffer);
                materials[i].SetBuffer("cubeEdgeFlags", mcDefines.CubeEdgeFlagsBuffer);
                materials[i].SetBuffer("edgeConnection", mcDefines.EdgeConnectionBuffer);
                materials[i].SetBuffer("edgeDirection", mcDefines.EdgeDirectionBuffer);
                materials[i].SetBuffer("triangleConnectionTable", mcDefines.TriangleConnectionTableBuffer);

                Graphics.DrawMesh(meshes[i], Matrix4x4.identity, materials[i], 0);
            }
        }
    }
}