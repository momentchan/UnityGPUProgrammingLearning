using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AppendBufferStudy {
    public class AppendBufferCs : MonoBehaviour {
        public enum Mode { AppendSingle, AppendMultiple }

        public Material material;
        public ComputeShader appendBufferShader;
        public Mode mode = Mode.AppendSingle;

        const int width = 32;
        const float size = 5.0f;

        ComputeBuffer buffer;
        ComputeBuffer argBuffer;

        int count = 0;

        void Start() {
            buffer = new ComputeBuffer(width * width, sizeof(float) * 3, ComputeBufferType.Append);
            buffer.SetCounterValue(0);
            argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            
            AppendBuffer();
        }

        void AppendBuffer() {
            var kernelId = appendBufferShader.FindKernel(mode.ToString());

            switch (mode) {
                case Mode.AppendSingle: {
                        appendBufferShader.SetBuffer(kernelId, "_AppendBuffer", buffer);
                        appendBufferShader.SetFloat("_Size", size);
                        appendBufferShader.SetFloat("_Width", width * 0.5f);
                        appendBufferShader.SetInt("_Count", count);
                        appendBufferShader.Dispatch(kernelId, 1, 1, 1);
                    }
                    break;
                case Mode.AppendMultiple: {
                        appendBufferShader.SetBuffer(kernelId, "_AppendBuffer", buffer);
                        appendBufferShader.SetFloat("_Size", size);
                        appendBufferShader.SetFloat("_Width", width);
                        appendBufferShader.Dispatch(kernelId, width / 8, width / 8, 1);
                    }
                    break;
            }

            int[] args = new int[] { 0, 1, 0, 0 };
            argBuffer.SetData(args);

            ComputeBuffer.CopyCount(buffer, argBuffer, 0);
            argBuffer.GetData(args);
            count = args[0];

            Debug.Log("vertex count " + args[0]);
            Debug.Log("instance count " + args[1]);
            Debug.Log("start vertex " + args[2]);
            Debug.Log("start instance " + args[3]);
        }
        private void Update() {
            if (Input.GetKeyDown(KeyCode.S) && mode == Mode.AppendSingle) {
                AppendBuffer();
            }
            material.SetPass(0);
            material.SetBuffer("_Buffer", buffer);
            material.SetColor("_Color", Color.white);
            //Graphics.DrawProceduralIndirectNow(MeshTopology.Points, argBuffer, 0);
            Graphics.DrawProceduralIndirect(material, new Bounds(Vector3.zero, Vector3.one * 10), MeshTopology.Points, argBuffer, 0);
        }

        void OnPostRender() {
            
        }

        void OnDestroy() {
            buffer.Release();
            argBuffer.Release();
        }
    }
}