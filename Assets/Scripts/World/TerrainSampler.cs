using UnityEngine;

namespace Survival.World
{
    public enum Biome
    {
        Water,
        Sand,
        Grassland,
        Forest,
        Rocky
    }

    /// <summary>
    /// Pure, deterministic terrain function: the same seed and coordinates always give the same result.
    /// This is what lets the save file store only the seed instead of the whole map.
    /// </summary>
    public sealed class TerrainSampler
    {
        public const float WaterLevel = 0.30f;
        public const float SandLevel = 0.36f;
        public const float RockLevel = 0.74f;
        public const float ForestMoisture = 0.56f;

        const float HeightScale = 0.03f;
        const float MoistureScale = 0.045f;

        readonly Vector2 _heightOffset;
        readonly Vector2 _moistureOffset;

        public int Seed { get; }

        public TerrainSampler(int seed)
        {
            Seed = seed;
            var rng = new System.Random(seed);
            _heightOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));
            _moistureOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));
        }

        public float Height(int x, int y) => Stretch(Fbm(x, y, _heightOffset, HeightScale, 4));
        public float Moisture(int x, int y) => Stretch(Fbm(x, y, _moistureOffset, MoistureScale, 3));

        public Biome GetBiome(int x, int y)
        {
            float h = Height(x, y);
            if (h < WaterLevel) return Biome.Water;
            if (h < SandLevel) return Biome.Sand;
            if (h > RockLevel) return Biome.Rocky;
            return Moisture(x, y) > ForestMoisture ? Biome.Forest : Biome.Grassland;
        }

        /// <summary>Deterministic pseudo-random value in [0, 1) for a tile and a "channel" (salt).</summary>
        public float Random01(int x, int y, int salt) => (Hash(x, y, Seed, salt) & 0xFFFFFF) / 16777216f;

        public static uint Hash(int x, int y, int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA6Bu;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xC2B2AE35u;
                h = (h << 17) | (h >> 15);
                h ^= (uint)salt * 0x27D4EB2Fu;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }

        static float Fbm(int x, int y, Vector2 offset, float scale, int octaves)
        {
            float sum = 0f, amplitude = 1f, frequency = scale, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Mathf.PerlinNoise(x * frequency + offset.x, y * frequency + offset.y);
                norm += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }
            return sum / norm;
        }

        // Layered Perlin noise clusters around 0.5; spread it out so thresholds give varied terrain.
        static float Stretch(float v) => Mathf.Clamp01((v - 0.5f) * 2.2f + 0.5f);
    }
}
