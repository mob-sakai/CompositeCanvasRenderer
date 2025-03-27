using System.Collections.Generic;
using Coffee.CompositeCanvasRendererInternal;
using CompositeCanvas.Enums;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace CompositeCanvas.Effects
{
    public class CompositeCanvasMirror : CompositeCanvasBlur, IMeshModifier
    {
        [SerializeField]
        private float m_Offset;

        [SerializeField]
        private Gradient m_Gradient = new Gradient
        {
            alphaKeys = new[]
            {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(0, 0.5f)
            },
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0)
            }
        };

        private List<(float time, Color color)> _gradientCache;
        private bool _gradientDirty = true;

        public float offset
        {
            get => m_Offset;
            set
            {
                if (Mathf.Approximately(m_Offset, value)) return;

                m_Offset = value;
                SetRendererVerticesDirty();
            }
        }

        public Gradient gradient
        {
            get => m_Gradient;
            set
            {
                if (m_Gradient.Equals(value)) return;

                m_Gradient = value;
                _gradientDirty = true;
                SetRendererVerticesDirty();
            }
        }

        /// <summary>
        /// Reset to default values.
        /// </summary>
        public override void Reset()
        {
            if (!compositeCanvasRenderer) return;
            compositeCanvasRenderer.blendType = BlendType.AlphaBlend;
            compositeCanvasRenderer.colorMode = ColorMode.Multiply;
            compositeCanvasRenderer.foreground = false;
            compositeCanvasRenderer.showSourceGraphics = true;
            compositeCanvasRenderer.color = new Color(1, 1, 1, 1f);
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            SetRendererDirty();
            SetRendererVerticesDirty();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDisable()
        {
            SetRendererDirty();
            SetRendererVerticesDirty();
        }

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene receiving OnDestroy.
        /// </summary>
        private void OnDestroy()
        {
            InternalListPool<(float time, Color color)>.Return(ref _gradientCache);
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            SetRendererDirty();
            SetRendererVerticesDirty();
            _gradientDirty = true;

            base.OnValidate();
        }

        /// <summary>
        /// Call used to modify mesh.
        /// Place any custom mesh processing in this function.
        /// </summary>
        public void ModifyMesh(Mesh mesh)
        {
        }

        /// <summary>
        /// Call used to modify mesh.
        /// Place any custom mesh processing in this function.
        /// </summary>
        public virtual void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasMirror] ModifyMesh");
            vh.Clear();

            // Cache gradient as list to avoid GC
            if (_gradientDirty)
            {
                _gradientDirty = false;
                _gradientCache = _gradientCache ?? InternalListPool<(float time, Color color)>.Rent();
                gradient.ToList(_gradientCache);
            }

            var r = compositeCanvasRenderer.GetRenderingRect();
            foreach (var (time, color) in _gradientCache)
            {
                AddVert(vh, r, time, color);
            }

            Profiler.EndSample();
        }

        private void AddVert(VertexHelper vh, Rect r, float time, Color color)
        {
            var xMin = r.x;
            var xMax = r.x + r.width;
            var y = r.y - r.height * time + m_Offset;
            Color32 color32 = color;

            Profiler.BeginSample("(CCR)[CompositeCanvasMirror] AddVert > Add vertex");
            vh.AddVert(new Vector3(xMin, y), color32, new Vector2(0f, time));
            vh.AddVert(new Vector3(xMax, y), color32, new Vector2(1f, time));
            Profiler.EndSample();

            if (time <= 0) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasMirror] AddVert > Add triangle");
            var c = vh.currentVertCount;
            vh.AddTriangle(c - 4, c - 3, c - 2);
            vh.AddTriangle(c - 2, c - 3, c - 1);
            Profiler.EndSample();
        }
    }
}
