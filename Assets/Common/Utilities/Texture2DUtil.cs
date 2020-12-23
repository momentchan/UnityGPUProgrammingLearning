using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    public class Texture2DUtil {
        public static Texture2D CreateTexureFromGradient(Gradient grad, int width) {
            var tex = new Texture2D(width, 1);
            var inv = 1f / width;
            for (var x = 0; x < width; x++) {
                tex.SetPixel(x, 0, grad.Evaluate(x * inv));
            }
            tex.Apply();
            return tex;
        }
    }
}
