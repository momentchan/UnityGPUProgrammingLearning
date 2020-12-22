using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Common {
    public class Kernel {
        public int Index { get { return index; } }
        public uint ThreadX { get { return threadX; } }
        public uint ThreadY { get { return threadY; } }
        public uint ThreadZ { get { return threadZ; } }
        int index;
        uint threadX, threadY, threadZ;

        public Kernel(ComputeShader shader, string key) {
            index = shader.FindKernel(key);
            if (index < 0) {
                Debug.LogWarning("Can't find kernel: " + key);
                return;
            }
            shader.GetKernelThreadGroupSizes(index, out threadX, out threadY, out threadZ);
        }
    }

    public class PingPongBuffer : System.IDisposable {
        public ComputeBuffer Read  => buffers[read];
        public ComputeBuffer Write => buffers[write];

        private int read = 0, write = 1;
        private ComputeBuffer[] buffers;

        public PingPongBuffer(int count, int stride) {
            buffers = new ComputeBuffer[2];
            buffers[0] = new ComputeBuffer(count, stride, ComputeBufferType.Default);
            buffers[1] = new ComputeBuffer(count, stride, ComputeBufferType.Default);
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

    public struct GPUThreads {
        public int x;
        public int y;
        public int z;

        public GPUThreads(uint x, uint y, uint z) {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }
    }

    public class ComputeShaderUtil {
        public static void ReleaseBuffer(ComputeBuffer buffer) {
            if (buffer != null) {
                buffer.Release();
                buffer = null;
            }
        }
        public static void SwapBuffer(ref ComputeBuffer ping, ref ComputeBuffer pong) {
            var temp = pong;
            pong = ping;
            ping = temp;
        }

        public static GPUThreads GetThreadGroupSize(ComputeShader compute, int kernel) {
            uint threadX, threadY, threadZ;
            compute.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
            return new GPUThreads(threadX, threadY, threadZ);
        }
        public static void InitialCheck(int count, GPUThreads gpuThreads) {
            Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50, "Under the DirectCompute5.0 (DX11 GPU) doesn't work");
            Assert.IsTrue(gpuThreads.x * gpuThreads.y * gpuThreads.z <= DirectCompute5_0.MAX_PROCESS, "Resolution is too heigh");
            Assert.IsTrue(gpuThreads.x <= DirectCompute5_0.MAX_X, "THREAD_X is too large");
            Assert.IsTrue(gpuThreads.y <= DirectCompute5_0.MAX_Y, "THREAD_Y is too large");
            Assert.IsTrue(gpuThreads.z <= DirectCompute5_0.MAX_Z, "THREAD_Z is too large");
            Assert.IsTrue(count <= DirectCompute5_0.MAX_PROCESS, "particleNumber is too large");
        }
    }

    public static class DirectCompute5_0 {
        //Use DirectCompute 5.0 on DirectX11 hardware.
        public const int MAX_THREAD = 1024;
        public const int MAX_X = 1024;
        public const int MAX_Y = 1024;
        public const int MAX_Z = 64;
        public const int MAX_DISPATCH = 65535;
        public const int MAX_PROCESS = MAX_DISPATCH * MAX_THREAD;
    }
}