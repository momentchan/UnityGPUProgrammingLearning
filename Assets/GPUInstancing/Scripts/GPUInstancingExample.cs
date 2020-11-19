using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancing {
    public class GPUInstancingExample : MonoBehaviour {
        public Transform prefab;
        public int instances = 5000;
        public float radius = 50f;

        void Start() {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (var i = 0; i < instances; i++) {
                var t = Instantiate(prefab, Random.insideUnitSphere * radius, Quaternion.identity, transform);


                // Method 1 : Creating a new material
                // t.GetComponent<MeshRenderer>().material.color =
                //     new Color(Random.value, Random.value, Random.value);

                // Method 2 : Use property block
                block.SetColor("_Color", new Color(Random.value, Random.value, Random.value));
                var r = t.GetComponent<MeshRenderer>();
                if (r) {
                    r.SetPropertyBlock(block);
                } else {
                    for (int ci = 0; ci < t.childCount; ci++) {
                        r = t.GetChild(ci).GetComponent<MeshRenderer>();
                        if (r) {
                            r.SetPropertyBlock(block);
                        }
                    }
                }
            }

        }
    }
}