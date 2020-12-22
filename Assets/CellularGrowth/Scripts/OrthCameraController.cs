using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CellularGrowth {

    [RequireComponent(typeof(Camera))]
    public class OrthCameraController : MonoBehaviour {

        protected Camera orthCam;

        static readonly string kMouseX = "Mouse X";
        static readonly string kMouseY = "Mouse Y";
        static readonly string kMouseScrollWheel = "Mouse ScrollWheel";

        [SerializeField, Range(0.1f, 10f)] protected float panSpeed = 3f, panDelta = 2f;
        [SerializeField, Range(0.1f, 10f)] protected float zoomSpeed = 8f, zoomDelta = 2f;

        protected Vector3 moveTarget;
        protected float orthographicSize;

        void Start() {
            orthCam = GetComponent<Camera>();
            moveTarget = transform.position;
            orthographicSize = orthCam.orthographicSize;
        }

        void Update() {
            var dt = Time.deltaTime;
            Pan(dt);
            Zoom(dt);
        }

        private void Pan(float dt) {
            if (Input.GetMouseButton(1)) {
                var mouseX = Input.GetAxis(kMouseX);
                var mouseY = Input.GetAxis(kMouseY);
                moveTarget = transform.position;
                moveTarget -= transform.right * mouseX * panSpeed;
                moveTarget -= transform.up * mouseY * panSpeed;
            }
            transform.position = Vector3.Lerp(transform.position, moveTarget, dt * panDelta);
        }

        private void Zoom(float dt) {
            float scroll = Input.GetAxis(kMouseScrollWheel);
            if (scroll != 0) {
                orthographicSize -= scroll * zoomSpeed;
            }
            orthCam.orthographicSize = Mathf.Lerp(orthCam.orthographicSize, orthographicSize, dt * zoomDelta);
        }
    }
}