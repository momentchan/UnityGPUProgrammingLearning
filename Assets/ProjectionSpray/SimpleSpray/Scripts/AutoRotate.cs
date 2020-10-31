using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    public class AutoRotate : MonoBehaviour {
        [SerializeField] protected float rotateAngle = 90f;

        void Update() {
            transform.Rotate(Vector3.up, rotateAngle * Time.deltaTime, Space.World);
        }
    }
}