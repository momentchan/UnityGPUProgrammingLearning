using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    [RequireComponent(typeof(Camera))]
    public class CameraViewController : MonoBehaviour {

        [SerializeField] protected float speedH, speedV;
        private Camera camera;
        private float yaw, pitch;

        void Start() {
            camera = GetComponent<Camera>();
        }

        void Update() {
            yaw += speedH * Input.GetAxis("Mouse X");
            pitch -= speedV * Input.GetAxis("Mouse Y");

            transform.eulerAngles = new Vector3(pitch, yaw, 0);
        }
    }
}