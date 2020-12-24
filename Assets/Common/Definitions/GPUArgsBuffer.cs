using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    public class GPUArgsBuffer : IDisposable {

        public ComputeBuffer Buffer => buffer;

        private ComputeBuffer buffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        public GPUArgsBuffer(uint vertexCount, uint instanceCount) {
            args[0] = vertexCount;
            args[1] = instanceCount;
            buffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            buffer.SetData(args);
        }

        public void Dispose() {
            buffer.Dispose();
        }
    }
}