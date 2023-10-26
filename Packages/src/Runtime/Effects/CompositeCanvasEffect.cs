using UnityEngine;
using UnityEngine.Rendering;

namespace CompositeCanvas
{
    [ExecuteAlways]
    [RequireComponent(typeof(CompositeCanvasRenderer))]
    [DisallowMultipleComponent]
    public abstract class CompositeCanvasEffect : MonoBehaviour
    {
        private CompositeCanvasRenderer _compositeCanvasRenderer;

        protected CompositeCanvasRenderer compositeCanvasRenderer =>
            _compositeCanvasRenderer
                ? _compositeCanvasRenderer
                : _compositeCanvasRenderer = GetComponent<CompositeCanvasRenderer>();

        public virtual void Reset()
        {
        }

        protected virtual void OnEnable()
        {
            SetRendererDirty();
        }

        protected virtual void OnDisable()
        {
            SetRendererDirty();
        }

        protected virtual void OnValidate()
        {
            SetRendererDirty();
        }

        public void SetRendererDirty()
        {
            if (!compositeCanvasRenderer) return;

            compositeCanvasRenderer.SetDirty();
        }

        public void SetRendererVerticesDirty()
        {
            if (!compositeCanvasRenderer) return;

            compositeCanvasRenderer.SetVerticesDirty();
        }

        public void SetRendererMaterialDirty()
        {
            if (!compositeCanvasRenderer) return;

            compositeCanvasRenderer.SetMaterialDirty();
        }

        public abstract void ApplyBakedEffect(CommandBuffer cb);
    }
}
