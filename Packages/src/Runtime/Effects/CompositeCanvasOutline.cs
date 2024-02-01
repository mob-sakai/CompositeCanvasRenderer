using CompositeCanvas.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace CompositeCanvas.Effects
{
    public class CompositeCanvasOutline : CompositeCanvasShadow
    {
        [SerializeField]
        private OutlinePattern m_OutlinePattern = OutlinePattern.x4;

        public OutlinePattern outlinePattern
        {
            get => m_OutlinePattern;
            set
            {
                if (m_OutlinePattern == value) return;

                m_OutlinePattern = value;
                SetRendererVerticesDirty();
            }
        }

        /// <summary>
        /// Call used to modify mesh.
        /// Place any custom mesh processing in this function.
        /// </summary>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled) return;

            if (OutlinePattern.x0 == outlinePattern)
            {
                vh.Clear();
                return;
            }

            var count = vh.currentVertCount;
            var x = effectDistance.x;
            var y = effectDistance.y;
            Color32 c = compositeCanvasRenderer.color;

            if (OutlinePattern.x8 <= outlinePattern)
            {
                ApplyShadowZeroAlloc(true, vh, count, +x, 0, c);
                ApplyShadowZeroAlloc(true, vh, count, -x, 0, c);
                ApplyShadowZeroAlloc(true, vh, count, 0, +y, c);
                ApplyShadowZeroAlloc(true, vh, count, 0, -y, c);
            }

            if (OutlinePattern.x4 <= outlinePattern)
            {
                ApplyShadowZeroAlloc(true, vh, count, +x, -y, c);
                ApplyShadowZeroAlloc(true, vh, count, -x, +y, c);
                ApplyShadowZeroAlloc(true, vh, count, -x, -y, c);
            }

            ApplyShadowZeroAlloc(false, vh, count, +x, +y, c);
        }
    }
}
