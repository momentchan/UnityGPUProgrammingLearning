using UnityEngine;
using mj.gist;

namespace GPUClothSimulation {
    public class GPUClothSimulator : MonoBehaviour, IComputeShaderUser {

        [SerializeField] private ComputeShader cs;
        [SerializeField, Range(1, 16)] private int verletInterations = 4;
        [SerializeField] private Vector2Int clothResolution;
        [SerializeField] private float resolutionLength = 0.01f;

        [SerializeField] private float timeStep = 0.01f;
        [SerializeField] private float stiffness = 10000f;
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float damp = 0.9996f;

        [Header("Force")]
        [SerializeField] private float windIntensity = 0f;
        [SerializeField] private float windSpeed = 1f;
        [SerializeField] private Vector3 gravity = Vector3.down;
        [SerializeField] private Transform collisionSphere;

        [Header("Debug")]
        [SerializeField] private bool enableDebugOnGUI;
        [SerializeField] private float debugGUIScale = 2.0f;

        public float GetResolutionLength() => resolutionLength;
        public Vector2Int GetClothResolution() => clothResolution;
        public RenderTexture GetPositionBuffer() { return IsInit ? positionBuffer[0] : null; }
        public RenderTexture GetNormalBuffer() { return IsInit ? normalBuffer : null; }
        public bool IsInit { get; private set; }

        
        private ComputeKernel<Kernel> kernels;
        private enum Kernel { Reset, Simulation }

        private RenderTexture[] positionBuffer;
        private RenderTexture[] positionPrevBuffer;
        private RenderTexture normalBuffer;

        #region Shader Properties
        private int clothResolutionProp = Shader.PropertyToID("_ClothResolution");
        private int totalClothLengthProp = Shader.PropertyToID("_TotalClothLength");
        private int resolutionLengthProp = Shader.PropertyToID("_ResolutionLength");

        private int positionBufferReadProp = Shader.PropertyToID("_PositionBufferRead");
        private int positionPrevBufferReadProp = Shader.PropertyToID("_PositionPrevBufferRead");
        private int positionBufferWriteProp = Shader.PropertyToID("_PositionBufferWrite");
        private int positionPrevBufferWriteProp = Shader.PropertyToID("_PositionPrevBufferWrite");
        private int normalBufferWriteProp = Shader.PropertyToID("_NormalBufferWrite");

        private int gravityProp = Shader.PropertyToID("_Gravity");
        private int stiffnessProp = Shader.PropertyToID("_Stiffness");
        private int dampProp = Shader.PropertyToID("_Damp");
        private int inverseMassProp = Shader.PropertyToID("_InverseMass");
        private int deltaTimeProp = Shader.PropertyToID("_DeltaTime");
        private int detectCollisionProp = Shader.PropertyToID("_DetectCollision");
        private int collisionParams = Shader.PropertyToID("_CollisionParams");
        #endregion

        void Start() {
            InitBuffers();
            InitKernels();
            ExecuteResetKernel();

            IsInit = true;
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.R)) {
                ExecuteResetKernel();
            }
            if (Input.GetKeyDown(KeyCode.D)) {
                enableDebugOnGUI = !enableDebugOnGUI;
            }

