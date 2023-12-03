using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace Coffee.NanoMonitor
{
#if UNITY_EDITOR
    [CustomEditor(typeof(MonitorUI))]
    public class MonitorTextEditor : Editor
    {
        private SerializedProperty _color;
        private SerializedProperty _font;
        private SerializedProperty _fontSize;
        private SerializedProperty _mode;
        private SerializedProperty _text;
        private SerializedProperty _textAnchor;

        private void OnEnable()
        {
            _mode = serializedObject.FindProperty("m_Mode");
            _text = serializedObject.FindProperty("m_Text");
            _color = serializedObject.FindProperty("m_Color");
            var fontData = serializedObject.FindProperty("m_FontData");
            _fontSize = fontData.FindPropertyRelative("m_FontSize");
            _font = fontData.FindPropertyRelative("m_Font");
            _textAnchor = serializedObject.FindProperty("m_TextAnchor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_mode);
            if ((MonitorUI.Mode)_mode.intValue == MonitorUI.Mode.Text)
            {
                EditorGUILayout.PropertyField(_text);
                EditorGUILayout.PropertyField(_fontSize);
                EditorGUILayout.PropertyField(_textAnchor);
                EditorGUILayout.PropertyField(_font);
            }

            EditorGUILayout.PropertyField(_color);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    public class MonitorUI : Text
    {
        public enum Mode
        {
            Text,
            Fill
        }

        public enum TextAnchor
        {
            Left,
            Center,
            Right
        }


        //################################
        // Serialize Members.
        //################################
        [SerializeField] private Mode m_Mode = Mode.Text;
        [SerializeField] private TextAnchor m_TextAnchor;


        //################################
        // Private Members.
        //################################
        private readonly StringBuilder _sb = new StringBuilder(64);
        private UnityAction _checkTextLengthChanged;

        private int _textLength;
        private UnityAction _updateFont;

        //################################
        // Public Members.
        //################################
        public override string text
        {
            get => _sb.ToString();
            set
            {
                m_Text = value;
                if (_sb.IsEqual(m_Text)) return;

                _sb.Length = 0;
                _sb.Append(m_Text);
                SetVerticesDirty();
            }
        }

        public TextAnchor textAnchor
        {
            get => m_TextAnchor;
            set
            {
                if (m_TextAnchor == value) return;

                m_TextAnchor = value;
                SetVerticesDirty();
            }
        }

        public override bool raycastTarget
        {
            get => m_Mode == Mode.Fill;
            set { }
        }

        public override float preferredWidth
        {
            get
            {
                var fontData = FixedFont.GetOrCreate(font);
                if (fontData == null) return 0;

                var scale = (float)fontSize / fontData.fontSize;
                var offset = 0f;
                for (var i = 0; i < _sb.Length; i++)
                {
                    offset = fontData.Layout(_sb[i], offset, scale);
                }

                return offset;
            }
        }


        //################################
        // Unity Callbacks.
        //################################
        protected override void OnEnable()
        {
            Profiler.BeginSample("(NM)[MonitorUI] OnEnable");

            _updateFont = _updateFont ?? UpdateFont;
            _checkTextLengthChanged = _checkTextLengthChanged ?? CheckTextLengthChanged;

            RegisterDirtyMaterialCallback(_updateFont);
            RegisterDirtyVerticesCallback(_checkTextLengthChanged);

            base.OnEnable();
            raycastTarget = false;
            maskable = false;
            _sb.Length = 0;
            _sb.Append(m_Text);

            Profiler.EndSample();
        }

        protected override void OnDisable()
        {
            Profiler.BeginSample("(NM)[MonitorUI] OnDisable");

            UnregisterDirtyMaterialCallback(_updateFont);
            UnregisterDirtyVerticesCallback(_checkTextLengthChanged);

            base.OnDisable();

            Profiler.EndSample();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!_sb.IsEqual(m_Text))
            {
                _sb.Length = 0;
                _sb.Append(m_Text);
                SetVerticesDirty();
            }
        }
#endif

        private void CheckTextLengthChanged()
        {
            Profiler.BeginSample("(NM)[MonitorUI] CheckTextLengthChanged");

            if (_textLength != _sb.Length)
            {
                _textLength = _sb.Length;
                SetLayoutDirty();
            }

            Profiler.EndSample();
        }

        public void SetText(string format, double arg0 = 0, double arg1 = 0, double arg2 = 0, double arg3 = 0)
        {
            Profiler.BeginSample("(NM)[MonitorUI] SetText");

            _sb.Length = 0;
            _sb.AppendFormatNoAlloc(format, arg0, arg1, arg2, arg3);
            SetVerticesDirty();

            Profiler.EndSample();
        }

        public void SetText(StringBuilder builder)
        {
            Profiler.BeginSample("(NM)[MonitorUI] SetText");

            _sb.Length = 0;
            _sb.Append(builder);
            SetVerticesDirty();

            Profiler.EndSample();
        }

        private void UpdateFont()
        {
            Profiler.BeginSample("(NM)[MonitorUI] UpdateFont");

            var fontData = FixedFont.GetOrCreate(font);
            if (fontData != null)
            {
                fontData.Invalidate();
                fontData.UpdateFont();
            }

            Profiler.EndSample();
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            Profiler.BeginSample("(NM)[MonitorUI] OnPopulateMesh");

            toFill.Clear();

            var fontData = FixedFont.GetOrCreate(font);
            if (fontData == null)
            {
                Profiler.EndSample();

                return;
            }

            fontData.UpdateFont();

            if (m_Mode == Mode.Fill)
            {
                fontData.Fill(toFill, color, rectTransform);
                Profiler.EndSample();

                return;
            }

            var scale = (float)fontSize / fontData.fontSize;
            float offset = 0;
            switch (textAnchor)
            {
                case TextAnchor.Left:
                    offset = rectTransform.rect.xMin;
                    break;
                case TextAnchor.Center:
                    for (var i = 0; i < _sb.Length; i++)
                    {
                        offset = fontData.Layout(_sb[i], offset, scale);
                    }

                    offset = -offset / 2;
                    break;
                case TextAnchor.Right:
                    for (var i = 0; i < _sb.Length; i++)
                    {
                        offset = fontData.Layout(_sb[i], offset, scale);
                    }

                    offset = rectTransform.rect.xMax - offset;
                    break;
            }

            for (var i = 0; i < _sb.Length; i++)
            {
                offset = fontData.Append(toFill, _sb[i], offset, scale, color);
            }

            Profiler.EndSample();
        }

        protected override void UpdateMaterial()
        {
            Profiler.BeginSample("(NM)[MonitorUI] UpdateMaterial");

            base.UpdateMaterial();

            var fontData = FixedFont.GetOrCreate(font);
            if (fontData != null)
            {
                fontData.UpdateFont();
            }

            Profiler.EndSample();
        }
    }
}
