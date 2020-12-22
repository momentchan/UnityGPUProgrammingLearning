using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Common;

namespace GravitationalNBodySimulation {
    public class NBodySimulation : MonoBehaviour {

        public ComputeBuffer GetParticleBuffer() => bufferRead;
        public int GetParticleNumbers() => particleNumbers;

        [SerializeField] protected ComputeShader cs;
        [SerializeField] protected NumOptions numOptions = NumOptions.NUM_64K;
        [SerializeField] protected float positionScale = 1f;
        [SerializeField] protected int divideLevel;
        [SerializeField] protected float damping = 0.95f;
        [SerializeField] protected float softeningSquared = 0.1f;

        protected const int DEFAULT_PARTICLE_NUM = 65536;
        protected int particleNumbers;
        protected ComputeBuffer bufferRead, bufferWrite;
        protected Kernel updateKernel;

        void Start() {
            particleNumbers = (int)numOptions;
            updateKernel = new Kernel(cs, "Update");
            InitBuffer();
        }

        private void InitBuffer() {
            Random.InitState(0);
            bufferRead = new ComputeBuffer(particleNumbers, Marshal.SizeOf(typeof(Body)));
            bufferWrite = new ComputeBuffer(particleNumbers, Marshal.SizeOf(typeof(Body)));

            float scale = positionScale * Mathf.Max(1, particleNumbers / DEFAULT_PARTICLE_NUM);

            var bodies = Enumerable.Range(0, particleNumbers).Select(_ =>
                new Body() { 
                    position = Random.insideUnitSphere * scale, 
                    velocity = Vector3.zero, 
                    mass = Random.Range(0.1f, 1f) 
                }).ToArray();

            bufferRead.SetData(bodies);
            bufferWrite.SetData(bodies);
        }

        void Update() {
            cs.SetFloat("_DeltaTime", Time.deltaTime);
            cs.SetFloat("_Damping", damping);
            cs.SetFloat("_SofteningSquared", softeningSquared);
            cs.SetInt("_ParticleNumbers", particleNumbers);
            cs.SetInt("_DivideLevel", Mathf.Max(divideLevel, 1));

            cs.SetBuffer(updateKernel.Index, "_ParticleBufferRead", bufferRead);
            cs.SetBuffer(updateKernel.Index, "_ParticleBufferWrite", bufferWrite);

            cs.Dispatch(updateKernel.Index, Mathf.CeilToInt(particleNumbers / (int)updateKernel.ThreadX), (int)updateKernel.ThreadY, (int)updateKernel.ThreadZ);

            ComputeShaderUtil.SwapBuffer(ref bufferRead, ref bufferWrite);
        }
        
        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(bufferRead);
            ComputeShaderUtil.ReleaseBuffer(bufferWrite);
        }
    }
}