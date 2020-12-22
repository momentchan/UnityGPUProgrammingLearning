using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    interface IComputeShader  {
        void InitBuffers();
        void InitKernels();
    }
}