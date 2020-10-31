using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralModeling {
    public abstract class ParametricPlaneBase : Plane {

		[SerializeField] protected float depth = 1f;
        protected abstract float Depth(float u, float v);

        protected override Mesh Build() {
            var mesh = base.Build();

            var vertices = mesh.vertices;
            float hinv = 1.0f / (heightSegments - 1);
            float winv = 1.0f / (widthSegments - 1);


            for (var y = 0; y < heightSegments; y++) {
                var ry = y * hinv;

                for (var x = 0; x < widthSegments; x++) {
                    var rx = x * winv;

                    var index = y * widthSegments + x;

                    vertices[index].y = Depth(rx, ry);
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

    }
}