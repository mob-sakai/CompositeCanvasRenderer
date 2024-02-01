using CompositeCanvas.Enums;
using UnityEngine;

namespace CompositeCanvas.Effects
{
    public class CompositeCanvasGlow : CompositeCanvasOutline
    {
        [Header("Inner")]
        [Range(0, 1)]
        [SerializeField]
        private float m_InnerCutoff = 1;

        public override float innerCutoff
        {
            get => m_InnerCutoff;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_InnerCutoff, value)) return;

                m_InnerCutoff = value;
                SetRendererDirty();
            }
        }

        /// <summary>
        /// Reset to default values.
        /// </summary>
        public override void Reset()
        {
            if (!compositeCanvasRenderer) return;
            compositeCanvasRenderer.blendType = BlendType.Additive;
            compositeCanvasRenderer.foreground = true;
            compositeCanvasRenderer.showSourceGraphics = true;
            compositeCanvasRenderer.color = new Color(0, 1, 0, 0.5f);
            innerCutoff = 0.8f;
            effectDistance = new Vector2(5, -5);
            blur = 1f;
            iteration = 10;
            power = 1f;
            multiplier = 1f;
        }
    }
}
