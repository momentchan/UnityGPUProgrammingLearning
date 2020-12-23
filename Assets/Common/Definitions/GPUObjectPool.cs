using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Common {
    public class GPUObjectPool : GPUPool {

        public ComputeBuffer ObjectBuffer => objectBuffer;

        protected ComputeBuffer objectBuffer;

        public GPUObjectPool(int count, System.Type type) : base(count, type) {
            objectBuffer = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
        }

        public override void Dispose() {
            base.Dispose();
            objectBuffer.Dispose();
        }
    }
}