#pragma warning disable CS0414
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Build;
#endif

namespace Coffee.SimpleSceneNavigator
{
    public class SimpleSceneNavigatorProjectSettings : PreloadedProjectSettings<SimpleSceneNavigatorProjectSettings>
    {
        [SerializeField]
        private bool m_NavigatorEnabled = true;

        [SerializeField]
        private bool m_EnabledInEditor = true;

        [SerializeField]
        private bool m_AlwaysIncludeAssembly = true;

        [SerializeField]
        private bool m_InstantiateOnLoad = true;

        [SerializeField]
        private GameObject m_Prefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnAfterSceneLoad()
        {
            if (!instance.m_InstantiateOnLoad) return;

#if UNITY_EDITOR
            if (!instance.IsValid(out var reason))
            {
                Debug.LogWarning($"[SimpleSceneNavigator] SimpleSceneNavigator does not run on load:\n{reason}");
                return;
            }
#endif

            if (!instance.m_Prefab) return;

            var go = Instantiate(instance.m_Prefab);
            DontDestroyOnLoad(go);
        }

        private bool IsValid(out string invalidReason)
        {
            invalidReason = "";
            if (!m_NavigatorEnabled)
            {
                invalidReason +=
                    " - SimpleSceneNavigator is disabled. (See Edit>Project Settings>Simple Scene Navigator)\n";
            }

            if (!m_Prefab)
            {
                invalidReason +=
                    " - SimpleSceneNavigator prefab is not set. (See Edit>Project Settings>Simple Scene Navigator)\n";
            }
#if UNITY_EDITOR
            if (Application.isPlaying && !m_EnabledInEditor)
            {
                invalidReason +=
                    " - SimpleSceneNavigator is disabled in editor. (See Edit>Project Settings>Simple Scene Navigator)\n";
            }
#endif

            return string.IsNullOrEmpty(invalidReason);
        }

#if UNITY_EDITOR
        protected void Reset()
        {
            const string prefabPath = "Packages/com.coffee.simple-scene-navigator/Prefab/SimpleSceneNavigator.prefab";
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private class FilterBuildAssemblies : IFilterBuildAssemblies
        {
            public int callbackOrder => 0;

            public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                if (instance.m_AlwaysIncludeAssembly || instance.IsValid(out var reason))
                {
                    return assemblies;
                }

                var assemblyName = typeof(SimpleSceneNavigatorProjectSettings).Assembly.GetName().Name + ".dll";
                Debug.LogWarning($"[SimpleSceneNavigator] Assembly '{assemblyName}' will be excluded in build. " +
                                 $"SimpleSceneNavigator will not run on load:\n{reason}");
                return assemblies
                    .Where(x => !x.EndsWith(assemblyName))
                    .ToArray();
            }
        }

        [CustomEditor(typeof(SimpleSceneNavigatorProjectSettings))]
        private class SimpleSceneNavigatorProjectSettingsEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (!instance.IsValid(out var reason))
                {
                    var message = $"SimpleSceneNavigator will be excluded in build: \n{reason}";
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                }
            }
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new PreloadedProjectSettingsProvider("Project/Development/Scene Navigator");
        }
#endif
    }
}
