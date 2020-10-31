using UnityEngine;

namespace Voxelizer {
    public class GPUVoxelizerAnimTest : MonoBehaviour {

        [SerializeField] protected ComputeShader voxelizer;
        [SerializeField] protected SkinnedMeshRenderer skinned;
        [SerializeField] protected int resolution = 64;

        protected Mesh mesh;
        protected GPUVoxelData data;
        protected MeshFilter filter;

        void Start() {
            filter = GetComponent<MeshFilter>();
        }
        void Update() {
            Sample();
        }

        private void Sample() {
            if (mesh == null)
                mesh = new Mesh();

            skinned.BakeMesh(mesh);

            if (data != null) {
                data.Dispose();
                data = null;
            }

            data = GPUVoxelizer.Voxelize(voxelizer, mesh, resolution);

            var voxels = data.GetData();
            filter.sharedMesh = VoxelMesh.Build(voxels, data.UnitLength);
            data.Dispose();
        }
    }
}