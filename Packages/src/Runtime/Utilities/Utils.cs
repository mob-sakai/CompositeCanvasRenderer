using System.Collections.Generic;
using Coffee.CompositeCanvasRendererInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace CompositeCanvas
{
    internal static class ShaderPropertyIds
    {
        public static readonly int mainTex = Shader.PropertyToID("_MainTex");
        public static readonly int color = Shader.PropertyToID("_Color");
        public static readonly int textureSampleAdd = Shader.PropertyToID("_TextureSampleAdd");
        public static readonly int gammaToLinear = Shader.PropertyToID("_UIVertexColorAlwaysGammaSpace");
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

    internal static class MeshExtensions
    {
        internal static readonly InternalObjectPool<Mesh> s_MeshPool = new InternalObjectPool<Mesh>(
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

            var vector3List = InternalListPool<Vector3>.Rent();
            var vector4List = InternalListPool<Vector4>.Rent();
            var color32List = InternalListPool<Color32>.Rent();
            var intList = InternalListPool<int>.Rent();

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
            InternalListPool<Vector3>.Return(ref vector3List);
            InternalListPool<Vector4>.Return(ref vector4List);
            InternalListPool<Color32>.Return(ref color32List);
            InternalListPool<int>.Return(ref intList);
        }
    }

    internal static class GradientExtensions
    {
        public static void ToList(this Gradient self, List<(float time, Color color)> results)
        {
            Profiler.BeginSample("(CCR)[GradientExtensions] ToList");
            results.Clear();

            var times = InternalListPool<float>.Rent();
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

            InternalListPool<float>.Return(ref times);
            Profiler.EndSample();
        }
    }
}
