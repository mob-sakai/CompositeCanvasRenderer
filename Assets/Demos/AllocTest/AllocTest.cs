using System.Collections;
using CompositeCanvas;
using UnityEngine;

public class AllocTest : MonoBehaviour
{
    [SerializeField] private GameObject m_Target;
    [SerializeField] private bool m_SwitchActivation;
    [SerializeField] private bool m_SetDirty;
    private CompositeCanvasRenderer[] _renderers;

    public bool switchActivation
    {
        get => m_SwitchActivation;
        set => m_SwitchActivation = value;
    }

    public bool setDirty
    {
        get => m_SetDirty;
        set => m_SetDirty = value;
    }

    private void Start()
    {
        _renderers = m_Target.GetComponentsInChildren<CompositeCanvasRenderer>();
    }

    private void Update()
    {
        if (m_SwitchActivation)
        {
            m_Target.SetActive(!m_Target.activeSelf);
        }
        else if (m_SetDirty)
        {
            if (!m_Target.activeSelf)
            {
                m_Target.SetActive(true);
            }

            foreach (var r in _renderers)
            {
                r.SetDirty();
            }
        }
    }

    public void Clone()
    {
        StartCoroutine(Co_Clone());
    }

    private IEnumerator Co_Clone()
    {
        yield return new WaitForSeconds(1);

        var clone = Instantiate(m_Target, m_Target.transform.parent);
        clone.SetActive(true);

        yield return new WaitForSeconds(1);
        Destroy(clone);
    }
}
