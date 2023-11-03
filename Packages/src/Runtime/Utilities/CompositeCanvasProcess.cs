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

        public virtual bool OnPreBake(Graphic graphic, ref Mesh mesh, MaterialPropertyBlock mpb)
        {
            var crColor = graphic.canvasRenderer.GetColor();
            crColor.a *= graphic.canvasRenderer.GetInheritedAlpha();

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

            if (graphicMesh)
            {
                graphicMesh.CopyTo(ref mesh);

                var colors = ListPool<Color32>.Rent();
                mesh.GetColors(colors);
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
                return true;
            }
#endif

            mpb.SetColor(ShaderPropertyIds.color, crColor);
            return true;
        }
    }
}
