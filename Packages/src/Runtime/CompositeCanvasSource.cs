using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CompositeCanvas
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class CompositeCanvasSource : UIBehaviour, IMeshModifier, IMaterialModifier
    {
        private static readonly ObjectPool<MaterialPropertyBlock> s_MaterialPropertyBlockPool =
            new ObjectPool<MaterialPropertyBlock>(
                () => new MaterialPropertyBlock(),
                x => x != null,
                x => x.Clear());

        [SerializeField]
        private bool m_IgnoreSelf;

        [SerializeField]
        private bool m_IgnoreChildren;

        private Action _checkRenderColor;
        private Color _color;
        private Graphic _graphic;
        private bool _isBaking;
        private Material _material;
        internal Mesh _mesh;
        private MaterialPropertyBlock _mpb;
        private Matrix4x4 _prevTransformMatrix;
        internal CompositeCanvasRenderer _renderer;
        private UnityAction _setRendererDirty;

        public Graphic graphic
        {
            get
            {
                if (_graphic) return _graphic;
                TryGetComponent(out _graphic);
                return _graphic;
            }
        }

        public bool ignored
        {
            get
            {
                if (m_IgnoreSelf || !_renderer) return true;

                var rendererTr = _renderer.transform;
                var tr = transform.parent;
                while (tr && tr != rendererTr)
                {
                    if (tr.TryGetComponent<CompositeCanvasSource>(out var source) && source.m_IgnoreChildren)
                    {
                        return true;
                    }

                    tr = tr.parent;
                }

                return false;
            }
        }

        protected override void OnEnable()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasSource] OnEnable > Register onBeforeCanvasRebuild");
            _checkRenderColor = _checkRenderColor ?? CheckRenderColor;
            UIExtraCallbacks.onBeforeCanvasRebuild += _checkRenderColor;
            Profiler.EndSample();

            if (graphic)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasSource] OnEnable > Register graphic dirty callbacks");
                graphic.SetVerticesDirty();
                graphic.SetMaterialDirty();

                _setRendererDirty = _setRendererDirty ?? SetRendererDirty;
                graphic.RegisterDirtyMaterialCallback(_setRendererDirty);
                graphic.RegisterDirtyVerticesCallback(_setRendererDirty);
                Profiler.EndSample();
            }

            UpdateRenderer();

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] OnEnable > AddComponentOnChildren");
            this.AddComponentOnChildren<CompositeCanvasSource>(HideFlags.DontSave, false);
            Profiler.EndSample();
        }

        protected override void OnDisable()
        {
            UIExtraCallbacks.onBeforeCanvasRebuild -= _checkRenderColor;

            MaterialRegistry.Release(ref _material);
            MeshExtensions.Return(ref _mesh);
            s_MaterialPropertyBlockPool.Return(ref _mpb);
            UpdateRenderer(null);

            if (graphic)
            {
                graphic.UnregisterDirtyMaterialCallback(_setRendererDirty);
                graphic.UnregisterDirtyVerticesCallback(_setRendererDirty);

                graphic.SetMaterialDirty();
                graphic.SetVerticesDirty();
            }
        }

        protected override void OnDestroy()
        {
            _graphic = null;
            _material = null;
            _mesh = null;
            _mpb = null;
            _renderer = null;
            _checkRenderColor = null;
            _setRendererDirty = null;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetRendererDirty();
        }

        private void OnTransformChildrenChanged()
        {
            this.AddComponentOnChildren<CompositeCanvasSource>(HideFlags.DontSave, false);
        }

        protected override void OnTransformParentChanged()
        {
            UpdateRenderer();
            SetRendererDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            hideFlags = m_IgnoreSelf || m_IgnoreChildren ? HideFlags.None : HideFlags.DontSave;
            base.OnValidate();
            SetRendererDirty();
            if (_graphic)
            {
                _graphic.SetMaterialDirty();
            }
        }
