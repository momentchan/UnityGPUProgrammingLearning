using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BoidsSimulationOnGPU {
    public class GPUBoids : MonoBehaviour {
        struct BoidData {
            public Vector3 position;
            public Vector3 velocity;
        };

        public ComputeShader BoidsCS;

        const int SIMULATION_BLOCK_SIZE = 256;

        [SerializeField, Range(256, 32768)] protected int maxObjectNum = 16384;

        [SerializeField, Range(0f, 3f)]  protected float cohesionNeighborhoodRadius = 2.0f;
        [SerializeField, Range(0f, 3f)]  protected float alignmentNeighborhoodRadius = 2.0f;
        [SerializeField, Range(0f, 3f)]  protected float separateNeighborhoodRadius = 1.0f;

        [SerializeField, Range(0f, 3f)]  protected float cohesionWeight = 1.0f;
        [SerializeField, Range(0f, 3f)]  protected float alignmentWeight = 1.0f;
        [SerializeField, Range(0f, 3f)]  protected float separateWeight = 3.0f;

        [SerializeField, Range(0f, 10f)] protected float maxSpeed = 5.0f;
        [SerializeField, Range(0f, 3f)]  protected float maxSteerForce = 0.5f;

        [SerializeField, Range(5f, 20f)] protected float avoidWallWeight = 10.0f;

        [SerializeField] protected Vector3 wallCenter = Vector3.zero;
        [SerializeField] protected Vector3 wallSize = new Vector3(32.0f, 32.0f, 32.0f);

        ComputeBuffer _boidForceBuffer;
        ComputeBuffer _boidDataBuffer;

        public int GetMaxObjectNum() => maxObjectNum;
        public ComputeBuffer GetBoidDataBuffer() => _boidDataBuffer;
        public Vector3 GetSimulationAreaCenter() =>  wallCenter;
        public Vector3 GetSimulationAreaSize() => wallSize;

        void Start() {
            InitBuffer();     
        }
        void Update() {
            Simulation();
        }
        void OnDestroy() {
            ReleaseBuffer();
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wallCenter, wallSize);
        }

        void InitBuffer() {
            _boidForceBuffer = new ComputeBuffer(maxObjectNum, Marshal.SizeOf(typeof(Vector3)));
            _boidDataBuffer = new ComputeBuffer(maxObjectNum, Marshal.SizeOf(typeof(BoidData)));

            var forceArr = new Vector3[maxObjectNum];
            var boidDataArr = new BoidData[maxObjectNum];
            for(var i=0; i < maxObjectNum; i++) {
                forceArr[i] = Vector3.zero;
                boidDataArr[i].position = Random.insideUnitSphere * 1.0f;
                boidDataArr[i].velocity = Random.insideUnitSphere * 0.1f;
            }
            _boidForceBuffer.SetData(forceArr);
            _boidDataBuffer.SetData(boidDataArr);
            boidDataArr = null;
            forceArr = null;
        }

        void Simulation() {
            var cs = BoidsCS;
            var id = -1;

            int threadGroupSize = Mathf.CeilToInt(maxObjectNum / SIMULATION_BLOCK_SIZE);

            id = cs.FindKernel("ForceCS");
            cs.SetInt("_MaxBoidObjectNum", maxObjectNum);
            cs.SetFloat("_CohesionNeighborhoodRadius", cohesionNeighborhoodRadius);
            cs.SetFloat("_AlignmentNeighborhoodRadius", alignmentNeighborhoodRadius);
            cs.SetFloat("_SeparateNeighborhoodRadius", separateNeighborhoodRadius);
            cs.SetFloat("_CohesionWeight", cohesionWeight);
            cs.SetFloat("_AlignmentWeight", alignmentWeight);
            cs.SetFloat("_SeparateWeight", separateWeight);
            cs.SetFloat("_MaxSpeed", maxSpeed);
            cs.SetFloat("_MaxSteerForce", maxSteerForce);
            cs.SetFloat("_AvoidWallWeight", avoidWallWeight);
            cs.SetVector("_WallCenter", wallCenter);
            cs.SetVector("_WallSize", wallSize);
            cs.SetBuffer(id, "_BoidForceBufferWrite", _boidForceBuffer);
            cs.SetBuffer(id, "_BoidDataBufferRead", _boidDataBuffer);
            cs.Dispatch(id, threadGroupSize, 1, 1);

            id = cs.FindKernel("IntegrateCS");
            cs.SetFloat("_DeltaTime", Time.deltaTime);
            cs.SetBuffer(id, "_BoidForceBufferRead", _boidForceBuffer);
            cs.SetBuffer(id, "_BoidDataBufferWrite", _boidDataBuffer);
            cs.Dispatch(id, threadGroupSize, 1, 1);
        }

        void ReleaseBuffer() {
            if (_boidForceBuffer != null) {
                _boidForceBuffer.Release();
                _boidForceBuffer = null;
            }
            if (_boidDataBuffer != null) {
                _boidDataBuffer.Release();
                _boidDataBuffer = null;
            }
        }
    }
}