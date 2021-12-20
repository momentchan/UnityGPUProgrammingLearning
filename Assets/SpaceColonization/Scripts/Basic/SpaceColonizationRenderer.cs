using mj.gist;
using UnityEngine;

namespace SpaceColonization {
    public class SpaceColonizationRenderer : MonoBehaviour {

        [SerializeField] Material mat;

        private Mesh mesh;
        private GPUDrawArgsBuffer drawBuffer;
        private SpaceColonizationSimulatorBase simulator;
        private MaterialPropertyBlock block;

        void Start() {
            mesh = MeshUtil.CreateLine();
            block = new MaterialPropertyBlock();
            simulator = GetComponent<SpaceColonizationSimulatorBase>();
        }

        void Update() {
            if (!mat || !simulator.IsReady) return;

            if (drawBuffer == null)
                drawBuffer = new GPUDrawArgsBuffer(mesh.GetIndexCount(0), (uint)simulator.BufferSize);

            block.SetBuffer(CSParams.Nodes, simulator.GetNodeBuffer());
            block.SetBuffer(CSParams.Edges, simulator.GetEdgeBuffer());
            block.SetInt(CSParams.EdgesCount, simulator.EdgesCount);
            block.SetMatrix(CSParams.Local2World, transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, new Bounds(Vector3.zero, Vector3.one * 100f), drawBuffer.Buffer, 0, block);
        }

        private void OnDestroy() {
            drawBuffer.Dispose();
        }
    }
}