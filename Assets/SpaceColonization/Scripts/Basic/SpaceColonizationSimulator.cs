using Common;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceColonization {
    public static class CSParams {
        public static readonly int Nodes = Shader.PropertyToID("_Nodes");
        public static readonly int NodesPoolAppend = Shader.PropertyToID("_NodesPoolAppend");
        public static readonly int NodesPoolConsume = Shader.PropertyToID("_NodesPoolConsume");

        public static readonly int Edges = Shader.PropertyToID("_Edges");
        public static readonly int EdgesAppend = Shader.PropertyToID("_EdgesAppend");
        public static readonly int EdgesConsume = Shader.PropertyToID("_EdgesConsume");

        public static readonly int CandidatesAppend = Shader.PropertyToID("_CandidatesAppend");
        public static readonly int CandidatesConsume = Shader.PropertyToID("_CandidatesConsume");

        public static readonly int Attractions = Shader.PropertyToID("_Attractions");
        public static readonly int Seeds = Shader.PropertyToID("_Seeds");

        public static readonly int ConnectCount = Shader.PropertyToID("_ConnectCount");
        public static readonly int EdgesCount = Shader.PropertyToID("_EdgesCount");

        public static readonly int MassMin = Shader.PropertyToID("_MassMin");
        public static readonly int MassMax = Shader.PropertyToID("_MassMax");

        public static readonly int InfluenceDistance = Shader.PropertyToID("_InfluenceDistance");
        public static readonly int GrowthDistance = Shader.PropertyToID("_GrowthDistance");
        public static readonly int KillDistance = Shader.PropertyToID("_KillDistance");

        public static readonly int DT = Shader.PropertyToID("_DT");
        public static readonly int Local2World = Shader.PropertyToID("_Local2World");

        // Skinned
        public static readonly int Bones = Shader.PropertyToID("_Bones");
        public static readonly int Vertices = Shader.PropertyToID("_Vertices");
        public static readonly int BindPoses = Shader.PropertyToID("_BindPoses");
        public static readonly int BoneMatrices = Shader.PropertyToID("_BoneMatrices");
    }

    public class SpaceColonizationSimulator : SpaceColonizationSimulatorBase {

        public override ComputeBuffer GetNodeBuffer() => nodeObjectPoolBuffer.ObjectBuffer;
        public override ComputeBuffer GetEdgeBuffer() => edgePoolBuffer;

        [SerializeField] private ComputeShader cs;

        [Header("Seeds")]
        [SerializeField, Range(8, 32)] private int side = 16;
        [SerializeField, Range(4, 16)] private int seedCount = 6;

        [SerializeField, Range(1, 5)] private int frame = 1;
        [SerializeField, Range(1, 5)] private int iterations = 1;
        [SerializeField, Range(0f, 1f)] private float massMin = 0.25f, massMax = 1f;

        [Header("Distances")]
        [SerializeField, Range(0.25f, 3f)] private float influenceDistance = 0.25f;
        [SerializeField, Range(0.25f, 1f)] private float growthDistance = 0.2f, killDistance = 0.2f;
        [SerializeField] private float growthSpeed = 22f;

        private ComputeKernel<Kernal> kernels;
        private enum Kernal { Setup, Seed, Search, Attract, Connect, Remove, Grow }

        private GPUObjectPool nodeObjectPoolBuffer;
        private ComputeBuffer attractionBuffer;
        private ComputeBuffer candidateBuffer;
        private ComputeBuffer edgePoolBuffer;

        private ComputeBuffer poolArgsBuffer;
        private int[] poolArgs = new int[4] { 0, 1, 0, 0 };

        private float unitDistance;

        void Start() {
            kernels = new ComputeKernel<Kernal>(cs);
            poolArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            unitDistance = Mathf.Min(Mathf.Min(1f / side, 1f / side), 1f / side) * 2f;
            Reset();
        }

        void Update() {
            if (Time.frameCount % frame != 0) return;

            for (int i = 0; i < iterations; i++)
                Step(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.R))
                Reset();
        }

        private void Reset() {
            if (IsReady)
                Release();

            var attractions = GenerateSphereAttractions();
            BufferSize = attractions.Length;

            attractionBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Attraction)), ComputeBufferType.Default);
            attractionBuffer.SetData(attractions);

            nodeObjectPoolBuffer = new GPUObjectPool(BufferSize, typeof(Node));
            nodeObjectPoolBuffer.ResetPoolCounter();

            candidateBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Candidate)), ComputeBufferType.Append);
            candidateBuffer.SetCounterValue(0);

            edgePoolBuffer = new ComputeBuffer(BufferSize * 2, Marshal.SizeOf(typeof(Edge)), ComputeBufferType.Append);
            edgePoolBuffer.SetCounterValue(0);

            var seeds = Enumerable.Range(0, seedCount).Select(_ => attractions[Random.Range(0, BufferSize)].position).ToArray();

            SetupNodePool();
            SetupNodeSeeds(seeds);

            CopyNodesCount();
            CopyEdgesCount();

            Step(0f);

            IsReady = true;
        }

        private Attraction[] GenerateSphereAttractions() {
            float invW = 1f / (side - 1), invH = 1f / (side - 1), invD = 1f / (side - 1);
            var offset = -new Vector3(0.5f, 0.5f, 0.5f);
            var scale = new Vector3(invW, invH, invD);

            var attractions = new List<Attraction>();
            for (var x = 0; x < side; x++) {
                for (var y = 0; y < side; y++) {
                    for (var z = 0; z < side; z++) {
                        var pos = Vector3.Scale(new Vector3(x, y, z), scale) + offset;

                        if (pos.sqrMagnitude >= 0.25f) continue;

                        attractions.Add(new Attraction() {
                            position = pos + Vector3.Scale(new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)), scale),
                            nearestIndex = 0,
                            found = 0,
                            active = 1
                        });
                    }
                }
            }
            return attractions.ToArray();
        }

        private void SetupNodePool() {
            var kernel = kernels.GetKernelIndex(Kernal.Setup);
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
            attractionBuffer.Dispose();
            nodeObjectPoolBuffer.Dispose();
            candidateBuffer.Dispose();
            edgePoolBuffer.Dispose();
        }

        private void OnDestroy() {
            Release();
            poolArgsBuffer.Dispose();
        }
    }
}