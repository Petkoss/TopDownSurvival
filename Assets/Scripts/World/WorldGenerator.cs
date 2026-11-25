using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Survival.World
{
    /// <summary>
    /// Infinite, chunk-streamed procedural world. Terrain and resource placement come from
    /// <see cref="TerrainSampler"/>, so only the seed and the set of harvested nodes need saving.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Streaming")]
        [SerializeField, Min(8)] int chunkSize = 32;
        [SerializeField, Min(1)] int loadRadius = 1;
        [SerializeField] Transform viewer;

        [Header("Tilemaps")]
        [SerializeField] Tilemap groundTilemap;
        [SerializeField] Tilemap waterTilemap;

        [Header("Tiles")]
        [SerializeField] TileBase grassTile;
        [SerializeField] TileBase grassDetailTile;
        [SerializeField] TileBase flowerTile;
        [SerializeField] TileBase sandTile;
        [SerializeField] TileBase sandDetailTile;
        [SerializeField] TileBase dirtTile;
        [SerializeField] TileBase waterTile;

        [Header("Resources")]
        [SerializeField] ResourceSpawnRule[] resourceRules = new ResourceSpawnRule[0];
        [Tooltip("No resources spawn this close to the spawn point.")]
        [SerializeField] float spawnClearRadius = 2.5f;

        const int SaltTileVariant = 1;
        const int SaltResource = 2;
        const int SaltJitterX = 3;
        const int SaltJitterY = 4;

        class Chunk
        {
            public Vector2Int coord;
            public GameObject root;
        }

        readonly Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();
        readonly HashSet<string> _harvested = new HashSet<string>();
        TerrainSampler _sampler;
        Vector2Int _spawnTile;
        Vector2Int? _lastViewerChunk;

        public bool IsInitialized => _sampler != null;
        public int Seed => _sampler?.Seed ?? 0;
        public TerrainSampler Sampler => _sampler;
        public IReadOnlyCollection<string> HarvestedNodeIds => _harvested;
        public int LoadedChunkCount => _chunks.Count;
        public int ChunkSize => chunkSize;

        public Transform Viewer
        {
            get => viewer;
            set => viewer = value;
        }

        /// <summary>Resets the world to a new seed. <paramref name="harvested"/> lists nodes that stay removed.</summary>
        public void Initialize(int seed, IEnumerable<string> harvested)
        {
            UnloadAll();
            _sampler = new TerrainSampler(seed);
            _harvested.Clear();
            if (harvested != null)
                foreach (var id in harvested) _harvested.Add(id);
            _spawnTile = FindSpawnTile();
            _lastViewerChunk = null;
        }

        /// <summary>World position (bottom-centre of a tile) where a new player should start.</summary>
        public Vector2 SpawnPoint => new Vector2(_spawnTile.x + 0.5f, _spawnTile.y + 0.3f);

        public Biome GetBiomeAt(Vector2 worldPosition)
        {
            return _sampler.GetBiome(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
        }

        void Update()
        {
            if (_sampler == null || viewer == null) return;
            var current = WorldToChunk(viewer.position);
            if (_lastViewerChunk != current) RefreshAround(viewer.position);
        }

        /// <summary>Loads chunks around <paramref name="position"/> and unloads far ones.</summary>
        public void RefreshAround(Vector2 position)
        {
            if (_sampler == null) return;
            var center = WorldToChunk(position);
            _lastViewerChunk = center;

            for (int dy = -loadRadius; dy <= loadRadius; dy++)
            for (int dx = -loadRadius; dx <= loadRadius; dx++)
            {
                var coord = new Vector2Int(center.x + dx, center.y + dy);
                if (!_chunks.ContainsKey(coord)) LoadChunk(coord);
            }

            // Keep a one-chunk margin so walking back and forth on a border doesn't thrash.
            var toUnload = new List<Vector2Int>();
            foreach (var coord in _chunks.Keys)
            {
                if (Mathf.Abs(coord.x - center.x) > loadRadius + 1 || Mathf.Abs(coord.y - center.y) > loadRadius + 1)
                    toUnload.Add(coord);
            }
            foreach (var coord in toUnload) UnloadChunk(coord);
        }

        Vector2Int WorldToChunk(Vector2 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));
        }

        void LoadChunk(Vector2Int coord)
        {
            int count = chunkSize * chunkSize;
            var positions = new Vector3Int[count];
            var groundTiles = new TileBase[count];
            var waterTiles = new TileBase[count];

            var root = new GameObject($"Chunk {coord.x},{coord.y}");
            root.transform.SetParent(transform, false);

            int originX = coord.x * chunkSize;
            int originY = coord.y * chunkSize;
            int i = 0;
            for (int ly = 0; ly < chunkSize; ly++)
            for (int lx = 0; lx < chunkSize; lx++, i++)
            {
                int x = originX + lx;
                int y = originY + ly;
                positions[i] = new Vector3Int(x, y, 0);

                var biome = _sampler.GetBiome(x, y);
                if (biome == Biome.Water)
                {
                    waterTiles[i] = waterTile;
                    continue;
                }

                groundTiles[i] = PickGroundTile(biome, x, y);
                TrySpawnResource(biome, x, y, root.transform);
            }

            groundTilemap.SetTiles(positions, groundTiles);
            waterTilemap.SetTiles(positions, waterTiles);
            _chunks[coord] = new Chunk { coord = coord, root = root };
        }

        void UnloadChunk(Vector2Int coord)
        {
            if (!_chunks.TryGetValue(coord, out var chunk)) return;
            int count = chunkSize * chunkSize;
            var positions = new Vector3Int[count];
            var empty = new TileBase[count];
            int i = 0;
            for (int ly = 0; ly < chunkSize; ly++)
            for (int lx = 0; lx < chunkSize; lx++, i++)
                positions[i] = new Vector3Int(coord.x * chunkSize + lx, coord.y * chunkSize + ly, 0);

            groundTilemap.SetTiles(positions, empty);
            waterTilemap.SetTiles(positions, empty);
            if (chunk.root != null) Destroy(chunk.root);
            _chunks.Remove(coord);
        }

        void UnloadAll()
        {
            foreach (var chunk in _chunks.Values)
                if (chunk.root != null) Destroy(chunk.root);
            _chunks.Clear();
            if (groundTilemap != null) groundTilemap.ClearAllTiles();
            if (waterTilemap != null) waterTilemap.ClearAllTiles();
        }

        TileBase PickGroundTile(Biome biome, int x, int y)
        {
            float r = _sampler.Random01(x, y, SaltTileVariant);
            switch (biome)
            {
                case Biome.Sand:
                    return r < 0.2f ? sandDetailTile : sandTile;
                case Biome.Rocky:
                    return dirtTile;
                case Biome.Forest:
                    return r < 0.35f ? grassDetailTile : grassTile;
                default:
                    if (r < 0.06f) return flowerTile;
                    return r < 0.28f ? grassDetailTile : grassTile;
            }
        }

        void TrySpawnResource(Biome biome, int x, int y, Transform parent)
        {
            if (Vector2.Distance(new Vector2(x, y), _spawnTile) < spawnClearRadius) return;

            string id = NodeIdFor(x, y);
            if (_harvested.Contains(id)) return;

            var rule = PickRule(biome, _sampler.Random01(x, y, SaltResource));
            if (rule == null || rule.prefab == null) return;

            float jitterX = (_sampler.Random01(x, y, SaltJitterX) - 0.5f) * 0.4f;
            float jitterY = (_sampler.Random01(x, y, SaltJitterY) - 0.5f) * 0.3f;
            var position = new Vector3(x + 0.5f + jitterX, y + 0.2f + jitterY, 0f);

            var node = Instantiate(rule.prefab, position, Quaternion.identity, parent);
            node.name = $"{rule.prefab.name} {x},{y}";
            node.NodeId = id;
            node.Depleted += OnNodeDepleted;
        }

        /// <summary>Picks at most one rule for a tile, using a single random roll across matching rules.</summary>
        ResourceSpawnRule PickRule(Biome biome, float roll)
        {
            float cumulative = 0f;
            foreach (var rule in resourceRules)
            {
                if (rule.biome != biome) continue;
                cumulative += rule.chance;
                if (roll < cumulative) return rule;
            }
            return null;
        }

        void OnNodeDepleted(ResourceNode node)
        {
            if (!string.IsNullOrEmpty(node.NodeId)) _harvested.Add(node.NodeId);
        }

        public static string NodeIdFor(int x, int y) => $"{x},{y}";

        Vector2Int FindSpawnTile()
        {
            // Spiral outwards from the origin to the first grassland tile with some land around it.
            for (int radius = 0; radius < 2000; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                    if (IsGoodSpawn(dx, dy)) return new Vector2Int(dx, dy);
                }
            }
            return Vector2Int.zero;
        }

        bool IsGoodSpawn(int x, int y)
        {
            for (int oy = -2; oy <= 2; oy++)
            for (int ox = -2; ox <= 2; ox++)
            {
                var b = _sampler.GetBiome(x + ox, y + oy);
                if (b == Biome.Water) return false;
            }
            return _sampler.GetBiome(x, y) == Biome.Grassland;
        }
    }
}
