using UnityEngine;
using UnityEngine.Rendering;

namespace CompositeCanvas.Effects
{
    [ExecuteAlways]
    [RequireComponent(typeof(CompositeCanvasRenderer))]
    [DisallowMultipleComponent]
    public abstract class CompositeCanvasEffectBase : MonoBehaviour
    {
        private CompositeCanvasRenderer _compositeCanvasRenderer;

        protected CompositeCanvasRenderer compositeCanvasRenderer =>
            _compositeCanvasRenderer
                ? _compositeCanvasRenderer
                : _compositeCanvasRenderer = GetComponent<CompositeCanvasRenderer>();

        /// <summary>
        /// Reset to default values.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            SetRendererDirty();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            SetRendererDirty();
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            SetRendererDirty();
        }

        public void SetRendererDirty()
        {
            if (!compositeCanvasRenderer) return;

            compositeCanvasRenderer.SetDirty(false);
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
