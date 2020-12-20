using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace StrangeAttractor {
    public class ThomasAttractor : StrangeAttractor {

        [SerializeField, Tooltip("Default is 0.32899f")]
        float b = 0.32899f;

        private int bId;

        protected override void InitializeComputeBuffer() {
            if (computeBuffer != null)
                computeBuffer.Release();

            computeBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(Particle)));

            var particles = new Particle[instanceCount];
            for (var i = 0; i < instanceCount; i++) {
                var rs = Random.insideUnitSphere;
                var color = gradient.Evaluate(rs.magnitude);
                particles[i] = new Particle(rs * emitterSize, particleSize, color);
            }
            computeBuffer.SetData(particles);
        }

        protected override void InitializeShaderUniforms() {
            bId = Shader.PropertyToID("b");
        }

        protected override void UpdateShaderUniforms() {
            computeShaderInstance.SetFloat(bId, b);
        }

        protected override void Initialize() {
            Assert.IsTrue(computeShader.name == "ThomasAttractor", "Please set ThomasAttractor compute shader.");
            base.Initialize();
        }
    }
}