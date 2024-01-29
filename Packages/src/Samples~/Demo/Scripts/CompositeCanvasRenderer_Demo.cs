using System;
using CompositeCanvas.Effects;
using CompositeCanvas.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace CompositeCanvas.Demos
{
    public class CompositeCanvasRenderer_Demo : MonoBehaviour
    {
        [SerializeField] private CompositeCanvasRenderer m_CompositeCanvasRenderer;
        [SerializeField] private Shadow[] m_Shadows;

        public void EnableCameraPan(bool flag)
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var angles = canvas.worldCamera.transform.rotation.eulerAngles;
            angles.y = flag ? -5 : 0;
            canvas.worldCamera.transform.rotation = Quaternion.Euler(angles);
        }

        public void EnableCameraTilt(bool flag)
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var angles = canvas.worldCamera.transform.rotation.eulerAngles;
            angles.x = flag ? 10 : 0;
            canvas.worldCamera.transform.rotation = Quaternion.Euler(angles);
        }

        public void EnableCameraRoll(bool flag)
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var angles = canvas.worldCamera.transform.rotation.eulerAngles;
            angles.z = flag ? 10 : 0;
            canvas.worldCamera.transform.rotation = Quaternion.Euler(angles);
        }

        public void EnableOrthographic(bool flag)
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            canvas.worldCamera.orthographic = flag;
        }

        public void SetDownSamplingRate(int value)
        {
            m_CompositeCanvasRenderer.downSamplingRate = (DownSamplingRate)value;
        }

        public void SetTransformSensitivity(int index)
        {
            var values = (TransformSensitivity[])Enum.GetValues(typeof(TransformSensitivity));
            CompositeCanvasRendererProjectSettings.transformSensitivity = values[index];
        }

        public void EnableMaskable(bool flag)
        {
            m_CompositeCanvasRenderer.maskable = flag;
            m_CompositeCanvasRenderer.RecalculateClipping();
        }

        public void SetIteration(float iteration)
        {
            var value = Mathf.RoundToInt(iteration);
            var blur = m_CompositeCanvasRenderer.GetComponent<CompositeCanvasBlur>();
            blur.iteration = value;
        }

        public void SetEffectDistance(float distance)
        {
            var value = new Vector2(distance, -distance);
            var blur = m_CompositeCanvasRenderer.GetComponent<CompositeCanvasShadow>();
            blur.effectDistance = value;

            foreach (var shadow in m_Shadows)
            {
                shadow.effectDistance = value;
            }
        }


        public void SetMirrorAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            var mirror = m_CompositeCanvasRenderer.GetComponent<CompositeCanvasMirror>();
            var gradient = mirror.gradient;
            mirror.gradient = new Gradient
            {
                alphaKeys = new[]
                {
                    new GradientAlphaKey(alpha, 0),
                    gradient.alphaKeys[1]
                },
                colorKeys = gradient.colorKeys
            };
        }

        public void SetMirrorHeight(float height)
        {
            height = Mathf.Clamp01(Mathf.Max(0.01f, height));
            var mirror = m_CompositeCanvasRenderer.GetComponent<CompositeCanvasMirror>();
            var gradient = mirror.gradient;
            mirror.gradient = new Gradient
            {
                alphaKeys = new[]
                {
                    gradient.alphaKeys[0],
                    new GradientAlphaKey(0, height)
                },
                colorKeys = gradient.colorKeys
            };
        }

        public void SetR(float value)
        {
            var color = m_CompositeCanvasRenderer.color;
            color.r = value;
            m_CompositeCanvasRenderer.color = color;

            foreach (var shadow in m_Shadows)
            {
                shadow.effectColor = color;
            }
        }

        public void SetG(float value)
        {
            var color = m_CompositeCanvasRenderer.color;
            color.g = value;
            m_CompositeCanvasRenderer.color = color;

            foreach (var shadow in m_Shadows)
            {
                shadow.effectColor = color;
            }
        }

        public void SetB(float value)
        {
            var color = m_CompositeCanvasRenderer.color;
            color.b = value;
            m_CompositeCanvasRenderer.color = color;

            foreach (var shadow in m_Shadows)
            {
                shadow.effectColor = color;
            }
        }

        public void SetA(float value)
        {
            var color = m_CompositeCanvasRenderer.color;
            color.a = value;
            m_CompositeCanvasRenderer.color = color;

            foreach (var shadow in m_Shadows)
            {
                shadow.effectColor = color;
            }
        }

        public void SetCanvasRenderMode(int mode)
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (canvas.renderMode == (RenderMode)mode)
            {
                return;
            }

            if (mode == (int)RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.transform.rotation = Quaternion.Euler(new Vector3(0, 6, 0));
            }
            else
            {
                canvas.renderMode = (RenderMode)mode;
            }
        }
    }
}
