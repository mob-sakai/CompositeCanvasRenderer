using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Coffee.NanoMonitor
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(CustomMonitorItem))]
    internal sealed class CustomMonitorItemDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect p, SerializedProperty property, GUIContent label)
        {
            var pos = new Rect(p.x, p.y + 18 * 0, p.width, 16);
            EditorGUI.PropertyField(pos, property.FindPropertyRelative("m_Format"));
            pos.y += 18;
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(pos, property.FindPropertyRelative("m_Arg0"));
            pos.y += 18;
            EditorGUI.PropertyField(pos, property.FindPropertyRelative("m_Arg1"));
            pos.y += 18;
            EditorGUI.PropertyField(pos, property.FindPropertyRelative("m_Arg2"));
            EditorGUI.indentLevel--;

            property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + 2) * 4;
        }

        public static ReorderableList CreateReorderableList(SerializedProperty items)
        {
            var headerContent = new GUIContent("Custom Monitor Items");
            return new ReorderableList(items.serializedObject, items)
            {
                draggable = true,
                drawHeaderCallback = r => EditorGUI.LabelField(r, headerContent),
                drawElementCallback = (r, i, _, __) =>
                {
                    var rect = new Rect(r.x, r.y, r.width, r.height - 2);
                    EditorGUI.LabelField(rect, GUIContent.none,
                        EditorStyles.helpBox);
                    var labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 80;
                    rect = new Rect(r.x + 2, r.y + 3, r.width - 4, r.height - 4);
                    EditorGUI.PropertyField(rect, items.GetArrayElementAtIndex(i), true);
                    EditorGUIUtility.labelWidth = labelWidth;
                },
                elementHeightCallback = i => EditorGUI.GetPropertyHeight(items.GetArrayElementAtIndex(i)) + 6
            };
        }
    }
#endif

    [Serializable]
    public class CustomMonitorItem
    {
        [SerializeField] private string m_Format = "";
        [SerializeField] private NumericProperty m_Arg0;
        [SerializeField] private NumericProperty m_Arg1;
        [SerializeField] private NumericProperty m_Arg2;

        [NonSerialized]
        public MonitorUI ui;

        public CustomMonitorItem()
        {
        }

        public CustomMonitorItem(string format, params (Type type, string member)[] props)
        {
            m_Format = format;
            m_Arg0 = 0 < props.Length ? new NumericProperty(props[0].type, props[0].member) : new NumericProperty();
            m_Arg1 = 1 < props.Length ? new NumericProperty(props[1].type, props[1].member) : new NumericProperty();
            m_Arg2 = 2 < props.Length ? new NumericProperty(props[2].type, props[2].member) : new NumericProperty();
        }

        public void UpdateText()
        {
            if (ui)
            {
                ui.SetText(m_Format, m_Arg0.Get(), m_Arg1.Get(), m_Arg2.Get());
            }
        }
    }
}
