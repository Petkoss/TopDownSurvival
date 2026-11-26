using System.Collections.Generic;
using System.IO;
using Survival.Core;
using Survival.InventorySystem;
using Survival.Items;
using Survival.Player;
using Survival.UI;
using Survival.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Survival.EditorTools
{
    /// <summary>
    /// Generates all game content (tiles, items, prefabs) and the Main scene from the sprites in Assets/Art/Sprites.
    /// Safe to run repeatedly: existing assets are updated in place so references stay intact.
    /// </summary>
    public static class ProjectBuilder
    {
        const string SpriteDir = "Assets/Art/Sprites";
        const string TileDir = "Assets/Art/Tiles";
        const string ItemDir = "Assets/Data/Items";
        const string DatabasePath = "Assets/Data/ItemDatabase.asset";
        const string PrefabDir = "Assets/Prefabs";
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string RendererPath = "Assets/Settings/Renderer2D.asset";

        static readonly Color WaterColor = new Color32(77, 155, 230, 255);

        [MenuItem("Survival/Rebuild Content and Main Scene")]
        public static void BuildAll()
        {
            // Create the scene first: NewScene(Single) unloads unused assets, which would
            // invalidate any tile/item/prefab references loaded before it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ReimportSprites();
            ConfigureYSorting();
            var tiles = BuildTiles();
            var items = BuildItems();
            var prefabs = BuildResourcePrefabs(items);
            BuildScene(scene, tiles, items, prefabs);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectBuilder] Content and Main scene rebuilt.");
        }

        // ---------------------------------------------------------------- sprites & settings

        static void ReimportSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{name}.png");
            if (sprite == null) throw new FileNotFoundException($"Missing sprite {SpriteDir}/{name}.png");
            return sprite;
        }

        /// <summary>Top-down games draw things lower on screen in front: sort by the Y axis.</summary>
        static void ConfigureYSorting()
        {
            GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath);
            if (renderer == null) return;
            var so = new SerializedObject(renderer);
            var mode = so.FindProperty("m_TransparencySortMode");
            var axis = so.FindProperty("m_TransparencySortAxis");
            if (mode != null) mode.intValue = (int)TransparencySortMode.CustomAxis;
            if (axis != null) axis.vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- tiles

        class TileSet
        {
            public Tile grass, grassDetail, flowers, sand, sandDetail, dirt, water;
        }

        static TileSet BuildTiles()
        {
            Directory.CreateDirectory(TileDir);
            return new TileSet
            {
                grass = MakeTile("Grass", "grass_0", Tile.ColliderType.None),
                grassDetail = MakeTile("GrassDetail", "grass_1", Tile.ColliderType.None),
                flowers = MakeTile("Flowers", "grass_flowers", Tile.ColliderType.None),
                sand = MakeTile("Sand", "sand_0", Tile.ColliderType.None),
                sandDetail = MakeTile("SandDetail", "sand_1", Tile.ColliderType.None),
                dirt = MakeTile("Dirt", "dirt", Tile.ColliderType.None),
                water = MakeTile("Water", "water", Tile.ColliderType.Grid),
            };
        }

        static Tile MakeTile(string assetName, string spriteName, Tile.ColliderType collider)
        {
            var tile = LoadOrCreate<Tile>($"{TileDir}/{assetName}.asset");
            tile.sprite = LoadSprite(spriteName);
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ---------------------------------------------------------------- items

        class ItemSet
        {
            public ItemDefinition wood, stone, mushroom;
            public ItemDatabase database;
        }

        static ItemSet BuildItems()
        {
            Directory.CreateDirectory(ItemDir);
            var set = new ItemSet
            {
                wood = MakeItem("wood", "Wood", "icon_wood", 99),
                stone = MakeItem("stone", "Stone", "icon_stone", 99),
                mushroom = MakeItem("mushroom", "Mushroom", "mushroom", 20),
            };

            set.database = LoadOrCreate<ItemDatabase>(DatabasePath);
            set.database.items = new List<ItemDefinition> { set.wood, set.stone, set.mushroom };
            EditorUtility.SetDirty(set.database);
            return set;
        }

        static ItemDefinition MakeItem(string id, string displayName, string iconSprite, int maxStack)
        {
            var item = LoadOrCreate<ItemDefinition>($"{ItemDir}/{displayName}.asset");
            item.id = id;
            item.displayName = displayName;
            item.icon = LoadSprite(iconSprite);
            item.maxStack = maxStack;
            EditorUtility.SetDirty(item);
            return item;
        }

        // ---------------------------------------------------------------- resource prefabs

        class PrefabSet
        {
            public ResourceNode pine, roundTree, autumnTree, rock, mushroom;
        }

        static PrefabSet BuildResourcePrefabs(ItemSet items)
        {
            Directory.CreateDirectory($"{PrefabDir}/Resources");
            var trunk = new Vector2(0.45f, 0.3f);
            return new PrefabSet
            {
                pine = MakeNode("PineTree", "tree_pine", items.wood, 4, trunk, new Vector2(0f, 0.15f), false),
                roundTree = MakeNode("RoundTree", "tree_round", items.wood, 4, trunk, new Vector2(0f, 0.15f), false),
                autumnTree = MakeNode("AutumnTree", "tree_autumn", items.wood, 4, trunk, new Vector2(0f, 0.15f), false),
                rock = MakeNode("Rock", "rock", items.stone, 5, new Vector2(0.8f, 0.45f), new Vector2(0f, 0.3f), false),
                mushroom = MakeNode("Mushroom", "mushroom", items.mushroom, 1, new Vector2(0.7f, 0.5f), new Vector2(0f, 0.25f), true),
            };
        }

        static ResourceNode MakeNode(string name, string sprite, ItemDefinition drop, int hits, Vector2 colliderSize,
            Vector2 colliderOffset, bool isTrigger)
        {
            var root = new GameObject(name);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.spriteSortPoint = SpriteSortPoint.Pivot;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = colliderOffset;
            box.isTrigger = isTrigger;

            var node = root.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            so.FindProperty("dropItem").objectReferenceValue = drop;
            so.FindProperty("hitsToDeplete").intValue = hits;
            so.FindProperty("yieldPerHit").intValue = 1;
            so.FindProperty("visual").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabDir}/Resources/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var saved = AssetDatabase.LoadAssetAtPath<ResourceNode>(path);
            if (saved == null) throw new System.InvalidOperationException($"Prefab {path} has no ResourceNode after saving");
            return saved;
        }

        // ---------------------------------------------------------------- scene

        static void BuildScene(Scene scene, TileSet tiles, ItemSet items, PrefabSet prefabs)
        {

            // Lighting: URP 2D uses lit sprites, so add one global light.
            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            // Tilemaps.
            var gridGo = new GameObject("Grid", typeof(Grid));
            var ground = CreateTilemap(gridGo.transform, "Ground", -20);
            var water = CreateTilemap(gridGo.transform, "Water", -19);
            var waterCollider = water.gameObject.AddComponent<TilemapCollider2D>();
            var waterBody = water.gameObject.AddComponent<Rigidbody2D>();
            waterBody.bodyType = RigidbodyType2D.Static;
            water.gameObject.AddComponent<CompositeCollider2D>();
            waterCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            // Player.
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            var body = playerGo.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var feet = playerGo.AddComponent<CircleCollider2D>();
            feet.radius = 0.22f;
            feet.offset = new Vector2(0f, 0.2f);
            var playerVisual = new GameObject("Visual");
            playerVisual.transform.SetParent(playerGo.transform, false);
            var playerSr = playerVisual.AddComponent<SpriteRenderer>();
            playerSr.sprite = LoadSprite("player_down_0");
            playerSr.spriteSortPoint = SpriteSortPoint.Pivot;
            var movement = playerGo.AddComponent<PlayerMovement>();
            var animator = playerGo.AddComponent<PlayerAnimator>();
            Set(animator, "visual", playerSr);
            SetSprites(animator, "walkDown", "player_down");
            SetSprites(animator, "walkUp", "player_up");
            SetSprites(animator, "walkSide", "player_side");
            var inventory = playerGo.AddComponent<Inventory>();
            var gatherer = playerGo.AddComponent<PlayerGatherer>();

            // Camera.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.75f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = WaterColor;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = SpriteImportSettings.PixelsPerUnit;
            ppc.refResolutionX = 384;
            ppc.refResolutionY = 216;
            var follow = camGo.AddComponent<CameraFollow>();
            follow.Target = playerGo.transform;

            // UI.
            var uiGo = new GameObject("UI");
            var toast = uiGo.AddComponent<MessageToast>();
            var inventoryUi = uiGo.AddComponent<InventoryUI>();
            Set(inventoryUi, "inventory", inventory);
            Set(gatherer, "inventory", inventory);
            Set(gatherer, "movement", movement);
            Set(gatherer, "toast", toast);

            // World.
            var worldGo = new GameObject("World");
            var world = worldGo.AddComponent<WorldGenerator>();
            var wso = new SerializedObject(world);
            wso.FindProperty("viewer").objectReferenceValue = playerGo.transform;
            wso.FindProperty("groundTilemap").objectReferenceValue = ground;
            wso.FindProperty("waterTilemap").objectReferenceValue = water;
            wso.FindProperty("grassTile").objectReferenceValue = tiles.grass;
            wso.FindProperty("grassDetailTile").objectReferenceValue = tiles.grassDetail;
            wso.FindProperty("flowerTile").objectReferenceValue = tiles.flowers;
            wso.FindProperty("sandTile").objectReferenceValue = tiles.sand;
            wso.FindProperty("sandDetailTile").objectReferenceValue = tiles.sandDetail;
            wso.FindProperty("dirtTile").objectReferenceValue = tiles.dirt;
            wso.FindProperty("waterTile").objectReferenceValue = tiles.water;

            var rules = new[]
            {
                ("Forest pines", prefabs.pine, Biome.Forest, 0.10f),
                ("Forest round trees", prefabs.roundTree, Biome.Forest, 0.05f),
                ("Forest mushrooms", prefabs.mushroom, Biome.Forest, 0.025f),
                ("Grassland trees", prefabs.roundTree, Biome.Grassland, 0.018f),
                ("Grassland autumn trees", prefabs.autumnTree, Biome.Grassland, 0.008f),
                ("Grassland rocks", prefabs.rock, Biome.Grassland, 0.008f),
                ("Grassland mushrooms", prefabs.mushroom, Biome.Grassland, 0.01f),
                ("Rocky rocks", prefabs.rock, Biome.Rocky, 0.09f),
                ("Rocky pines", prefabs.pine, Biome.Rocky, 0.01f),
                ("Beach rocks", prefabs.rock, Biome.Sand, 0.01f),
            };
            var rulesProp = wso.FindProperty("resourceRules");
            rulesProp.arraySize = rules.Length;
            for (int i = 0; i < rules.Length; i++)
            {
                var element = rulesProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("label").stringValue = rules[i].Item1;
                element.FindPropertyRelative("prefab").objectReferenceValue = rules[i].Item2;
                element.FindPropertyRelative("biome").enumValueIndex = (int)rules[i].Item3;
                element.FindPropertyRelative("chance").floatValue = rules[i].Item4;
            }
            wso.ApplyModifiedPropertiesWithoutUndo();

            // Game manager.
            var gameGo = new GameObject("Game Manager");
            var game = gameGo.AddComponent<GameManager>();
            var gso = new SerializedObject(game);
            gso.FindProperty("world").objectReferenceValue = world;
            gso.FindProperty("player").objectReferenceValue = movement;
            gso.FindProperty("inventory").objectReferenceValue = inventory;
            gso.FindProperty("itemDatabase").objectReferenceValue = items.database;
            gso.FindProperty("toast").objectReferenceValue = toast;
            gso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity") != null)
                AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
        }

        static Tilemap CreateTilemap(Transform grid, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return go.GetComponent<Tilemap>();
        }

        // ---------------------------------------------------------------- helpers

        static void Set(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null) throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{property}'");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fills a Sprite[] field with name_0, name_1, ... until a frame is missing.</summary>
        static void SetSprites(Object target, string property, string prefix)
        {
            var frames = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; ; i++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{prefix}_{i}.png");
                if (sprite == null) break;
                frames.Add(sprite);
            }
            if (frames.Count == 0) throw new FileNotFoundException($"No frames for {SpriteDir}/{prefix}_N.png");

            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null) throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{property}'");
            prop.arraySize = frames.Count;
            for (int i = 0; i < frames.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
