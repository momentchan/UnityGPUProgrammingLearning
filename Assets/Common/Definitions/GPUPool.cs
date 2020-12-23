using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    public class GPUPool : IDisposable {

        public ComputeBuffer PoolBuffer => poolBuffer;
        public ComputeBuffer CountBuffer => countBuffer;

        protected ComputeBuffer poolBuffer, countBuffer;
        protected int[] countArgs = new int[4] { 0, 1, 0, 0 };

        public GPUPool(int count, Type type) {
            poolBuffer = new ComputeBuffer(count, sizeof(int), ComputeBufferType.Append);
            poolBuffer.SetCounterValue(0);
            countBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            countBuffer.SetData(countArgs);
        }

        public void ResetPoolCounter() {
            poolBuffer.SetCounterValue(0);
        }

        public int CopyPoolSize() {
            countBuffer.SetData(countArgs);
            ComputeBuffer.CopyCount(poolBuffer, countBuffer, 0);
            countBuffer.GetData(countArgs);
            return countArgs[0];
        }

        public virtual void Dispose() {
            poolBuffer.Dispose();
            countBuffer.Dispose();
        }
    }
}