using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    interface ComputeShaderUser  {
        void InitBuffers();
        void InitKernels();
    }
}