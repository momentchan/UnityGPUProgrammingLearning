using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using mj.gist;

namespace GPUBasedTrails {

    [RequireComponent(typeof(GPUTrails))]
    public class GPUTrailParticles : MonoBehaviour {

        #region data define
        public static class CSPARAM {
            public const string UPDATE = "Update";
            public const string WRITE_TO_INPUT = "WriteToInput";

            public const string PARTICLE_NUM = "_ParticleNum";
            public const string TIME = "_Time";
            public const string TIME_SCALE = "_TimeScale";
            public const string POSITION_SCALE = "_PositionScale";
            public const string NOISE_SCALE = "_NoiseScale";
            public const string PARTICLE_BUFFER_WRITE = "_ParticleBufferWrite";
            public const string PARTICLE_BUFFER_READ = "_ParticleBufferRead";
            public const string INPUT_BUFFER = "_InputBuffer";

            // Interaction
            public const string INPUT_MOUSE = "_InputMouse";
            public const string INPUT_MOUSE_Force = "_InputMouseForce";
            public const string INPUT_MOUSE_POSITION = "_InputMousePosition";
        }

        public struct Particle {
            public Vector3 position;
        }
        #endregion
        
        [SerializeField] protected ComputeShader cs;
        [SerializeField] protected float initRadius = 30f;
        [SerializeField] protected float timeScale = 0.1f;
        [SerializeField] protected float positionScale = 0.01f;
        [SerializeField] protected float noiseScale = 0.1f;
        [SerializeField] protected bool inputMouse;
        [SerializeField] protected float inputMouseForce = 1f;

        protected ComputeBuffer particleBuffer;
        protected GPUTrails trails;
        protected Vector3 inputMousePosition;
        protected int particleNum => trails.TrailNum;
        
        void Start() {
            trails = GetComponent<GPUTrails>();

            particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(Particle)));

            particleBuffer.SetData(
                Enumerable.Range(0, particleNum).Select(_ => new Particle() { position = Random.insideUnitSphere * initRadius })
                .ToArray()
            );
        }

        void Update() {

            inputMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.x, Camera.main.nearClipPlane));

            cs.SetInt(CSPARAM.PARTICLE_NUM, particleNum);
            cs.SetFloat(CSPARAM.TIME, Time.time);
            cs.SetFloat(CSPARAM.TIME_SCALE, timeScale);
            cs.SetFloat(CSPARAM.POSITION_SCALE, positionScale);
            cs.SetFloat(CSPARAM.NOISE_SCALE, noiseScale);
            cs.SetBool(CSPARAM.INPUT_MOUSE, inputMouse);
            cs.SetVector(CSPARAM.INPUT_MOUSE_POSITION, inputMousePosition);
            cs.SetFloat(CSPARAM.INPUT_MOUSE_Force, inputMouseForce);


            var updateKer = new Kernel(cs, CSPARAM.UPDATE);
            cs.SetBuffer(updateKer.Index, CSPARAM.PARTICLE_BUFFER_WRITE, particleBuffer);
            cs.Dispatch(updateKer.Index, Mathf.CeilToInt(particleNum / updateKer.ThreadX), 1, 1);

            var inputKer = new Kernel(cs, CSPARAM.WRITE_TO_INPUT);
            cs.SetBuffer(inputKer.Index, CSPARAM.PARTICLE_BUFFER_READ, particleBuffer);
            cs.SetBuffer(inputKer.Index, CSPARAM.INPUT_BUFFER, trails.InputBuffer);

            cs.Dispatch(inputKer.Index, Mathf.CeilToInt(particleNum / inputKer.ThreadX), 1, 1);
        }

        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(particleBuffer);
        }
    }
}