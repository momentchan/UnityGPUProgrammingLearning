using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Render {
    public class RenderEffect : MonoBehaviour {

        public TextureEvent OnCreateTexture;
        public RenderTextureFormat format;
        public TextureWrapMode wrapMode;
        public FilterMode filterMode;

        [SerializeField] protected bool enable;
        [SerializeField] protected string propName = "_PropName";
        [SerializeField] protected Material[] effects;
        [SerializeField] protected int downSample = 0;

        private RenderTexture output;
        private RenderTexture[] rts = new RenderTexture[2];

        void Update() {
            if (Input.GetKeyDown(KeyCode.D))
                enable = !enable;
        }

        void OnRenderImage(RenderTexture s, RenderTexture d) {
            CheckRTs(s);
            Graphics.Blit(s, rts[0]);

            foreach(var e in effects) {
                Graphics.Blit(rts[0], rts[1], e);
                SwapRTs();
            }

            Graphics.Blit(rts[0], output);
            Shader.SetGlobalTexture(propName, output);

            if (enable) Graphics.Blit(output, d);
            else        Graphics.Blit(s, d);
        }

        void SwapRTs() {
            var temp = rts[0];
            rts[0] = rts[1];
            rts[1] = temp;
        }

        void CheckRTs(RenderTexture s) {
            if (rts[0] == null || rts[0].width != s.width >> downSample || rts[1].height != s.height >> downSample) {

                for (var i= 0; i < rts.Length; i++) {
                    var rt = rts[i];
                    rts[i] = RenderUtility.CreateRenderTexture(s.width >> downSample, s.height >> downSample, 16, format, wrapMode, filterMode, rt);
                }
                output = RenderUtility.CreateRenderTexture(s.width >> downSample, s.height >> downSample, 16, format, wrapMode, filterMode, output);
                OnCreateTexture.Invoke(output);
            }
        }

        void OnDestroy() {
            foreach (var rt in rts)
                RenderUtility.ReleaseRenderTexture(rt);
            RenderUtility.ReleaseRenderTexture(output);
        }

        [System.Serializable]
        public class TextureEvent : UnityEngine.Events.UnityEvent<Texture> { }
    }
}