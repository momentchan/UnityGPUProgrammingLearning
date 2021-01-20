﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Common {
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
}