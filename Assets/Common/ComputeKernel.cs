using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Common {
    public class ComputeKernel<T> {

        public GPUThreads Threads => threads;

        public int GetKernelIndex(T type) {
            return kernelMap[type];
        }

        private Dictionary<T, int> kernelMap = new Dictionary<T, int>();
        private GPUThreads threads;

        public ComputeKernel(ComputeShader cs) {
            kernelMap = Enum.GetValues(typeof(T)).Cast<T>().ToDictionary(t => t, t => cs.FindKernel(t.ToString()));
            threads = ComputeShaderUtil.GetThreadGroupSize(cs, kernelMap.FirstOrDefault().Value);
        }
    }
}