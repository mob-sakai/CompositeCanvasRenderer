using CompositeCanvas.Enums;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace CompositeCanvas.Effects
{
    public class CompositeCanvasShadow : CompositeCanvasBlur, IMeshModifier
    {
        [Header("Effect Settings")]
        [SerializeField]
        private Vector2 m_EffectDistance = new Vector2(1, -1);

        public Vector2 effectDistance
        {
            get => m_EffectDistance;
            set
            {
                if (m_EffectDistance == value)
                {
                    return;
                }

                m_EffectDistance = value;
                SetRendererVerticesDirty();
            }
        }

        /// <summary>
        /// Reset to default values.
        /// </summary>
        public override void Reset()
        {
            if (!compositeCanvasRenderer) return;
            compositeCanvasRenderer.colorMode = ColorMode.Fill;
            compositeCanvasRenderer.blendType = BlendType.AlphaBlend;
            compositeCanvasRenderer.foreground = false;
            compositeCanvasRenderer.showSourceGraphics = true;
            compositeCanvasRenderer.color = new Color(0, 1, 0, 1f);
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
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            SetRendererDirty();
            SetRendererVerticesDirty();
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
            if (!isActiveAndEnabled || !compositeCanvasRenderer) return;

            Profiler.BeginSample("(CCR)[CompositeCanvasShadow] ModifyMesh > Add shadow");
            var count = vh.currentVertCount;
            var x = effectDistance.x;
            var y = effectDistance.y;
            Color32 c = compositeCanvasRenderer.color;
            ApplyShadowZeroAlloc(false, vh, count, x, y, c);
            Profiler.EndSample();
        }

        protected static void ApplyShadowZeroAlloc(bool add, VertexHelper vh, int count, float x, float y, Color32 c)
        {
            if (add)
            {
                Profiler.BeginSample("(CCR)[CompositeCanvasShadow] ModifyMesh > Add Triangle");
                var start = vh.currentVertCount;
                vh.AddTriangle(start + 0, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start + 0);
                Profiler.EndSample();
            }

            var vt = new UIVertex();
            for (var i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vt, i);
                var position = vt.position;
                position.x += x;
                position.y += y;
                vt.position = position;
                vt.color = c;

                if (add)
                {
                    Profiler.BeginSample("(CCR)[CompositeCanvasShadow] ModifyMesh > Add Vert");
                    vh.AddVert(vt);
                    Profiler.EndSample();
                }
                else
                {
                    vh.SetUIVertex(vt, i);
                }
            }
        }
    }
}
