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

       public static Texture2D RenderTextureToTexture2D(RenderTexture rt) {
            TextureFormat format;

            switch (rt.format) {
                case RenderTextureFormat.ARGBFloat:
                    format = TextureFormat.RGBAFloat;
                    break;
                case RenderTextureFormat.ARGBHalf:
                    format = TextureFormat.RGBAHalf;
                    break;
                case RenderTextureFormat.ARGBInt:
                    format = TextureFormat.RGBA32;
                    break;
                case RenderTextureFormat.ARGB32:
                    format = TextureFormat.ARGB32;
                    break;
                default:
                    format = TextureFormat.ARGB32;
                    Debug.LogWarning("Unsuported RenderTextureFormat.");
                    break;
            }

            var tex2D = new Texture2D(rt.width, rt.height, format, false);
            var rect = Rect.MinMaxRect(0f, 0f, tex2D.width, tex2D.height);
            RenderTexture.active = rt;
            tex2D.ReadPixels(rect, 0, 0);
            RenderTexture.active = null;
            return tex2D;
        }

        public static bool IsResolutionChanged(RenderTexture rt, int width, int height) {
            if (rt.width != width || rt.height != height)
                return true;
            else
                return false;
        }
    }
}