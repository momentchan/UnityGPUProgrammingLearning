using UnityEngine;

namespace StableFluid {
    public class MouseSourceProvider : MonoBehaviour {

        public SourceEvent OnSourceUpdated;

        [SerializeField] protected float sourceRadius;
        [SerializeField] protected Material sourceMat;
        [SerializeField] protected RenderTexture sourceTex;

        private string source2dProp = "_Source";
        private string sourceRadiusProp = "_Radius";
        private int source2dId, sourceRadiusId;

        private Vector3 lastSourcePos;

        void Awake() {
            source2dId = Shader.PropertyToID(source2dProp);
            sourceRadiusId = Shader.PropertyToID(sourceRadiusProp);
        }

        void Update() {
            InitializeSourceTex(Screen.width, Screen.height);
            UpdateSource();
        }

        void OnDestroy() {
            ReleaseForceField();
        }

        private void InitializeSourceTex(int width, int height) {
            if (sourceTex == null || sourceTex.width != width || sourceTex.height != height) {
                ReleaseForceField();
                sourceTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            }
        }

        private void UpdateSource() {
            var mousePos = Input.mousePosition;
            var velocity = GetSourceNormalizedVelocity(mousePos);
            var uv = Vector2.zero;

            if (Input.GetMouseButton(0)) {
                uv = Camera.main.ScreenToViewportPoint(mousePos);
                sourceMat.SetVector(source2dId, new Vector4(velocity.x, velocity.y, uv.x, uv.y));
                sourceMat.SetFloat(sourceRadiusId, sourceRadius);
                Graphics.Blit(null, sourceTex, sourceMat);
                NotifySourceTexUpdated();
            } else {
                NotifyNoSourceTexUpdated();
            }
        }


        void NotifySourceTexUpdated() {
            OnSourceUpdated.Invoke(sourceTex);
        }

        void NotifyNoSourceTexUpdated() {
            OnSourceUpdated.Invoke(null);
        }

        Vector3 GetSourceNormalizedVelocity(Vector3 sourcePos) {
            var dpdt = (lastSourcePos - sourcePos).normalized;
            lastSourcePos = sourcePos;
            return dpdt;
        }

        private void ReleaseForceField() {
            Destroy(sourceTex);
        }

        [System.Serializable]
        public class SourceEvent : UnityEngine.Events.UnityEvent<RenderTexture> { }
    }
}