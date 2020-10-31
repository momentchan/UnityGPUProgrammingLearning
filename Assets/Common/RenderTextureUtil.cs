using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    public class RenderTextureUtil {

        public static RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, FilterMode filterMode, bool useMipMap, bool autoGenerateMips, bool enableRandomWrite) {
            var rt = new RenderTexture(width, height, depth, format);
            rt.filterMode = filterMode;
            rt.useMipMap = useMipMap;
            rt.autoGenerateMips = autoGenerateMips;
            rt.enableRandomWrite = enableRandomWrite;
            rt.Create();
            return rt;
        }


        public static bool IsResolutionChanged(RenderTexture rt, int width, int height) {
            if (rt.width != width || rt.height != height)
                return true;
            else
                return false;
        }
    }
}