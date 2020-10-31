using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxelizer {
    public class CPUVoxelizer {

        public static void Voxelize(Mesh mesh, int resolution, out List<Voxel_t> voxels, out float unit, bool surfaceOnly = false) {
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;

            var maxLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            unit = maxLength / resolution;

            var hunit = 0.5f * unit;
            var start = bounds.min - Vector3.one * hunit;
            var end = bounds.max + Vector3.one * hunit;
            var size = end - start;

            var width   = Mathf.CeilToInt(size.x / unit);
            var height  = Mathf.CeilToInt(size.y / unit);
            var depth   = Mathf.CeilToInt(size.z / unit);
            var volume  = new Voxel_t[width, height, depth];

            #region Create AABB
            var boxes = new Bounds[width, height, depth];
            var voxelUnitSize = Vector3.one * unit;

            for(var x=0; x < width; x++) {
                for(var y = 0; y < height; y++) {
                    for(var z=0; z < depth; z++) {
                        var p = start + new Vector3(x, y, z) * unit;
                        var aabb = new Bounds(p, voxelUnitSize);
                        boxes[x, y, z] = aabb;
                    }
                }
            }

            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            var direction = Vector3.forward;
            #endregion

            #region Create Surface Voxels
            for (var i = 0; i < indices.Length; i += 3) {
                // Create mesh triangle
                var tri = new Triangle(
                    vertices[indices[i]],
                    vertices[indices[i + 1]],
                    vertices[indices[i + 2]],
                    direction);

                // Create aabb box contains this triangle
                var min = tri.bounds.min - start;
                var max = tri.bounds.max - start;
                int iminX = Mathf.RoundToInt(min.x / unit), iminY = Mathf.RoundToInt(min.y / unit), iminZ = Mathf.RoundToInt(min.z / unit);
                int imaxX = Mathf.RoundToInt(max.x / unit), imaxY = Mathf.RoundToInt(max.y / unit), imaxZ = Mathf.RoundToInt(max.z / unit);
                iminX = Mathf.Clamp(iminX, 0, width - 1);
                iminY = Mathf.Clamp(iminY, 0, height - 1);
                iminZ = Mathf.Clamp(iminZ, 0, depth - 1);
                imaxX = Mathf.Clamp(imaxX, 0, width - 1);
                imaxY = Mathf.Clamp(imaxY, 0, height - 1);
                imaxZ = Mathf.Clamp(imaxZ, 0, depth - 1);

                // Intersection Check
                uint front = (uint)(tri.frontFacing ? 1 : 0);
                for (var x = iminX; x <= imaxX; x++) {
                    for (var y = iminY; y <= imaxY; y++) {
                        for (var z = iminZ; z <= imaxZ; z++) {
                            if (Intersects(tri, boxes[x, y, z])) {
                                var voxel = volume[x, y, z];
                                voxel.position = boxes[x, y, z].center;

                                if ((voxel.fill & 1) == 0)                  // Haven't been filled
                                    voxel.front = 1;
                                else
                                    voxel.front = voxel.front & front;     // make back higher priority
                                voxel.fill = 1;
                                volume[x, y, z] = voxel;
                            }
                        }
                    }
                }
            }
            #endregion

            #region Create Inside Voxels
            if (!surfaceOnly) {
                for(var x = 0; x < width; x++) {
                    for(var y = 0; y < height; y++) {
                        for(var z = 0; z < depth; z++) {
                            if (volume[x, y, z].IsEmpty()) continue;

                            // Find front index
                            int ifront = z;
                            for (; ifront < depth && volume[x, y, ifront].IsFrontFace(); ifront++) { }
                            if (ifront >= depth) break;
                            
                            // Find back index
                            int iback = ifront;
                            for (; iback < depth && volume[x, y, iback].IsEmpty(); iback++) { }
                            if (iback >= depth) break;

                            if (volume[x, y, iback].IsBackFace()) {
                                for (; iback < depth && volume[x, y, iback].IsBackFace(); iback++) { }
                            }

                            for(int z2 = ifront; z2 < iback; z2++) {
                                var p = boxes[x, y, z2].center;
                                var voxel = volume[x, y, z2];
                                voxel.position = p;
                                voxel.fill = 1;
                                volume[x, y, z2] = voxel;
                            }
                            z = iback;
                        }
                    }
                }
            }
            #endregion

            #region Get Non-Empty Voxels
            voxels = new List<Voxel_t>();
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    for (var z = 0; z < depth; z++) {
                        if (!volume[x, y, z].IsEmpty())
                            voxels.Add(volume[x, y, z]);
                    }
                }
            }
            #endregion
        }

        private static bool Intersects(Triangle tri, Bounds aabb) {
            Vector3 center = aabb.center, extents = aabb.extents;

            // Translate triange
            Vector3 v0 = tri.a - center,
                    v1 = tri.b - center,
                    v2 = tri.c - center;

            // Edge vectors
            Vector3 e0 = v1 - v0,
                    e1 = v0 - v2,
                    e2 = v2 - v1;

            // cross products of triangle edges & aabb edges
            // AABB normals are the x (1, 0, 0), y (0, 1, 0), z (0, 0, 1) axis.
            // so we can get the cross products between triangle edge vectors and AABB normals without calculation
            Vector3 a00 = new Vector3(0, -e0.z, e0.y), // cross product of X and e0
                    a01 = new Vector3(0, -e1.z, e1.y), // X and e1
                    a02 = new Vector3(0, -e2.z, e2.y), // X and e2
                    a10 = new Vector3(e0.z, 0, -e0.x), // Y and e0
                    a11 = new Vector3(e1.z, 0, -e1.x), // Y and e1
                    a12 = new Vector3(e2.z, 0, -e2.x), // Y and e2
                    a20 = new Vector3(-e0.y, e0.x, 0), // Z and e0
                    a21 = new Vector3(-e1.y, e1.x, 0), // Z and e1
                    a22 = new Vector3(-e2.y, e2.x, 0); // Z and e2

            // Test 9 axis
            if(
                !Intersects(v0, v1, v2, extents, a00) ||
                !Intersects(v0, v1, v2, extents, a01) ||
                !Intersects(v0, v1, v2, extents, a02) ||
                !Intersects(v0, v1, v2, extents, a10) ||
                !Intersects(v0, v1, v2, extents, a11) ||
                !Intersects(v0, v1, v2, extents, a12) ||
                !Intersects(v0, v1, v2, extents, a20) ||
                !Intersects(v0, v1, v2, extents, a21) ||
                !Intersects(v0, v1, v2, extents, a22)
            ) {
                return false;
            }

            // Test X axis
            if (Mathf.Max(v0.x, v1.x, v2.x) < -extents.x || extents.x < Mathf.Min(v0.x, v1.x, v2.x))
                return false;
            // Test Y axis
            if (Mathf.Max(v0.y, v1.y, v2.y) < -extents.y || extents.y < Mathf.Min(v0.y, v1.y, v2.y))
                return false;
            // Test Z axis
            if (Mathf.Max(v0.z, v1.z, v2.z) < -extents.z || extents.z < Mathf.Min(v0.z, v1.z, v2.z))
                return false;

            // test triangle normal
            var normal = Vector3.Cross(e1, e0).normalized;
            var pl = new Plane(normal, Vector3.Dot(normal, tri.a));
            return Intersects(pl, aabb);

        }
        private static bool Intersects(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 extents, Vector3 axis) {
            // project triangles
            float p0 = Vector3.Dot(v0, axis);
            float p1 = Vector3.Dot(v1, axis);
            float p2 = Vector3.Dot(v2, axis);

            // project aabb
            float r = extents.x * Mathf.Abs(axis.x) + extents.y * Mathf.Abs(axis.y) + extents.z * Mathf.Abs(axis.z);
            float minP = Mathf.Min(p0, p1, p2);
            float maxP = Mathf.Max(p0, p1, p2);
            return !(maxP < -r || r < minP);
        }
        private static bool Intersects(Plane pl, Bounds aabb) {
            Vector3 center = aabb.center, extents = aabb.extents;
            // project the extents onto the plane normal
            var r = extents.x * Mathf.Abs(pl.normal.x) + extents.y * Mathf.Abs(pl.normal.y) + extents.z * Mathf.Abs(pl.normal.z);

            // compute the distance of box center from plane
            var s = Vector3.Dot(pl.normal, center) - pl.distance;

            // check if s is within [-r, r]
            return Mathf.Abs(s) <= r;
        }
    }
}