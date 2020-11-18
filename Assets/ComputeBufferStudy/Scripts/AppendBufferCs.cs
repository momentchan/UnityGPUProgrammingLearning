using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeBufferStudy {
    public class AppendBufferCs : MonoBehaviour {
        public enum Mode { AppendSingle, AppendMultiple }

        public ComputeShader appendBufferShader;
        public ComputeShader consumeBufferShader;
        public Material material;
        public Mode mode = Mode.AppendSingle;

        const int width = 32;
        const float size = 5.0f;
        int count = 0;
        int[] args = new int[] { 0, 1, 0, 0 };

        ComputeBuffer buffer;
        ComputeBuffer argBuffer;

        void Start() {
            buffer = new ComputeBuffer(width * width, sizeof(float) * 3, ComputeBufferType.Append);
            buffer.SetCounterValue(0);
            argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

            AppendBuffer();
        }

        void AppendBuffer() {
            var kernelId = appendBufferShader.FindKernel(mode.ToString());
            appendBufferShader.SetBuffer(kernelId, "_AppendBuffer", buffer);
            appendBufferShader.SetFloat("_Size", size);
            appendBufferShader.SetFloat("_Width", width);

            if (mode == Mode.AppendSingle) {
                appendBufferShader.SetInt("_Count", count);
                appendBufferShader.Dispatch(kernelId, 1, 1, 1);
            } else {
                appendBufferShader.Dispatch(kernelId, width / 8, width / 8, 1);
            }

            LogBufferInfo();
        }

        void ConsumeBuffer() {
            consumeBufferShader.SetBuffer(0, "_ConsumeBuffer", buffer);
            consumeBufferShader.Dispatch(0, 1, 1, 1);

            LogBufferInfo();
        }

        void LogBufferInfo() {
            argBuffer.SetData(args);

            ComputeBuffer.CopyCount(buffer, argBuffer, 0);
            argBuffer.GetData(args);
            count = args[0];
            Debug.Log("vertex count " + args[0]);
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.S) && mode == Mode.AppendSingle) {
                AppendBuffer();
            }

            if (Input.GetKeyDown(KeyCode.D) && mode == Mode.AppendMultiple) {
                ConsumeBuffer();
            }
            material.SetPass(0);
            material.SetBuffer("_Buffer", buffer);
            material.SetColor("_Color", Color.white);
            Graphics.DrawProceduralIndirect(material, new Bounds(Vector3.zero, Vector3.one * 10), MeshTopology.Points, argBuffer, 0);
        }

        void OnDestroy() {
            buffer.Release();
            argBuffer.Release();
        }
    }
}