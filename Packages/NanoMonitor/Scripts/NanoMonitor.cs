using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Coffee.NanoMonitor
{
#if UNITY_EDITOR
    [CustomEditor(typeof(NanoMonitor))]
    internal class NanoMonitorEditor : Editor
    {
        private ReorderableList _monitorItemList;

        private void OnEnable()
        {
            var items = serializedObject.FindProperty("m_CustomMonitorItems");
            _monitorItemList = CustomMonitorItemDrawer.CreateReorderableList(items);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _monitorItemList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif


    [DisallowMultipleComponent]
    public sealed class NanoMonitor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        private bool m_Opened;

        [SerializeField]
        [Range(0.01f, 2f)]
        private float m_Interval = 1f;

        [SerializeField]
        private Font m_Font;

        [SerializeField]
        private Image.OriginVertical m_Anchor;

        [Header("Controls")]
        [SerializeField]
        private GameObject m_FoldoutObject;

        [SerializeField]
        private Button m_OpenButton;

        [SerializeField]
        private Button m_CloseButton;

        [Header("View")]
        [SerializeField]
        private MonitorUI m_Time;

        [SerializeField]
        private MonitorUI m_Fps;

        [SerializeField]
        private MonitorUI m_Gc;

        [SerializeField]
        private MonitorUI m_MonoUsage;

        [SerializeField]
        private MonitorUI m_UnityUsage;

        [Header("Custom")]
        [SerializeField]
        private MonitorUI m_CustomUITemplate;

        [HideInInspector]
        [SerializeField]
        private CustomMonitorItem[] m_CustomMonitorItems = new CustomMonitorItem[0];

        private double _elapsed;
        private double _fpsElapsed;
        private int _frames;

        private void Start()
        {
            Profiler.BeginSample("(NM)[NanoMonitor] Start");

            var top = m_Anchor == Image.OriginVertical.Top;
            if (m_FoldoutObject && m_FoldoutObject.transform is RectTransform rtFoldout)
            {
                rtFoldout.anchorMin = top ? new Vector2(0, 1) : new Vector2(0, 0);
                rtFoldout.anchorMax = top ? new Vector2(1, 1) : new Vector2(1, 0);
                rtFoldout.pivot = top ? new Vector2(0.5f, 1) : new Vector2(0.5f, 0);
            }

            if (m_OpenButton && m_OpenButton.transform is RectTransform rtButton)
            {
                rtButton.anchorMin = rtButton.anchorMax = rtButton.pivot = top ? Vector2.up : Vector2.zero;
            }

            if (m_CustomUITemplate)
            {
                m_CustomUITemplate.gameObject.SetActive(false);

                var parent = m_CustomUITemplate.transform.parent;
                foreach (var item in m_CustomMonitorItems)
                {
                    item.ui = Instantiate(m_CustomUITemplate, parent);
                    item.ui.name = "CustomMonitorUI";
                    item.ui.gameObject.SetActive(true);
                }
            }

            Profiler.EndSample();
        }

        private void Update()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            _fpsElapsed += Time.unscaledDeltaTime;
            if (_elapsed < m_Interval) return;

            Profiler.BeginSample("(NM)[NanoMonitor] Update");

            if (m_Time)
            {
                m_Time.SetText("Time:{0,3}", (int)Time.realtimeSinceStartup);
            }

            if (m_Fps)
            {
                m_Fps.SetText("FPS:{0,3}", (int)(_frames / _fpsElapsed));
            }

            if (m_Gc)
            {
                m_Gc.SetText("GC:{0,3}", GC.CollectionCount(0));
            }

            if (m_MonoUsage)
            {
                var monoUsed = (Profiler.GetMonoUsedSizeLong() >> 10) / 1024f;
                var monoTotal = (Profiler.GetMonoHeapSizeLong() >> 10) / 1024f;
                m_MonoUsage.SetText("Mono:{0,7:N3}/{1,7:N3}MB", monoUsed, monoTotal);
            }

            if (m_UnityUsage)
            {
                var unityUsed = (Profiler.GetTotalAllocatedMemoryLong() >> 10) / 1024f;
                var unityTotal = (Profiler.GetTotalReservedMemoryLong() >> 10) / 1024f;
                m_UnityUsage.SetText("Unity:{0,7:N3}/{1,7:N3}MB", unityUsed, unityTotal);
            }

            foreach (var item in m_CustomMonitorItems)
            {
                item.UpdateText();
            }

            _frames = 0;
            _elapsed %= m_Interval;
            _fpsElapsed = 0;
            Profiler.EndSample();
        }

        private void OnEnable()
        {
            Profiler.BeginSample("(NM)[NanoMonitor] OnEnable");

            if (m_OpenButton)
            {
                m_OpenButton.onClick.AddListener(Open);
            }

            if (m_CloseButton)
            {
                m_CloseButton.onClick.AddListener(Close);
            }

            if (m_Opened)
            {
                Open();
            }
            else
            {
                Close();
            }

            Profiler.EndSample();
        }

        private void OnDisable()
        {
            Profiler.BeginSample("(NM)[NanoMonitor] OnDisable");

            if (m_OpenButton)
            {
                m_OpenButton.onClick.RemoveListener(Open);
            }

            if (m_CloseButton)
            {
                m_CloseButton.onClick.RemoveListener(Close);
            }

            Profiler.EndSample();
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Font)
            {
                foreach (var ui in GetComponentsInChildren<MonitorUI>(true))
                {
                    ui.font = m_Font;
                }
            }

            if (m_Opened)
            {
                Open();
            }
            else
            {
                Close();
            }
        }
#endif

        private void Open()
        {
            Profiler.BeginSample("(NM)[NanoMonitor] Open");

            _frames = 0;
            _elapsed = m_Interval;
            _fpsElapsed = 0;

            if (m_FoldoutObject)
            {
                m_FoldoutObject.SetActive(true);
            }

            if (m_CloseButton)
            {
                m_CloseButton.gameObject.SetActive(true);
            }

            if (m_OpenButton)
            {
                m_OpenButton.gameObject.SetActive(false);
            }

            Profiler.EndSample();
        }

        private void Close()
        {
            Profiler.BeginSample("(NM)[NanoMonitor] Close");

            if (m_FoldoutObject)
            {
                m_FoldoutObject.SetActive(false);
            }

            if (m_CloseButton)
            {
                m_CloseButton.gameObject.SetActive(false);
            }

            if (m_OpenButton)
            {
                m_OpenButton.gameObject.SetActive(true);
            }

            Profiler.EndSample();
        }

        public void SetUp(Image.OriginVertical anchor, bool opened, float interval, CustomMonitorItem[] customs)
        {
            m_Anchor = anchor;
            m_Opened = opened;
            m_Interval = interval;
            m_CustomMonitorItems = customs;
        }
    }
}
