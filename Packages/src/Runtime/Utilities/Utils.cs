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
