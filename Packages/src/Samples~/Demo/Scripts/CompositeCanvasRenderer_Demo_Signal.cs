using System;
using Coffee.CompositeCanvasRendererInternal;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace CompositeCanvas.Demos
{
    [DefaultExecutionOrder(32000)]
    public class CompositeCanvasRenderer_Demo_Signal : RawImage
    {
        [SerializeField] private CompositeCanvasRenderer m_CompositeCanvasRenderer;
        [SerializeField] private Mode m_Mode;

        private Action _checkDirty;
        public static int bakedCount { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying) return;

            _checkDirty = _checkDirty ?? CheckDirty;
            UIExtraCallbacks.onBeforeCanvasRebuild += _checkDirty;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UIExtraCallbacks.onBeforeCanvasRebuild -= _checkDirty;
            canvasRenderer.SetAlpha(1);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            bakedCount = 0;
            CompositeCanvasRenderer.onBaked += _ => bakedCount++;
        }

        private void CheckDirty()
        {
            if (!canvasRenderer || !m_CompositeCanvasRenderer) return;

            switch (m_Mode)
            {
                case Mode.Baking:
                    canvasRenderer.SetAlpha(m_CompositeCanvasRenderer.isDirty ? 1 : 0);
                    break;
                case Mode.Perspective:
                    canvasRenderer.SetAlpha(m_CompositeCanvasRenderer.perspectiveBaking ? 1 : 0);
                    break;
            }
        }

        private enum Mode
        {
            Baking,
            Perspective
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CompositeCanvasRenderer_Demo_Signal))]
        private class Editor : RawImageEditor
        {
            private SerializedProperty _compositeCanvasRenderer;
            private SerializedProperty _mode;

            protected override void OnEnable()
            {
                base.OnEnable();

                _compositeCanvasRenderer = serializedObject.FindProperty("m_CompositeCanvasRenderer");
                _mode = serializedObject.FindProperty("m_Mode");
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorGUILayout.PropertyField(_compositeCanvasRenderer);
                EditorGUILayout.PropertyField(_mode);

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}
