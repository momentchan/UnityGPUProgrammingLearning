using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUClothSimulation {
    public class CollisionController : MonoBehaviour {

        [SerializeField] private Transform cloth;
        [SerializeField] private float collisionSize = 1f;
        [SerializeField] private float maxPressDistance = 1f;
        [SerializeField] private float pressSpeed = 1f;

        private float pressNearDepth;
        private float pressFarDepth => pressNearDepth + maxPressDistance;

        private Camera cam;
        private Vector3 worldPos;

        void Start() {
            cam = Camera.main;
            pressNearDepth = transform.position.z;
        }

        void Update() {
            transform.localScale = Vector3.one * collisionSize;

            if (Input.GetMouseButton(0)) {
                var mousePos = new Vector3(Input.mousePosition.x, 
                                           Input.mousePosition.y, 
                                           cam.transform.InverseTransformPoint(transform.position).z);

                worldPos = cam.ScreenToWorldPoint(mousePos);
                worldPos.z = Mathf.Lerp(transform.position.z, pressFarDepth, Time.deltaTime * pressSpeed);
            } else {
                worldPos = transform.position;
                worldPos.z = Mathf.Lerp(worldPos.z, pressNearDepth, Time.deltaTime * pressSpeed);
            }

            transform.position = worldPos;
        }

        private void OnDrawGizmos() {
            Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
            Gizmos.DrawSphere(transform.position, collisionSize * 0.5f);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawCube(new Vector3(transform.position.x, transform.position.y, pressNearDepth), new Vector3(1, 1, 0));

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawCube(new Vector3(transform.position.x, transform.position.y, pressFarDepth), new Vector3(1, 1, 0));
        }
    }
}