using System.Runtime.InteropServices;
using UnityEngine;
using mj.gist;

namespace ComputeShaderStudy {
    [ExecuteInEditMode]
    public class IndexTest : MonoBehaviour {
        [SerializeField] ComputeShader cs;
        [SerializeField] IndexType type = IndexType.DispatchThreadID;
        
        int num = 1024;

        ComputeBuffer dispatchThreadIDBuffer; // float
        ComputeBuffer groupIDBuffer;          // float3
        ComputeBuffer groupThreadIDBuffer;    // float3
        ComputeBuffer groupIndexBuffer;       // float3
        ComputeBuffer memorySharedBuffer;     // float3

        float[] floatArray;
        Vector3[] vectorArray;

        const int SIMULATION_BLOCK_SIZE = 128;

        void OnEnable() {
            floatArray  = new float[num];
            vectorArray = new Vector3[num];

            dispatchThreadIDBuffer = new ComputeBuffer(num, Marshal.SizeOf(typeof(float)));
            groupIDBuffer          = new ComputeBuffer(num, Marshal.SizeOf(typeof(Vector3)));
            groupThreadIDBuffer    = new ComputeBuffer(num, Marshal.SizeOf(typeof(Vector3)));
            groupIndexBuffer       = new ComputeBuffer(num, Marshal.SizeOf(typeof(float)));
            memorySharedBuffer     = new ComputeBuffer(num, Marshal.SizeOf(typeof(float)));

            var kernelID = cs.FindKernel(type.ToString());
            switch (kernelID) {
                case (int)IndexType.DispatchThreadID:
                    Dispath(kernelID, "_DispatchThreadIDBuffer", dispatchThreadIDBuffer, floatArray);
                    LogArray(floatArray);
                    break;

                case (int)IndexType.GroupID:
                    Dispath(kernelID, "_GroupIDBuffer", groupIDBuffer, vectorArray);
                    LogArray(vectorArray);
                    break;

                case (int)IndexType.GroupThreadID:
                    Dispath(kernelID, "_GroupThreadIDBuffer", groupThreadIDBuffer, vectorArray);
                    LogArray(vectorArray);
                    break;

                case (int)IndexType.GroupIndex:
                    Dispath(kernelID, "_GroupIndexBuffer", groupIndexBuffer, floatArray);
                    LogArray(floatArray);
                    break;

                case (int)IndexType.MemoryShared:
                    var temp = new float[num];
                    for (var i = 0; i < num; i++)
                        temp[i] = Mathf.Floor(i / 128) / 128.0f;

                    memorySharedBuffer.SetData(temp);
                    Dispath(kernelID, "_MemorySharedBuffer", memorySharedBuffer, floatArray);
                    LogArray(floatArray);
                    break;
            }

            ComputeShaderUtil.ReleaseBuffer(dispatchThreadIDBuffer);
            ComputeShaderUtil.ReleaseBuffer(groupIDBuffer);
            ComputeShaderUtil.ReleaseBuffer(groupThreadIDBuffer);
            ComputeShaderUtil.ReleaseBuffer(groupIndexBuffer);
            ComputeShaderUtil.ReleaseBuffer(memorySharedBuffer);
        }

        void LogArray(System.Array array) {
            for (var i = 0; i < array.Length; i++)
                Debug.Log($"{i} : {array.GetValue(i).ToString()}");
        }
        void Dispath(int kernelID, string name, ComputeBuffer buffer, System.Array array) {
            cs.SetBuffer(kernelID, name, buffer);
            cs.Dispatch(kernelID, SIMULATION_BLOCK_SIZE, 1, 1);
            buffer.GetData(array);
        }

        enum IndexType {
            DispatchThreadID,
            GroupID,
            GroupThreadID,
            GroupIndex,
            MemoryShared
        }
    }
}