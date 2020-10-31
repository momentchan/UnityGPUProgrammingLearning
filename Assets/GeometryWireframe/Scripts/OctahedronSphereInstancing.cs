using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Common;

namespace GeometryWireframe {
    public class OctahedronSphereInstancing : WireframeBase {

        [SerializeField] protected ComputeShader cs;
        [SerializeField] protected int particleNum = 8192;
        [SerializeField] protected float radius = 1f;
        [SerializeField] protected float noiseScale = 1f;
        [SerializeField] protected float noiseSpeed = 1f;

        protected ComputeBuffer buffer;
        protected Kernel updateKer;

        void Start() {
            buffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(OctahedronSphere)));

            buffer.SetData(
            Enumerable.Range(0, particleNum).Select(_ => new OctahedronSphere {
                position = Random.insideUnitSphere * radius,
                rotation = Random.rotation,
                scale = Random.Range(0.5f, 1f),
                leve = 1
            }).ToArray());

            updateKer = new Kernel(cs, CSPARAM.UPDATE);
        }

        void Update() {
            cs.SetFloat(CSPARAM.TIME, Time.time);
            cs.SetFloat(CSPARAM.NOISE_SCALE, noiseScale);
            cs.SetFloat(CSPARAM.NOISE_SPEED, noiseSpeed);
            cs.SetInt(CSPARAM.SUB_DIVISION_NUM, 9);
            cs.SetBuffer(updateKer.Index, CSPARAM.PARTICLE_BUFFER, buffer);
            cs.Dispatch(updateKer.Index, Mathf.CeilToInt(particleNum / updateKer.ThreadX), 1, 1);
        }
        protected override void OnRenderObject() {
            material.SetBuffer(CSPARAM.PARTICLE_BUFFER, buffer);
            material.SetPass(0);

            Graphics.DrawProceduralNow(MeshTopology.Points, particleNum * 8);
        }

        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(buffer);
        }

        #region Define
        public struct OctahedronSphere {
            public Vector3 position;
            public Quaternion rotation;
            public float scale;
            public int leve;
        }

        #endregion
    }
}