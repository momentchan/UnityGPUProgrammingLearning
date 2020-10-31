using UnityEngine;

namespace SimpleComputeShader {
    public class TextureTest : MonoBehaviour {
        public MeshRenderer rendererA, rendererB;
        public ComputeShader cs;

        int kernelA, kernelB;
        RenderTexture textureA, textureB;

        void Start() {
            kernelA = cs.FindKernel("KernelA");
            kernelB = cs.FindKernel("KernelB");

            textureA = CreateTexture(512, 512);
            textureB = CreateTexture(512, 512);

            Run(kernelA, textureA);
            Run(kernelB, textureB);

            rendererA.material.mainTexture = textureA;
            rendererB.material.mainTexture = textureB;
        }

        RenderTexture CreateTexture(int width, int height) {
            var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            texture.enableRandomWrite = true;
            texture.Create();
            return texture;
        }

        void Run(int kernel, RenderTexture texture) {
            uint x, y, z;
            cs.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
            cs.SetTexture(kernel, "_textureBuffer", texture);
            cs.Dispatch(kernel, texture.width / (int)x, texture.height / (int)y, (int)z);
        }
    }
}