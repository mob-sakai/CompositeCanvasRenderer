using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace CompositeCanvas.Demos
{
    [ExecuteAlways]
    public class CompositeCanvasRenderer_Demo_BufferImage : RawImage
    {
        [SerializeField] private CompositeCanvasRenderer m_CompositeCanvasRenderer;

        private void LateUpdate()
        {
            var valid = m_CompositeCanvasRenderer && m_CompositeCanvasRenderer.currentBakeBuffer;
            texture = valid ? m_CompositeCanvasRenderer.mainTexture : null;
            canvasRenderer.SetAlpha(valid ? 1 : 0);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            texture = null;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CompositeCanvasRenderer_Demo_BufferImage))]
    internal class CompositeCanvasRenderer_Demo_PreBakeImageEditor : RawImageEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CompositeCanvasRenderer"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
