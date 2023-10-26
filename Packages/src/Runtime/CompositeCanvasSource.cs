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

        private static readonly ObjectPool<Mesh> s_MeshPool = new ObjectPool<Mesh>(
            () =>
            {
                var mesh = new Mesh
                {
                    hideFlags = HideFlags.DontSave | HideFlags.NotEditable
                };
                mesh.MarkDynamic();
                return mesh;
            },
            mesh => mesh,
            mesh =>
            {
                if (mesh)
                {
                    mesh.Clear();
                }
            });

        private Action _checkRenderColor;
        private Color _color;
        private Graphic _graphic;
        private bool _isBaking;
        private Material _material;
        private Mesh _mesh;
        private MaterialPropertyBlock _mpb;
        private Matrix4x4 _prevTransformMatrix;
        private CompositeCanvasRenderer _renderer;
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

        protected override void OnEnable()
        {
            Profiler.BeginSample("(CCR)[CompositeCanvasSource] OnEnable > Set hideFlags");
            hideFlags = CompositeCanvasRenderer.k_Temporary;
            Profiler.EndSample();

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
            this.AddComponentOnChildren<CompositeCanvasSource>(CompositeCanvasRenderer.k_Temporary, false);
            Profiler.EndSample();
        }

        protected override void OnDisable()
        {
            UIExtraCallbacks.onBeforeCanvasRebuild -= _checkRenderColor;

            MaterialRegistry.Release(ref _material);
            s_MeshPool.Return(ref _mesh);
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
            this.AddComponentOnChildren<CompositeCanvasSource>(CompositeCanvasRenderer.k_Temporary, false);
        }

        protected override void OnTransformParentChanged()
        {
            UpdateRenderer();
            SetRendererDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetRendererDirty();
        }
#endif

        // public int CompareTo(CompositeCanvasSource other)
        // {
        //     Profiler.BeginSample("(CCR)[CompositeCanvasSource] CompareTo");
        //
        //     if (this == other)
        //     {
        //         Profiler.EndSample();
        //         return 0;
        //     }
        //
        //     if (!this && other)
        //     {
        //         Profiler.EndSample();
        //         return -1;
        //     }
        //
        //     if (this && !other)
        //     {
        //         Profiler.EndSample();
        //         return 1;
        //     }
        //
        //     Profiler.BeginSample("(CCR)[CompositeCanvasSource] CompareTo > depth");
        //     var depth = graphic ? graphic.depth : -1;
        //     var otherDepth = other.graphic ? other.graphic.depth : -1;
        //     Profiler.EndSample();
        //
        //     if (depth != -1 && otherDepth != -1)
        //     {
        //         Profiler.EndSample();
        //         return depth - otherDepth;
        //     }
        //
        //     var compare = transform.CompareHierarchyIndex(other.transform, _renderer ? _renderer.transform : null);
        //
        //     Profiler.EndSample();
        //     return compare;
        // }

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || !graphic || !_renderer || !_renderer.isActiveAndEnabled)
            {
                return baseMaterial;
            }

            if (_isBaking)
            {
                var (_, _, graphicMat) = CompositeCanvasRenderer.Decompose(graphic);
                graphicMat = graphicMat ? graphicMat : baseMaterial;
                var isDefaultShader = graphicMat.shader == graphic.defaultMaterial.shader;
                if (isDefaultShader)
                {
                    const ColorMode colorMode = ColorMode.Multiply;
                    const BlendMode srcBlend = BlendMode.One;
                    const BlendMode dstBlend = BlendMode.OneMinusSrcAlpha;
                    var hash = CompositeCanvasRenderer.CreateHash(colorMode, srcBlend, dstBlend);
                    MaterialRegistry.Get(hash, ref _material,
                        () => CompositeCanvasRenderer.CreateMaterial(colorMode, srcBlend, dstBlend),
                        CompositeCanvasRendererProjectSettings.cacheRendererMaterial);
                    return _material;
                }
            }

            MaterialRegistry.Release(ref _material);
            return _renderer.showSourceGraphics ? baseMaterial : null;
        }

        void IMeshModifier.ModifyMesh(Mesh mesh)
        {
        }

        void IMeshModifier.ModifyMesh(VertexHelper verts)
        {
            if (!isActiveAndEnabled || !_renderer || !_renderer.isActiveAndEnabled || !graphic) return;

            // Find graphic mesh.
            if (CompositeCanvasRenderer.Decompose(graphic).graphicMesh) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] ModifyMesh");
            if (!_mesh)
            {
                _mesh = s_MeshPool.Rent();
            }

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
            if (!IsInScreen()) return;

            // Get the mesh, texture and material for rendering.
            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Decompose");
            var (graphicMesh, graphicTex, graphicMat) = CompositeCanvasRenderer.Decompose(graphic);
            graphicMesh = graphicMesh ? graphicMesh : _mesh;
            Profiler.EndSample();

            if (!graphicMesh) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Get Modified Material");
            _isBaking = true;
            graphicMat = graphicMat ? graphicMat : graphic.materialForRendering;
            _isBaking = false;
            Profiler.EndSample();

            if (!graphicMat) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Mpb setup");
            if (_mpb == null)
            {
                _mpb = s_MaterialPropertyBlockPool.Rent();
            }

            _mpb.Clear();

            if (graphicTex != null)
            {
                _mpb.SetTexture(ShaderPropertyIds.mainTex, graphicTex);

                // Use _TextureSampleAdd for alpha only texture
                if (GraphicsFormatUtility.IsAlphaOnlyFormat(graphicTex.graphicsFormat))
                {
                    _mpb.SetVector(ShaderPropertyIds.textureSampleAdd, new Vector4(1, 1, 1, 0));
                }
            }

            _mpb.SetVector(ShaderPropertyIds.color, graphic.canvasRenderer.GetColor());
            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > Calc Matrix");
            var matrix = _renderer.isWorldSpace
                ? _renderer.transform.worldToLocalMatrix * transform.localToWorldMatrix
                : transform.localToWorldMatrix;
            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[CompositeCanvasSource] Bake > DrawMesh");
            cb.DrawMesh(graphicMesh, matrix, graphicMat, 0, 0, _mpb);
            Logging.Log(this, $"<color=orange> >>>> Mesh '{name}' will render.</color>");

            Profiler.EndSample();
        }

        internal bool HasTransformChanged(Transform baseTransform)
        {
            return transform.HasChanged(baseTransform, ref _prevTransformMatrix);
        }

        public bool IsInScreen()
        {
            // Cull if there is no graphic or the scale is too small.
            if (!graphic || !transform.lossyScale.IsVisible()) return false;

            return !CompositeCanvasRendererProjectSettings.enableCulling
                   || graphic.IsInScreen();
        }
    }
}
