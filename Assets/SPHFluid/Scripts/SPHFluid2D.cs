using UnityEngine;

namespace SPHFluid {

    public struct Particle {
        public Vector2 position;
        public Vector2 velocity;
    }

    public class SPHFluid2D : SPHFluidBase<Particle> {

        [SerializeField] protected float ballRadius = 0.1f;
        [SerializeField] protected float mouseRadius = 1f;

        private Vector3 screenToWorldPos;
        private bool isMouseDown;
        protected override void InitParticleData(Particle[] particles) {
            for(var i=0; i< ParticleNums; i++) {
                particles[i].velocity = Vector2.zero;
                particles[i].position = range / 2f + Random.insideUnitCircle * ballRadius;
            }
        }

        protected override void AdditionalCSParams(ComputeShader computeShader) {
            if (Input.GetMouseButtonDown(0)) {
                isMouseDown = true;
            }
            if (Input.GetMouseButtonUp(0)) {
                isMouseDown = false;
            }
            if (isMouseDown) {
                var mousePos = Input.mousePosition;
                mousePos.z = 10;
                screenToWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
            }

            computeShader.SetVector("_MousePos", screenToWorldPos);
            computeShader.SetFloat("_MouseRadius", mouseRadius);
            computeShader.SetBool("_MouseDown", isMouseDown);
        }

    }
}