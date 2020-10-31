using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleComputeShader {
    public class ArrayTest : MonoBehaviour {
        [SerializeField] protected ComputeShader cs;
        [SerializeField] int testInt = 2;

        int size = 4;
        int kernelA, kernelB;
        ComputeBuffer buffer;

        private void Start() {
            kernelA = cs.FindKernel("_KernelA");
            kernelB = cs.FindKernel("_KernelB");
            buffer = new ComputeBuffer(size, sizeof(int));

            Run(kernelA, buffer);
            Run(kernelB, buffer);

            buffer.Release();
        }

        void Run(int kernel, ComputeBuffer buffer) {
            cs.SetBuffer(kernel, "_intBuffer", buffer);
            cs.SetInt("_value", testInt);
            cs.Dispatch(kernel, 1, 1, 1);

            int[] result = new int[size];
            buffer.GetData(result);
            Debug.Log($"Result of kernel : {kernel} ");

            for (int i = 0; i < size; i++) {
                Debug.Log(result[i]);
            }
        }
    }
}