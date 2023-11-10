using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if TMP_ENABLE
using TMPro;
#endif

namespace CompositeCanvas
{
    public class CompositeCanvasProcess
    {
        public static CompositeCanvasProcess instance { get; private set; } = new CompositeCanvasProcess();

#if TMP_ENABLE
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void InitializeOnLoad()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(obj =>
            {
                if (!(obj is TextMeshProUGUI textMeshProUGUI)) return;
                if (!textMeshProUGUI.TryGetComponent<CompositeCanvasSource>(out var source)) return;
                if (!source || !source.isActiveAndEnabled) return;
                if (!source._renderer || !source._renderer.isActiveAndEnabled) return;

                textMeshProUGUI.mesh.CopyTo(ref source._mesh);

                var subMeshes = ListPool<TMP_SubMeshUI>.Rent();
                textMeshProUGUI.GetComponentsInChildren(subMeshes);
                foreach (var subMesh in subMeshes)
                {
                    if (!subMesh.TryGetComponent<CompositeCanvasSource>(out var subSource)) continue;
                    subMesh.mesh.CopyTo(ref subSource._mesh);
                }

                ListPool<TMP_SubMeshUI>.Return(ref subMeshes);
            });
        }
#endif

        public virtual Texture GetMainTexture(Graphic graphic)
        {
#if TMP_ENABLE
            if (graphic is TextMeshProUGUI textMeshProUGUI)
            {
                var mat = textMeshProUGUI.fontSharedMaterial;
                return mat ? mat.mainTexture : null;
            }

            if (graphic is TMP_SubMeshUI subMeshUI)
            {
                var mat = subMeshUI.sharedMaterial;
                return mat ? mat.mainTexture : null;
            }
#endif
            return graphic.mainTexture;
        }

        public virtual bool IsModifyMeshSupported(Graphic graphic)
        {
#if TMP_ENABLE
            if (graphic is TextMeshProUGUI || graphic is TMP_SubMeshUI) return false;
#endif
            return true;
        }

        public virtual bool OnPreBake(
            CompositeCanvasRenderer renderer,
            Graphic graphic,
            ref Mesh mesh,
            MaterialPropertyBlock mpb,
            float alphaScale)
        {
            var crColor = graphic.canvasRenderer.GetColor();
            crColor.a *= graphic.canvasRenderer.GetInheritedAlpha() * alphaScale;

#if TMP_ENABLE
            Mesh graphicMesh = null;
            if (graphic is TextMeshProUGUI textMeshProUGUI)
            {
                graphicMesh = textMeshProUGUI.mesh;
            }
            else if (graphic is TMP_SubMeshUI subMeshUI)
            {
                graphicMesh = subMeshUI.mesh;
            }

            if (graphicMesh && mesh && graphicMesh.vertexCount == mesh.vertexCount)
            {
                var colors = ListPool<Color32>.Rent();
                graphicMesh.GetColors(colors);
                for (var i = 0; i < colors.Count; i++)
                {
                    var c = colors[i];
                    c.r = (byte)(c.r * crColor.r);
                    c.g = (byte)(c.g * crColor.g);
                    c.b = (byte)(c.b * crColor.b);
                    c.a = (byte)(c.a * crColor.a);
                    colors[i] = c;
                }

                mesh.SetColors(colors);
                ListPool<Color32>.Return(ref colors);

                if (renderer.isRelativeSpace)
                {
                    var xScale = 1f / graphic.canvas.rootCanvas.transform.lossyScale.x;
                    var uv2s = ListPool<Vector2>.Rent();
                    graphicMesh.GetUVs(1, uv2s);
                    for (var i = 0; i < uv2s.Count; i++)
                    {
                        var uv2 = uv2s[i];
                        uv2.y *= xScale;
                        uv2s[i] = uv2;
                    }

                    mesh.SetUVs(1, uv2s);
                    ListPool<Vector2>.Return(ref uv2s);
                }

                return true;
            }
#endif

            mpb.SetColor(ShaderPropertyIds.color, crColor);
            return true;
        }
    }
}
