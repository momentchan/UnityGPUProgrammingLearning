using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using mj.gist;


namespace Voxelizer {
    public class GPUVoxelizer  {
        public static GPUVoxelData Voxelize(ComputeShader voxelizer, Mesh mesh, int resolution = 64) {
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;

            var maxLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var unit = maxLength / resolution;

            var hunit = 0.5f * unit;
            var start = bounds.min - Vector3.one * hunit;
            var end = bounds.max + Vector3.one * hunit;
            var size = end - start;

            var width = Mathf.CeilToInt(size.x / unit);
            var height = Mathf.CeilToInt(size.y / unit);
            var depth = Mathf.CeilToInt(size.z / unit);

            var voxelBuffer = new ComputeBuffer(width * height * depth, Marshal.SizeOf(typeof(Voxel_t)));
            var voxels = new Voxel_t[voxelBuffer.count];
            voxelBuffer.SetData(voxels);

            voxelizer.SetVector("_Start", start);
            voxelizer.SetVector("_End", end);
            voxelizer.SetVector("_Size", size);

            voxelizer.SetFloat("_Unit", unit);
            voxelizer.SetFloat("_InvUnit", 1 / unit);
            voxelizer.SetFloat("_HalfUnit", hunit);

            voxelizer.SetInt("_Width", width);
            voxelizer.SetInt("_Height", height);
            voxelizer.SetInt("_Depth", depth);

            var vertices = mesh.vertices;
            var verticeBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(Vector3)));
            verticeBuffer.SetData(vertices);

            var triangles = mesh.triangles;
            var triangleBuffer = new ComputeBuffer(triangles.Length, Marshal.SizeOf(typeof(int)));
            triangleBuffer.SetData(triangles);
            var triangleCount = triangleBuffer.count / 3;
            voxelizer.SetInt("_TriangleCount", triangleCount);

            var surfaceFrontKer = new Kernel(voxelizer, "SurfaceFront");
            voxelizer.SetBuffer(surfaceFrontKer.Index, "_VoxelBuffer", voxelBuffer);
            voxelizer.SetBuffer(surfaceFrontKer.Index, "_VertBuffer", verticeBuffer);
            voxelizer.SetBuffer(surfaceFrontKer.Index, "_TriBuffer", triangleBuffer);
            voxelizer.Dispatch(surfaceFrontKer.Index, triangleCount / (int)surfaceFrontKer.ThreadX + 1, (int)surfaceFrontKer.ThreadY, (int)surfaceFrontKer.ThreadZ);

            var surfaceBackKer = new Kernel(voxelizer, "SurfaceBack");
            voxelizer.SetBuffer(surfaceBackKer.Index, "_VoxelBuffer", voxelBuffer);
            voxelizer.SetBuffer(surfaceBackKer.Index, "_VertBuffer", verticeBuffer);
            voxelizer.SetBuffer(surfaceBackKer.Index, "_TriBuffer", triangleBuffer);
            voxelizer.Dispatch(surfaceBackKer.Index, triangleCount / (int)surfaceBackKer.ThreadX + 1, (int)surfaceBackKer.ThreadY, (int)surfaceBackKer.ThreadZ);

            var volumeKer = new Kernel(voxelizer, "Volume");
            voxelizer.SetBuffer(volumeKer.Index, "_VoxelBuffer", voxelBuffer);
            voxelizer.Dispatch(volumeKer.Index, width / (int)volumeKer.ThreadX + 1, height / (int)volumeKer.ThreadY + 1, (int)volumeKer.ThreadZ);

            verticeBuffer.Release();
            triangleBuffer.Release();
            return new GPUVoxelData(voxelBuffer, width, height, depth, unit);
        }

        internal static GPUVoxelData Voxelize(object voxelizer, Mesh mesh, int resolution) {
            throw new NotImplementedException();
        }
    }
}