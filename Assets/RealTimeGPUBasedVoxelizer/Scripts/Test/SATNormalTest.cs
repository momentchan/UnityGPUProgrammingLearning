using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Voxelizer.Test {

    public class SATNormalTest : MonoBehaviour {
        [SerializeField] protected Bounds aabb;
        [SerializeField] protected Vector3 a = Vector3.left, b = Vector3.up, c = Vector3.right;
        [SerializeField] protected float projSize = 0.1f;
        [SerializeField] protected bool intersection;

        private void OnDrawGizmos() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(aabb.center, aabb.size);

            Vector3 v0 = a - aabb.center;
            Vector3 v1 = b - aabb.center;
            Vector3 v2 = c - aabb.center;
            Vector3 e0 = v1 - v0,
                    e1 = v0 - v2;
#if UNITY_EDITOR
            Handles.Label(v0, "v0");
            Handles.Label(v1, "v1");
            Handles.Label(v2, "v2");
#endif
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v0, v2);

            var normal = Vector3.Cross(e1, e0).normalized;
            var pl = new Plane(normal, Vector3.Dot(normal, a));
            Gizmos.DrawLine(-normal * 100, normal * 100);

            var extents = aabb.extents;
            var r = extents.x * Mathf.Abs(pl.normal.x) + extents.y * Mathf.Abs(pl.normal.y) + extents.z * Mathf.Abs(pl.normal.z);
            var s = Vector3.Dot(pl.normal, aabb.center) - pl.distance;
            intersection = Mathf.Abs(s) <= r;

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(aabb.center + pl.normal * Vector3.Dot(pl.normal, aabb.center), projSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(aabb.center + pl.normal * pl.distance, projSize);
            Gizmos.color = intersection ? Color.green : Color.red;
            Gizmos.DrawSphere(aabb.center + pl.normal * r, projSize);
        }
    }
}