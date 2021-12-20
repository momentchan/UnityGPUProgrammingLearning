using mj.gist;
using SpaceColonization.VolumeSampler;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceColonization {
    public class SkinnedSpaceColonizationSimulator : SpaceColonizationSimulatorBase {

        public override ComputeBuffer GetNodeBuffer() => nodeObjectPoolBuffer.ObjectBuffer;
        public override ComputeBuffer GetEdgeBuffer() => edgePoolBuffer;

        [SerializeField] private ComputeShader cs;
        [SerializeField] private Volume volume;
        [SerializeField] private SkinnedMeshRenderer skinnedRenderer;

        [Header("Seeds")]
        [SerializeField, Range(4, 16)] private int seedCount = 6;

        [SerializeField, Range(1, 5)] private int frame = 1;
        [SerializeField, Range(1, 5)] private int iterations = 1;
        [SerializeField, Range(0f, 1f)] private float massMin = 0.25f, massMax = 1f;

        [Header("Distances")]
        [SerializeField, Range(1f, 10f)] protected float unitScale = 1f;
        [SerializeField, Range(0.25f, 3f)] private float influenceDistance = 0.25f;
        [SerializeField, Range(0.25f, 1f)] private float growthDistance = 0.2f, killDistance = 0.2f;
        [SerializeField] private float growthSpeed = 22f;

        private ComputeKernel<Kernal> kernels;
        private enum Kernal { SetupSkin, SetupAttractions, SetupNodes, Seed, Search, Attract, Connect, Remove, Grow, Animate }

        private GPUObjectPool nodeObjectPoolBuffer;
        private ComputeBuffer attractionBuffer;
        private ComputeBuffer candidateBuffer;
        private ComputeBuffer edgePoolBuffer;
        private ComputeBuffer bindPoseBuffer;

        private ComputeBuffer poolArgsBuffer;
        private int[] poolArgs = new int[4] { 0, 1, 0, 0 };

        private float unitDistance;
        private SkinnedAttraction[] attractions;

        void Start() {
            kernels = new ComputeKernel<Kernal>(cs);
            poolArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            unitDistance = volume.UnitLength * unitScale;

            var bindPoses = skinnedRenderer.sharedMesh.bindposes;
            bindPoseBuffer = new ComputeBuffer(bindPoses.Length, Marshal.SizeOf(typeof(Matrix4x4)));
            bindPoseBuffer.SetData(bindPoses);

            attractions = GenerateSkinnedAttractions(volume);
            BufferSize = attractions.Length;
            attractionBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(SkinnedAttraction)), ComputeBufferType.Default);
            attractionBuffer.SetData(attractions);

            SetupSkin();
            Reset();
        }

        void Update() {
            if (Time.frameCount % frame != 0) return;

            for (int i = 0; i < iterations; i++)
                Step(Time.deltaTime);

            Animate();

            if (Input.GetKeyDown(KeyCode.R))
                Reset();
        }

        private SkinnedAttraction[] GenerateSkinnedAttractions(Volume volume) {
            var count = volume.Points.Count;
            var attractions = new SkinnedAttraction[count];
            for (var i = 0; i < count; i++) {
                var pos = volume.Points[i];

                attractions[i] = new SkinnedAttraction() {
                    position = pos,
                    bone = -1,
                    active = 1
                };
            }
            return attractions;
        }

        private void SetupSkin() {
            var mesh = skinnedRenderer.sharedMesh;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var indices = new int[weights.Length];
            for (var i = 0; i < weights.Length; i++)
                indices[i] = weights[i].boneIndex0;

            using (ComputeBuffer 
                   vertBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(Vector3))),
                   boneBuffer = new ComputeBuffer(weights.Length, sizeof(uint))) {

                vertBuffer.SetData(vertices);
                boneBuffer.SetData(indices);

                var kernel = kernels.GetKernelIndex(Kernal.SetupSkin);
                cs.SetBuffer(kernel, CSParams.Attractions, attractionBuffer);
                cs.SetBuffer(kernel, CSParams.Bones, boneBuffer);
                cs.SetBuffer(kernel, CSParams.Vertices, vertBuffer);
                ComputeShaderUtil.Dispatch1D(cs, kernel, attractionBuffer.count);
            }
        }

        private void Animate() {
            var bones = skinnedRenderer.bones.Select(b => b.localToWorldMatrix).ToArray();

            using (var bondMatrixBuffer = new ComputeBuffer(bones.Length, Marshal.SizeOf(typeof(Matrix4x4)))) {
                bondMatrixBuffer.SetData(bones);
                var kernel = kernels.GetKernelIndex(Kernal.Animate);
                cs.SetBuffer(kernel, CSParams.BindPoses, bindPoseBuffer);
                cs.SetBuffer(kernel, CSParams.BoneMatrices, bondMatrixBuffer);
                cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
                ComputeShaderUtil.Dispatch1D(cs, kernel, BufferSize);
            }
        }

        private void Reset() {
            if (IsReady)
                Release();

            nodeObjectPoolBuffer = new GPUObjectPool(BufferSize, typeof(SkinnedNode));
            nodeObjectPoolBuffer.ResetPoolCounter();

            candidateBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(SkinnedCandidate)), ComputeBufferType.Append);
            candidateBuffer.SetCounterValue(0);

            edgePoolBuffer = new ComputeBuffer(BufferSize * 2, Marshal.SizeOf(typeof(Edge)), ComputeBufferType.Append);
            edgePoolBuffer.SetCounterValue(0);

            SetupAttractions();

            var seeds = Enumerable.Range(0, seedCount).Select(_ => attractions[Random.Range(0, BufferSize)].position).ToArray();

            SetupNodePool();
            SetupNodeSeeds(seeds);

            CopyNodesCount();

            CopyEdgesCount();

            Step(0f);

            IsReady = true;
        }

        private void SetupAttractions() {
            var kernel = kernels.GetKernelIndex(Kernal.SetupAttractions);
            cs.SetBuffer(kernel, CSParams.Attractions, attractionBuffer);
            ComputeShaderUtil.Dispatch1D(cs, kernel, BufferSize);
        }

        private void SetupNodePool() {
            var kernel = kernels.GetKernelIndex(Kernal.SetupNodes);
            cs.SetBuffer(kernel, CSParams.NodesPoolAppend, nodeObjectPoolBuffer.PoolBuffer);
            cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
            ComputeShaderUtil.Dispatch1D(cs, kernel, BufferSize);
        }

        private void SetupNodeSeeds(Vector3[] seeds) {
            using (var seedBuffer = new ComputeBuffer(seeds.Length, Marshal.SizeOf(typeof(Vector3)))) {
                seedBuffer.SetData(seeds);
                var kernel = kernels.GetKernelIndex(Kernal.Seed);
                cs.SetFloat(CSParams.MassMin, massMin);
                cs.SetFloat(CSParams.MassMax, massMax);
                cs.SetBuffer(kernel, CSParams.Seeds, seedBuffer);
                cs.SetBuffer(kernel, CSParams.Attractions, attractionBuffer);
                cs.SetBuffer(kernel, CSParams.NodesPoolConsume, nodeObjectPoolBuffer.PoolBuffer);
                cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
                ComputeShaderUtil.Dispatch1D(cs, kernel, seedBuffer.count);
            }
        }

        private void CopyNodesCount() => NodesCount = nodeObjectPoolBuffer.CopyPoolSize();

        private void CopyEdgesCount() => EdgesCount = CopyCount(edgePoolBuffer);

        private int CopyCount(ComputeBuffer buffer) {
            poolArgsBuffer.SetData(poolArgs);
            ComputeBuffer.CopyCount(buffer, poolArgsBuffer, 0);
            poolArgsBuffer.GetData(poolArgs);
            return poolArgs[0];
        }

        private void Step(float dt) {
            if (NodesCount > 0) {
                Search();
                Attract();
                Connect();
                Remove();

                CopyEdgesCount();
                CopyNodesCount();
            }
            Grow(dt);
        }

        private void Search() {
            var kernel = kernels.GetKernelIndex(Kernal.Search);
            cs.SetFloat(CSParams.InfluenceDistance, unitDistance * influenceDistance);
            cs.SetBuffer(kernel, CSParams.Attractions, attractionBuffer);
            cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
            ComputeShaderUtil.Dispatch1D(cs, kernel, BufferSize);
        }

        private void Attract() {
            var kernel = kernels.GetKernelIndex(Kernal.Attract);
            cs.SetFloat(CSParams.GrowthDistance, unitDistance * growthDistance);

            cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
            cs.SetBuffer(kernel, CSParams.Attractions, attractionBuffer);

            candidateBuffer.SetCounterValue(0);
            cs.SetBuffer(kernel, CSParams.CandidatesAppend, candidateBuffer);
            ComputeShaderUtil.Dispatch1D(cs, kernel, BufferSize);
        }

        private void Connect() {
            var kernel = kernels.GetKernelIndex(Kernal.Connect);
            cs.SetFloat(CSParams.MassMin, massMin);
            cs.SetFloat(CSParams.MassMax, massMax);
            cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
            cs.SetBuffer(kernel, CSParams.NodesPoolConsume, nodeObjectPoolBuffer.PoolBuffer);
            cs.SetBuffer(kernel, CSParams.EdgesAppend, edgePoolBuffer);
            cs.SetBuffer(kernel, CSParams.CandidatesConsume, candidateBuffer);

            var connectCount = Mathf.Min(NodesCount, CopyCount(candidateBuffer));
            if (connectCount > 0) {
                cs.SetInt(CSParams.ConnectCount, connectCount);
                ComputeShaderUtil.Dispatch1D(cs, kernel, connectCount);
            }
        }

        private void Remove() {
            var kernel = kernels.GetKernelIndex(Kernal.Remove);
            cs.SetFloat(CSParams.KillDistance, unitDistance * killDistance);
            cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
            cs.SetBuffer(kernel, CSParams.Attractions, attractionBuffer);
            ComputeShaderUtil.Dispatch1D(cs, kernel, BufferSize);
        }

        private void Grow(float dt) {
            var kernel = kernels.GetKernelIndex(Kernal.Grow);
            var delta = dt * growthSpeed;
            cs.SetFloat(CSParams.DT, delta);
            cs.SetBuffer(kernel, CSParams.Nodes, nodeObjectPoolBuffer.ObjectBuffer);
            ComputeShaderUtil.Dispatch1D(cs, kernel, BufferSize);
        }

        void Release() {
            if (!IsReady) return;
            nodeObjectPoolBuffer.Dispose();
            candidateBuffer.Dispose();
            edgePoolBuffer.Dispose();
        }

        private void OnDestroy() {
            Release();
            attractionBuffer.Dispose();
            poolArgsBuffer.Dispose();
            bindPoseBuffer.Dispose();
        }

        private void OnDrawGizmosSelected() {
            if (!IsReady) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white;

            var nodes = new SkinnedNode[nodeObjectPoolBuffer.ObjectBuffer.count];
            nodeObjectPoolBuffer.ObjectBuffer.GetData(nodes);

            var edges = new Edge[edgePoolBuffer.count];
            edgePoolBuffer.GetData(edges);

            for(var i=0; i< edgePoolBuffer.count; i++) {
                var pa = nodes[edges[i].a].position;
                var pb = nodes[edges[i].b].position;
                Gizmos.DrawLine(pa, pb);
            }
        }
    }
}
