using System;
using System.Linq;
using CompositeCanvas.Effects;
using CompositeCanvas.Enums;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CompositeCanvas.Demos
{
    [RequireComponent(typeof(Dropdown))]
    public class CompositeCanvasRenderer_Demo_Enum : MonoBehaviour
    {
        [SerializeField] private CompositeCanvasRenderer m_CompositeCanvasRenderer;
        [SerializeField] private EnumType m_EnumType;
        [SerializeField] private Dropdown m_SrcBlendDropdown;
        [SerializeField] private Dropdown m_DstBlendDropdown;
        private Dropdown _dropdown;

        private void OnEnable()
        {
            _dropdown = GetComponent<Dropdown>();
            _dropdown.ClearOptions();
            _dropdown.onValueChanged.AddListener(OnValueChanged);

            switch (m_EnumType)
            {
                case EnumType.ColorMode:
                    _dropdown.AddOptions(Enum.GetNames(typeof(ColorMode)).ToList());
                    _dropdown.SetValueWithoutNotify((int)m_CompositeCanvasRenderer.colorMode);
                    break;
                case EnumType.SrcBlendMode:
                    _dropdown.AddOptions(Enum.GetNames(typeof(BlendMode)).ToList());
                    _dropdown.SetValueWithoutNotify((int)m_CompositeCanvasRenderer.srcBlendMode);
                    break;
                case EnumType.DstBlendMode:
                    _dropdown.AddOptions(Enum.GetNames(typeof(BlendMode)).ToList());
                    _dropdown.SetValueWithoutNotify((int)m_CompositeCanvasRenderer.dstBlendMode);
                    break;
                case EnumType.BlendType:
                    _dropdown.AddOptions(Enum.GetNames(typeof(BlendType)).ToList());
                    _dropdown.SetValueWithoutNotify((int)m_CompositeCanvasRenderer.blendType);

                    var isCustom = m_CompositeCanvasRenderer.blendType == BlendType.Custom;
                    m_SrcBlendDropdown.interactable = isCustom;
                    m_DstBlendDropdown.interactable = isCustom;
                    break;
                case EnumType.OutlinePattern:
                    var outline = m_CompositeCanvasRenderer.GetComponent<CompositeCanvasOutline>();
                    _dropdown.AddOptions(Enum.GetNames(typeof(OutlinePattern)).ToList());
                    _dropdown.SetValueWithoutNotify((int)outline.outlinePattern);
                    break;
            }
        }

        private void OnDisable()
        {
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(int value)
        {
            switch (m_EnumType)
            {
                case EnumType.ColorMode:
                    m_CompositeCanvasRenderer.colorMode = (ColorMode)value;
                    break;
                case EnumType.SrcBlendMode:
                    m_CompositeCanvasRenderer.srcBlendMode = (BlendMode)value;
                    break;
                case EnumType.DstBlendMode:
                    m_CompositeCanvasRenderer.dstBlendMode = (BlendMode)value;
                    break;
                case EnumType.BlendType:
                    var type = (BlendType)value;
                    var isCustom = type == BlendType.Custom;
                    m_CompositeCanvasRenderer.blendType = type;
                    m_SrcBlendDropdown.interactable = isCustom;
                    m_DstBlendDropdown.interactable = isCustom;
                    m_SrcBlendDropdown.SetValueWithoutNotify((int)m_CompositeCanvasRenderer.srcBlendMode);
                    m_DstBlendDropdown.SetValueWithoutNotify((int)m_CompositeCanvasRenderer.dstBlendMode);
                    break;
                case EnumType.OutlinePattern:
                    var outline = m_CompositeCanvasRenderer.GetComponent<CompositeCanvasOutline>();
                    outline.outlinePattern = (OutlinePattern)value;
                    break;
            }
        }

        private enum EnumType
        {
            ColorMode,
            SrcBlendMode,
            DstBlendMode,
            BlendType,
            OutlinePattern
        }
    }
}
