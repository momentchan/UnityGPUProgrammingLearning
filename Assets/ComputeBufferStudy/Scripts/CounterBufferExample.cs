using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeBufferStudy {
    public class CounterBufferExample : MonoBehaviour {

        public ComputeShader incrementCount;

        void Start() {
            int size = 4 * 4 * 1 * 1;

            ComputeBuffer counterBuffer = new ComputeBuffer(size, sizeof(int), ComputeBufferType.Counter);
            counterBuffer.SetCounterValue(0);

            incrementCount.SetBuffer(0, "counterBuffer", counterBuffer);
            incrementCount.Dispatch(0, 1, 1, 1);

            int[] data = new int[size];
            counterBuffer.GetData(data);

            for (int i = 0; i < data.Length; i++)
                Debug.Log(data[i]);

            ComputeBuffer argBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);

            int[] args = new int[] { 0 };
            ComputeBuffer.CopyCount(counterBuffer, argBuffer, 0);
            argBuffer.GetData(args);
            Debug.Log("Count " + args[0]);

            argBuffer.Release();
            counterBuffer.Release();
        }

    }
}