            ExecuteSimulationKernel();
        }

        public void InitBuffers() {
            CreateRenderTextureBuffers();
        }

        public void InitKernels() {
            kernels = new ComputeKernel<Kernel>(cs);
        }

        private void CreateRenderTextureBuffers() {
            var w = clothResolution.x;
            var h = clothResolution.y;
            var format = RenderTextureFormat.ARGBFloat;
            var filterMode = FilterMode.Point;

            positionBuffer = new RenderTexture[2];
            positionPrevBuffer = new RenderTexture[2];

            for (var i = 0; i < 2; i++) {
                CreateRenderTexture(ref positionBuffer[i], w, h, format, filterMode);
                CreateRenderTexture(ref positionPrevBuffer[i], w, h, format, filterMode);
            }

            CreateRenderTexture(ref normalBuffer, w, h, format, filterMode);
        }

        private void CreateRenderTexture(ref RenderTexture rt, int w, int h, RenderTextureFormat format, FilterMode filterMode) {
            rt = new RenderTexture(w, h, 0, format) {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                enableRandomWrite = true
            };
            rt.Create();
        }

        private void ExecuteResetKernel() {
            var totalClothLength = new Vector2(resolutionLength * clothResolution.x, resolutionLength * clothResolution.y);

            cs.SetInts(clothResolutionProp, new int[2] { clothResolution.x, clothResolution.y });
            cs.SetFloat(resolutionLengthProp, resolutionLength);
            cs.SetFloats(totalClothLengthProp, new float[2] { totalClothLength.x, totalClothLength.y });
            cs.SetTexture(kernels.GetKernelIndex(Kernel.Reset), positionBufferWriteProp, positionBuffer[0]);
            cs.SetTexture(kernels.GetKernelIndex(Kernel.Reset), positionPrevBufferWriteProp, positionPrevBuffer[0]);
            cs.SetTexture(kernels.GetKernelIndex(Kernel.Reset), normalBufferWriteProp, normalBuffer);

            cs.Dispatch(kernels.GetKernelIndex(Kernel.Reset), Mathf.CeilToInt((float)clothResolution.x / kernels.Threads.x), Mathf.CeilToInt((float)clothResolution.y / kernels.Threads.y), 1);

            Graphics.Blit(positionBuffer[0], positionBuffer[1]);
            Graphics.Blit(positionPrevBuffer[0], positionPrevBuffer[1]);
        }

        private void ExecuteSimulationKernel() {
            var deltaTime = timeStep / verletInterations;

            var wind = new Vector3(Mathf.PerlinNoise(Time.time * windSpeed, 0f) - 0.5f,
                                   0,
                                   Mathf.PerlinNoise(Time.time * windSpeed, 2f) - 0.5f).normalized
                                   * windIntensity;
            var force = gravity + wind;


            cs.SetInts(clothResolutionProp, new int[2] { clothResolution.x, clothResolution.y });
            cs.SetFloat(resolutionLengthProp, resolutionLength);
            cs.SetFloat(stiffnessProp, stiffness);
            cs.SetFloat(dampProp, damp);
            cs.SetFloat(inverseMassProp, 1.0f / mass);
            cs.SetFloat(deltaTimeProp, deltaTime);
            cs.SetVector(gravityProp, force);

            var detectCollision = collisionSphere != null;
            if (detectCollision) {
                var collisionCenter = collisionSphere.position;
                var collisionRadius = collisionSphere.localScale.x * 0.5f + 0.01f;

                cs.SetBool(detectCollisionProp, true);
                cs.SetFloats(collisionParams,
                    new float[4]{
                        collisionCenter.x,
                        collisionCenter.y,
                        collisionCenter.z,
                        collisionRadius
                    });
            } else
                cs.SetBool(detectCollisionProp, false);

            for (var i = 0; i < verletInterations; i++) {
                cs.SetTexture(kernels.GetKernelIndex(Kernel.Simulation), positionBufferReadProp, positionBuffer[0]);
                cs.SetTexture(kernels.GetKernelIndex(Kernel.Simulation), positionPrevBufferReadProp, positionPrevBuffer[0]);
                cs.SetTexture(kernels.GetKernelIndex(Kernel.Simulation), positionBufferWriteProp, positionBuffer[1]);
                cs.SetTexture(kernels.GetKernelIndex(Kernel.Simulation), positionPrevBufferWriteProp, positionPrevBuffer[1]);
                cs.SetTexture(kernels.GetKernelIndex(Kernel.Simulation), normalBufferWriteProp, normalBuffer);

                cs.Dispatch(kernels.GetKernelIndex(Kernel.Simulation), Mathf.CeilToInt((float)clothResolution.x / kernels.Threads.x), Mathf.CeilToInt((float)clothResolution.y / kernels.Threads.y), 1);

                SwapBuffer(ref positionBuffer[0], ref positionBuffer[1]);
                SwapBuffer(ref positionPrevBuffer[0], ref positionPrevBuffer[1]);
            }
        }

        private void SwapBuffer(ref RenderTexture ping, ref RenderTexture pong) {
            var temp = ping;
            ping = pong;
            pong = temp;
        }

        private void OnDestroy() {
            for(var i = 0; i < 2; i++) {
                DestoryRenderTexture(ref positionBuffer[i]);
                DestoryRenderTexture(ref positionPrevBuffer[i]);
            }
            DestoryRenderTexture(ref normalBuffer);
        }

        private void DestoryRenderTexture(ref RenderTexture rt) {
            if (Application.isEditor)
                DestroyImmediate(rt);
            else
                Destroy(rt);
            rt = null;
        }

        private void OnGUI() {
            DrawSimulationBufferOnGUI();
        }

        void DrawSimulationBufferOnGUI() {
            if (!enableDebugOnGUI)
                return;

            int rw = Mathf.RoundToInt(clothResolution.x * debugGUIScale);
            int rh = Mathf.RoundToInt(clothResolution.y * debugGUIScale);

            var color = GUI.color;
            GUI.color = Color.gray;

            if (positionBuffer != null) {
                var r00 = new Rect(rw * 0, rh * 0, rw, rh);
                var r01 = new Rect(rw * 0, rh * 1, rw, rh);
                GUI.DrawTexture(r00, positionBuffer[0]);
                GUI.DrawTexture(r01, positionBuffer[1]);
                GUI.Label(r00, "PositionBuffer[0]");
                GUI.Label(r01, "PositionBuffer[1]");
            }

            if (positionPrevBuffer != null) {
                var r10 = new Rect(rw * 1, rh * 0, rw, rh);
                var r11 = new Rect(rw * 1, rh * 1, rw, rh);
                GUI.DrawTexture(r10, positionPrevBuffer[0]);
                GUI.DrawTexture(r11, positionPrevBuffer[1]);
                GUI.Label(r10, "PositionPrevBuffer[0]");
                GUI.Label(r11, "PositionPrevBuffer[1]");
            }

            if (normalBuffer != null) {
                var r20 = new Rect(rw * 2, rh * 0, rw, rh);
                GUI.DrawTexture(r20, normalBuffer);
                GUI.Label(r20, "NormalBuffer");
            }

            GUI.color = color;
        }
    }
}