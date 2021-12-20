using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using mj.gist;

namespace GPUBasedTrails {


    public class GPUTrails : MonoBehaviour {

        #region data define
    public static class CSPARAM {
        //kernels
        public const string CALC_INPUT = "CalcInput";

        public const string TIME = "_Time";
        public const string TRAIL_NUM = "_TrailNum";
        public const string LIFE = "_Life";
        public const string NODE_NUM_PER_TRAIL = "_NodeNumPerTrail";
        public const string TRAIL_BUFFER = "_TrailBuffer";
        public const string NODE_BUFFER = "_NodeBuffer";
        public const string INPUT_BUFFER = "_InputBuffer";
    }
    public struct Trail {
        public int currentNodeIdx;
    }

    public struct Node {
        public float time;
        public Vector3 pos;
    }

    public struct Input {
        public Vector3 pos;
    }
    #endregion

        [SerializeField] protected ComputeShader cs;
        [SerializeField] protected int trailNum = 10000;
        [SerializeField] protected float life = 10;

        public int TrailNum => trailNum;
        public int NodeNum => nodeNum;
        public float Life => life;
        public ComputeBuffer TrailBuffer => trailBuffer;
        public ComputeBuffer NodeBuffer => nodeBuffer;
        public ComputeBuffer InputBuffer => inputBuffer;

        protected ComputeBuffer trailBuffer, nodeBuffer, inputBuffer;
        protected int nodeNum;
        const int MAX_FPS = 60;

        void Start() {
            Assert.IsNotNull(cs);

            nodeNum = Mathf.CeilToInt(life * MAX_FPS);
            var totalNodeNum = trailNum * nodeNum;

            trailBuffer = new ComputeBuffer(trailNum, Marshal.SizeOf(typeof(Trail)));
            nodeBuffer = new ComputeBuffer(totalNodeNum, Marshal.SizeOf(typeof(Node)));
            inputBuffer = new ComputeBuffer(trailNum, Marshal.SizeOf(typeof(Input)));


            var initTrail = new Trail() { currentNodeIdx = -1 };
            var initNode = new Node() { time = -1 };

            trailBuffer.SetData(Enumerable.Repeat(initTrail, trailNum).ToArray());
            nodeBuffer.SetData(Enumerable.Repeat(initNode, totalNodeNum).ToArray());
        }

        void LateUpdate() {
            cs.SetFloat(CSPARAM.TIME, Time.time);
            cs.SetInt(CSPARAM.TRAIL_NUM, trailNum);
            cs.SetInt(CSPARAM.NODE_NUM_PER_TRAIL, nodeNum);

            var kernel = new Kernel(cs, CSPARAM.CALC_INPUT);
            cs.SetBuffer(kernel.Index, CSPARAM.TRAIL_BUFFER, trailBuffer);
            cs.SetBuffer(kernel.Index, CSPARAM.NODE_BUFFER, nodeBuffer);
            cs.SetBuffer(kernel.Index, CSPARAM.INPUT_BUFFER, inputBuffer);
            cs.Dispatch(kernel.Index, Mathf.CeilToInt(trailNum / kernel.ThreadX), 1, 1);
        }

        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(trailBuffer);
            ComputeShaderUtil.ReleaseBuffer(nodeBuffer);
            ComputeShaderUtil.ReleaseBuffer(inputBuffer);
        }

        
    }
}