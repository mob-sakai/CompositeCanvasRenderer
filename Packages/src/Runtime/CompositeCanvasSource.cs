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
    /// <summary>
    /// The source graphic for bake-buffer of the CompositeCanvasRenderer.
    /// </summary>
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

        [Tooltip("This source graphic will be ignored when baking")]
        [SerializeField]
        private bool m_IgnoreSelf;

        [Tooltip("Child source graphics will be ignored when baking")]
        [SerializeField]
        private bool m_IgnoreChildren;

        private Material _bakingMaterial;
        private Action _checkRenderColor;
        private Color _color;
        private Graphic _graphic;
        internal Mesh _mesh;
        private MaterialPropertyBlock _mpb;
        private Matrix4x4 _prevTransformMatrix;
        internal CompositeCanvasRenderer _renderer;
        private UnityAction _setRendererDirty;

        /// <summary>
        /// The graphic associated with the source.
        /// </summary>
        public Graphic graphic
        {
            get
            {
                if (_graphic) return _graphic;
                TryGetComponent(out _graphic);
                return _graphic;
            }
        }

        /// <summary>
        /// Gets whether this source graphic is ignored when baking.
        /// </summary>
        public bool ignoreSelf
        {
            get => m_IgnoreSelf;
            set
            {
                if (m_IgnoreSelf == value) return;
                m_IgnoreSelf = value;
                hideFlags = m_IgnoreSelf || m_IgnoreChildren ? HideFlags.None : HideFlags.DontSave;
                SetRendererDirty();
            }
        }

        /// <summary>
        /// Gets whether the child source graphics are ignored when baking.
        /// </summary>
        public bool ignoreChildren
        {
            get => m_IgnoreChildren;
            set
            {
                if (m_IgnoreChildren == value) return;
                m_IgnoreChildren = value;
                hideFlags = m_IgnoreSelf || m_IgnoreChildren ? HideFlags.None : HideFlags.DontSave;
                SetRendererDirty();
            }
        }

        /// <summary>
        /// Gets whether this source graphic is ignored when baking.
        /// </summary>
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

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
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

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDisable()
        {
            UIExtraCallbacks.onBeforeCanvasRebuild -= _checkRenderColor;

            MeshExtensions.Return(ref _mesh);
            s_MaterialPropertyBlockPool.Return(ref _mpb);
            UpdateRenderer(null);
            _bakingMaterial = null;

            if (graphic)
            {
                graphic.UnregisterDirtyMaterialCallback(_setRendererDirty);
                graphic.UnregisterDirtyVerticesCallback(_setRendererDirty);

                graphic.SetMaterialDirty();
                graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene receiving OnDestroy.
        /// </summary>
        protected override void OnDestroy()
        {
            _graphic = null;
            _bakingMaterial = null;
            _mesh = null;
            _mpb = null;
            _renderer = null;
            _checkRenderColor = null;
            _setRendererDirty = null;
        }

        /// <summary>
        /// This callback is called if an associated RectTransform has its dimensions changed.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            SetRendererDirty();
        }

        /// <summary>
        /// This function is called when the list of children of the transform of the GameObject has changed.
        /// </summary>
        private void OnTransformChildrenChanged()
        {
            this.AddComponentOnChildren<CompositeCanvasSource>(HideFlags.DontSave, false);
        }

        /// <summary>
        /// This function is called when a direct or indirect parent of the transform of the GameObject has changed.
        /// </summary>
        protected override void OnTransformParentChanged()
        {
            UpdateRenderer();
            SetRendererDirty();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
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

        /// <summary>
        /// Perform material modification in this function.
        /// </summary>
        /// <param name="baseMaterial">The material that is to be modified</param>
        /// <returns>The modified material.</returns>
        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            _bakingMaterial = baseMaterial;
            if (!isActiveAndEnabled
                || !graphic
                || !_renderer || !_renderer.isActiveAndEnabled || _renderer.showSourceGraphics
                || ignored)
            {
                return baseMaterial;
            }

            return null;
        }

        /// <summary>
        /// Call used to modify mesh.
        /// Place any custom mesh processing in this function.
        /// </summary>
        void IMeshModifier.ModifyMesh(Mesh mesh)
        {
        }

        /// <summary>
        /// Call used to modify mesh.
        /// Place any custom mesh processing in this function.
        /// </summary>
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

            SetRendererDirty();
        }

        /// <summary>
        /// Check if the render color has changed.
        /// </summary>
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
                _renderer.SetDirty(false);
            }
        }

        internal void Bake(CommandBuffer cb, bool usePopMaterial)
        {
            if (!_graphic || !_graphic.isActiveAndEnabled || !_graphic.canvasRenderer || !IsInScreen()) return;
            var cr = _graphic.canvasRenderer;
            if (usePopMaterial && cr.popMaterialCount == 0) return;

            _mpb = _mpb ?? s_MaterialPropertyBlockPool.Rent();
            _mpb.Clear();
            var graphicMat = usePopMaterial
                ? cr.GetPopMaterial(0)
                : _bakingMaterial;
            if (!graphicMat) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Mpb setup");
            var graphicTex = CompositeCanvasProcess.instance.GetMainTexture(graphic);
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

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Calc Matrix");
            var matrix = _renderer.perspectiveBaking
                ? transform.localToWorldMatrix
                : _renderer.transform.worldToLocalMatrix * transform.localToWorldMatrix;
            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > DrawMesh");
            var alpha = _renderer.GetParentGroupAlpha();
            var alphaScale = Mathf.Approximately(alpha, 0) ? 0 : 1f / alpha;
            var crColor = graphic.canvasRenderer.GetColor();
            crColor.a *= graphic.canvasRenderer.GetInheritedAlpha() * alphaScale;
            if (CompositeCanvasProcess.instance.OnPreBake(_renderer, graphic, ref _mesh, _mpb, crColor) && _mesh)
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
