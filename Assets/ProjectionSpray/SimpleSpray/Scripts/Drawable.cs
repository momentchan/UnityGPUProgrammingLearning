using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    public class Drawable : MonoBehaviour {
        [SerializeField] protected int textureSize = 1024;
        [SerializeField] protected Color initialColor = Color.gray;
        [SerializeField] protected Material fillCrack;
        [SerializeField] RenderTexture output;

        protected RenderTexture[] pingpongRts;
        protected Mesh mesh;

        private void Start() {
            output = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
            output.Create();
            var m = GetComponent<Renderer>().material;
            m.SetTexture("_MainTex", output);

            pingpongRts = new RenderTexture[2];
            for(var i=0; i< pingpongRts.Length; i++ ) {
                var outputRt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
                outputRt.Create();
                RenderTexture.active = outputRt;
                GL.Clear(true, true, initialColor);
                pingpongRts[i] = outputRt;
            }
            mesh = GetComponent<MeshFilter>().sharedMesh;
            Graphics.CopyTexture(pingpongRts[0], output);
        }

        private void OnDestroy() {
            foreach (var rt in pingpongRts)
                rt.Release();
            output.Release();
        }

        public void Draw(Material drawingMat) {
            drawingMat.SetTexture("_MainTex", pingpongRts[0]);

            var currentActive = RenderTexture.active;
            RenderTexture.active = pingpongRts[1];
            GL.Clear(true, true, Color.clear);
            drawingMat.SetPass(0);
            Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix);
            RenderTexture.active = currentActive;

            Swap(pingpongRts);

            if (fillCrack != null) {
                Graphics.Blit(pingpongRts[0], pingpongRts[1], fillCrack);
                Swap(pingpongRts);
            }

            Graphics.CopyTexture(pingpongRts[0], output);
        }

        private void Swap<T>(T[] array) {
            var temp = array[0];
            array[0] = array[1];
            array[1] = temp;
        }
    }
}