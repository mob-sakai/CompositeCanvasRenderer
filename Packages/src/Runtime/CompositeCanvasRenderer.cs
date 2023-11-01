using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CompositeCanvas
{
    [RequireComponent(typeof(CanvasRenderer))]
    public partial class CompositeCanvasRenderer : MaskableGraphic
    {
        internal const HideFlags k_Temporary = HideFlags.DontSave | HideFlags.NotEditable;

        private static readonly string[][] s_ColorModeKeywords =
        {
            Array.Empty<string>(),
            new[] { "COLOR_MODE_ADDITIVE" },
            new[] { "COLOR_MODE_SUBTRACT" },
            new[] { "COLOR_MODE_FILL" }
        };

        private static readonly ObjectPool<CommandBuffer> s_CommandBufferPool =
            new ObjectPool<CommandBuffer>(
                () => new CommandBuffer(),
                x => x != null,
                x => x.Clear());

        private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        [SerializeField]
        [Header("Settings")]
        private DownSamplingRate m_DownSamplingRate = DownSamplingRate.x1;

        [SerializeField]
        private bool m_ShowSourceGraphics = true;

        [SerializeField]
        private Vector2 m_Extends;

        [Header("Rendering")]
        [SerializeField]
        private bool m_Foreground;

        [SerializeField]
        private ColorMode m_ColorMode = ColorMode.Multiply;

        [SerializeField]
        private BlendType m_BlendType = BlendType.AlphaBlend;

        [SerializeField]
        private BlendMode m_SrcBlendMode = BlendMode.One;

        [SerializeField]
        private BlendMode m_DstBlendMode = BlendMode.OneMinusSrcAlpha;

        private Action _bake;
        private RenderTexture _bakeBuffer;
        private CommandBuffer _cb;
        private Action _checkTransformChanged;
        private Func<Material> _createMaterial;
        private Matrix4x4 _prevTransformMatrix;
        private Material _renderingMaterial;
        private List<CompositeCanvasSource> _sources;
        public static ulong bakedCount { get; set; }

        public List<CompositeCanvasSource> sources => _sources ?? (_sources = ListPool<CompositeCanvasSource>.Rent());

        public bool isDirty { get; private set; }

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

        public RenderTexture currentBakeBuffer => _bakeBuffer;

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

        public Vector2 extends
        {
            get => m_Extends;
            set
            {
                if (m_Extends == value) return;
                m_Extends = value;

                SetVerticesDirty();
                SetDirty();
            }
        }

        public Vector2 renderingSize => rectTransform.rect.size + m_Extends;

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

        public override Texture mainTexture
        {
            get
            {
                if (isActiveAndEnabled && rectTransform && canvas)
                {
                    var size = renderingSize * canvas.scaleFactor;
                    var rate = (int)downSamplingRate;
                    return TemporaryRenderTexture.Get(size, rate, ref _bakeBuffer);
                }

                TemporaryRenderTexture.Release(ref _bakeBuffer);
                return null;
            }
        }

        public override Material materialForRendering
        {
            get
            {
                var components = ListPool<Component>.Rent();
                GetComponents(typeof(IMaterialModifier), components);

                var currentMat = material;
                for (var i = 0; i < components.Count; i++)
                {
                    currentMat = (components[i] as IMaterialModifier)?.GetModifiedMaterial(currentMat);
                }

                ListPool<Component>.Return(ref components);
                return base.GetModifiedMaterial(currentMat);
            }
        }

        public bool isWorldSpace => canvas && canvas.renderMode == RenderMode.WorldSpace;

        public bool perspective
        {
            get
            {
                if (FrameCache.TryGet(this, nameof(perspective), out bool isPerspective))
                {
                    return isPerspective;
                }

                // Default: false.
                if (!canvas || canvas.renderMode != RenderMode.ScreenSpaceCamera
                            || !canvas.worldCamera || canvas.worldCamera.orthographic)
                {
                    FrameCache.Set(this, nameof(perspective), false);
                    return false;
                }

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

                FrameCache.Set(this, nameof(perspective), isPerspective);
                return isPerspective;
            }
        }

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
            this.AddComponentOnChildren<CompositeCanvasSource>(k_Temporary, false);
            Profiler.EndSample();

            if (!showSourceGraphics)
            {
                SetSourcesMaterialDirty();
            }

            SetSourcesVerticesDirty();

            isDirty = true;
        }

        protected override void OnDisable()
        {
            UIExtraCallbacks.onBeforeCanvasRebuild -= _checkTransformChanged;
            UIExtraCallbacks.onAfterCanvasRebuild -= _bake;

            isDirty = false;
            s_CommandBufferPool.Return(ref _cb);
            TemporaryRenderTexture.Release(ref _bakeBuffer);
            MaterialRegistry.Release(ref _renderingMaterial);
            base.OnDisable();

            canvasRenderer.hasPopInstruction = false;
            canvasRenderer.popMaterialCount = 0;

            if (!showSourceGraphics)
            {
                SetSourcesMaterialDirty();
            }
        }

        protected override void OnDestroy()
        {
            ListPool<CompositeCanvasSource>.Return(ref _sources);
            _checkTransformChanged = null;
            _bake = null;
            _createMaterial = null;
            _cb = null;
            _bakeBuffer = null;
            _renderingMaterial = null;

            base.OnDestroy();
        }

        private void OnTransformChildrenChanged()
        {
            this.AddComponentOnChildren<CompositeCanvasSource>(k_Temporary, false);
        }

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

        public Rect GetRenderingRect()
        {
            var r = GetPixelAdjustedRect();
            return new Rect(r.x - m_Extends.x / 2,
                r.y - m_Extends.y / 2,
                r.width + m_Extends.x,
                r.height + m_Extends.y);
        }

        protected override void UpdateGeometry()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] UpdateGeometry");
            var size = renderingSize;
            if (rectTransform != null && size.x >= 0 && size.y >= 0)
            {
                Color32 color32 = color;

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
            var components = ListPool<Component>.Rent();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
            {
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);
            }

            ListPool<Component>.Return(ref components);
            Profiler.EndSample();

            if (!isWorldSpace)
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

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || baseMaterial.shader != defaultMaterial.shader)
            {
                MaterialRegistry.Release(ref _renderingMaterial);
                return baseMaterial;
            }

            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] GetModifiedMaterial > Get material");
            var hash = CreateHash(colorMode, srcBlendMode, dstBlendMode);
            _createMaterial = _createMaterial ?? CreateMaterial;
            MaterialRegistry.Get(hash, ref _renderingMaterial,
                _createMaterial,
                CompositeCanvasRendererProjectSettings.cacheRendererMaterial);
            Profiler.EndSample();

            return _renderingMaterial;
        }

        public static Hash128 CreateHash(ColorMode colorMode, BlendMode srcBlendMode, BlendMode dstBlendMode)
        {
            return new Hash128(
                ShaderPropertyIds.compositeCanvas,
                (uint)colorMode,
                (uint)srcBlendMode,
                (uint)dstBlendMode
            );
        }

        private Material CreateMaterial()
        {
            return CreateMaterial(colorMode, srcBlendMode, dstBlendMode);
        }

        public static Material CreateMaterial(ColorMode colorMode, BlendMode srcBlendMode, BlendMode dstBlendMode)
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CreateMaterial");
            var mat = new Material(Shader.Find("UI/CompositeCanvasRenderer"))
            {
                hideFlags = HideFlags.DontSave | HideFlags.NotEditable,
                shaderKeywords = s_ColorModeKeywords[(int)colorMode]
            };

            mat.SetInt(ShaderPropertyIds.srcBlendMode, (int)srcBlendMode);
            mat.SetInt(ShaderPropertyIds.dstBlendMode, (int)dstBlendMode);
            Profiler.EndSample();

            return mat;
        }

        public override void SetVerticesDirty()
        {
            base.SetVerticesDirty();
            SetDirty();
        }

        public override void SetMaterialDirty()
        {
            base.SetMaterialDirty();
            SetDirty();
        }

        public void SetDirty()
        {
            if (isDirty || !isActiveAndEnabled) return;
            Logging.LogIf(!isDirty, this, $"! SetDirty {GetInstanceID()}");
            isDirty = true;
            if (perspective)
            {
                SetVerticesDirty();
            }
        }

        private void CheckTransformChanged()
        {
            if (isDirty) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CheckTransformChanged > Sources");
            var isPerspective = perspective;
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

            if (isPerspective)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CheckTransformChanged > Perspective");
                if (transform.HasChanged(null, ref _prevTransformMatrix))
                {
                    SetDirty();
                }
            }
            else
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] CheckTransformChanged > Orthographic");
                var m = Matrix4x4.Rotate(transform.localRotation) * Matrix4x4.Scale(transform.localScale);
                if (_prevTransformMatrix != m)
                {
                    SetVerticesDirty();
                }

                _prevTransformMatrix = m;
            }

            Profiler.EndSample();
        }

        internal void Register(CompositeCanvasSource canvasSource)
        {
            if (!canvasSource || sources.Contains(canvasSource)) return;

            sources.Add(canvasSource);
            isDirty = true;
            Logging.Log(this, $"Register #{sources.Count}: {canvasSource} {canvasSource.GetInstanceID()}");
        }

        internal void Unregister(CompositeCanvasSource canvasSource)
        {
            if (!sources.Contains(canvasSource)) return;

            sources.Remove(canvasSource);
            isDirty = true;
            Logging.Log(this, $"Unregister #{sources.Count}: {canvasSource} {canvasSource.GetInstanceID()}");
        }

        private bool IsInScreen()
        {
            if (!transform.lossyScale.IsVisible()) return false;

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source && source.IsInScreen())
                {
                    return true;
                }
            }

            return false;
        }

        private void Bake()
        {
            if (FrameCache.TryGet(this, nameof(Bake), out bool _)) return;
            FrameCache.Set(this, nameof(Bake), true);

            if (!isDirty) return;
            isDirty = false;

            if (!canvas || !IsInScreen()) return;

            if (_cb == null)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Rent cb");
                _cb = s_CommandBufferPool.Rent();
                _cb.name = "[CompositeCanvasRenderer] Bake";
                Profiler.EndSample();
            }

            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Init command buffer");
                _cb.Clear();
                _cb.SetRenderTarget(mainTexture);
                _cb.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
                Profiler.EndSample();
            }

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

                // In world space, orthographic vp will be used.
                if (isWorldSpace)
                {
                    //var biasScale = (Vector3.one * 2).GetScaled(relative.lossyScale.Inverse());
                    var viewMatrix = Matrix4x4.identity;
                    var projectionMatrix = Matrix4x4.TRS(
                        new Vector3(pivot.x, pivot.y, 0), //.GetScaled(biasScale),
                        Quaternion.identity,
                        new Vector3(2 / size.x, 2 / size.y, -2 / 10000f));
                    _cb.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                }
                // In default: use camera's vp.
                else
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

                Profiler.EndSample();
            }

            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Sort sources");
                sources.Sort((l, r) => CompositeCanvasSource.Compare(l, r));
                Profiler.EndSample();

                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Bake sources");
                for (var i = 0; i < sources.Count; i++)
                {
                    if (!sources[i]) continue;
                    sources[i].Bake(_cb);
                }

                Profiler.EndSample();
            }

            {
                Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] Bake > Apply Baked Effect");
                var list = ListPool<CompositeCanvasEffect>.Rent();
                GetComponents(list);

                if (0 < list.Count)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var effect = list[i];
                        if (effect.isActiveAndEnabled)
                        {
                            effect.ApplyBakedEffect(_cb);
                        }
                    }
                }

                ListPool<CompositeCanvasEffect>.Return(ref list);
                Profiler.EndSample();
            }

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
            bakedCount++;
        }

        private static void GetViewProjectionMatrix(Canvas canvas, out Matrix4x4 vMatrix, out Matrix4x4 pMatrix)
        {
            // Get view and projection matrices.
            Profiler.BeginSample("(CCR)[CompositeCanvasRenderer] GetViewProjectionMatrix");
            var rootCanvas = canvas.rootCanvas;
            var cam = rootCanvas.worldCamera;
            if (rootCanvas && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && cam)
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
        protected override void OnValidate()
        {
            base.OnValidate();

            blendType = blendType;
            isDirty = true;

            SetAllDirty();
        }

        private void OnDrawGizmos()
        {
            if (!isActiveAndEnabled || !canvas) return;
            var rootCanvas = canvas.rootCanvas.transform;
            var relative = rootCanvas.worldToLocalMatrix * transform.localToWorldMatrix;
            var pos = relative.MultiplyPoint3x4(Vector3.zero);
            pos.z = 0;

            var pivot = rectTransform.pivot - new Vector2(0.5f, 0.5f);
            var size = ((Vector3)renderingSize).GetScaled(relative.lossyScale);
            var center = pos - size.GetScaled(pivot);
            Gizmos.color = new Color(1, 0, 1, 0.5f);
            Gizmos.matrix = rootCanvas.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
            Gizmos.DrawWireCube(center, size.GetScaled(Vector3.one * 0.995f));
        }
#endif
    }
}
