using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Common;

namespace Voxelizer {
    public class GPUVoxelParticleSystem : MonoBehaviour {

        #region Shader property keys
        protected const string VoxelBufferKey = "_VoxelBuffer", VoxelCountKey = "_VoxelCount";
        protected const string ParticleBufferKey = "_ParticleBuffer", ParticleCountKey = "_ParticleCount";
        #endregion

        [SerializeField] protected ComputeShader voxelizer, particleCompute;
        [SerializeField] protected SkinnedMeshRenderer skinned;
        [SerializeField] protected Material material;
        [SerializeField] protected ShadowCastingMode castShadows = ShadowCastingMode.On;
        [SerializeField] protected bool receiveShadow = true;
        [SerializeField] protected int count = 65000;
        [SerializeField, Range(32, 256)] protected int resolution = 64;

        #region Particle properties
        [SerializeField] protected float speedScaleMin = 1.0f, speedScaleMax = 2.5f;
        [SerializeField] protected Vector3 gravity = Vector3.down;
        [SerializeField, Range(0.5f, 1f)] protected float decay = 0.925f;
        #endregion

        protected Mesh mesh, cube;
        protected ComputeBuffer argsBuffer, particleBuffer;
        protected Bounds bounds;
        protected GPUVoxelData data;
        protected uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        protected Kernel setupKer, updateKer;

        #region MonoBehaviour
        void Start() {
            mesh = new Mesh();
            Sample();
            cube = VoxelMesh.BuildSingleCube(data.UnitLength);

            args[0] = cube.GetIndexCount(0);
            args[1] = (uint)count;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            particleBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(VoxelParticle_t)));

            setupKer = new Kernel(particleCompute, "Setup");
            updateKer = new Kernel(particleCompute, "Update");

            Setup();
        }
        void Update() {
            Sample();
            Compute(updateKer, Time.deltaTime);

            material.SetBuffer(ParticleBufferKey, particleBuffer);
            material.SetMatrix("_WorldToLocal", transform.worldToLocalMatrix);
            material.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(cube, 0, material, new Bounds(Vector3.zero, Vector3.one*100f), argsBuffer, 0, null, castShadows, receiveShadow);
        }
        void OnDestroy() {
            if (data != null) {
                data.Dispose();
                data = null;
            }

            if (argsBuffer != null) {
                argsBuffer.Release();
                argsBuffer = null;
            }

            if (particleBuffer != null) {
                particleBuffer.Release();
                particleBuffer = null;
            }
        }
        #endregion

        void Setup() {
            particleCompute.SetBuffer(setupKer.Index, VoxelBufferKey, data.Buffer);
            particleCompute.SetInt(VoxelCountKey, data.Buffer.count);
            particleCompute.SetBuffer(setupKer.Index, ParticleBufferKey, particleBuffer);
            particleCompute.SetInt(ParticleCountKey, particleBuffer.count);
            particleCompute.SetFloat("_Width", data.Width);
            particleCompute.SetFloat("_Height", data.Height);
            particleCompute.SetFloat("_Depth", data.Depth);
            particleCompute.SetVector("_Speed", new Vector2(speedScaleMin, speedScaleMax));

            particleCompute.Dispatch(setupKer.Index, particleBuffer.count / (int)setupKer.ThreadX + 1, (int)setupKer.ThreadY, (int)setupKer.ThreadZ);
        }
        
        void Compute(Kernel kernel, float dt) {
            particleCompute.SetBuffer(kernel.Index, VoxelBufferKey, data.Buffer);
            particleCompute.SetInt(VoxelCountKey, data.Buffer.count);
            particleCompute.SetBuffer(kernel.Index, ParticleBufferKey, particleBuffer);
            particleCompute.SetInt(ParticleCountKey, particleBuffer.count);

            particleCompute.SetVector("_DT", new Vector2(dt, 1/dt));
            particleCompute.SetVector("_Gravity", gravity);
            particleCompute.SetFloat("_Decay", decay);

            particleCompute.Dispatch(kernel.Index, particleBuffer.count / (int)kernel.ThreadX + 1, (int)kernel.ThreadY, (int)kernel.ThreadZ);
        }

        protected void Sample() {
            skinned.BakeMesh(mesh);

            bounds.Encapsulate(mesh.bounds.min);
            bounds.Encapsulate(mesh.bounds.max);

            if (data != null) {
                data.Dispose();
                data = null;
            }

            data = GPUVoxelizer.Voxelize(voxelizer, mesh, resolution);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelParticle_t {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Vector3 velocity;
        public float speed;
        public float size;
        public float lifetime;
    };
}