#endif

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled
                || !graphic
                || !_renderer || !_renderer.isActiveAndEnabled || _renderer.showSourceGraphics
                || _isBaking || ignored)
            {
                return baseMaterial;
            }

            return null;
        }

        void IMeshModifier.ModifyMesh(Mesh mesh)
        {
        }

        void IMeshModifier.ModifyMesh(VertexHelper verts)
        {
            if (!isActiveAndEnabled || !_renderer || !_renderer.isActiveAndEnabled || !graphic) return;
            if (!CompositeCanvasProcess.instance.IsModifyMeshSupported(graphic)) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] ModifyMesh");
            _mesh = _mesh ? _mesh : MeshExtensions.Rent();
            verts.FillMesh(_mesh);
            _mesh.RecalculateBounds();
            Profiler.EndSample();
            Logging.Log(this, " >>>> Graphic mesh is modified.");
        }

        public static int Compare(CompositeCanvasSource l, CompositeCanvasSource r)
        {
            if (l == r) return 0;
            if ((!l || !l._graphic) && (r && r._graphic)) return -1;
            if (l && l._graphic && (!r || !r._graphic)) return 1;

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Compare > depth");
            var lDepth = l._graphic ? l._graphic.depth : -1;
            var rDepth = r._graphic ? r._graphic.depth : -1;
            Profiler.EndSample();
            if (lDepth != -1 && rDepth != -1) return lDepth - rDepth;

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Compare > CompareHierarchyIndex");
            var compare = l.transform.CompareHierarchyIndex(r.transform, l._renderer ? l._renderer.transform : null);
            Profiler.EndSample();
            return compare;
        }

        private void CheckRenderColor()
        {
            if (!graphic) return;
            var prevColor = _color;
            _color = graphic.canvasRenderer.GetColor();
            _color.a *= graphic.canvasRenderer.GetInheritedAlpha();
            if (prevColor != _color)
            {
                SetRendererDirty();
            }
        }

        private void UpdateRenderer()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasSource] UpdateRenderer (Auto)");
            UpdateRenderer(GetComponentInParent<CompositeCanvasRenderer>());
            Profiler.EndSample();
        }

        private void UpdateRenderer(CompositeCanvasRenderer newRenderer)
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasSource] UpdateRenderer");


            // Ignore if the renderer is the same GameObject.
            if (newRenderer && newRenderer.transform == transform)
            {
                newRenderer = null;
            }

            // Update the renderer.
            if (newRenderer != _renderer)
            {
                if (_renderer)
                {
                    _renderer.Unregister(this);
                }

                if (newRenderer)
                {
                    newRenderer.Register(this);
                }
            }

            _renderer = newRenderer;
            Profiler.EndSample();
        }

        private void SetRendererDirty()
        {
            if (_renderer)
            {
                _renderer.SetDirty();
            }
        }

        internal void Bake(CommandBuffer cb)
        {
            if (!_graphic || !IsInScreen()) return;

            _mpb = _mpb ?? s_MaterialPropertyBlockPool.Rent();
            _mpb.Clear();
            Material graphicMat = null;
            Texture graphicTex = null;

            {
                Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Get Modified Material For Baking");
                _isBaking = true;
                graphicMat = graphic.materialForRendering;
                _isBaking = false;
                Profiler.EndSample();

                // Skip baking when `ColorMask=0` (for masking)
                if (graphicMat.HasProperty(ShaderPropertyIds.colorMask)
                    && graphicMat.GetInt(ShaderPropertyIds.colorMask) == 0)
                {
                    return;
                }

                var isDefaultShader = graphicMat.shader == Graphic.defaultGraphicMaterial.shader;
                if (isDefaultShader)
                {
                    // Use CCR Material instead of default material to blend with One-OneMinusSrcAlpha.
                    Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Use CCR Material");
                    const ColorMode colorMode = ColorMode.Multiply;
                    const BlendMode srcBlend = BlendMode.One;
                    const BlendMode dstBlend = BlendMode.OneMinusSrcAlpha;
                    var hash = CompositeCanvasRenderer.CreateHash(colorMode, srcBlend, dstBlend);
                    MaterialRegistry.Get(hash, ref _material,
                        () => CompositeCanvasRenderer.CreateMaterial(colorMode, srcBlend, dstBlend),
                        CompositeCanvasRendererProjectSettings.cacheRendererMaterial);
                    graphicMat = _material;
                    _mpb.SetFloat(ShaderPropertyIds.alphaMultiplier, 1);
                    Profiler.EndSample();
                }
                else
                {
                    MaterialRegistry.Release(ref _material);
                }
            }

            {
                Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Mpb setup");
                graphicTex = CompositeCanvasProcess.instance.GetMainTexture(graphic);
                if (graphicTex != null)
                {
                    _mpb.SetTexture(ShaderPropertyIds.mainTex, graphicTex);

                    // Use _TextureSampleAdd for alpha only texture
                    if (GraphicsFormatUtility.IsAlphaOnlyFormat(graphicTex.graphicsFormat))
                    {
                        _mpb.SetVector(ShaderPropertyIds.textureSampleAdd, new Vector4(1, 1, 1, 0));
                    }
                }

                Profiler.EndSample();
            }

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Calc Matrix");
            var matrix = _renderer.isRelativeSpace
                ? _renderer.transform.worldToLocalMatrix * transform.localToWorldMatrix
                : transform.localToWorldMatrix;
            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > DrawMesh");
            if (CompositeCanvasProcess.instance.OnPreBake(_renderer, graphic, ref _mesh, _mpb, _renderer.alphaScale) &&
                _mesh)
            {
                Logging.Log(this, $"<color=orange> >>>> Mesh '{name}' will render.</color>");
                cb.DrawMesh(_mesh, matrix, graphicMat, 0, 0, _mpb);
            }

            Profiler.EndSample();
        }

        internal bool HasTransformChanged(Transform baseTransform)
        {
            return transform.HasChanged(baseTransform, ref _prevTransformMatrix);
        }

        public bool IsInScreen()
        {
            if (FrameCache.TryGet(this, nameof(IsInScreen), out bool result))
            {
                return result;
            }

            // Cull if there is no graphic or the scale is too small.
            if (!_renderer || !_graphic || !transform.lossyScale.IsVisible())
            {
                result = false;
            }
            else if (!_renderer.culling)
            {
                result = true;
            }
            else
            {
                var viewport = _renderer.rectTransform;
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, transform);
                var viewportRect = viewport.rect;
                var ex = _renderer.extents;
                viewportRect.Set(viewportRect.xMin - ex.x / 2,
                    viewportRect.yMin - ex.y / 2,
                    viewportRect.width + ex.x,
                    viewportRect.height + ex.y);
                var rect = new Rect(bounds.min, bounds.size);
                result = viewportRect.Overlaps(rect, true);
            }

            FrameCache.Set(this, nameof(IsInScreen), result);
            return result;
        }
    }
}
