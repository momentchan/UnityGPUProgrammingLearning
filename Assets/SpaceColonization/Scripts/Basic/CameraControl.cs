using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceColonization {
    public class CameraControl : MonoBehaviour {

        [SerializeField] protected float zoomSpeed = 1f;
        [SerializeField] protected bool start;
        Vector3 initialPos;

        private void Start() {
            initialPos = transform.position;
        }
        void Update() {
            if (Input.GetKeyDown(KeyCode.R))
                transform.position = initialPos;
            if (Input.GetKeyDown(KeyCode.S))
                start = !start;
            if (start) {
                transform.position += transform.forward * zoomSpeed * Time.deltaTime;
            }

        }
    }
}