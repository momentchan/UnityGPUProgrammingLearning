using System.Collections.Generic;
using UnityEngine;
using mj.gist;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace ReactionDiffusion {
    public class ReactionDiffusion3D : MonoBehaviour, IComputeShaderUser {

        [SerializeField] protected ComputeShader cs;
        [SerializeField] protected Material mat;
        [SerializeField] protected RenderTexture heightMap;
        public RenderTexture HeightMap => heightMap;

        [Header("Seed")]
        [SerializeField] protected int maxSeedNum = 32;
        [SerializeField] protected int seedNum = 10;
        [SerializeField] protected Vector2 seedSize; // x: add y: remove

        [Header("Parameters")]
        [SerializeField] protected float du = 1f;
        [SerializeField] protected float dv = 0.5f;
        [SerializeField, Range(0f, 0.1f)] protected float feed = 0.05f;
        [SerializeField, Range(0f, 0.1f)] protected float kill = 0.06f;
        [SerializeField, Range(0, 64)] int steps = 32;

        [Header("Rendering")]
        [SerializeField] protected Color bottomColor;
        [SerializeField] protected Color topColor;

        #region Shader Props
        protected int seedNumProp = Shader.PropertyToID("_SeedNum");
        protected int widthProp = Shader.PropertyToID("_Width");
        protected int heightProp = Shader.PropertyToID("_Height");
        protected int depthProp = Shader.PropertyToID("_Depth");
        protected int seedSizeProp = Shader.PropertyToID("_SeedSize");
        protected int seedBufferProp = Shader.PropertyToID("_SeedBuffer");
        protected int duProp = Shader.PropertyToID("_DU");
        protected int dvProp = Shader.PropertyToID("_DV");
        protected int feedProp = Shader.PropertyToID("_Feed");
        protected int killProp = Shader.PropertyToID("_Kill");
        protected int pixelBufferReadProp = Shader.PropertyToID("_PixelRead");
        protected int pixelBufferWriteProp = Shader.PropertyToID("_PixelWrite");
        protected int heightMapProp = Shader.PropertyToID("_HeightMap");
        protected int normalMapProp = Shader.PropertyToID("_NormalMap");
        protected int texProp = Shader.PropertyToID("_MainTex");
        protected int bottomColProp = Shader.PropertyToID("_Color0");
        protected int topColProp = Shader.PropertyToID("_Color1");
        #endregion

        [SerializeField] protected int width = 128;
        [SerializeField] protected int height = 128;
        [SerializeField] protected int depth = 128;

        private int texels;

        private Queue<Vector4> seeds;       // x, y, z, v
        protected ComputeBuffer seedBuffer;
        protected PingPongBuffer pixelBuffer;

        protected Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        protected GPUThreads threads;

        protected enum ComputeKernel {
            Clear, Update, Draw, AddSeed
        }

        public void InitBuffers() {
            pixelBuffer = new PingPongBuffer(texels, typeof(RDData));
            seedBuffer = new ComputeBuffer(maxSeedNum, Marshal.SizeOf(typeof(Vector4)));

            var initData = Enumerable.Repeat(new RDData() { u = 0, v = 1 }, texels).ToArray();
            pixelBuffer.Read.SetData(initData);
            pixelBuffer.Write.SetData(initData);
        }

        public void InitKernels() {
            kernelMap = Enum.GetValues(typeof(ComputeKernel)).Cast<ComputeKernel>()
                .ToDictionary(
                    t => t,
                    t => cs.FindKernel(t.ToString())
                );
            threads = ComputeShaderUtil.GetThreadGroupSize(cs, kernelMap[ComputeKernel.Update]);
            ComputeShaderUtil.InitialCheck(texels, threads);
        }

        protected void Initialize() {
            texels = width * height * depth;
            seeds = new Queue<Vector4>();
            heightMap = CreateRenderTexture3D(width, height, depth);

            InitBuffers();
            InitKernels();
        }

        protected void Start() {
            Initialize();
        }
        
        private RenderTexture CreateRenderTexture3D(int width, int height, int depth) {
            var tex = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            tex.volumeDepth = depth;
            tex.enableRandomWrite = true;
            tex.dimension = TextureDimension.Tex3D;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Create();

            return tex;
        }

        void Update() {
            if (Input.GetMouseButton(0)) {
                AddSeed(width / 2, height / 2, depth / 2, 1);
            }

            if (Input.GetKey(KeyCode.A)) {
                AddRandomSeeds(seedNum, 1);
            }

            if (Input.GetKey(KeyCode.R)) {
                ResetKernel();
            }

            AddSeedKernel();
            UpdateKernel();
            DrawKernel();

            UpdateMaterial();
        }

        private void ResetKernel() {
            cs.SetBuffer(kernelMap[ComputeKernel.Clear], pixelBufferWriteProp, pixelBuffer.Read);
            cs.Dispatch(kernelMap[ComputeKernel.Clear], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), Mathf.CeilToInt(1f * depth / threads.z));

            cs.SetBuffer(kernelMap[ComputeKernel.Clear], pixelBufferWriteProp, pixelBuffer.Write);
            cs.Dispatch(kernelMap[ComputeKernel.Clear], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), Mathf.CeilToInt(1f * depth / threads.z));
        }

        private void AddSeed(int x, int y, int z, float v) {
            if (seeds.Count < maxSeedNum) {
                seeds.Enqueue(new Vector4(x, y, z, v));
            }
        }

        private void AddRandomSeeds(int num, float v) {
            for (var i = 0; i < num; i++) {
                AddSeed(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height), UnityEngine.Random.Range(0, depth), v);
            }
        }

        private void AddSeedKernel() {
            var count = seeds.Count;
            if (count > 0) {
                seedBuffer.SetData(seeds.ToArray());
                cs.SetInt(seedNumProp, count);
                cs.SetInt(widthProp, width);
                cs.SetInt(heightProp, height);
                cs.SetInt(depthProp, depth);
                cs.SetVector(seedSizeProp, seedSize);
                cs.SetBuffer(kernelMap[ComputeKernel.AddSeed], seedBufferProp, seedBuffer);
                cs.SetBuffer(kernelMap[ComputeKernel.AddSeed], pixelBufferWriteProp, pixelBuffer.Read);
                cs.Dispatch(kernelMap[ComputeKernel.AddSeed], Mathf.CeilToInt(1f * count / threads.x), 1, 1);
                seeds.Clear();
            }
        }

        private void UpdateKernel() {
            for (int i = 0; i < steps; i++) {
                cs.SetFloat(duProp, du);
                cs.SetFloat(dvProp, dv);
                cs.SetFloat(feedProp, feed);
                cs.SetFloat(killProp, kill);

                cs.SetBuffer(kernelMap[ComputeKernel.Update], pixelBufferReadProp, pixelBuffer.Read);
                cs.SetBuffer(kernelMap[ComputeKernel.Update], pixelBufferWriteProp, pixelBuffer.Write);
                cs.Dispatch(kernelMap[ComputeKernel.Update], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), Mathf.CeilToInt(1f * depth / threads.z));

                pixelBuffer.Swap();
            }
        }

        protected void DrawKernel() {
            cs.SetInt(widthProp, width);
            cs.SetInt(heightProp, height);
            cs.SetInt(depthProp, depth);
            cs.SetBuffer(kernelMap[ComputeKernel.Draw], pixelBufferReadProp, pixelBuffer.Read);
            cs.SetTexture(kernelMap[ComputeKernel.Draw], heightMapProp, heightMap);
            cs.Dispatch(kernelMap[ComputeKernel.Draw], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), Mathf.CeilToInt(1f * depth / threads.z));
        }

        protected void UpdateMaterial() {
            mat.SetTexture(texProp, heightMap);
        }

        protected virtual void OnDestroy() {
            pixelBuffer.Dispose();
            seedBuffer.Dispose();
            Destroy(heightMap);
        }
    }
}