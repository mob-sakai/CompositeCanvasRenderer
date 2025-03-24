using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CompositeCanvas.Effects;
using CompositeCanvas.Enums;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace CompositeCanvas
{
    [CustomEditor(typeof(CompositeCanvasRenderer))]
    [CanEditMultipleObjects]
    public class CompositeCanvasRendererEditor : GraphicEditor
    {
        private static readonly GUIContent s_ContentPrimaryRenderer = new GUIContent("Primary Renderer");
        private static readonly GUIContent s_ContentNone = new GUIContent("None");
        private static readonly GUIContent s_ContentEffect = new GUIContent("Effect");
        private static readonly GUIContent s_ContentAttachedEffect = new GUIContent("Attached Effect");
        private static readonly Dictionary<Type, GUIContent> s_EffectLabels = new Dictionary<Type, GUIContent>();

        private static readonly MethodInfo s_DrawSprite =
            Type.GetType("UnityEditor.UI.SpriteDrawUtility, UnityEditor.UI")
                ?.GetMethod("DrawSprite", BindingFlags.NonPublic | BindingFlags.Static);

        private SerializedProperty _bakingTrigger;
        private SerializedProperty _blendType;
        private SerializedProperty _colorMode;
        private SerializedProperty _culling;
        private CompositeCanvasRenderer _current;
        private SerializedProperty _downSamplingRate;
        private SerializedProperty _dstBlendMode;
        private SerializedProperty _extents;
        private SerializedProperty _foreground;
        private Editor _materialEditor;
        private SerializedProperty _sharingGroupId;
        private SerializedProperty _showSourceGraphics;
        private SerializedProperty _srcBlendMode;
        private SerializedProperty _useCanvasScaler;
        private SerializedProperty _useStencil;
        private SerializedProperty _viewType;

        protected override void OnEnable()
        {
            base.OnEnable();
            _current = target as CompositeCanvasRenderer;

            _showSourceGraphics = serializedObject.FindProperty("m_ShowSourceGraphics");
            _downSamplingRate = serializedObject.FindProperty("m_DownSamplingRate");
            _useCanvasScaler = serializedObject.FindProperty("m_UseCanvasScaler");
            _sharingGroupId = serializedObject.FindProperty("m_SharingGroupId");
            _foreground = serializedObject.FindProperty("m_Foreground");
            _extents = serializedObject.FindProperty("m_Extents");
            _culling = serializedObject.FindProperty("m_Culling");
            _useStencil = serializedObject.FindProperty("m_UseStencil");
            _bakingTrigger = serializedObject.FindProperty("m_BakingTrigger");
            _viewType = serializedObject.FindProperty("m_ViewType");
            _colorMode = serializedObject.FindProperty("m_ColorMode");
            _blendType = serializedObject.FindProperty("m_BlendType");
            _srcBlendMode = serializedObject.FindProperty("m_SrcBlendMode");
            _dstBlendMode = serializedObject.FindProperty("m_DstBlendMode");
        }

        protected override void OnDisable()
        {
            if (_materialEditor)
            {
                DestroyImmediate(_materialEditor);
            }

            _materialEditor = null;

            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Color);
            RaycastControlsGUI();
            MaskableControlsGUI();

            // Buffer Settings
            EditorGUILayout.PropertyField(_downSamplingRate);
            EditorGUILayout.PropertyField(_useCanvasScaler);
            EditorGUILayout.PropertyField(_extents);
            EditorGUILayout.PropertyField(_useStencil);
            EditorGUILayout.PropertyField(_sharingGroupId);
            ShowPrimaryRenderer();

            // Baking Settings
            EditorGUILayout.PropertyField(_bakingTrigger);
            EditorGUILayout.PropertyField(_viewType);
            EditorGUILayout.PropertyField(_culling);

            // Rendering Settings
            ShowSourceGraphicsControlGUI();
            EditorGUILayout.PropertyField(_foreground);
            EditorGUILayout.PropertyField(m_Material);

            if (!m_Material.objectReferenceValue)
            {
                EditorGUILayout.PropertyField(_colorMode);
                EditorGUILayout.PropertyField(_blendType);
                if (_blendType.intValue == (int)BlendType.Custom)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_srcBlendMode);
                    EditorGUILayout.PropertyField(_dstBlendMode);
                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();

            // Effect Settings
            DrawEffectDropdown();

            if (_current.foreground)
            {
                var mat = _current.materialForRendering;
                if (mat)
                {
                    EditorGUI.BeginDisabledGroup(0 < (mat.hideFlags & HideFlags.NotEditable));
                    CreateCachedEditor(mat, typeof(MaterialEditor), ref _materialEditor);
                    _materialEditor.DrawHeader();
                    _materialEditor.OnInspectorGUI();
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        private void ShowPrimaryRenderer()
        {
            if (_sharingGroupId.intValue == 0) return;

            EditorGUI.indentLevel++;
            EditorGUI.showMixedValue = _sharingGroupId.hasMultipleDifferentValues;
            EditorGUI.BeginDisabledGroup(true);
            var obj = CompositeCanvasRenderer.GetFirstGroupedRenderer(_sharingGroupId.intValue);
            EditorGUILayout.ObjectField(s_ContentPrimaryRenderer, obj, typeof(CompositeCanvasRenderer), true);
            EditorGUI.EndDisabledGroup();
            EditorGUI.showMixedValue = false;
            EditorGUI.indentLevel--;
        }

        private void ShowSourceGraphicsControlGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_showSourceGraphics);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                foreach (var renderer in targets.OfType<CompositeCanvasRenderer>())
                {
                    renderer.SetSourcesMaterialDirty();
                }
            }
        }

        private void DrawEffectDropdown()
        {
            var current = target as CompositeCanvasRenderer;
            if (!current) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(s_ContentEffect, EditorStyles.boldLabel);

            var rect = EditorGUILayout.GetControlRect(true, 16, EditorStyles.popup);
            rect = EditorGUI.PrefixLabel(rect, s_ContentAttachedEffect);

            var currentEffectType = current.GetComponent<CompositeCanvasEffectBase>()?.GetType();
            if (!GUI.Button(rect, GetEffectLabel(currentEffectType), EditorStyles.popup)) return;

            var menu = new GenericMenu();
            menu.AddItem(s_ContentNone, currentEffectType == null, DrawEffectDropdown, null);

            foreach (var type in TypeCache.GetTypesDerivedFrom<CompositeCanvasEffectBase>())
            {
                menu.AddItem(GetEffectLabel(type), type == currentEffectType, DrawEffectDropdown, type);
            }

            menu.DropDown(rect);
        }

        private void DrawEffectDropdown(object typeObject)
        {
            var type = typeObject as Type;
            foreach (var renderer in targets.OfType<CompositeCanvasRenderer>())
            {
                var effect = renderer.GetComponent<CompositeCanvasEffectBase>();
                if (effect)
                {
                    ConvertTo(effect, type);
                    renderer.GetComponent<CompositeCanvasEffectBase>()?.Reset();
                }
                else if (type != null)
                {
                    renderer.gameObject.AddComponent(type);
                }
            }
        }


        private static GUIContent GetEffectLabel(Type type)
        {
            if (type == null) return s_ContentNone;
            if (s_EffectLabels.TryGetValue(type, out var label)) return label;
            return s_EffectLabels[type] = new GUIContent(type.Name.Replace("CompositeCanvas", ""));
        }

        private static void ConvertTo(Behaviour target, Type type)
        {
            if (!target || target.GetType() == type) return;
            if (type == null)
            {
                DestroyImmediate(target);
                return;
            }

            var so = new SerializedObject(target);
            so.Update();

            var oldEnable = target.enabled;
            target.enabled = false;

            // Find MonoScript of the specified component.
            foreach (var script in Resources.FindObjectsOfTypeAll<MonoScript>())
            {
                if (script.GetClass() != type)
                {
                    continue;
                }

                // Set 'm_Script' to convert.
                so.FindProperty("m_Script").objectReferenceValue = script;
                so.ApplyModifiedProperties();
                break;
            }

            if (so.targetObject is MonoBehaviour mb)
            {
                mb.enabled = oldEnable;
            }
        }

        public override bool HasPreviewGUI()
        {
            return _current && _current.currentBakeBuffer;
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var tex = _current.currentBakeBuffer;
            var outer = new Rect(0, 0, tex.width, tex.height);
            var uv = new Rect(0, 0, 1, 1);
            var color = Color.white;
            s_DrawSprite.Invoke(null, new object[] { tex, rect, Vector4.zero, outer, outer, uv, color, null });
        }

        public override string GetInfoString()
        {
            var tex = _current.currentBakeBuffer;
            return $"Texture Size: {tex.width}x{tex.height}";
        }
    }
}
