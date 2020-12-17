using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravitationalNBodySimulation {
    public class CameraMotion : MonoBehaviour {

        [SerializeField] protected Transform target;
        [SerializeField] protected float radius = 40f;
        [SerializeField] protected float speed = 1f;

        protected float time;
        void Update() {
            time += Time.deltaTime * speed;

            transform.position = new Vector3(radius * Mathf.Cos(time),
                                             radius * Mathf.Sin(time * 0.5f), 
                                             radius * Mathf.Sin(time));

            if(target != null)
                transform.LookAt(target);
        }
    }
}