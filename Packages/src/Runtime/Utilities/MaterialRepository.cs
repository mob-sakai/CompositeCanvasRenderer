﻿using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace CompositeCanvas
{
    /// <summary>
    /// Provides functionality to manage materials.
    /// </summary>
    internal static class MaterialRepository
    {
        private static readonly ObjectRepository<Material> s_Repository =
            new ObjectRepository<Material>(nameof(MaterialRepository));

        public static int count => s_Repository.count;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            s_Repository.Clear();
        }
#endif

        /// <summary>
        /// Retrieves a cached material based on the hash.
        /// </summary>
        public static bool Valid(Hash128 hash, ref Material material)
        {
            Profiler.BeginSample("(CCR)[MaterialRegistry] Valid");
            var ret = s_Repository.Valid(hash, material);
            Profiler.EndSample();
            return ret;
        }

        /// <summary>
        /// Adds or retrieves a cached material based on the hash.
        /// </summary>
        public static void Get(Hash128 hash, ref Material material, Func<Material> onCreate)
        {
            Profiler.BeginSample("(CCR)[MaterialRegistry] Get");
            s_Repository.Get(hash, ref material, onCreate);
            Profiler.EndSample();
        }

        /// <summary>
        /// Removes a soft mask material from the cache.
        /// </summary>
        public static void Release(ref Material material)
        {
            Profiler.BeginSample("(CCR)[MaterialRegistry] Release");
            s_Repository.Release(ref material);
            Profiler.EndSample();
        }
    }
}