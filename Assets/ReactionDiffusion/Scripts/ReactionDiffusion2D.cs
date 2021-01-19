using System.Collections.Generic;
using UnityEngine;
using Common;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace ReactionDiffusion {
    public class ReactionDiffusion2D : MonoBehaviour, IComputeShader {

        [SerializeField] protected ComputeShader cs;
        [SerializeField] protected Material mat;
        [SerializeField] protected RenderTexture resultRT;

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
        private int seedNumProp          = Shader.PropertyToID("_SeedNum");
        private int widthProp            = Shader.PropertyToID("_Width");
        private int heightProp           = Shader.PropertyToID("_Height");
        private int seedSizeProp         = Shader.PropertyToID("_SeedSize");
        private int seedBufferProp       = Shader.PropertyToID("_SeedBuffer");
        private int duProp               = Shader.PropertyToID("_DU");
        private int dvProp               = Shader.PropertyToID("_DV");
        private int feedProp             = Shader.PropertyToID("_Feed");
        private int killProp             = Shader.PropertyToID("_Kill");
        private int pixelBufferReadProp  = Shader.PropertyToID("_PixelRead");
        private int pixelBufferWriteProp = Shader.PropertyToID("_PixelWrite");
        private int heightMapProp        = Shader.PropertyToID("_HeightMap");
        private int texProp              = Shader.PropertyToID("_MainTex");
        private int bottomColProp        = Shader.PropertyToID("_Color0");
        private int topColProp           = Shader.PropertyToID("_Color1");
        #endregion

        private int width, height;
        private int pixels;

        private Queue<Vector4> seeds;       // x, y, u, v
        private ComputeBuffer seedBuffer;
        private PingPongBuffer pixelBuffer;

        private Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        private GPUThreads threads;
        
        private enum ComputeKernel {
            Update, Draw, AddSeed
        }

        public void InitBuffers() {
            pixelBuffer = new PingPongBuffer(pixels, typeof(RDData));
            seedBuffer = new ComputeBuffer(maxSeedNum, Marshal.SizeOf(typeof(Vector2)));

            var initData = Enumerable.Repeat(new RDData() { u = 0, v = 1 }, pixels).ToArray();
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
            ComputeShaderUtil.InitialCheck(pixels, threads);
        }

        void Start() {
            width = Screen.width;
            height = Screen.height;
            pixels = width * height;
            seeds = new Queue<Vector4>();
            resultRT = RenderTextureUtil.CreateRenderTexture(width, height, 0, RenderTextureFormat.RFloat, true, false, false, TextureWrapMode.Repeat);

            InitBuffers();
            InitKernels();

            // Post Effect
            var cam = Camera.main;
            if (cam == null) return;
            var buf = new CommandBuffer();
            buf.name = "PostEffect";
            buf.Blit(resultRT, BuiltinRenderTextureType.CurrentActive, mat);
            cam.RemoveAllCommandBuffers();
            cam.AddCommandBuffer(CameraEvent.AfterEverything, buf);
        }
        
        void Update() {
            if (Input.GetMouseButton(0)) {
                AddSeed((int)Input.mousePosition.x, (int)Input.mousePosition.y, 0, 1);
            }
            if (Input.GetMouseButton(1)) {
                AddSeed((int)Input.mousePosition.x, (int)Input.mousePosition.y, 1, 0);
            }

            AddSeedKernel();
            UpdateKernel();
            DrawKernel();

            UpdateMaterial();
        }

        private void AddSeed(int x, int y, float u, float v) {
            if (seeds.Count < maxSeedNum) {
                seeds.Enqueue(new Vector4(x, y, u, v));
            }
        }

        private void AddRandomSeeds(int num, float u, float v) {
            for (var i = 0; i < num; i++) {
                AddSeed(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height), u, v);
            }
        }

        private void AddSeedKernel() {
            var count = seeds.Count;
            if (count > 0) {
                seedBuffer.SetData(seeds.ToArray());
                cs.SetInt(seedNumProp, count);
                cs.SetInt(widthProp, width);
                cs.SetInt(heightProp, height);
                cs.SetVector(seedSizeProp, seedSize);
                cs.SetBuffer(kernelMap[ComputeKernel.AddSeed], seedBufferProp, seedBuffer);
                cs.SetBuffer(kernelMap[ComputeKernel.AddSeed], pixelBufferWriteProp, pixelBuffer.Read);
                cs.Dispatch(kernelMap[ComputeKernel.AddSeed], Mathf.CeilToInt(1f * count / threads.x), 1, 1);
                seeds.Clear();
            }
        }

        private void UpdateKernel() {
            for (int i = 0; i < steps; i++) {
                cs.SetInt(widthProp, width);
                cs.SetInt(heightProp, height);
                cs.SetFloat(duProp, du);
                cs.SetFloat(dvProp, dv);
                cs.SetFloat(feedProp, feed);
                cs.SetFloat(killProp, kill);

                cs.SetBuffer(kernelMap[ComputeKernel.Update], pixelBufferReadProp, pixelBuffer.Read);
                cs.SetBuffer(kernelMap[ComputeKernel.Update], pixelBufferWriteProp, pixelBuffer.Write);
                cs.Dispatch(kernelMap[ComputeKernel.Update], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), 1);

                pixelBuffer.Swap();
            }
        }

        private void DrawKernel() {
            cs.SetInt(widthProp, width);
            cs.SetInt(heightProp, height);
            cs.SetBuffer(kernelMap[ComputeKernel.Draw], pixelBufferReadProp, pixelBuffer.Read);
            cs.SetTexture(kernelMap[ComputeKernel.Draw], heightMapProp, resultRT);
            cs.Dispatch(kernelMap[ComputeKernel.Draw], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), 1);
        }

        private void UpdateMaterial() {
            mat.SetTexture(texProp, resultRT);
            mat.SetColor(bottomColProp, bottomColor);
            mat.SetColor(topColProp, topColor);
        }

        private void OnDestroy() {
            pixelBuffer.Dispose();
            seedBuffer.Dispose();
            Destroy(resultRT);
        }
    }
}