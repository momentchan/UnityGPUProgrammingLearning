using Common;
using UnityEngine;

namespace SpaceColonization {
    [RequireComponent(typeof(SpaceColonizationSimulator))]

    public class SpaceColonizationRenderer : MonoBehaviour {

        [SerializeField] Material mat;

        private Mesh mesh;
        private GPUDrawArgsBuffer drawBuffer;
        private SpaceColonizationSimulator simulator;
        private MaterialPropertyBlock block;

        void Start() {
            mesh = MeshUtil.CreateLine();
            block = new MaterialPropertyBlock();
            simulator = GetComponent<SpaceColonizationSimulator>();
        }

        void Update() {
            if (!mat || !simulator.IsReady) return;

            if (drawBuffer == null)
                drawBuffer = new GPUDrawArgsBuffer(mesh.GetIndexCount(0), (uint)simulator.BufferSize);

            block.SetBuffer(CSParams.Nodes, simulator.NodeBuffer);
            block.SetBuffer(CSParams.Edges, simulator.EdgeBuffer);
            block.SetInt(CSParams.EdgesCount, simulator.EdgesCount);
            block.SetMatrix(CSParams.Local2World, transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, new Bounds(Vector3.zero, Vector3.one * 100f), drawBuffer.Buffer, 0, block);
        }

        private void OnDestroy() {
            drawBuffer.Dispose();
        }
    }
}