using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Common {
    public class PingPongBuffer : System.IDisposable {
        public ComputeBuffer Read => buffers[read];
        public ComputeBuffer Write => buffers[write];

        private int read = 0, write = 1;
        private ComputeBuffer[] buffers;

        public PingPongBuffer(int count, Type type) {
            buffers = new ComputeBuffer[2];
            buffers[0] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
            buffers[1] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
        }

        public void Swap() {
            var temp = read;
            read = write;
            write = temp;
        }

        public void Dispose() {
            buffers[0].Dispose();
            buffers[1].Dispose();
        }
    }
}