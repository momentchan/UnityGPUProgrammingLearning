using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryShader {

    public class GeometryGrass : MonoBehaviour {
        [SerializeField, Range(1, 50)] protected int width  = 10;
        [SerializeField, Range(1, 50)] protected int height = 10;
        [SerializeField, Range(0, 10)] protected float distance = 5;
        [SerializeField] protected Material grassMat;

        void Start() {
            for(int i=0; i<width; i++) {
                for(int j=0; j < height; j++) {
                    var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    plane.GetComponent<Renderer>().material = grassMat;

                    plane.transform.parent = transform;
                    plane.transform.position = new Vector3(
                        distance * (i - width / 2),
                        0,
                        distance * (j - height / 2)
                    );

                    var rand = Random.value;
                    plane.transform.rotation = Quaternion.Euler(0f, rand * 360, 0f);
                }
            }

        }
    }
}