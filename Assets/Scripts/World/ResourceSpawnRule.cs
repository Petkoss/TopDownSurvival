using System;
using UnityEngine;

namespace Survival.World
{
    /// <summary>"In biome X, spawn prefab Y on this fraction of tiles."</summary>
    [Serializable]
    public class ResourceSpawnRule
    {
        public string label;
        public ResourceNode prefab;
        public Biome biome;
        [Range(0f, 1f)] public float chance;
    }
}
