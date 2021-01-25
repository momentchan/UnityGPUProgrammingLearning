using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Common;
using System;

namespace StarGlow {
    [ExecuteInEditMode]
    public class StarGlow : ImageEffectBase {

        [SerializeField, Range(1, 20)] protected int downsampling = 3;
        
        
        [SerializeField, Range(0f, 1f)]  protected float glowThreshold = 0.5f;
        [SerializeField, Range(0f, 10f)] protected float glowIntensity = 5f;

        [SerializeField, Range(1, 16)]    protected int   glowAngleDivision = 8;
        [SerializeField, Range(0f, 360f)] protected float glowAngleOffset = 180;

        [SerializeField, Range(1, 5)]   protected int   blurIternation = 3;
        [SerializeField, Range(0f, 1f)] protected float blurAttenuation = 0.5f;

        [SerializeField] protected CompositeType compositeType = CompositeType._COMPOSITE_TYPE_ADDITIVE;
        [SerializeField] protected Color compositeColor = Color.white;

        private int glowThresholdProp   = Shader.PropertyToID("_GlowThreshold");
        private int glowIntensityProp   = Shader.PropertyToID("_GlowIntensity");

        private int blurOffsetProp      = Shader.PropertyToID("_BlurOffset");
        private int blurIternationProp  = Shader.PropertyToID("_BlurIteration");
        private int blurAttenuationProp = Shader.PropertyToID("_BlurAttenuation");

        private int compositeTexProp    = Shader.PropertyToID("_CompositeTex");
        private int compositeColorProp  = Shader.PropertyToID("_CompositeColor");

        protected enum Pass {
            Debug = 0, 
            Brightness = 1, 
            Blur = 2, 
            ComposeBlurs = 3, 
            ComposeOrigin = 4
        }

        protected enum CompositeType {
            _COMPOSITE_TYPE_ADDITIVE,
            _COMPOSITE_TYPE_SCREEN,
            _COMPOSITE_TYPE_COLORED_ADDITIVE,
            _COMPOSITE_TYPE_COLORED_SCREEN,
            _COMPOSITE_TYPE_DEBUG,
        }

        protected override void OnRenderImage(RenderTexture src, RenderTexture dst) {
            if (!IsSupportAndEnable())
                return;

            var brightness = RenderTexture.GetTemporary(src.width / downsampling, src.height / downsampling, src.depth, src.format);
            var blur1      = RenderTexture.GetTemporary(brightness.descriptor);
            var blur2      = RenderTexture.GetTemporary(brightness.descriptor);
            var composite  = RenderTexture.GetTemporary(brightness.descriptor);

            SetShaderProperties();
            ApplyBrightnessEffect(src, brightness);
            ApplyBlurEffect(brightness, blur1, blur2, composite);
            CompositeEffect(src, dst, composite);

            RenderTexture.ReleaseTemporary(brightness);
            RenderTexture.ReleaseTemporary(blur1);
            RenderTexture.ReleaseTemporary(blur2);
            RenderTexture.ReleaseTemporary(composite);
        }

        private void SetShaderProperties() {
            material.SetFloat(glowThresholdProp, glowThreshold);
            material.SetFloat(glowIntensityProp, glowIntensity);
            material.SetFloat(blurAttenuationProp, blurAttenuation);
        }

        private void ApplyBrightnessEffect(RenderTexture src, RenderTexture brightness) {
            Graphics.Blit(src, brightness, material, (int)Pass.Brightness);
        }

        private void ApplyBlurEffect(RenderTexture brightness, RenderTexture blur1, RenderTexture blur2, RenderTexture composite) {
            var anglePerDivision = 360f / glowAngleDivision;

            for (var i = 0; i < glowAngleDivision; i++) {

                var glowAngle = anglePerDivision * i + glowAngleOffset;
                var blurOffset = (Quaternion.AngleAxis(glowAngle, Vector3.forward) * Vector2.up).normalized;

                material.SetVector(blurOffsetProp, blurOffset);
                Graphics.Blit(brightness, blur1);

                for (var j = 1; j <= blurIternation; j++) {
                    material.SetInt(blurIternationProp, j);
                    Graphics.Blit(blur1, blur2, material, (int)Pass.Blur);
                    SwapRenderTexture(ref blur1, ref blur2);
                }

                Graphics.Blit(blur1, composite, material, (int)Pass.ComposeBlurs);
            }
        }

        private void CompositeEffect(RenderTexture src, RenderTexture dst, RenderTexture composite) {
            material.EnableKeyword(compositeType.ToString());

            material.SetColor(compositeColorProp, compositeColor);
            material.SetTexture(compositeTexProp, composite);
            Graphics.Blit(src, dst, material, (int)Pass.ComposeOrigin);

            material.DisableKeyword(compositeType.ToString());
        }

        private void SwapRenderTexture(ref RenderTexture ping, ref RenderTexture pong) {
            var temp = ping;
            ping = pong;
            pong = temp;
        }
    }
}