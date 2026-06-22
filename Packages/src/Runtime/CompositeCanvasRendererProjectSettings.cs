using System;
using System.Linq;
using Coffee.CompositeCanvasRendererInternal;
using CompositeCanvas.Enums;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CompositeCanvas
{
    [Icon("Packages/com.coffee.composite-canvas-renderer/Editor/CompositeCanvasRendererIcon.png")]
    public class CompositeCanvasRendererProjectSettings
        : PreloadedProjectSettings<CompositeCanvasRendererProjectSettings>
    {
        [Header("Setting")]
        [Tooltip("Sensitivity of transform that automatically rebuilds the soft mask buffer.")]
        [SerializeField]
        private TransformSensitivity m_TransformSensitivity = TransformSensitivity.Medium;

        [SerializeField]
        private bool m_EnableCullingInPlayMode = true;

        [SerializeField]
        private bool m_EnableCullingInEditMode = true;

        [HideInInspector]
        [SerializeField]
        private ShaderVariantRegistry m_ShaderVariantRegistry = new ShaderVariantRegistry();

        public static ShaderVariantRegistry shaderRegistry => instance.m_ShaderVariantRegistry;

        public static ShaderVariantCollection shaderVariantCollection => shaderRegistry.shaderVariantCollection;

        public static bool enableCulling
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return instance.m_EnableCullingInEditMode;
                }
#endif
                return instance.m_EnableCullingInPlayMode;
            }
        }

        public static TransformSensitivity transformSensitivity
        {
            get => instance.m_TransformSensitivity;
            set => instance.m_TransformSensitivity = value;
        }

        public static float sensitivity
        {
            get
            {
                switch (instance.m_TransformSensitivity)
                {
                    case TransformSensitivity.Low: return 1f / (1 << 2);
                    case TransformSensitivity.Medium: return 1f / (1 << 5);
                    case TransformSensitivity.High: return 1f / (1 << 12);
                    default: return 1f / (1 << (int)instance.m_TransformSensitivity);
                }
            }
        }

#if !UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();
            m_ShaderVariantRegistry.InitializeShaderLookup();
        }
#endif

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();
            m_ShaderVariantRegistry.ClearCache();
            m_ShaderVariantRegistry.InitializeShaderLookup();
        }

        protected override void OnCreateAsset()
        {
            m_ShaderVariantRegistry.InitializeIfNeeded(this);
        }

        protected override void OnInitialize()
        {
            m_ShaderVariantRegistry.InitializeIfNeeded(this);
            RegisterCcrShaderFromAlwaysIncludedShaders();
        }

        private void Reset()
        {
            m_ShaderVariantRegistry.InitializeIfNeeded(this);
        }

        private void RegisterCcrShaderFromAlwaysIncludedShaders()
        {
            // Remove CCR shaders in AlwaysIncludedShaders
            var shaders = AlwaysIncludedShadersProxy.GetShaders();
            foreach (var shader in shaders)
            {
                if (shader.name == "UI/CompositeCanvasRenderer")
                {
                    AlwaysIncludedShadersProxy.Remove(shader);
                    var keywordsList = new[]
                    {
                        new[] { "", "UNITY_UI_CLIP_RECT" },
                        new[] { "", "UNITY_UI_ALPHACLIP" },
                        new[] { "", "COLOR_MODE_ADDITIVE", "COLOR_MODE_FILL", "COLOR_MODE_SUBTRACT" },
                        new[] { "", "ENABLE_DETAIL" },
                        new[] { "", "ENABLE_UV_ANIMATION" },
                        new[] { "", "ENABLE_MASK" }
                    }.Aggregate(
                        new[] { Array.Empty<string>() }.AsEnumerable(),
                        (acc, items) => acc.SelectMany(prefix => items,
                            (prefix, item) => prefix.Append(item).ToArray())
                    );

                    var svc = m_ShaderVariantRegistry.shaderVariantCollection;
                    foreach (var keywords in keywordsList)
                    {
                        var validKeywords = keywords.Where(k => !string.IsNullOrEmpty(k)).ToArray();
                        svc.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.Normal, validKeywords));
                    }
                }
                else if (shader.name == "Hidden/UI/CompositeCanvasRenderer/Blur")
                {
                    AlwaysIncludedShadersProxy.Remove(shader);
                    var svc = m_ShaderVariantRegistry.shaderVariantCollection;
                    svc.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.Normal));
                }
            }
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new PreloadedProjectSettingsProvider("Project/UI/Composite Canvas Renderer");
        }
#endif
    }
}
