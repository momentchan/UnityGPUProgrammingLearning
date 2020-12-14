using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GravitationalNBodySimulation {
    public class SimpleNBodySimulation : MonoBehaviour {

        const int DEFAULT_PARTICLE_NUM = 65536;

        [SerializeField] protected NumOptions numOptions = NumOptions.NUM_64K;
        [SerializeField] protected float positionScale = 1f;

        protected ComputeBuffer bufferRead, bufferWrite;
        protected int particleNumbers;

        public ComputeBuffer GetParticleBuffer() => bufferRead;
        public int GetParticleNumbers() => particleNumbers;

        void Start() {
            particleNumbers = (int)numOptions;
            InitBuffer();
        }

        void Update() {
        }

        private void InitBuffer() {
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

    }
}