using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Coffee.CompositeCanvasRendererInternal;
#if TMP_ENABLE
using TMPro;
#endif

namespace CompositeCanvas
{
    public class CompositeCanvasProcess
    {
        public static CompositeCanvasProcess instance { get; set; } = new CompositeCanvasProcess();
        protected CompositeCanvasSource source { get; set; }
        protected Color color { get; set; }
        protected Material material { get; set; }
        protected Texture texture { get; set; }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void InitializeOnLoad()
        {
#if TMP_ENABLE
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(obj =>
            {
                if (!(obj is TextMeshProUGUI textMeshProUGUI)) return;
                if (!textMeshProUGUI.TryGetComponent<CompositeCanvasSource>(out var source)) return;
                if (!source || !source.isActiveAndEnabled) return;
                if (!source.renderer || !source.renderer.isActiveAndEnabled) return;

                textMeshProUGUI.mesh.CopyTo(source.mesh);

                var subMeshes = InternalListPool<TMP_SubMeshUI>.Rent();
                textMeshProUGUI.GetComponentsInChildren(subMeshes);
                foreach (var subMesh in subMeshes)
                {
                    if (!subMesh.TryGetComponent<CompositeCanvasSource>(out var subSource)) continue;
                    subMesh.mesh.CopyTo(subSource.mesh);
                }

                InternalListPool<TMP_SubMeshUI>.Return(ref subMeshes);
            });
#endif
        }

        protected virtual Texture GetMainTexture(Graphic graphic)
        {
#if TMP_ENABLE
            if (graphic is TextMeshProUGUI || graphic is TMP_SubMeshUI)
            {
                return material.mainTexture;
            }
#endif
            return graphic.mainTexture;
        }

        protected virtual void PreBake()
        {
#if TMP_ENABLE
            Mesh graphicMesh = null;
            if (source.graphic is TextMeshProUGUI textMeshProUGUI)
            {
                graphicMesh = textMeshProUGUI.mesh;
            }
            else if (source.graphic is TMP_SubMeshUI subMeshUI)
            {
                graphicMesh = subMeshUI.mesh;
            }

            if (graphicMesh && source.mesh && graphicMesh.vertexCount == source.mesh.vertexCount)
            {
                var colors = InternalListPool<Color32>.Rent();
                graphicMesh.GetColors(colors);
                for (var i = 0; i < colors.Count; i++)
                {
                    var c = colors[i];
                    if (source.graphic.canvas.ShouldGammaToLinearInMesh())
                    {
                        c.r = c.r.GammaToLinear();
                        c.g = c.g.GammaToLinear();
                        c.b = c.b.GammaToLinear();
                    }

                    c.r = (byte)(c.r * color.r);
                    c.g = (byte)(c.g * color.g);
                    c.b = (byte)(c.b * color.b);
                    c.a = (byte)(c.a * color.a);
                    colors[i] = c;
                }

                source.mesh.SetColors(colors);
                InternalListPool<Color32>.Return(ref colors);

#if UNITY_2023_2_OR_NEWER
                if (source.graphic.canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    var uv1s = InternalListPool<Vector2>.Rent();
                    graphicMesh.GetUVs(0, uv1s);
                    source.mesh.SetUVs(0, uv1s);
                    InternalListPool<Vector2>.Return(ref uv1s);
                }
#endif

                if (!source.renderer.perspectiveBaking)
                {
                    var xScale = 1f / source.graphic.canvas.rootCanvas.transform.lossyScale.x;
                    var uv2s = InternalListPool<Vector2>.Rent();
                    graphicMesh.GetUVs(1, uv2s);
                    for (var i = 0; i < uv2s.Count; i++)
                    {
                        var uv2 = uv2s[i];
                        uv2.y *= xScale;
                        uv2s[i] = uv2;
                    }

                    source.mesh.SetUVs(1, uv2s);
                    InternalListPool<Vector2>.Return(ref uv2s);
                }

                return;
            }
#endif

            source.mpb.SetColor(ShaderPropertyIds.color, color);
        }

        internal void Bake(CompositeCanvasRenderer renderer, CompositeCanvasSource source, bool usePopMaterial)
        {
            try
            {
                this.source = source;

                material = source.GetBakingMaterial(usePopMaterial);
                var cr = source.graphic.canvasRenderer;
                if (!material || !cr) return;

                var alpha = renderer.GetParentGroupAlpha();
                var alphaScale = Mathf.Approximately(alpha, 0) ? 0 : 1f / alpha;
                var crColor = cr.GetColor();
                crColor.a *= cr.GetInheritedAlpha() * alphaScale;
                color = crColor;

                var matrix = renderer.perspectiveBaking
                    ? source.transform.localToWorldMatrix
                    : renderer.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;

                texture = GetMainTexture(source.graphic);
                if (texture)
                {
                    source.mpb.SetTexture(ShaderPropertyIds.mainTex, texture);

                    if (GraphicsFormatUtility.IsAlphaOnlyFormat(texture.graphicsFormat))
                    {
                        // Use _TextureSampleAdd for alpha only texture
                        source.mpb.SetVector(ShaderPropertyIds.textureSampleAdd, new Vector4(1, 1, 1, 0));
                    }
                }

                if (renderer.canvas.ShouldGammaToLinearInShader())
                {
                    source.mpb.SetInt(ShaderPropertyIds.gammaToLinear, 1);
                }

                PreBake();

                renderer.cb.DrawMesh(source.mesh, matrix, material, 0, 0, source.mpb);
                Logging.Log(this,
                    $"<color=orange> >>>> '{source.name}' will render to bake-buffer '{renderer.name}'.</color>");
            }
            catch (Exception e)
            {
                Logging.LogError(this, e);
            }
            finally
            {
                material = default;
                this.source = default;
                texture = default;
            }
        }
    }
}
