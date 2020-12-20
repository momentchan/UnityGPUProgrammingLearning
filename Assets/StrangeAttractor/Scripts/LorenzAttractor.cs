using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace StrangeAttractor {
    public class LorenzAttractor : StrangeAttractor {

        [Header("Lorenz values are p:10, r:28, b:8/3")]
        [SerializeField, Tooltip("Default is 10")]
        private float p = 10f;
        [SerializeField, Tooltip("Default is 28")]
        private float r = 28f;
        [SerializeField, Tooltip("Default is 8/3")]
        private float b = 2.666667f;

        private int pId, rId, bId;

        protected sealed override void InitializeComputeBuffer() {
            if (computeBuffer != null)
                computeBuffer.Release();

            computeBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(Particle)));

            var particles = new Particle[instanceCount];
            for (var i = 0; i < instanceCount; i++) {
                var r = (float)i / instanceCount;
                var color = gradient.Evaluate(r);
                particles[i] = new Particle(Random.insideUnitSphere * emitterSize * r, particleSize, color);
            }
            computeBuffer.SetData(particles);
        }

        protected override void InitializeShaderUniforms() {
            pId = Shader.PropertyToID("p");
            rId = Shader.PropertyToID("r");
            bId = Shader.PropertyToID("b");
        }

        protected override void UpdateShaderUniforms() {
            computeShaderInstance.SetFloat(pId, p);
            computeShaderInstance.SetFloat(rId, r);
            computeShaderInstance.SetFloat(bId, b);
        }

        protected override void Initialize() {
            Assert.IsTrue(computeShader.name == "LorenzAttractor", "Please set LorenzAttractor compute shader.");
            base.Initialize();
        }
    }
}