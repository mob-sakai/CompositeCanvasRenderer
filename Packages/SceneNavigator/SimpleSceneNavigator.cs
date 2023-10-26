using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Coffee.SimpleSceneNavigator
{
    public class SimpleSceneNavigator : MonoBehaviour
    {
        [SerializeField]
        private Button m_PrevButton;

        [SerializeField]
        private Button m_NextButton;

        private void Awake()
        {
            var canMove = 1 < SceneManager.sceneCountInBuildSettings;
            if (m_PrevButton)
            {
                m_PrevButton.gameObject.SetActive(canMove);
                m_PrevButton.onClick.AddListener(() => MoveScene(-1));
            }

            if (m_NextButton)
            {
                m_NextButton.gameObject.SetActive(canMove);
                m_NextButton.onClick.AddListener(() => MoveScene(+1));
            }
        }

        private static void MoveScene(int add)
        {
            var count = SceneManager.sceneCountInBuildSettings;
            if (count <= 1) return;

            var current = SceneManager.GetActiveScene().buildIndex;
            var next = (current + add + count) % count;
            SceneManager.LoadScene(next);
        }
    }
}
