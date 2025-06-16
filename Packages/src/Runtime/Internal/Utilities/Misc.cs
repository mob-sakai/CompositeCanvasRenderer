using System;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;
using System.Reflection;
#endif

namespace Coffee.CompositeCanvasRendererInternal
{
#if !UNITY_2021_2_OR_NEWER
    [AttributeUsage(AttributeTargets.Class)]
    [Conditional("UNITY_EDITOR")]
    internal class IconAttribute : Attribute
    {
        private readonly string _path;

        public IconAttribute(string path)
        {
            _path = path;
        }

#if UNITY_EDITOR
        private static Action<Object, Texture2D> s_SetIconForObject = typeof(EditorGUIUtility)
            .GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Action<Object, Texture2D>), null) as Action<Object, Texture2D>;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if (Application.isBatchMode || BuildPipeline.isBuildingPlayer) return;

            var types = TypeCache.GetTypesWithAttribute<IconAttribute>();
            var scripts = MonoImporter.GetAllRuntimeMonoScripts();
            foreach (var type in types)
            {
                var script = scripts.FirstOrDefault(x => x.GetClass() == type);
                if (!script) continue;

                var path = type.GetCustomAttribute<IconAttribute>()?._path;
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!icon) continue;

                s_SetIconForObject(script, icon);
            }
        }
#endif
    }
#endif
}
