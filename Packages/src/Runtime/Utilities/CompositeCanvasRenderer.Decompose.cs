using UnityEngine;
using UnityEngine.UI;
#if TMP_ENABLE
using TMPro;
#endif

namespace CompositeCanvas
{
    public partial class CompositeCanvasRenderer
    {
        public delegate (Mesh, Texture, Material, int) GraphicDecomposeDelegate(Graphic graphic);

        /// <summary>
        /// Custom delegate to get the mesh, texture, and material for rendering from the graphic.
        /// </summary>
        public static GraphicDecomposeDelegate graphicDecomposeDelegate { private get; set; }

        private static (Mesh, Texture, Material, int) DefaultGraphicDecompose(Graphic graphic)
        {
#if TMP_ENABLE
            if (graphic is TextMeshProUGUI textMeshProUGUI)
            {
                var mat = textMeshProUGUI.fontSharedMaterial;
                return (textMeshProUGUI.mesh, mat.mainTexture, mat, ShaderPropertyIds.faceColor);
            }

            if (graphic is TMP_SubMeshUI subMeshUI)
            {
                var mat = subMeshUI.sharedMaterial;
                return (subMeshUI.mesh, mat.mainTexture, mat, ShaderPropertyIds.faceColor);
            }
#endif

            return (null, graphic.mainTexture, null, ShaderPropertyIds.color);
        }

        /// <summary>
        /// Gets the mesh, texture, and material for rendering from the graphic.
        /// </summary>
        internal static (Mesh graphicMesh, Texture graphicTex, Material graphicMat, int colorId) Decompose(Graphic graphic)
        {
            var (mesh, tex, mat, id) = graphicDecomposeDelegate?.Invoke(graphic) ?? (null, null, null, 0);
            return mesh || tex || mat
                ? (mesh, tex, mat, id)
                : DefaultGraphicDecompose(graphic);
        }
    }
}
