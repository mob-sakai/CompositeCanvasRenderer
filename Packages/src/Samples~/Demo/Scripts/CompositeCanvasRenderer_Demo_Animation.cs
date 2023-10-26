using System;
using System.Collections;
using UnityEngine;

namespace CompositeCanvas.Demos
{
    public class CompositeCanvasRenderer_Demo_Animation : MonoBehaviour
    {
        [SerializeField] private AnimationType m_AnimationType;

        public float timeScale { get; set; } = 1;

        private void OnEnable()
        {
            switch (m_AnimationType)
            {
                case AnimationType.Rotation:
                    StartCoroutine(Co_Rotate(transform));
                    break;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public void ResetRotation()
        {
            transform.localRotation = Quaternion.identity;
        }

        public void ResetRotation30()
        {
            transform.localRotation = Quaternion.Euler(new Vector3(0, 30, 0));
        }

        private IEnumerator Co_Rotate(Transform t)
        {
            while (true)
            {
                yield return Co_Tween(0, 360, 4, v => t.localRotation = Quaternion.Euler(0, v, 0));
            }
        }

        private IEnumerator Co_Tween(float from, float to, float duration, Action<float> callback)
        {
            var value = from;
            var diff = (to - from) / duration;

            callback(value);

            while (0 < diff ? value < to : to < value)
            {
                yield return null;

                value += diff * Time.deltaTime * timeScale;
                callback(0 < diff ? Mathf.Clamp(value, from, to) : Mathf.Clamp(value, to, from));
            }
        }

        private enum AnimationType
        {
            Rotation
        }
    }
}
