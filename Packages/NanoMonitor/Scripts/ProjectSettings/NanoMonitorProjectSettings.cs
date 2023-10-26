#pragma warning disable CS0414
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditorInternal;
#endif

namespace Coffee.NanoMonitor
{
    public class NanoMonitorProjectSettings : PreloadedProjectSettings<NanoMonitorProjectSettings>
    {
        [Header("Condition")]
        [SerializeField]
        private bool m_NanoMonitorEnabled = true;

        [SerializeField]
        private string m_BootSceneNameRegex = ".*";

        [SerializeField]
        private bool m_DevelopmentBuildOnly = true;

        [SerializeField]
        private bool m_EnabledInEditor = true;

        [SerializeField]
        private bool m_AlwaysIncludeAssembly = true;

        [SerializeField]
        private bool m_InstantiateOnLoad = true;

        [Header("Settings")]
        [SerializeField]
        private GameObject m_Prefab;

        [SerializeField]
        private bool m_Opened = true;

        [SerializeField]
        [Range(0.01f, 2f)]
        private float m_Interval = 0.5f;

        [SerializeField]
        private Image.OriginVertical m_Anchor = Image.OriginVertical.Top;


        [HideInInspector]
        [SerializeField]
        private CustomMonitorItem[] m_CustomMonitorItems =
        {
            new CustomMonitorItem("Screen:{0}x{1}", (typeof(Screen), "width"), (typeof(Screen), "height"))
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnAfterSceneLoad()
        {
            if (!instance.m_InstantiateOnLoad) return;

#if UNITY_EDITOR
            var development = EditorUserBuildSettings.development;
            if (!instance.IsValid(SceneManager.GetActiveScene().name, development, out var reason))
            {
                Debug.LogWarning($"[NanoMonitor] NanoMonitor does not run on load:\n{reason}");
                return;
            }
#endif

            if (!instance.m_Prefab) return;

            var go = Instantiate(instance.m_Prefab);
            DontDestroyOnLoad(go);

            var monitor = go.GetComponent<NanoMonitor>();
            monitor.SetUp(instance.m_Anchor, instance.m_Opened, instance.m_Interval, instance.m_CustomMonitorItems);
        }

        private bool IsValid(string bootSceneName, bool development, out string invalidReason)
        {
            invalidReason = "";
            if (!m_NanoMonitorEnabled)
            {
                invalidReason += " - NanoMonitor is disabled. (See Edit>Project Settings>Nano Monitor)\n";
            }

            if (!m_Prefab)
            {
                invalidReason += " - NanoMonitor prefab is not set. (See Edit>Project Settings>Nano Monitor)\n";
            }

            if (m_DevelopmentBuildOnly && !development)
            {
                invalidReason += " - Development build only.\n";
            }

            if (!Regex.IsMatch(bootSceneName, m_BootSceneNameRegex))
            {
                invalidReason +=
                    $" - Boot scene name '{bootSceneName}' does not match regex '{m_BootSceneNameRegex}'.\n";
            }
#if UNITY_EDITOR
            if (Application.isPlaying && !m_EnabledInEditor)
            {
                invalidReason += " - NanoMonitor is disabled in editor. (See Edit>Project Settings>Nano Monitor)\n";
            }
#endif

            return string.IsNullOrEmpty(invalidReason);
        }

#if UNITY_EDITOR
        protected void Reset()
        {
            const string prefabPath = "Packages/com.coffee.nano-monitor/Prefab/NanoMonitor.prefab";
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static string GetBootSceneName()
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scene = SceneManager.GetSceneByBuildIndex(i);
                if (!scene.IsValid()) continue;

                return scene.name;
            }

            return "";
        }

        private class FilterBuildAssemblies : IFilterBuildAssemblies
        {
            public int callbackOrder => 0;

            public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                var development = 0 != (buildOptions & BuildOptions.Development);
                if (instance.m_AlwaysIncludeAssembly ||
                    instance.IsValid(GetBootSceneName(), development, out var reason))
                {
                    return assemblies;
                }

                var assemblyName = typeof(NanoMonitor).Assembly.GetName().Name + ".dll";
                Debug.LogWarning($"[NanoMonitor] Assembly '{assemblyName}' will be excluded in build. " +
                                 $"NanoMonitor will not run on load:\n{reason}");
                return assemblies
                    .Where(x => !x.EndsWith(assemblyName))
                    .ToArray();
            }
        }

        [CustomEditor(typeof(NanoMonitorProjectSettings))]
        private class NanoMonitorProjectSettingsEditor : Editor
        {
            private ReorderableList _itemsRo;

            private void OnEnable()
            {
                var sp = serializedObject.FindProperty("m_CustomMonitorItems");
                _itemsRo = CustomMonitorItemDrawer.CreateReorderableList(sp);
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                _itemsRo.DoLayoutList();
                serializedObject.ApplyModifiedProperties();

                var development = EditorUserBuildSettings.development;
                if (!instance.IsValid(GetBootSceneName(), development, out var reason))
                {
                    var message = $"NanoMonitor will be excluded in build: \n{reason}";
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                }
            }
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new PreloadedProjectSettingsProvider("Project/Development/Nano Monitor");
        }
#endif
    }
}
