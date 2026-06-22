using Coffee.CompositeCanvasRendererInternal;
using UnityEditor;
using UnityEngine;

namespace CompositeCanvas
{
    [CustomEditor(typeof(CompositeCanvasRendererProjectSettings))]
    internal class CompositeCanvasRendererProjectSettingsEditor : PreloadedProjectSettingsEditor
    {
        private ShaderVariantRegistryEditor _shaderVariantRegistryEditor;

        protected override void OnEnable()
        {
            base.OnEnable();

            _shaderVariantRegistryEditor = ShaderVariantRegistryEditor.CreateWithoutOption(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            EditorGUIUtility.labelWidth = 180;
            base.OnInspectorGUI();

            // Shader
            // Shader registry
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shader", EditorStyles.boldLabel);
            _shaderVariantRegistryEditor.Draw();

            // Advanced
            DrawPreLoadSettingsInBuild("CompositeCanvasRenderer");
            GUILayout.FlexibleSpace();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
