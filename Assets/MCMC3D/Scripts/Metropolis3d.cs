using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCMC {
    public class Metropolis3d {

        private readonly int limitResetLoopCount = 100;
        private readonly int weightReferenceloopCount = 500;

        private Vector4[] data;
        private Vector3 scale;

        private Vector3 current;
        private float currentDensity = 0;

        public Metropolis3d(Vector4[] data, Vector3 scale) {
            this.data = data;
            this.scale = scale;
        }

        public IEnumerable<Vector3> Chain(int nIntialize, int nLimit, float threshold) {

            ResetCurrent();

            for (var i = 0; i < nIntialize; i++)
                if (Next(threshold)) 
                    break;

            for (var i = 0; i < nLimit; i++)
                if(Next(threshold))
                    yield return current;
        }

        private void ResetCurrent() {

            for (var i = 0; currentDensity <= 0 && i < limitResetLoopCount; i++) {
                current = new Vector3(scale.x * Random.value, scale.y * Random.value, scale.z * Random.value);
                currentDensity = ComputeDensity(current);
            }
        }

        private bool Next(float threshold) {
            Vector3 next = current + GaussianDistribution3d.GenerateRandomPointStandard();

            var nextDensity = ComputeDensity(next);
            var f = (currentDensity <= 0 || Mathf.Clamp(nextDensity / currentDensity, 0f, 1f) >= Random.value) && nextDensity > threshold;
            if (f) {
                current = next;
                currentDensity = nextDensity;
            }

            return f;
        }

        private float ComputeDensity(Vector3 pos) {
            float weight = 0f;
            for(var i=0; i < weightReferenceloopCount; i++) {
                int id = Mathf.FloorToInt(Random.Range(0f, data.Length));
                Vector3 npos = data[id];
                float mag = Vector3.Magnitude(pos - npos);
                weight += Mathf.Exp(-mag) * data[id].w;
            }
            return weight;
        }

    }
}