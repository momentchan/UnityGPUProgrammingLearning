using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryWireframe {
    public class AutoRate : MonoBehaviour {
        [SerializeField] protected Vector3 axis = Vector3.up;
        [SerializeField] protected float speed = 30;

        void Update() {
            transform.Rotate(axis, speed * Time.deltaTime);
        }
    }
}