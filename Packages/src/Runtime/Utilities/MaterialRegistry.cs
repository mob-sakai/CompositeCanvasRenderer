using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace CompositeCanvas
{
    /// <summary>
    /// Provides functionality to manage materials.
    /// </summary>
    public static class MaterialRegistry
    {
        private static readonly ObjectPool<MatEntry> s_MatEntryPool =
            new ObjectPool<MatEntry>(() => new MatEntry(), _ => true, ent => ent.Release());

        private static readonly List<MatEntry> s_List = new List<MatEntry>();
        public static int activeMaterialCount => s_List.Count;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            foreach (var ent in s_List)
            {
                ent.Release();
            }

            s_List.Clear();
        }
#endif

        /// <summary>
        /// Adds or retrieves a cached material based on the hash.
        /// </summary>
        public static void Get(Hash128 hash, ref Material material, Func<Material> onCreate, bool globalCache = false)
        {
            // Find existing entry.
            Profiler.BeginSample("(CCR)[MaterialRegistry] Get > Find existing entry");
            for (var i = 0; i < s_List.Count; ++i)
            {
                var ent = s_List[i];
                if (ent.hash != hash) continue;

                // Existing entry found.
                if (ent.customMat != material)
                {
                    // if the material is different, release the old one.
                    Release(ref material);
                    ++ent.count;
                    material = ent.customMat;
                    Logging.Log(typeof(MaterialRegistry),
                        $"Get(#{s_List.Count}): {ent.hash.GetHashCode()} (#{ent.count}), {ent.customMat.shader}");
                }

                Profiler.EndSample();
                return;
            }

            Profiler.EndSample();

            // Create new entry.
            Profiler.BeginSample("(CCR)[MaterialRegistry] Get > Create new entry");
            var entry = s_MatEntryPool.Rent();
            entry.customMat = onCreate();
            entry.hash = hash;
            entry.count = globalCache ? 2 : 1;
            s_List.Add(entry);
            Logging.Log(typeof(MaterialRegistry),
                $"Get(#{s_List.Count}): {entry.hash.GetHashCode()}, {entry.customMat.shader}");

            Release(ref material);
            material = entry.customMat;
            Profiler.EndSample();
        }

        /// <summary>
        /// Removes a soft mask material from the cache.
        /// </summary>
        public static void Release(ref Material customMat)
        {
            if (customMat == null) return;

            Profiler.BeginSample("(CCR)[MaterialRegistry] Release");
            for (var i = 0; i < s_List.Count; i++)
            {
                var ent = s_List[i];

                if (ent.customMat != customMat)
                {
                    continue;
                }

                if (--ent.count <= 0)
                {
                    Profiler.BeginSample("(CCR)[MaterialRegistry] Release > RemoveAt");
                    Logging.Log(typeof(MaterialRegistry),
                        $"Release(#{s_List.Count - 1}): {ent.hash.GetHashCode()}, {ent.customMat.shader}");
                    s_List.RemoveAtFast(i);
                    s_MatEntryPool.Return(ref ent);
                    Profiler.EndSample();
                }
                else
                {
                    Logging.Log(typeof(MaterialRegistry),
                        $"Release(#{s_List.Count}): {ent.hash.GetHashCode()} (#{ent.count}), {ent.customMat.shader}");
                }

                customMat = null;
                break;
            }

            Profiler.EndSample();
        }

        private class MatEntry
        {
            public int count;
            public Material customMat;
            public Hash128 hash;

            public void Release()
            {
                count = 0;
                if (customMat)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Object.DestroyImmediate(customMat, false);
                    }
                    else
#endif
                    {
                        Object.Destroy(customMat);
                    }
                }

                customMat = null;
            }
        }
    }
}
