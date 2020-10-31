using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCMC {
    public class Metropolis3dDemo : MonoBehaviour {

        [SerializeField] protected int lEdge = 20;
        [SerializeField] protected int loop = 400;
        [SerializeField] protected int nInitialize = 100;
        [SerializeField] protected int nLimit = 100;
        [SerializeField] protected float threshold = -100;
        [SerializeField] protected GameObject[] prefabs;

        private Vector4[] data;
        private Metropolis3d metropolis;

        void Start() {
            InitializeData();
            metropolis = new Metropolis3d(data, lEdge * Vector3.one);
            StartCoroutine(Generate());
        }

        private void InitializeData() {
            data = new Vector4[lEdge * lEdge * lEdge];
            var sn = new SimplexNoiseGenerator();
            for (var x = 0; x < lEdge; x++) {
                for (var y = 0; y < lEdge; y++) {
                    for(var z =0; z < lEdge; z++) {
                        var i = x + y * lEdge + z * lEdge * lEdge;
                        var val = sn.noise(x, y, z);
                        data[i] = new Vector4(x, y, z, val);
                    }
                }
            }
        }

        IEnumerator Generate() {
            for (int i = 0; i < loop; i++) {
                var prefab = prefabs[Mathf.FloorToInt(UnityEngine.Random.value * prefabs.Length)];
                yield return new WaitForSeconds(0.1f);
                foreach (var pos in metropolis.Chain(nInitialize, nLimit, threshold)) {
                    Instantiate(prefab, pos, Quaternion.identity, this.transform);
                }
            }
        }
    }
}