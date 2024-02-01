using System.Linq;
using Coffee.CompositeCanvasRendererInternal;
using CompositeCanvas.Enums;
using UnityEditor;
using UnityEngine;

namespace CompositeCanvas
{
    public class CompositeCanvasRendererProjectSettings
        : PreloadedProjectSettings<CompositeCanvasRendererProjectSettings>
    {
        [Header("Setting")]
        [SerializeField]
        private TransformSensitivity m_TransformSensitivity = TransformSensitivity.Medium;

        [SerializeField]
        private bool m_EnableCullingInPlayMode = true;

        [SerializeField]
        private bool m_EnableCullingInEditMode = true;

#if UNITY_EDITOR
        [Header("Shader")]
        [SerializeField]
        private bool m_AutoIncludeShaders = true;
#endif

        public static bool enableCulling
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return instance.m_EnableCullingInEditMode;
                }
#endif
                return instance.m_EnableCullingInPlayMode;
            }
        }

        public static TransformSensitivity transformSensitivity
        {
            get => instance.m_TransformSensitivity;
            set => instance.m_TransformSensitivity = value;
        }

        public static float sensitivity
        {
            get
            {
                switch (instance.m_TransformSensitivity)
                {
                    case TransformSensitivity.Low: return 1f / (1 << 2);
                    case TransformSensitivity.Medium: return 1f / (1 << 5);
                    case TransformSensitivity.High: return 1f / (1 << 12);
                    default: return 1f / (1 << (int)instance.m_TransformSensitivity);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reset to default values.
        /// </summary>
        private void Reset()
        {
            ReloadShaders(false);
        }

        private void ReloadShaders(bool force)
        {
            if (!force && !m_AutoIncludeShaders) return;

            foreach (var shader in AssetDatabase.FindAssets("t:Shader")
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Select(AssetDatabase.LoadAssetAtPath<Shader>)
                         .Where(CanIncludeShader))
            {
                AlwaysIncludedShadersProxy.Add(shader);
            }
        }

        private static bool CanIncludeShader(Shader shader)
        {
            if (!shader) return false;

            var name = shader.name;
            return name.Contains("CompositeCanvasRenderer");
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new PreloadedProjectSettingsProvider("Project/UI/Composite Canvas Renderer");
        }

        [CustomEditor(typeof(CompositeCanvasRendererProjectSettings))]
        private class CompositeCanvasRendererProjectSettingsEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel("Included Shaders");
                    if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        instance.ReloadShaders(true);
                    }
                }
                EditorGUILayout.EndHorizontal();

                foreach (var shader in AlwaysIncludedShadersProxy.GetShaders())
                {
                    if (!CanIncludeShader(shader)) continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(shader, typeof(Shader), false);
                    if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        AlwaysIncludedShadersProxy.Remove(shader);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }
#endif
    }
}
