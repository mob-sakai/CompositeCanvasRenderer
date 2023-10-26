#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class GameObjectNaming
{
    [MenuItem("Development/Naming")]
    private static void DoGameObjectNaming()
    {
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            var selectable = go.GetComponent<Selectable>();
            var mask = go.GetComponent<Mask>();
            if (selectable)
            {
                go.name = selectable.GetType().Name;
                var text = selectable.GetComponentInChildren<Text>(true);
                if (text)
                {
                    go.name += $" - {text.text}";
                }
            }
            else if (mask)
            {
                go.name = mask.GetType().Name;
            }
            else if (go.name.StartsWith("Controls"))
            {
                go.name = "Controls";
                var text = go.transform.Find("Label")?.GetComponent<Text>();
                if (text)
                {
                    go.name += $" - {text.text}";
                }

                foreach (var childText in go.GetComponentsInChildren<Text>(true))
                {
                    childText.fontSize = 20;
                    var outline = childText.GetComponent<Outline>() ?? childText.gameObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(1, -1);
                    outline.effectColor = new Color(0, 0, 0, 0.5f);
                }
            }
            else if (go.name.StartsWith("Label"))
            {
                go.name = "Label";
            }
            else if (go.name == "TITLE" && go.transform.parent.name != "Content")
            {
                var text = go.GetComponent<Text>();
                if (text)
                {
                    go.transform.parent.name = text.text;
                }
            }

            if (go.name == "TITLE" || go.name == "SUBTITLE")
            {
                var text = go.GetComponent<Text>();
                if (text)
                {
                    text.fontSize = 26;
                    var rt = text.transform as RectTransform;
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, 30);
                }
            }
        }
    }
}
#endif
