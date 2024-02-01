using CompositeCanvas.Enums;
using UnityEngine;

namespace CompositeCanvas.Effects
{
    public class CompositeCanvasBloom : CompositeCanvasBlur
    {
        public override void Reset()
        {
            if (!compositeCanvasRenderer) return;

            compositeCanvasRenderer.colorMode = ColorMode.Multiply;
            compositeCanvasRenderer.blendType = BlendType.MultiplyAdditive;
            compositeCanvasRenderer.foreground = true;
            compositeCanvasRenderer.showSourceGraphics = true;
            compositeCanvasRenderer.color = new Color(1, 1, 1, 0.5f);
            blur = 1f;
            iteration = 10;
            power = 2f;
            multiplier = 1.5f;
        }
    }
}
