using System.Collections.Generic;
using UnityEngine;
using Common;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace ReactionDiffusion {
    public class ReactionDiffusion2D : MonoBehaviour, ComputeShaderUser {

        [SerializeField] protected ComputeShader cs;
        [SerializeField] protected Material mat;
        [SerializeField] protected RenderTexture heightMap;
        [SerializeField] protected RenderTexture normalMap;

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
        protected int seedNumProp          = Shader.PropertyToID("_SeedNum");
        protected int widthProp            = Shader.PropertyToID("_Width");
        protected int heightProp           = Shader.PropertyToID("_Height");
        protected int seedSizeProp         = Shader.PropertyToID("_SeedSize");
        protected int seedBufferProp       = Shader.PropertyToID("_SeedBuffer");
        protected int duProp               = Shader.PropertyToID("_DU");
        protected int dvProp               = Shader.PropertyToID("_DV");
        protected int feedProp             = Shader.PropertyToID("_Feed");
        protected int killProp             = Shader.PropertyToID("_Kill");
        protected int pixelBufferReadProp  = Shader.PropertyToID("_PixelRead");
        protected int pixelBufferWriteProp = Shader.PropertyToID("_PixelWrite");
        protected int heightMapProp        = Shader.PropertyToID("_HeightMap");
        protected int normalMapProp        = Shader.PropertyToID("_NormalMap");
        protected int texProp              = Shader.PropertyToID("_MainTex");
        protected int bottomColProp        = Shader.PropertyToID("_Color0");
        protected int topColProp           = Shader.PropertyToID("_Color1");
        #endregion

        protected int width, height;
        private int pixels;

        private Queue<Vector3> seeds;       // x, y, v
        protected ComputeBuffer seedBuffer;
        protected PingPongBuffer pixelBuffer;

        protected Dictionary<ComputeKernel, int> kernelMap = new Dictionary<ComputeKernel, int>();
        protected GPUThreads threads;

        protected enum ComputeKernel {
            Update, Draw, AddSeed, Clear
        }

        public void InitBuffers() {
            pixelBuffer = new PingPongBuffer(pixels, typeof(RDData));
            seedBuffer = new ComputeBuffer(maxSeedNum, Marshal.SizeOf(typeof(Vector3)));

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

        protected virtual void Initialize() {
            width = Screen.width;
            height = Screen.height;
            pixels = width * height;
            seeds = new Queue<Vector3>();
            heightMap = RenderTextureUtil.CreateRenderTexture(width, height, 0, RenderTextureFormat.RFloat, true, false, false, TextureWrapMode.Repeat);

            InitBuffers();
            InitKernels();
        }

        protected virtual void Start() {
            Initialize();

            //Create a Quad:
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = MeshUtil.CreateQuad();
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = mat;
            transform.position = Vector3.forward;
            var height = Camera.main.orthographicSize * 2;
            var width = height * Camera.main.aspect;

            transform.localScale = new Vector3(width, height, 1);

            //// Post Effect
            //// Couldn't be applied to surface shader
            //var cam = Camera.main;
            //if (cam == null) return;
            //var buf = new CommandBuffer();
            //buf.name = "PostEffect";
            //buf.Blit(heightMap, BuiltinRenderTextureType.CurrentActive, mat);
            //cam.AddCommandBuffer(CameraEvent.AfterEverything, buf);
        }

        void Update() {
            if (Input.GetMouseButton(0)) {
                AddSeed((int)Input.mousePosition.x, (int)Input.mousePosition.y, 1);
            }
            if (Input.GetMouseButton(1)) {
                AddSeed((int)Input.mousePosition.x, (int)Input.mousePosition.y, 0);
            }

            if (Input.GetKeyDown(KeyCode.R))
                ClearKernel();

            AddSeedKernel();
            UpdateKernel();
            DrawKernel();
            UpdateMaterial();
        }

        private void ClearKernel() {
            cs.SetBuffer(kernelMap[ComputeKernel.Clear], pixelBufferWriteProp, pixelBuffer.Read);
            cs.Dispatch(kernelMap[ComputeKernel.Clear], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), 1);

            cs.SetBuffer(kernelMap[ComputeKernel.Clear], pixelBufferWriteProp, pixelBuffer.Write);
            cs.Dispatch(kernelMap[ComputeKernel.Clear], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), 1);
        }

        private void AddSeed(int x, int y, float v) {
            if (seeds.Count < maxSeedNum) {
                seeds.Enqueue(new Vector3(x, y, v));
            }
        }

        private void AddRandomSeeds(int num, float v) {
            for (var i = 0; i < num; i++) {
                AddSeed(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height), v);
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

        protected virtual void DrawKernel() {
            cs.SetInt(widthProp, width);
            cs.SetInt(heightProp, height);
            cs.SetBuffer(kernelMap[ComputeKernel.Draw], pixelBufferReadProp, pixelBuffer.Read);
            cs.SetTexture(kernelMap[ComputeKernel.Draw], heightMapProp, heightMap);
            cs.Dispatch(kernelMap[ComputeKernel.Draw], Mathf.CeilToInt(1f * width / threads.x), Mathf.CeilToInt(1f * height / threads.y), 1);
        }

        protected virtual void UpdateMaterial() {
            mat.SetTexture(texProp, heightMap);
            mat.SetColor(bottomColProp, bottomColor);
            mat.SetColor(topColProp, topColor);
        }

        protected virtual void OnDestroy() {
            pixelBuffer.Dispose();
            seedBuffer.Dispose();
            Destroy(heightMap);
        }
    }
}