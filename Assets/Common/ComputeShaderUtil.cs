using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public class ComputeShaderUtil {
        public static void ReleaseBuffer(ComputeBuffer buffer) {
            if (buffer != null) {
                buffer.Release();
                buffer = null;
            }
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