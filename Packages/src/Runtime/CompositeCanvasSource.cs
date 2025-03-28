using System;
using Coffee.CompositeCanvasRendererInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;
#if TMP_ENABLE
using TMPro;
#endif

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
        private static readonly InternalObjectPool<MaterialPropertyBlock> s_MaterialPropertyBlockPool =
            new InternalObjectPool<MaterialPropertyBlock>(
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
        private Mesh _mesh;
        private MaterialPropertyBlock _mpb;
        private Matrix4x4 _prevTransformMatrix;
        private UnityAction _setRendererDirty;
#if TMP_ENABLE
        private TMP_Text _tmpText;
        private bool _tmpMaterialChanged;
        private bool _tmpNeedsUpdate = false;
#endif

        /// <summary>
        /// The renderer associated with the source.
        /// </summary>
        public new CompositeCanvasRenderer renderer { get; private set; }

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
                if (m_IgnoreSelf || !renderer) return true;

                var rendererTr = renderer.transform;
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
        /// The Mesh to bake.
        /// </summary>
        public Mesh mesh => _mesh ? _mesh : _mesh = MeshExtensions.Rent();

        /// <summary>
        /// The MaterialPropertyBlock to bake.
        /// </summary>
        public MaterialPropertyBlock mpb => _mpb ?? (_mpb = s_MaterialPropertyBlockPool.Rent());

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
#if TMP_ENABLE
                SetupTMProEventListeners();
#endif
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

#if TMP_ENABLE
            if (_tmpText != null)
            {
                _tmpText.OnPreRenderText -= OnTMPTextChanged;
                _tmpMaterialChanged = false;
            }
#endif

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
            renderer = null;
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
                || !renderer || !renderer.isActiveAndEnabled || renderer.showSourceGraphics
                || !graphic || !graphic.isActiveAndEnabled
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
            if (!isActiveAndEnabled
                || !renderer || !renderer.isActiveAndEnabled
                || !graphic || !graphic.isActiveAndEnabled)
            {
                return;
            }

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] ModifyMesh");
            _mesh = _mesh ? _mesh : MeshExtensions.Rent();
            mesh.CopyTo(_mesh);

            if (renderer.canvas.ShouldGammaToLinearInMesh())
            {
                _mesh.GammaToLinear();
            }

            Profiler.EndSample();
            Logging.Log(this, " >>>> Graphic mesh is modified.");

            SetRendererDirty();
        }

        /// <summary>
        /// Call used to modify mesh.
        /// Place any custom mesh processing in this function.
        /// </summary>
        void IMeshModifier.ModifyMesh(VertexHelper verts)
        {
            if (!isActiveAndEnabled
                || !renderer || !renderer.isActiveAndEnabled
                || !graphic || !graphic.isActiveAndEnabled)
            {
                return;
            }

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] ModifyMesh");
            _mesh = _mesh ? _mesh : MeshExtensions.Rent();
            verts.FillMesh(_mesh);
            _mesh.RecalculateBounds();

            if (renderer.canvas.ShouldGammaToLinearInMesh())
            {
                _mesh.GammaToLinear();
            }

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
            var parent = transform.parent;
            UpdateRenderer(parent ? parent.GetComponentInParent<CompositeCanvasRenderer>() : null);
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
            if (newRenderer != renderer)
            {
                if (renderer)
                {
                    renderer.Unregister(this);
                }

                if (newRenderer)
                {
                    newRenderer.Register(this);
                }
            }

            renderer = newRenderer;
            Profiler.EndSample();
        }

        private void SetRendererDirty()
        {
            if (renderer)
            {
                renderer.SetDirty(false);
            }
        }

        internal bool HasTransformChanged(Transform baseTransform)
        {
            return transform.HasChanged(baseTransform, ref _prevTransformMatrix,
                CompositeCanvasRendererProjectSettings.sensitivity);
        }

        public bool IsInScreen()
        {
            if (FrameCache.TryGet(this, nameof(IsInScreen), out bool result))
            {
                return result;
            }

            // Cull if there is no graphic or the scale is too small.
            if (!renderer || !_graphic || !transform.lossyScale.IsVisible())
            {
                result = false;
            }
            else if (!renderer.culling)
            {
                result = true;
            }
            else
            {
                var viewport = renderer.rectTransform;
                var bounds = viewport.GetRelativeBounds(transform);
                var viewportRect = viewport.rect;
                var ex = renderer.extents;
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

        internal Material GetBakingMaterial(bool usePopMaterial)
        {
            return usePopMaterial ? _graphic.canvasRenderer.GetPopMaterial(0) : _bakingMaterial;
        }

#if TMP_ENABLE
        private void OnTMPTextChanged(TMP_TextInfo textInfo)
        {
            _tmpMaterialChanged = true;
        }

        private void SetupTMProEventListeners()
        {
            if (graphic == null) return;

            if (graphic is TMP_Text tmpText)
            {
                _tmpText = tmpText;
                _tmpText.OnPreRenderText += OnTMPTextChanged;
                _tmpNeedsUpdate = true;
            }
        }

        private void LateUpdate()
        {
            if (_tmpMaterialChanged && isActiveAndEnabled && graphic && renderer && !renderer.showSourceGraphics)
            {
                graphic.SetMaterialDirty();
                _tmpMaterialChanged = false;
            }

            if (_tmpNeedsUpdate && isActiveAndEnabled)
            {
                _tmpText.ForceMeshUpdate(true, true);
                _tmpNeedsUpdate = false;
            }
        }
#endif
    }
}
