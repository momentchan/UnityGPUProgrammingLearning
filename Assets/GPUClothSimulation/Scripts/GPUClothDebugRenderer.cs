using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common;
using System.Runtime.InteropServices;

namespace GPUClothSimulation {
    [RequireComponent(typeof(GPUClothSimulator))]

    public class GPUClothDebugRenderer : MonoBehaviour {

        struct Spring {
            public Vector2Int a;
            public Vector2Int b;
            public Spring(Vector2Int a, Vector2Int b) {
                this.a = a;
                this.b = b;
            }
        }

        [SerializeField] private Material renderMat = null;
        [SerializeField] private float particleSize = 0.005f;

        [SerializeField] private Color massParticleColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        [SerializeField] private Color gridColor = new Color(0.0f, 0.0f, 1.0f, 0.5f);
        [SerializeField] private Color gridDiagonalColor = new Color(0.0f, 1.0f, 0.0f, 0.5f);
        [SerializeField] private Color gridDiagonalAlternateColor = new Color(1.0f, 0.0f, 0.0f, 0.5f);

        [SerializeField] private bool drawMassParticle = true;
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private bool drawGridDiagonal = true;
        [SerializeField] private bool drawGridDiagonalAlternate = true;

        private ComputeBuffer gridBuffer;
        private ComputeBuffer gridDiagonalBuffer;
        private ComputeBuffer gridDiagonalAlternateBuffer;
        private GPUClothSimulator simulator;

        private bool isInit = false;

        void Update() {
            if (!isInit) {
                if (!simulator)
                    simulator = GetComponent<GPUClothSimulator>();

                if (simulator && simulator.IsInit) {

                    var gridList = new List<Spring>();
                    var gridDiagonalList = new List<Spring>();
                    var gridDiagonalAlternateList = new List<Spring>();

                    var res = simulator.GetClothResolution();

                    for (var x = 0; x < res.x; x++) {
                        for (var y = 0; y < res.y; y++) {
                            gridList.Add(new Spring(new Vector2Int(x, y), new Vector2Int(x, y + 1)));
                            gridList.Add(new Spring(new Vector2Int(x, y), new Vector2Int(x + 1, y)));

                            if (x < res.x - 1 && y < res.y - 1) {
                                gridDiagonalList.Add(new Spring(new Vector2Int(x, y), new Vector2Int(x + 1, y + 1)));
                                gridDiagonalList.Add(new Spring(new Vector2Int(x + 1, y), new Vector2Int(x, y + 1)));
                            }

                            if (x < res.x - 2 && y < res.y - 2) {
                                gridDiagonalAlternateList.Add(new Spring(new Vector2Int(x, y), new Vector2Int(x + 2, y + 2)));
                            }

                            if (x >= 2 && y < res.y - 2) {
                                gridDiagonalAlternateList.Add(new Spring(new Vector2Int(x, y), new Vector2Int(x - 2, y + 2)));
                            }
                        }
                    }

                    gridBuffer = new ComputeBuffer(gridList.Count, Marshal.SizeOf(typeof(Spring)));
                    gridBuffer.SetData(gridList.ToArray());

                    gridDiagonalBuffer = new ComputeBuffer(gridDiagonalList.Count, Marshal.SizeOf(typeof(Spring)));
                    gridDiagonalBuffer.SetData(gridDiagonalList.ToArray());

                    gridDiagonalAlternateBuffer = new ComputeBuffer(gridDiagonalAlternateList.Count, Marshal.SizeOf(typeof(Spring)));
                    gridDiagonalAlternateBuffer.SetData(gridDiagonalAlternateList.ToArray());

                    gridList = null;
                    gridDiagonalList = null;
                    gridDiagonalAlternateList = null;

                    isInit = true;
                }
            }
        }

        private void OnRenderObject() {
            if (!isInit || !renderMat)
                return;

            if (drawMassParticle) {
                renderMat.SetFloat("_ParticleSize", particleSize);
                renderMat.SetColor("_Color", massParticleColor);
                renderMat.SetTexture("_PositionTex", simulator.GetPositionBuffer());
                renderMat.SetPass(0);

                Graphics.DrawProceduralNow(MeshTopology.Points, simulator.GetClothResolution().x * simulator.GetClothResolution().y);
            }

            if (drawGrid) {
                renderMat.SetColor("_Color", gridColor);
                renderMat.SetTexture("_PositionTex", simulator.GetPositionBuffer());
                renderMat.SetBuffer("_SpringBuffer", gridBuffer);
                renderMat.SetPass(1);

                Graphics.DrawProceduralNow(MeshTopology.Points, gridBuffer.count);
            }

            if (drawGridDiagonal) {
                renderMat.SetColor("_Color", gridDiagonalColor);
                renderMat.SetTexture("_PositionTex", simulator.GetPositionBuffer());
                renderMat.SetBuffer("_SpringBuffer", gridDiagonalBuffer);
                renderMat.SetPass(1);

                Graphics.DrawProceduralNow(MeshTopology.Points, gridDiagonalBuffer.count);
            }

            if (drawGridDiagonalAlternate) {
                renderMat.SetColor("_Color", gridDiagonalAlternateColor);
                renderMat.SetTexture("_PositionTex", simulator.GetPositionBuffer());
                renderMat.SetBuffer("_SpringBuffer", gridDiagonalAlternateBuffer);
                renderMat.SetPass(1);

                Graphics.DrawProceduralNow(MeshTopology.Points, gridDiagonalAlternateBuffer.count);
            }
        }

        private void OnDestroy() {
            ComputeShaderUtil.ReleaseBuffer(gridBuffer);
            ComputeShaderUtil.ReleaseBuffer(gridDiagonalBuffer);
            ComputeShaderUtil.ReleaseBuffer(gridDiagonalAlternateBuffer);
        }
    }
}