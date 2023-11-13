using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace CompositeCanvas
{
    /// <summary>
    /// Utility class for managing temporary render textures.
    /// </summary>
    public static class TemporaryRenderTexture
    {
        private static readonly GraphicsFormat s_GraphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);

#if UNITY_2021_3_OR_NEWER
        private static readonly GraphicsFormat s_StencilFormat = GraphicsFormatUtility.GetDepthStencilFormat(0, 8);
#endif

        public static int activeCount
        {
            get;
            private set;
        }

        public static RenderTexture Get(int downSamplingRate, ref RenderTexture buffer, bool useStencil)
        {
            return Get(GetScreenSize(), downSamplingRate, ref buffer, useStencil);
        }

        public static RenderTexture Get(Vector2 size, int downSamplingRate, ref RenderTexture buffer, bool useStencil)
        {
            var preferSize = GetPreferSize(new Vector2Int(
                Mathf.Max(8, Mathf.RoundToInt(size.x)),
                Mathf.Max(8, Mathf.RoundToInt(size.y))), downSamplingRate);

            Profiler.BeginSample("(CCR)[TemporaryRT] Get");
            if (buffer && (buffer.width != preferSize.x
                           || buffer.height != preferSize.y
#if UNITY_2021_3_OR_NEWER
                           || useStencil != (buffer.depthStencilFormat != GraphicsFormat.None))
#else
                           || useStencil != (0 < buffer.depth))
#endif
               )
            {
                Release(ref buffer);
            }

            if (!buffer)
            {
                var rtd = new RenderTextureDescriptor(
                    preferSize.x,
                    preferSize.y,
                    s_GraphicsFormat,
                    0);
                rtd.mipCount = -1;
#if UNITY_2021_3_OR_NEWER
                rtd.depthStencilFormat = useStencil ? s_StencilFormat : GraphicsFormat.None;
#else
                rtd.depthBufferBits = useStencil ? 24 : 0;
#endif
                buffer = RenderTexture.GetTemporary(rtd);
                activeCount++;
                Logging.Log(typeof(TemporaryRenderTexture), $"Generate (#{activeCount}): {buffer.name}");
            }

            Profiler.EndSample();

            return buffer;
        }

        /// <summary>
        /// Releases the RenderTexture buffer.
        /// </summary>
        public static void Release(ref RenderTexture buffer)
        {
            Profiler.BeginSample("(CCR)[TemporaryRT] Release");
            if (buffer)
            {
                activeCount--;
                Logging.Log(typeof(TemporaryRenderTexture), $"Release (#{activeCount}): {buffer.name}");
                RenderTexture.ReleaseTemporary(buffer);
            }

            buffer = null;
            Profiler.EndSample();
        }

        private static Vector2Int GetPreferSize(Vector2Int size, int downSamplingRate)
        {
            var aspect = (float)size.x / size.y;
            var screenSize = GetScreenSize();

            // Clamp to screen size.
            size.x = Mathf.Clamp(size.x, 8, screenSize.x);
            size.y = Mathf.Clamp(size.y, 8, screenSize.y);

            if (downSamplingRate <= 0)
            {
                if (size.x < size.y)
                {
                    size.x = Mathf.CeilToInt(size.y * aspect);
                }
                else
                {
                    size.y = Mathf.CeilToInt(size.x / aspect);
                }

                return size;
            }

            if (size.x < size.y)
            {
                size.y = Mathf.NextPowerOfTwo(size.y / 2) / downSamplingRate;
                size.x = Mathf.CeilToInt(size.y * aspect);
            }
            else
            {
                size.x = Mathf.NextPowerOfTwo(size.x / 2) / downSamplingRate;
                size.y = Mathf.CeilToInt(size.x / aspect);
            }

            return size;
        }

        private static Vector2Int GetScreenSize()
        {
#if !UNITY_EDITOR
            return new Vector2Int(Screen.width, Screen.height);
#else
            if (FrameCache.TryGet(nameof(TemporaryRenderTexture), nameof(GetScreenSize), out Vector2Int size))
            {
                return size;
            }

            if (!Application.isPlaying && !Camera.current)
            {
                var res = UnityStats.screenRes.Split('x');
                size.x = Mathf.Max(8, int.Parse(res[0]));
                size.y = Mathf.Max(8, int.Parse(res[1]));
            }
            else
            {
                size.x = Screen.width;
                size.y = Screen.height;
            }

            FrameCache.Set(nameof(TemporaryRenderTexture), nameof(GetScreenSize), size);
            return size;
#endif
        }
    }
}
