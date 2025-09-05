using System;
using Coffee.CompositeCanvasRendererInternal;
using CompositeCanvas.Enums;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace CompositeCanvas.Effects
{
    [Icon("Packages/com.coffee.composite-canvas-renderer/Editor/CompositeCanvasRendererIcon.png")]
    public class CompositeCanvasBlur : CompositeCanvasEffectBase
    {
        [Header("Blur Settings")]
        [SerializeField]
        private BlurMode m_BlurMode = BlurMode.Uniform;

        [Range(0, 10)]
        [SerializeField]
        private float m_Blur = 0.5f;

        [Range(-10, 10)]
        [SerializeField]
        private float m_BlurX = 0.5f;

        [Range(-10, 10)]
        [SerializeField]
        private float m_BlurY = 0.5f;

        [Range(1, 10)]
        [SerializeField]
        private int m_Iteration = 3;

        [Range(0.05f, 3f)]
        [SerializeField]
        private float m_Power = 1f;

        [Range(0.05f, 3f)]
        [SerializeField]
        private float m_Multiplier = 1;

        [Range(0, 1)]
        [SerializeField]
        private float m_Limit = 1;

        private Material _material;

        public BlurMode blurMode
        {
            get => m_BlurMode;
            set
            {
                if (m_BlurMode == value) return;

                m_BlurMode = value;
                SetRendererDirty();
            }
        }

        public float blur
        {
            get => m_Blur;
            set
            {
                value = Mathf.Clamp(value, 0, 10);
                if (Mathf.Approximately(m_Blur, value)) return;

                m_Blur = value;
                SetRendererDirty();
            }
        }

        public float blurX
        {
            get => m_BlurX;
            set
            {
                value = Mathf.Clamp(value, -10, 10);
                if (Mathf.Approximately(m_BlurX, value)) return;

                m_BlurX = value;
                if(m_BlurMode != BlurMode.Uniform)
                    SetRendererDirty();
            }
        }

        public float blurY
        {
            get => m_BlurY;
            set
            {
                value = Mathf.Clamp(value, -10, 10);
                if (Mathf.Approximately(m_BlurY, value)) return;

                m_BlurY = value;
                if(m_BlurMode != BlurMode.Uniform)
                    SetRendererDirty();
            }
        }

        public int iteration
        {
            get => m_Iteration;
            set
            {
                value = Mathf.Clamp(value, 1, 10);
                if (m_Iteration == value) return;

                m_Iteration = value;
                SetRendererDirty();
            }
        }

        public float power
        {
            get => m_Power;
            set
            {
                value = Mathf.Clamp(value, 0.05f, 3f);
                if (Mathf.Approximately(m_Power, value)) return;

                m_Power = value;
                SetRendererDirty();
            }
        }

        public float multiplier
        {
            get => m_Multiplier;
            set
            {
                value = Mathf.Clamp(value, 0.05f, 3f);
                if (Mathf.Approximately(m_Multiplier, value)) return;

                m_Multiplier = value;
                SetRendererDirty();
            }
        }

        public virtual float innerCutoff
        {
            get => 1f;
            set { }
        }

        public float limit
        {
            get => m_Limit;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_Limit, value)) return;

                m_Limit = value;
                SetRendererDirty();
            }
        }

        private bool useCutoffPass => !Mathf.Approximately(innerCutoff, 1)
                                      || !Mathf.Approximately(power, 1)
                                      || !Mathf.Approximately(multiplier, 1)
                                      || !Mathf.Approximately(limit, 1);

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDisable()
        {
            MaterialRepository.Release(ref _material);
            base.OnDisable();
        }

        public override void ApplyBakedEffect(CommandBuffer cb)
        {
            // Baked buffer
            Profiler.BeginSample("(CCR)[CompositeCanvasBlur] ApplyBakedEffect > Init");
            var rt = compositeCanvasRenderer.mainTexture;
            var w = rt.width;
            var h = rt.height;
            var result = new RenderTargetIdentifier(rt);

            var scale = w / compositeCanvasRenderer.renderingSize.x;
            var blurValue = blur * scale;
            var blurValueX = blurX * scale;
            var blurValueY = blurY * scale;

            Profiler.EndSample();

            var willRenderBlur = blurMode == BlurMode.Uniform && blurValue > 0
                                  || blurMode != BlurMode.Uniform && (!Mathf.Approximately(blurValueX, 0) || !Mathf.Approximately(blurValueY, 0));

            // Skip if won't render blur.
            if (!willRenderBlur && !useCutoffPass) return;

            // Get blur material
            Profiler.BeginSample("(CCR)[CompositeCanvasBlur] ApplyBakedEffect > Get blur material (lambda)");
            var hash = new Hash128(ShaderPropertyIds.compositeCanvasBlur, 0, 0, 0);
            MaterialRepository.Get(hash, ref _material,
                () => new Material(Shader.Find("Hidden/UI/CompositeCanvasRenderer/Blur"))
                {
                    hideFlags = HideFlags.DontSave | HideFlags.NotEditable
                });
            Profiler.EndSample();

            var blurRendered = false;

            switch (m_BlurMode)
            {
                case BlurMode.Uniform:
                    blurRendered = RenderBlurUniform(cb, w, h, result);
                    break;
                case BlurMode.SeparateAxis:
                    blurRendered = RenderBlurSeparateAxis(cb, w, h, result);
                    break;
                case BlurMode.Motion:
                default:
                    blurRendered = RenderBlurMotion(cb, w, h, result);
                    break;
            }

            if (!blurRendered && useCutoffPass)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasBlur] ApplyBakedEffect > Construct cutoff effect for cb");
                cb.GetTemporaryRT(ShaderPropertyIds.tmpRt, w, h, 0, FilterMode.Bilinear);

                // cutoff
                cb.SetGlobalFloat(ShaderPropertyIds.innerCutoff, innerCutoff);
                cb.SetGlobalFloat(ShaderPropertyIds.power, power);
                cb.SetGlobalFloat(ShaderPropertyIds.multiplier, multiplier);
                cb.SetGlobalFloat(ShaderPropertyIds.limit, limit);
                cb.Blit(result, ShaderPropertyIds.tmpRt, _material, 1);
                cb.CopyTexture(ShaderPropertyIds.tmpRt, result);

                cb.ReleaseTemporaryRT(ShaderPropertyIds.tmpRt);
                Profiler.EndSample();
            }
        }

        private bool RenderBlurUniform(CommandBuffer cb, int w, int h, RenderTargetIdentifier result)
        {
            var scale = w / compositeCanvasRenderer.renderingSize.x;
            var blurValue = blur * scale;

            if(blurValue <= 0)
                return false;

            Profiler.BeginSample("(CCR)[CompositeCanvasBlur] ApplyBakedEffect > Construct blur effect for cb");
            cb.GetTemporaryRT(ShaderPropertyIds.tmpRt, w, h, 0, FilterMode.Bilinear);
            for (var i = 0; i < m_Iteration; i++)
            {
                // Horizontal blur
                cb.SetGlobalVector(ShaderPropertyIds.blur, new Vector4(blurValue, 0));
                cb.Blit(result, ShaderPropertyIds.tmpRt, _material, 0);

                // Vertical blur
                cb.SetGlobalVector(ShaderPropertyIds.blur, new Vector4(0, blurValue));

                if (i == iteration - 1 && useCutoffPass)
                {
                    // blur and cutoff
                    cb.SetGlobalFloat(ShaderPropertyIds.innerCutoff, innerCutoff);
                    cb.SetGlobalFloat(ShaderPropertyIds.power, power);
                    cb.SetGlobalFloat(ShaderPropertyIds.multiplier, multiplier);
                    cb.SetGlobalFloat(ShaderPropertyIds.limit, limit);
                    cb.Blit(ShaderPropertyIds.tmpRt, result, _material);
                }
                else
                {
                    cb.Blit(ShaderPropertyIds.tmpRt, result, _material, 0);
                }
            }

            cb.ReleaseTemporaryRT(ShaderPropertyIds.tmpRt);
            Profiler.EndSample();

            return true;
        }

        private bool RenderBlurSeparateAxis(CommandBuffer cb, int w, int h, RenderTargetIdentifier result)
        {
            var scale = w / compositeCanvasRenderer.renderingSize.x;
            var blurValueX = blurX * scale;
            var blurValueY = blurY * scale;

            if(Mathf.Approximately(blurValueX, 0) && Mathf.Approximately(blurValueY, 0))
                return false;

            Profiler.BeginSample("(CCR)[CompositeCanvasBlur] ApplyBakedEffect > Construct blur effect for cb");
            cb.GetTemporaryRT(ShaderPropertyIds.tmpRt, w, h, 0, FilterMode.Bilinear);
            var rtSrc = result;
            var rtDst = new RenderTargetIdentifier(ShaderPropertyIds.tmpRt);

            for (var i = 0; i < m_Iteration; i++)
            {
                // Horizontal blur
                if(blurValueX > 0 && m_BlurMode != BlurMode.Motion)
                {
                    cb.SetGlobalVector(ShaderPropertyIds.blur, new Vector4(blurValueX, 0));
                    BlitSwapchain(cb, ref rtSrc, ref rtDst, _material, 0);
                }

                // Vertical blur
                cb.SetGlobalVector(ShaderPropertyIds.blur, new Vector4(0, blurValueY));

                if (i == iteration - 1 && useCutoffPass)
                {
                    // blur and cutoff
                    cb.SetGlobalFloat(ShaderPropertyIds.innerCutoff, innerCutoff);
                    cb.SetGlobalFloat(ShaderPropertyIds.power, power);
                    cb.SetGlobalFloat(ShaderPropertyIds.multiplier, multiplier);
                    cb.SetGlobalFloat(ShaderPropertyIds.limit, limit);
                    if(blurValueY > 0)
                        BlitSwapchain(cb, ref rtSrc, ref rtDst, _material);
                    else
                        BlitSwapchain(cb, ref rtSrc, ref rtDst, _material, 1);
                }
                else if(blurValueY > 0)
                {
                    BlitSwapchain(cb, ref rtSrc, ref rtDst, _material, 0);
                }
            }

            if (rtSrc != result)
                cb.CopyTexture(ShaderPropertyIds.tmpRt, result);

            cb.ReleaseTemporaryRT(ShaderPropertyIds.tmpRt);
            Profiler.EndSample();

            return true;
        }

        private bool RenderBlurMotion(CommandBuffer cb, int w, int h, RenderTargetIdentifier result)
        {
            var scale = w / compositeCanvasRenderer.renderingSize.x;
            var blurValueX = blurX * scale;
            var blurValueY = blurY * scale;

            if(Mathf.Approximately(blurValueX, 0) && Mathf.Approximately(blurValueY, 0))
                return false;

            Profiler.BeginSample("(CCR)[CompositeCanvasBlur] ApplyBakedEffect > Construct blur effect for cb");
            cb.GetTemporaryRT(ShaderPropertyIds.tmpRt, w, h, 0, FilterMode.Bilinear);
            var rtSrc = result;
            var rtDst = new RenderTargetIdentifier(ShaderPropertyIds.tmpRt);

            for (var i = 0; i < m_Iteration; i++)
            {
                // Vertical blur
                cb.SetGlobalVector(ShaderPropertyIds.blur, new Vector4(blurValueX, blurValueY));

                if (i == iteration - 1 && useCutoffPass)
                {
                    // blur and cutoff
                    cb.SetGlobalFloat(ShaderPropertyIds.innerCutoff, innerCutoff);
                    cb.SetGlobalFloat(ShaderPropertyIds.power, power);
                    cb.SetGlobalFloat(ShaderPropertyIds.multiplier, multiplier);
                    cb.SetGlobalFloat(ShaderPropertyIds.limit, limit);
                    BlitSwapchain(cb, ref rtSrc, ref rtDst, _material);
                }
                else
                {
                    BlitSwapchain(cb, ref rtSrc, ref rtDst, _material, 0);
                }
            }

            if (rtSrc != result)
                cb.CopyTexture(ShaderPropertyIds.tmpRt, result);

            cb.ReleaseTemporaryRT(ShaderPropertyIds.tmpRt);
            Profiler.EndSample();

            return true;
        }

        private static void BlitSwapchain(CommandBuffer cb, ref RenderTargetIdentifier src, ref RenderTargetIdentifier dst, Material material, int pass)
        {
            cb.Blit(src, dst, material, pass);
            (src, dst) = (dst, src);
        }

        private static void BlitSwapchain(CommandBuffer cb, ref RenderTargetIdentifier src, ref RenderTargetIdentifier dst, Material material)
        {
            cb.Blit(src, dst, material);
            (src, dst) = (dst, src);
        }
    }
}
