using System.Collections.Generic;
using UnityEngine;

namespace CompositeCanvas.Demos
{
    public class CompositeCanvasRenderer_Demo_SharingGroup : MonoBehaviour
    {
        [SerializeField] private int m_Count;
        [SerializeField] private GameObject m_Origin;
        private readonly List<CompositeCanvasRenderer> _renderers = new List<CompositeCanvasRenderer>();

        private void Start()
        {
            _renderers.Add(m_Origin.GetComponentInChildren<CompositeCanvasRenderer>());

            for (var i = 0; i < m_Count; i++)
            {
                var go = Instantiate(m_Origin, transform, false);
                _renderers.Add(go.GetComponentInChildren<CompositeCanvasRenderer>());
            }
        }

        public void EnableSharingGroup(bool flag)
        {
            _renderers.ForEach(r => r.sharingGroupId = flag ? 10 : 0);
        }
    }
}
