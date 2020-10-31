using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeMatching {
    public class ShapeMatching : MonoBehaviour {

        [SerializeField] protected GameObject destination;
        [SerializeField] protected GameObject target;
        [SerializeField] protected GameObject displayer;

        Vector2[] p, q;
        Vector2 centerP, centerQ;
        Vector2 t;

        Matrix2x2 R;

        void Start() {
            if (destination.transform.childCount != target.transform.childCount) return;


            // p, q
            var n = destination.transform.childCount;
            p = new Vector2[n];
            q = new Vector2[n];

            for (var i=0; i< n; i++) {
                p[i] = destination.transform.GetChild(i).position;
                q[i] = target.transform.GetChild(i).position;

                centerP += p[i];
                centerQ += q[i];
            }
            centerP /= n;
            centerQ /= n;


            Matrix2x2 H = new Matrix2x2(0, 0, 0, 0);
            // p', q'
            for(var i=0; i< n; i++) {
                p[i] -= centerP;
                q[i] -= centerQ;
                H += Matrix2x2.OuterProduct(q[i], p[i]);
            }

            Matrix2x2 u = new Matrix2x2();
            Matrix2x2 s = new Matrix2x2();
            Matrix2x2 v = new Matrix2x2();
            H.SVD(ref u, ref s, ref v);

            R = v * u.Transpose();
            var angle = Mathf.Rad2Deg * Mathf.Acos(R.m00);
            Debug.Log(angle);
            t = centerP - R * centerQ;

            displayer.transform.SetPositionAndRotation(displayer.transform.position + (Vector3)t, Quaternion.Euler(0, 0, angle));
        }
    }
}