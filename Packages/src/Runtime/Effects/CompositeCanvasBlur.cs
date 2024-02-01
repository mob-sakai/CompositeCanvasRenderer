using Coffee.CompositeCanvasRendererInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace CompositeCanvas.Effects
{
    public class CompositeCanvasBlur : CompositeCanvasEffectBase
    {
        [Header("Blur Settings")]
        [Range(0, 1)]
        [SerializeField]
        private float m_Blur = 0.5f;

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

        public float blur
        {
            get => m_Blur;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_Blur, value)) return;

                m_Blur = value;
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
            Profiler.EndSample();

            // Skip if blur value is zero.
            if (blurValue <= 0 && !useCutoffPass) return;

            // Get blur material
            Profiler.BeginSample("(CCR)[CompositeCanvasBlur] ApplyBakedEffect > Get blur material (lambda)");
            var hash = new Hash128(ShaderPropertyIds.compositeCanvasBlur, 0, 0, 0);
            MaterialRepository.Get(hash, ref _material,
                () => new Material(Shader.Find("Hidden/UI/CompositeCanvasRenderer/Blur"))
                {
                    hideFlags = HideFlags.DontSave | HideFlags.NotEditable
                });
            Profiler.EndSample();
            if (0 < blurValue)
            {
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
            }
            else if (useCutoffPass)
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
    }
}
