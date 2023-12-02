using System.Collections.Generic;
using CompositeCanvas.ProjectSettings;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace CompositeCanvas
{
    internal static class ShaderPropertyIds
    {
        public static readonly int mainTex = Shader.PropertyToID("_MainTex");
        public static readonly int color = Shader.PropertyToID("_Color");
        public static readonly int textureSampleAdd = Shader.PropertyToID("_TextureSampleAdd");
        public static readonly int colorMode = Shader.PropertyToID("Color_Mode");
        public static readonly int srcBlendMode = Shader.PropertyToID("_SrcBlend");
        public static readonly int dstBlendMode = Shader.PropertyToID("_DstBlend");
        public static readonly int innerCutoff = Shader.PropertyToID("_InnerCutoff");
        public static readonly int multiplier = Shader.PropertyToID("_Multiplier");
        public static readonly int limit = Shader.PropertyToID("_Limit");
        public static readonly int power = Shader.PropertyToID("_Power");
        public static readonly int blur = Shader.PropertyToID("_Blur");
        public static readonly int tmpRt = Shader.PropertyToID("CompositeCanvasTmpRt");
        public static readonly uint compositeCanvas = (uint)Shader.PropertyToID("CompositeCanvas");
        public static readonly uint compositeCanvasBlur = (uint)Shader.PropertyToID("CompositeCanvas/Blur");
    }

    internal static class ListExtensions
    {
        public static void RemoveAtFast<T>(this List<T> self, int index)
        {
            if (self == null) return;

            var lastIndex = self.Count - 1;
            self[index] = self[lastIndex];
            self.RemoveAt(lastIndex);
        }
    }

    internal static class MeshExtensions
    {
        internal static readonly ObjectPool<Mesh> s_MeshPool = new ObjectPool<Mesh>(
            () =>
            {
                var mesh = new Mesh
                {
                    hideFlags = HideFlags.DontSave | HideFlags.NotEditable
                };
                mesh.MarkDynamic();
                return mesh;
            },
            mesh => mesh,
            mesh =>
            {
                if (mesh)
                {
                    mesh.Clear();
                }
            });

        public static Mesh Rent()
        {
            return s_MeshPool.Rent();
        }

        public static void Return(ref Mesh mesh)
        {
            s_MeshPool.Return(ref mesh);
        }

        public static void CopyTo(this Mesh self, Mesh dst)
        {
            if (!self || !dst) return;

            var vector3List = ListPool<Vector3>.Rent();
            var vector4List = ListPool<Vector4>.Rent();
            var color32List = ListPool<Color32>.Rent();
            var intList = ListPool<int>.Rent();

            dst.Clear(false);

            self.GetVertices(vector3List);
            dst.SetVertices(vector3List);

            self.GetTriangles(intList, 0);
            dst.SetTriangles(intList, 0);

            self.GetNormals(vector3List);
            dst.SetNormals(vector3List);

            self.GetTangents(vector4List);
            dst.SetTangents(vector4List);

            self.GetColors(color32List);
            dst.SetColors(color32List);

            self.GetUVs(0, vector4List);
            dst.SetUVs(0, vector4List);

            self.GetUVs(1, vector4List);
            dst.SetUVs(1, vector4List);

            self.GetUVs(2, vector4List);
            dst.SetUVs(2, vector4List);

            self.GetUVs(3, vector4List);
            dst.SetUVs(3, vector4List);

            dst.RecalculateBounds();
            ListPool<Vector3>.Return(ref vector3List);
            ListPool<Vector4>.Return(ref vector4List);
            ListPool<Color32>.Return(ref color32List);
            ListPool<int>.Return(ref intList);
        }
    }

    internal static class Vector3Extensions
    {
        public static Vector3 Inverse(this Vector3 self)
        {
            self.x = Mathf.Approximately(self.x, 0) ? 1 : 1 / self.x;
            self.y = Mathf.Approximately(self.y, 0) ? 1 : 1 / self.y;
            self.z = Mathf.Approximately(self.z, 0) ? 1 : 1 / self.z;
            return self;
        }

        public static Vector3 GetScaled(this Vector3 self, Vector3 other1)
        {
            self.Scale(other1);
            return self;
        }

        public static Vector3 GetScaled(this Vector3 self, Vector3 other1, Vector3 other2)
        {
            self.Scale(other1);
            self.Scale(other2);
            return self;
        }

        public static bool IsVisible(this Vector3 self)
        {
            return 0 < Mathf.Abs(self.x * self.y);
        }
    }

    /// <summary>
    /// Extension methods for Graphic class.
    /// </summary>
    internal static class GraphicExtensions
    {
        private static readonly Vector3[] s_WorldCorners = new Vector3[4];
        private static readonly Bounds s_ScreenBounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1, 1, 1));

        /// <summary>
        /// Check if a Graphic component is currently in the screen view.
        /// </summary>
        public static bool IsInScreen(this Graphic self)
        {
            if (!self || !self.canvas) return false;

            if (FrameCache.TryGet(self, nameof(IsInScreen), out bool result))
            {
                return result;
            }

            Profiler.BeginSample("(CCR)[GraphicExtensions] InScreen");
            var cam = self.canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? self.canvas.worldCamera
                : null;
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            self.rectTransform.GetWorldCorners(s_WorldCorners);

            for (var i = 0; i < 4; i++)
            {
                if (cam)
                {
                    s_WorldCorners[i] = cam.WorldToViewportPoint(s_WorldCorners[i]);
                }
                else
                {
                    s_WorldCorners[i] = RectTransformUtility.WorldToScreenPoint(null, s_WorldCorners[i]);
                    s_WorldCorners[i].x /= Screen.width;
                    s_WorldCorners[i].y /= Screen.height;
                }

                s_WorldCorners[i].z = 0;
                min = Vector3.Min(s_WorldCorners[i], min);
                max = Vector3.Max(s_WorldCorners[i], max);
            }

            var bounds = new Bounds(min, Vector3.zero);
            bounds.Encapsulate(max);
            result = bounds.Intersects(s_ScreenBounds);
            FrameCache.Set(self, nameof(IsInScreen), result);
            Profiler.EndSample();

            return result;
        }

        public static float GetParentGroupAlpha(this Graphic self)
        {
            var alpha = self.canvasRenderer.GetAlpha();
            if (Mathf.Approximately(alpha, 0)) return 1;

            var inheritedAlpha = self.canvasRenderer.GetInheritedAlpha();
            return Mathf.Clamp01(inheritedAlpha / alpha);
        }
    }

    /// <summary>
    /// Extension methods for Component class.
    /// </summary>
    internal static class ComponentExtensions
    {
        /// <summary>
        /// Add a component of a specific type to the children of a GameObject.
        /// </summary>
        public static void AddComponentOnChildren<T>(this Component self, HideFlags hideFlags, bool includeSelf)
            where T : Component
        {
            if (self == null) return;

            Profiler.BeginSample("(CCR)[ComponentExtensions] AddComponentOnChildren > Self");
            if (includeSelf && !self.TryGetComponent<T>(out _))
            {
                var c = self.gameObject.AddComponent<T>();
                c.hideFlags = hideFlags;
            }

            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[ComponentExtensions] AddComponentOnChildren > Child");
            var childCount = self.transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = self.transform.GetChild(i);
                if (child.TryGetComponent<T>(out _)) continue;

                var c = child.gameObject.AddComponent<T>();
                c.hideFlags = hideFlags;
            }

            Profiler.EndSample();
        }
    }

    internal static class GradientExtensions
    {
        public static void ToList(this Gradient self, List<(float time, Color color)> results)
        {
            Profiler.BeginSample("(CCR)[GradientExtensions] ToList");
            results.Clear();

            var times = ListPool<float>.Rent();
            var alphaKeys = self.alphaKeys;
            var colorKeys = self.colorKeys;
            var capacity = alphaKeys.Length + colorKeys.Length + 2;
            if (times.Capacity < capacity)
            {
                times.Capacity = capacity;
            }

            times.Add(0);
            times.Add(1);
            for (var i = 0; i < alphaKeys.Length; i++)
            {
                var t = alphaKeys[i].time;
                if (times.Contains(t)) continue;
                times.Add(t);
            }

            for (var i = 0; i < colorKeys.Length; i++)
            {
                var t = colorKeys[i].time;
                if (times.Contains(t)) continue;
                times.Add(t);
            }

            times.Sort((l, r) => 0 < l - r ? 1 : -1);

            if (results.Capacity < times.Count)
            {
                results.Capacity = times.Count;
            }

            var time = -1f;
            for (var i = 0; i < times.Count; i++)
            {
                if (Mathf.Approximately(time, times[i])) continue;
                time = times[i];
                results.Add((time, self.Evaluate(time)));
            }

            ListPool<float>.Return(ref times);
            Profiler.EndSample();
        }
    }

    /// <summary>
    /// Extension methods for Transform class.
    /// </summary>
    internal static class TransformExtensions
    {
        private static readonly Vector3[] s_Corners = new Vector3[4];

        /// <summary>
        /// Compare the hierarchy index of one transform with another transform.
        /// </summary>
        public static int CompareHierarchyIndex(this Transform self, Transform other, Transform stopAt)
        {
            if (self == other) return 0;

            Profiler.BeginSample("(CCR)[TransformExtensions] CompareHierarchyIndex > GetTransforms");
            var lTrs = self.GetTransforms(stopAt, ListPool<Transform>.Rent());
            var rTrs = other.GetTransforms(stopAt, ListPool<Transform>.Rent());
            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[TransformExtensions] CompareHierarchyIndex > Calc");
            var loop = Mathf.Min(lTrs.Count, rTrs.Count);
            var result = 0;
            for (var i = 0; i < loop; ++i)
            {
                self = lTrs[lTrs.Count - i - 1];
                other = rTrs[rTrs.Count - i - 1];
                if (self == other) continue;

                result = self.GetSiblingIndex() - other.GetSiblingIndex();
                break;
            }

            Profiler.EndSample();

            Profiler.BeginSample("(CCR)[TransformExtensions] CompareHierarchyIndex > Return");
            ListPool<Transform>.Return(ref lTrs);
            ListPool<Transform>.Return(ref rTrs);
            Profiler.EndSample();

            return result;
        }

        private static List<Transform> GetTransforms(this Transform self, Transform stopAt, List<Transform> results)
        {
            results.Clear();
            while (self != stopAt)
            {
                results.Add(self);
                self = self.parent;
            }

            return results;
        }

        /// <summary>
        /// Check if a transform has changed.
        /// </summary>
        public static bool HasChanged(this Transform self, Transform baseTransform, ref Matrix4x4 prev)
        {
            if (!self) return false;

            var hash = baseTransform ? baseTransform.GetHashCode() : 0;
            if (FrameCache.TryGet(self, nameof(HasChanged), hash, out bool result)) return result;

            var matrix = baseTransform
                ? baseTransform.worldToLocalMatrix * self.localToWorldMatrix
                : self.localToWorldMatrix;
            var current = matrix * Matrix4x4.Scale(Vector3.one * 10000);
            result = !Approximately(current, prev);
            FrameCache.Set(self, nameof(HasChanged), hash, result);
            if (result)
            {
                prev = current;
            }

            return result;
        }

        private static bool Approximately(Matrix4x4 self, Matrix4x4 other)
        {
            var epsilon = 1f / CompositeCanvasRendererProjectSettings.transformSensitivityBias;
            for (var i = 0; i < 16; i++)
            {
                if (epsilon < Mathf.Abs(self[i] - other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static Bounds GetRelativeBounds(this Transform self, Transform child)
        {
            if (!self || !child)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var list = ListPool<RectTransform>.Rent();
            child.GetComponentsInChildren(false, list);
            if (list.Count == 0)
            {
                ListPool<RectTransform>.Return(ref list);
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var min = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var worldToLocalMatrix = self.worldToLocalMatrix;
            for (var i = 0; i < list.Count; i++)
            {
                list[i].GetWorldCorners(s_Corners);
                for (var j = 0; j < 4; j++)
                {
                    var lhs = worldToLocalMatrix.MultiplyPoint3x4(s_Corners[j]);
                    max = Vector3.Min(lhs, max);
                    min = Vector3.Max(lhs, min);
                }
            }

            ListPool<RectTransform>.Return(ref list);

            var rectTransformBounds = new Bounds(max, Vector3.zero);
            rectTransformBounds.Encapsulate(min);
            return rectTransformBounds;
        }
    }
}
