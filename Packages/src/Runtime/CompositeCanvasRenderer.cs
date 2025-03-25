using System;
using System.Collections.Generic;
using Coffee.CompositeCanvasRendererInternal;
using CompositeCanvas.Effects;
using CompositeCanvas.Enums;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CompositeCanvas
{
    /// <summary>
    /// CompositeCanvasRenderer bakes multiple source graphics into a bake-buffer (RenderTexture) and renders it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class CompositeCanvasRenderer : MaskableGraphic
    {
        private static readonly string[][] s_ColorModeKeywords =
        {
            Array.Empty<string>(),
            new[] { "COLOR_MODE_ADDITIVE" },
            new[] { "COLOR_MODE_SUBTRACT" },
            new[] { "COLOR_MODE_FILL" }
        };

        private static readonly InternalObjectPool<CommandBuffer> s_CommandBufferPool =
            new InternalObjectPool<CommandBuffer>(
                () => new CommandBuffer(),
                x => x != null,
                x => x.Clear());

        private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        private static readonly LinkedList<CompositeCanvasRenderer> s_ActiveRenderers =
            new LinkedList<CompositeCanvasRenderer>();

        [SerializeField]
        [Header("Buffer")]
        [Tooltip("Down sampling rate for baking.\n" +
                 "The higher this value, the lower the resolution of the bake, but the performance will improve.")]
        private DownSamplingRate m_DownSamplingRate = DownSamplingRate.x1;

        [SerializeField]
        [FormerlySerializedAs("m_UpscalingBuffer")]
        [Tooltip("Use canvas scaler to calculate bake-buffer size.\n" +
                 "If false, the bake-buffer is the same size as the rendering size.")]
        private bool m_UseCanvasScaler = true;

        [Tooltip("Bake buffer sharing group ID.\n" +
                 "If non-zero is specified, the baked buffer are shared within the group.")]
        [SerializeField]
        private int m_SharingGroupId;

        [SerializeField]
        [Tooltip("View type to bake.\n" +
                 "Automatic: Use orthographic space to bake if possible.\n" +
                 "Orthographic: Use orthographic space to bake.\n" +
                 "Perspective: Use perspective space to bake.\n")]
        private ViewType m_ViewType = ViewType.Automatic;

        [SerializeField]
        [Tooltip("The value to expand the baking range.")]
        private Vector2 m_Extents;

        [SerializeField]
        [Tooltip("Ignore source graphics outside the baking region.")]
        private bool m_Culling = true;

        [SerializeField]
        [Tooltip("Use stencil to mask for baking.")]
        private bool m_UseStencil = true;

        [SerializeField]
        [Header("Baking")]
        [Tooltip("Baking trigger mode.\n" +
                 "Automatic: Baking is performed automatically when the transform of the source graphic changes.\n" +
                 "Manually: Baking is performed manually by calling SetDirty().\n" +
                 "Always: Baking is performed every frame.\n" +
                 "OnEnable: Baking is performed once when enabled.")]
        private BakingTrigger m_BakingTrigger;

        [Header("Rendering")]
        [SerializeField]
        [Tooltip("Show the source graphics.")]
        private bool m_ShowSourceGraphics = true;

        [SerializeField]
        [Tooltip("Whether to render in front of the source graphics.")]
        private bool m_Foreground;

        [SerializeField]
        [Tooltip("Color mode for rendering.\n" +
                 "This is used when material is not set.")]
        private ColorMode m_ColorMode = ColorMode.Multiply;

        [SerializeField]
        [Tooltip("Blend type for rendering.\n" +
                 "This is used when material is not set.")]
        private BlendType m_BlendType = BlendType.AlphaBlend;

        [SerializeField]
        [Tooltip("Source blend type for rendering.\n" +
                 "This is used when material is not set and blend type is custom.")]
        private BlendMode m_SrcBlendMode = BlendMode.One;

        [SerializeField]
        [Tooltip("Destination blend type for rendering.\n" +
                 "This is used when material is not set and blend type is custom.")]
        private BlendMode m_DstBlendMode = BlendMode.OneMinusSrcAlpha;

        private Action _bake;
        private RenderTexture _bakeBuffer;
        private CommandBuffer _cb;
        private Action _checkTransformChanged;
        private Matrix4x4 _prevTransformMatrix;
        private Material _renderingMaterial;
        private List<CompositeCanvasSource> _sources;

        private List<CompositeCanvasSource> sources =>
            _sources ?? (_sources = InternalListPool<CompositeCanvasSource>.Rent());

        /// <summary>
        /// Whether to bake in the current frame.
        /// If the value of this property is true, bake after canvas update.
        /// </summary>
        public bool isDirty { get; private set; }

        /// <summary>
        /// Color mode for rendering.
        /// This is used when material is not set.
        /// </summary>
        public ColorMode colorMode
        {
            get => m_ColorMode;
            set
            {
                if (m_ColorMode == value) return;
                m_ColorMode = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// Blend type for rendering.
        /// This is used when material is not set.
        /// </summary>
        public BlendType blendType
        {
            get => m_BlendType;
            set
            {
                switch (value)
                {
                    case BlendType.AlphaBlend:
                        srcBlendMode = BlendMode.One;
                        dstBlendMode = BlendMode.OneMinusSrcAlpha;
                        break;
                    case BlendType.Additive:
                        srcBlendMode = BlendMode.One;
                        dstBlendMode = BlendMode.One;
                        break;
                    case BlendType.MultiplyAdditive:
                        srcBlendMode = BlendMode.DstColor;
                        dstBlendMode = BlendMode.One;
                        break;
                }

                if (m_BlendType == value) return;
                m_BlendType = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// Source blend type for rendering.
        /// This is used when material is not set and blend type is custom.
        /// </summary>
        public BlendMode srcBlendMode
        {
            get => m_SrcBlendMode;
            set
            {
                if (m_SrcBlendMode == value) return;
                m_SrcBlendMode = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// Destination blend type for rendering.
        /// This is used when material is not set and blend type is custom.
        /// </summary>
        public BlendMode dstBlendMode
        {
            get => m_DstBlendMode;
            set
            {
                if (m_DstBlendMode == value) return;
                m_DstBlendMode = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// Current bake buffer.
        /// </summary>
        public RenderTexture currentBakeBuffer => _bakeBuffer;

        /// <summary>
        /// Use upscaled bake-buffer.
        /// </summary>
        public bool useCanvasScaler
        {
            get => m_UseCanvasScaler;
            set
            {
                if (m_UseCanvasScaler == value) return;
                m_UseCanvasScaler = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Bake buffer sharing group ID.
        /// If non-zero is specified, the baked buffer are shared within the group.
        /// </summary>
        public int sharingGroupId
        {
            get => m_SharingGroupId;
            set
            {
                if (m_SharingGroupId == value) return;
                m_SharingGroupId = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Down sampling rate for baking.
        /// The higher this value, the lower the resolution of the bake, but the performance will improve.
        /// </summary>
        public DownSamplingRate downSamplingRate
        {
            get => m_DownSamplingRate;
            set
            {
                if (m_DownSamplingRate == value) return;
                m_DownSamplingRate = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Whether to render in front of the source graphics.
        /// </summary>
        public bool foreground
        {
            get => m_Foreground;
            set
            {
                if (m_Foreground == value) return;
                m_Foreground = value;

                SetMaterialDirty();
            }
        }

        /// <summary>
        /// The value to expand the baking range.
        /// </summary>
        public Vector2 extents
        {
            get => m_Extents;
            set
            {
                if (m_Extents == value) return;
                m_Extents = value;

                SetVerticesDirty();
                SetDirty();
            }
        }

        /// <summary>
        /// Rendering size.
        /// </summary>
        public Vector2 renderingSize => rectTransform.rect.size + m_Extents;

        /// <summary>
        /// Show the source graphics.
        /// </summary>
        public bool showSourceGraphics
        {
            get => m_ShowSourceGraphics;
            set
            {
                if (m_ShowSourceGraphics == value) return;
                m_ShowSourceGraphics = value;

                SetSourcesMaterialDirty();
            }
        }

        /// <summary>
        /// This is the texture used to render this graphic.
        /// This is the same as the bake buffer.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (isActiveAndEnabled && rectTransform && canvas)
                {
                    var size = renderingSize;
                    if (useCanvasScaler)
                    {
                        size *= canvas.scaleFactor;
                    }

                    var rate = (int)downSamplingRate;
                    var rtSize = RenderTextureRepository.GetPreferSize(Vector2Int.RoundToInt(size), rate);
                    var id = sharingGroupId == 0 ? GetInstanceID() : sharingGroupId;
                    var hash = new Hash128((uint)id, (uint)rtSize.x, (uint)rtSize.y, useStencil ? 1u : 0);
                    if (!RenderTextureRepository.Valid(hash, _bakeBuffer))
                    {
                        RenderTextureRepository.Get(hash, ref _bakeBuffer,
                            x => new RenderTexture(RenderTextureRepository.GetDescriptor(x.rtSize, x.useStencil))
                            {
                                hideFlags = HideFlags.DontSave
                            }, (rtSize, useStencil));
                    }

                    return _bakeBuffer;
                }

                RenderTextureRepository.Release(ref _bakeBuffer);
                return null;
            }
        }


        /// <summary>
        /// Ignore source graphics outside the baking region.
        /// </summary>
        public bool culling
        {
            get => CompositeCanvasRendererProjectSettings.enableCulling && m_Culling;
            set
            {
                if (m_Culling == value) return;
                m_Culling = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Use stencil to mask for baking.
        /// </summary>
        public bool useStencil
        {
            get => m_UseStencil;
            set
            {
                if (m_UseStencil == value) return;
                m_UseStencil = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Use perspective space to bake.
        /// </summary>
        public bool perspectiveBaking
        {
            get
            {
                if (viewType == ViewType.Orthographic
                    || !canvas || canvas.renderMode != RenderMode.ScreenSpaceCamera
                    || (canvas.worldCamera && canvas.worldCamera.orthographic))
                {
                    return false;
                }

                if (viewType == ViewType.Perspective)
                {
                    return true;
                }

                // Use cache if possible.
                if (FrameCache.TryGet(this, nameof(perspectiveBaking), out bool isPerspective))
                {
                    return isPerspective;
                }

                // Any source graphic is perspective, renderer is perspective mode.
                isPerspective = false;
                var w2L = transform.worldToLocalMatrix;
                for (var i = 0; i < sources.Count; i++)
                {
                    var source = sources[i];
                    if (!source || !source.graphic) continue;

                    // If perspective graphic in the source found, renderer is perspective mode.
                    var relative = w2L * source.transform.localToWorldMatrix;
                    if (0.001f < Mathf.Abs(relative.MultiplyPoint3x4(Vector3.zero).z)
                        || 0.001f < relative.rotation.eulerAngles.GetScaled(new Vector3(1, 1, 0)).sqrMagnitude)
                    {
                        isPerspective = true;
                        break;
                    }
                }

                FrameCache.Set(this, nameof(perspectiveBaking), isPerspective);
                return isPerspective;
            }
        }

        /// <summary>
        /// View type to bake.
        /// <para />
        /// Automatic: Use orthographic space to bake if possible.
        /// <para />
        /// Orthographic: Use orthographic space to bake.
        /// <para />
        /// Perspective: Use perspective space to bake.
        /// </summary>
        public ViewType viewType
        {
            get => m_ViewType;
            set
            {
                if (m_ViewType == value) return;
                m_ViewType = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Baking trigger mode.
        /// <para />
        /// Automatic: Baking is performed automatically when the transform of the source graphic changes.
        /// <para />
        /// Manually: Baking is performed manually by calling SetDirty().
        /// <para />
        /// Always: Baking is performed every frame.
        /// <para />
        /// OnEnable: Baking is performed once when enabled.
        /// </summary>
        public BakingTrigger bakingTrigger
        {
            get => m_BakingTrigger;
            set
            {
                if (Equals(m_BakingTrigger, value)) return;
                m_BakingTrigger = value;
            }
        }

        internal CommandBuffer cb => _cb ?? (_cb = s_CommandBufferPool.Rent());

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] OnEnable > Base");
            base.OnEnable();
            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] OnEnable > Register callbacks");
            useLegacyMeshGeneration = false;

            _checkTransformChanged = _checkTransformChanged ?? CheckTransformChanged;
            _bake = _bake ?? Bake;
            UIExtraCallbacks.onBeforeCanvasRebuild += _checkTransformChanged;
            UIExtraCallbacks.onAfterCanvasRebuild += _bake;
            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] OnEnable > Add source component to children");
            this.AddComponentOnChildren<CompositeCanvasSource>(HideFlags.DontSave, false);
            Profiler.EndSample();

            if (!showSourceGraphics)
            {
                SetSourcesMaterialDirty();
            }

            SetSourcesVerticesDirty();

            // Set dirty on enable.
            if (m_BakingTrigger != BakingTrigger.Manually)
            {
                SetDirty();
            }

            s_ActiveRenderers.AddLast(this);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDisable()
        {
            s_ActiveRenderers.Remove(this);

            UIExtraCallbacks.onBeforeCanvasRebuild -= _checkTransformChanged;
            UIExtraCallbacks.onAfterCanvasRebuild -= _bake;

            isDirty = false;
            s_CommandBufferPool.Return(ref _cb);
            RenderTextureRepository.Release(ref _bakeBuffer);
            MaterialRepository.Release(ref _renderingMaterial);
            base.OnDisable();

            canvasRenderer.hasPopInstruction = false;
            canvasRenderer.popMaterialCount = 0;

            if (!showSourceGraphics)
            {
                SetSourcesMaterialDirty();
            }
        }

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene receiving OnDestroy.
        /// </summary>
        protected override void OnDestroy()
        {
            InternalListPool<CompositeCanvasSource>.Return(ref _sources);
            _checkTransformChanged = null;
            _bake = null;
            _cb = null;
            _bakeBuffer = null;
            _renderingMaterial = null;

            base.OnDestroy();
        }

        /// <summary>
        /// This function is called when the list of children of the transform of the GameObject has changed.
        /// </summary>
        private void OnTransformChildrenChanged()
        {
            this.AddComponentOnChildren<CompositeCanvasSource>(HideFlags.DontSave, false);
        }

        /// <summary>
        /// Event that is fired after baking.
        /// </summary>
        public static event Action<CompositeCanvasRenderer> onBaked;

        /// <summary>
        /// Call to update the Material of the graphic onto the CanvasRenderer.
        /// </summary>
        protected override void UpdateMaterial()
        {
            if (!IsActive())
            {
                return;
            }

            var mat = materialForRendering;
            canvasRenderer.SetTexture(mainTexture);

            if (foreground)
            {
                canvasRenderer.hasPopInstruction = true;
                canvasRenderer.materialCount = 0;
                canvasRenderer.popMaterialCount = 1;
                canvasRenderer.SetPopMaterial(mat, 0);
            }
            else
            {
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.materialCount = 1;
                canvasRenderer.popMaterialCount = 0;
                canvasRenderer.SetMaterial(mat, 0);
            }
        }

        /// <summary>
        /// Get the rendering rect.
        /// </summary>
        /// <returns></returns>
        public Rect GetRenderingRect()
        {
            var r = GetPixelAdjustedRect();
            return new Rect(r.x - m_Extents.x / 2,
                r.y - m_Extents.y / 2,
                r.width + m_Extents.x,
                r.height + m_Extents.y);
        }

        /// <summary>
        /// Call to update the geometry of the Graphic onto the CanvasRenderer.
        /// </summary>
        protected override void UpdateGeometry()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] UpdateGeometry");
            var size = renderingSize;
            if (rectTransform != null && size.x >= 0 && size.y >= 0)
            {
                Color32 color32 = color;

                if (canvas.ShouldGammaToLinearInMesh())
                {
                    color32.r = color32.r.GammaToLinear();
                    color32.g = color32.g.GammaToLinear();
                    color32.b = color32.b.GammaToLinear();
                }

                var r = GetRenderingRect();
                var xMin = r.x;
                var xMax = r.x + r.width;
                var yMin = r.y;
                var yMax = r.y + r.height;

                s_VertexHelper.Clear();
                s_VertexHelper.AddVert(new Vector3(xMin, yMin), color32, new Vector2(0f, 0f));
                s_VertexHelper.AddVert(new Vector3(xMin, yMax), color32, new Vector2(0f, 1f));
                s_VertexHelper.AddVert(new Vector3(xMax, yMax), color32, new Vector2(1f, 1f));
                s_VertexHelper.AddVert(new Vector3(xMax, yMin), color32, new Vector2(1f, 0f));

                s_VertexHelper.AddTriangle(0, 1, 2);
                s_VertexHelper.AddTriangle(2, 3, 0);
            }
            else
            {
                s_VertexHelper.Clear();
            }

            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] UpdateGeometry > Modify mesh");
            var components = InternalListPool<Component>.Rent();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
            {
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);
            }

            InternalListPool<Component>.Return(ref components);
            Profiler.EndSample();

            // In perspective mode, modify the mesh to be rendered in perspective.
            if (perspectiveBaking)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] UpdateGeometry > Modify for perspective");
                // Relative L2W matrix from rootCanvas to this.
                var relative = canvas.rootCanvas.transform.worldToLocalMatrix * transform.localToWorldMatrix;
                var relativePos = relative.MultiplyPoint3x4(Vector3.zero);
                var matrix = relative.inverse * Matrix4x4.Translate(new Vector3(relativePos.x, relativePos.y));
                var vertex = new UIVertex();
                for (var i = 0; i < s_VertexHelper.currentVertCount; i++)
                {
                    s_VertexHelper.PopulateUIVertex(ref vertex, i);
                    vertex.position = matrix.MultiplyPoint3x4(vertex.position);
                    s_VertexHelper.SetUIVertex(vertex, i);
                }

                Profiler.EndSample();
            }

            s_VertexHelper.FillMesh(workerMesh);
            canvasRenderer.SetMesh(workerMesh);
            Profiler.EndSample();
        }

        /// <summary>
        /// Perform material modification in this function.
        /// </summary>
        /// <param name="baseMaterial">The material that is to be modified</param>
        /// <returns>The modified material.</returns>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || baseMaterial.shader != defaultMaterial.shader)
            {
                MaterialRepository.Release(ref _renderingMaterial);
                return baseMaterial;
            }

            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] GetModifiedMaterial > Get material");
            var hash = CreateHash(colorMode, srcBlendMode, dstBlendMode);
            MaterialRepository.Get(hash, ref _renderingMaterial,
                x => CreateMaterial(x.colorMode, x.srcBlendMode, x.dstBlendMode), this);
            Profiler.EndSample();

            return _renderingMaterial;
        }

        /// <summary>
        /// Create a hash.
        /// </summary>
        public static Hash128 CreateHash(ColorMode colorMode, BlendMode srcBlendMode, BlendMode dstBlendMode)
        {
            return new Hash128(
                ShaderPropertyIds.compositeCanvas,
                (uint)colorMode,
                (uint)srcBlendMode,
                (uint)dstBlendMode
            );
        }

        /// <summary>
        /// Create a material.
        /// </summary>
        private Material CreateMaterial()
        {
            return CreateMaterial(colorMode, srcBlendMode, dstBlendMode);
        }

        /// <summary>
        /// Create a material.
        /// </summary>
        public static Material CreateMaterial(ColorMode colorMode, BlendMode srcBlendMode, BlendMode dstBlendMode)
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CreateMaterial");
            var mat = new Material(Shader.Find("UI/CompositeCanvasRenderer"))
            {
                hideFlags = HideFlags.DontSave | HideFlags.NotEditable,
                shaderKeywords = s_ColorModeKeywords[(int)colorMode]
            };

            mat.SetInt(ShaderPropertyIds.colorMode, (int)colorMode);
            mat.SetInt(ShaderPropertyIds.srcBlendMode, (int)srcBlendMode);
            mat.SetInt(ShaderPropertyIds.dstBlendMode, (int)dstBlendMode);
            Profiler.EndSample();

            return mat;
        }

        /// <summary>
        /// Mark the baked buffer as dirty and needing re-bake.
        /// </summary>
        public void SetDirty(bool force = true)
        {
            if (isDirty || !isActiveAndEnabled) return;
            if (!force && m_BakingTrigger != BakingTrigger.Automatic)
            {
                Logging.LogIf(!isDirty, this,
                    $"<color=orange>! SetDirty {GetInstanceID()} is canceled due to non automatic mode).</color>");
                return;
            }

            Logging.LogIf(!isDirty, this, $"! SetDirty {GetInstanceID()}");
            isDirty = true;
        }

        /// <summary>
        /// If bakingTrigger is Automatic, call SetDirty when the transform of yourself or the source graphic is changed.
        /// </summary>
        private void CheckTransformChanged()
        {
            if (isDirty) return;

            switch (m_BakingTrigger)
            {
                case BakingTrigger.Always:
                    // Always set dirty.
                    SetDirty();
                    return;
                case BakingTrigger.Manually:
                case BakingTrigger.OnEnable:
                    ; // Do nothing.
                    return;
            }

            if (!IsInCanvasViewport()) return;

            // If the transform of any source graphic has changed, set dirty.
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CheckTransformChanged > Sources");
            var isPerspective = perspectiveBaking;
            var baseTransform = isPerspective ? null : transform;
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (!source || !source.IsInScreen()) continue;

                if (source.HasTransformChanged(baseTransform))
                {
                    SetDirty();
                }
            }

            Profiler.EndSample();

            // Set dirty when transform changed.
            if (isPerspective)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CheckTransformChanged > Perspective");
                if (transform.HasChanged(null, ref _prevTransformMatrix,
                        CompositeCanvasRendererProjectSettings.sensitivity))
                {
                    SetDirty(false);
                    SetVerticesDirty();
                }
            }
            else
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CheckTransformChanged > Orthographic");
                var m = Matrix4x4.Rotate(transform.localRotation) * Matrix4x4.Scale(transform.localScale);
                if (_prevTransformMatrix != m)
                {
                    SetDirty(false);
                    SetVerticesDirty();
                }

                _prevTransformMatrix = m;
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Register the source graphic.
        /// </summary>
        internal void Register(CompositeCanvasSource canvasSource)
        {
            if (!canvasSource || sources.Contains(canvasSource)) return;

            sources.Add(canvasSource);
            SetDirty(false);
            Logging.Log(this, $"Register #{sources.Count}: {canvasSource} {canvasSource.GetInstanceID()}");
        }

        /// <summary>
        /// Unregister the source graphic.
        /// </summary>
        internal void Unregister(CompositeCanvasSource canvasSource)
        {
            if (!sources.Contains(canvasSource)) return;

            sources.Remove(canvasSource);
            SetDirty(false);
            Logging.Log(this, $"Unregister #{sources.Count}: {canvasSource} {canvasSource.GetInstanceID()}");
        }

        private bool IsInCanvasViewport()
        {
            if (!culling) return true;

            if (FrameCache.TryGet(this, nameof(IsInCanvasViewport), out bool result))
            {
                return result;
            }

            // Cull if there is no graphic or the scale is too small.
            if (!transform.lossyScale.IsVisible())
            {
                result = false;
            }
            else
            {
                var viewport = canvas.rootCanvas.transform as RectTransform;
                var bounds = viewport.GetRelativeBounds(transform);
                var viewportRect = viewport.rect;
                var ex = extents;
                var rect = new Rect(bounds.min, bounds.size);
                rect.Set(rect.xMin - ex.x / 2, rect.yMin - ex.y / 2, rect.width + ex.x, rect.height + ex.y);
                result = viewportRect.Overlaps(rect, true);
            }

            FrameCache.Set(this, nameof(IsInCanvasViewport), result);
            return result;
        }

        internal static CompositeCanvasRenderer GetFirstGroupedRenderer(int groupId)
        {
            var node = s_ActiveRenderers.First;
            while (node != null)
            {
                if (node.Value.sharingGroupId == groupId) return node.Value;
                node = node.Next;
            }

            return null;
        }

        /// <summary>
        /// Render the bake buffer using the source graphic.
        /// </summary>
        private void Bake()
        {
            if (FrameCache.TryGet(this, nameof(Bake), out bool _)) return;
            FrameCache.Set(this, nameof(Bake), true);

            if (!isDirty) return;
            isDirty = false;
            if (!canvas)
            {
                Logging.Log(this, "<color=orange> Baking is canceled due to not in canvas.</color>");
                return;
            }

            if (sharingGroupId != 0)
            {
                if (GetFirstGroupedRenderer(sharingGroupId) != this)
                {
                    canvasRenderer.SetTexture(mainTexture);
                    return;
                }
            }

            // Get CommandBuffer.
            if (_cb == null)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Rent cb");
                _cb = s_CommandBufferPool.Rent();
                _cb.name = "[CompositeCanvasRenderer] Bake";
                Profiler.EndSample();
            }

            // Init CommandBuffer.
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Init command buffer");
                _cb.Clear();
                _cb.SetRenderTarget(mainTexture);
                _cb.ClearRenderTarget(true, true, Color.clear, 1f);
                Profiler.EndSample();
            }

            // Setup VP matrix.
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Setup VP matrix");
                var size = (Vector3)renderingSize;
                size.z = 1;
                var rootCanvas = canvas.rootCanvas;
                var rootRt = rootCanvas.transform as RectTransform;
                var rootSize = (Vector3)rootRt.rect.size;

                // L2W matrix from rootCanvas to this.
                var relative = rootRt.worldToLocalMatrix * transform.localToWorldMatrix;
                var pivot = rectTransform.pivot * 2 - Vector2.one;

                // Perspective mode.
                if (perspectiveBaking)
                {
                    GetViewProjectionMatrix(rootCanvas, out var viewMatrix, out var projectionMatrix);

                    var t = relative.MultiplyPoint3x4(Vector3.zero).GetScaled(-new Vector3(2, 2, 1), size.Inverse());
                    t += (Vector3)pivot;
                    var s = rootSize.GetScaled(size.Inverse());
                    var m22 = projectionMatrix.m22;
                    var m23 = projectionMatrix.m23;
                    projectionMatrix = Matrix4x4.Translate(t) * Matrix4x4.Scale(s) * projectionMatrix;
                    projectionMatrix.m22 = m22;
                    projectionMatrix.m23 = m23;
                    _cb.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                }
                // Orthographic mode.
                else
                {
                    var viewMatrix = Matrix4x4.identity;
                    var projectionMatrix = Matrix4x4.TRS(
                        new Vector3(pivot.x, pivot.y, 0),
                        Quaternion.identity,
                        new Vector3(2 / size.x, 2 / size.y, -2 / 10000f));
                    _cb.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                }

                Profiler.EndSample();
            }

            // Bake sources.
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Bake sources");
                Bake(this, transform, true, useStencil);
                Profiler.EndSample();
            }

            // Apply baked effect.
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Apply Baked Effect");
                if (TryGetComponent<CompositeCanvasEffectBase>(out var effect) && effect.isActiveAndEnabled)
                {
                    effect.ApplyBakedEffect(_cb);
                }

                Profiler.EndSample();
            }

            // Execute command buffer.
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Execute command buffer");
                Graphics.ExecuteCommandBuffer(_cb);
                Logging.Log(this, $"<color=orange> >>>> RT '{mainTexture.name}' will render.</color>");
                Profiler.EndSample();
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
            }
#endif
            canvasRenderer.SetTexture(_bakeBuffer);
            onBaked?.Invoke(this);
        }

        private static void Bake(CompositeCanvasRenderer root, Transform tr, bool isRoot, bool useStencil)
        {
            if (!tr || !tr.gameObject.activeInHierarchy) return;

            tr.TryGetComponent<CompositeCanvasSource>(out var source);
            var isActive = source && source.isActiveAndEnabled;
            var canRendering = !isRoot && isActive && !source.ignoreSelf && source.IsInScreen()
                               && source.graphic && source.graphic.isActiveAndEnabled;
            var canRenderingChildren = isRoot || (isActive && !source.ignoreChildren);
            if (canRendering)
            {
                if (source.graphic is CompositeCanvasRenderer r)
                {
                    r.Bake();
                }

                CompositeCanvasProcess.instance.Bake(root, source, false);
            }

            if (canRenderingChildren)
            {
                var childCount = tr.childCount;
                for (var i = 0; i < childCount; i++)
                {
                    Bake(root, tr.GetChild(i), false, useStencil);
                }
            }

            if (canRendering && useStencil)
            {
                CompositeCanvasProcess.instance.Bake(source.renderer, source, true);
            }
        }

        /// <summary>
        /// Get VP matrix for canvas.
        /// </summary>
        private void GetViewProjectionMatrix(Canvas canvas, out Matrix4x4 vMatrix, out Matrix4x4 pMatrix)
        {
            // Get view and projection matrices.
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] GetViewProjectionMatrix");
            var rootCanvas = canvas.rootCanvas;
            var cam = rootCanvas.worldCamera;
            if (perspectiveBaking)
            {
                vMatrix = cam.worldToCameraMatrix;
                pMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            }
            else
            {
                var pos = rootCanvas.transform.position;
                vMatrix = Matrix4x4.TRS(
                    new Vector3(-pos.x, -pos.y, -1000),
                    Quaternion.identity,
                    new Vector3(1, 1, -1f));
                pMatrix = Matrix4x4.TRS(
                    new Vector3(0, 0, -1),
                    Quaternion.identity,
                    new Vector3(1 / pos.x, 1 / pos.y, -2 / 10000f));
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Mark the materials of source graphics as dirty and needing rebuilt.
        /// </summary>
        public void SetSourcesMaterialDirty()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] SetSourcesMaterialDirty");
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (!source || !source.graphic) continue;
                source.graphic.SetMaterialDirty();
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Mark the vertices of source graphics as dirty and needing rebuilt.
        /// </summary>
        public void SetSourcesVerticesDirty()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] SetSourcesVerticesDirty");
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (!source || !source.graphic) continue;
                source.graphic.SetVerticesDirty();
            }

            Profiler.EndSample();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            blendType = blendType;
            SetDirty(false);
            SetVerticesDirty();
            SetMaterialDirty();
        }

        /// <summary>
        /// Implement OnDrawGizmos if you want to draw gizmos that are also pickable and always drawn.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!isActiveAndEnabled || !canvas) return;

            Vector3 center, size;
            if (perspectiveBaking)
            {
                var rootCanvas = canvas.rootCanvas.transform;
                var relative = rootCanvas.worldToLocalMatrix * transform.localToWorldMatrix;
                var pos = relative.MultiplyPoint3x4(Vector3.zero);
                pos.z = 0;

                var pivot = rectTransform.pivot - new Vector2(0.5f, 0.5f);
                size = ((Vector3)renderingSize).GetScaled(relative.lossyScale);
                center = pos - size.GetScaled(pivot);
                Gizmos.matrix = rootCanvas.localToWorldMatrix;
            }
            else
            {
                var pivot = rectTransform.pivot - new Vector2(0.5f, 0.5f);
                size = renderingSize;
                center = -size.GetScaled(pivot);
                Gizmos.matrix = transform.localToWorldMatrix;
            }

            Gizmos.color = new Color(1, 0, 1, 0.5f);
            Gizmos.DrawWireCube(center, size);
            Gizmos.DrawWireCube(center, size * 0.995f);
        }
#endif
    }
}
