using System.Linq;
using Survival.InventorySystem;
using Survival.Items;
using Survival.Player;
using Survival.Save;
using Survival.UI;
using Survival.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Survival.Core
{
    /// <summary>Starts a new game or restores the saved one, and handles save/load hotkeys.</summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] WorldGenerator world;
        [SerializeField] PlayerMovement player;
        [SerializeField] Inventory inventory;
        [SerializeField] ItemDatabase itemDatabase;
        [SerializeField] MessageToast toast;

        [Header("Behaviour")]
        [SerializeField] bool loadSaveOnStart = true;
        [SerializeField] bool saveOnQuit = true;
        [Tooltip("0 = random seed for each new world.")]
        [SerializeField] int fixedSeed;

        InputAction _save, _load, _newWorld;

        void Awake()
        {
            _save = new InputAction("Save", binding: "<Keyboard>/f5");
            _load = new InputAction("Load", binding: "<Keyboard>/f9");
            _newWorld = new InputAction("New World", binding: "<Keyboard>/f10");
        }

        void OnEnable()
        {
            _save.Enable();
            _load.Enable();
            _newWorld.Enable();
        }

        void OnDisable()
        {
            _save.Disable();
            _load.Disable();
            _newWorld.Disable();
        }

        void OnDestroy()
        {
            _save.Dispose();
            _load.Dispose();
            _newWorld.Dispose();
        }

        void Start()
        {
            if (!(loadSaveOnStart && LoadGame(showMessage: false))) NewGame();
        }

        void Update()
        {
            if (_save.WasPressedThisFrame()) SaveGame();
            if (_load.WasPressedThisFrame()) LoadGame();
            if (_newWorld.WasPressedThisFrame())
            {
                NewGame();
                Show("New world generated");
            }
        }

        void OnApplicationQuit()
        {
            if (saveOnQuit && world.IsInitialized) SaveSystem.Save(CaptureState());
        }

        public void NewGame(int seed = 0)
        {
            if (seed == 0) seed = fixedSeed != 0 ? fixedSeed : Random.Range(1, int.MaxValue);
            inventory.Clear();
            world.Initialize(seed, null);
            player.Teleport(world.SpawnPoint);
            world.RefreshAround(world.SpawnPoint);
        }

        public void SaveGame()
        {
            SaveSystem.Save(CaptureState());
            Show("Game saved");
        }

        public bool LoadGame(bool showMessage = true)
        {
            if (!SaveSystem.TryLoad(out var data))
            {
                if (showMessage) Show("No save found");
                return false;
            }
            ApplyState(data);
            if (showMessage) Show("Game loaded");
            return true;
        }

        public SaveData CaptureState()
        {
            var data = new SaveData
            {
                worldSeed = world.Seed,
                playerX = player.transform.position.x,
                playerY = player.transform.position.y,
                harvestedNodes = world.HarvestedNodeIds.ToList()
            };

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot.IsEmpty) continue;
                data.inventory.Add(new SavedSlot { slot = i, itemId = slot.item.id, count = slot.count });
            }
            return data;
        }

        public void ApplyState(SaveData data)
        {
            world.Initialize(data.worldSeed, data.harvestedNodes);

            inventory.Clear();
            foreach (var saved in data.inventory)
            {
                var item = itemDatabase.Get(saved.itemId);
                if (item == null)
                {
                    Debug.LogWarning($"Save references unknown item '{saved.itemId}'; skipped.");
                    continue;
                }
                inventory.SetSlot(saved.slot, item, saved.count);
            }

            var position = new Vector2(data.playerX, data.playerY);
            player.Teleport(position);
            world.RefreshAround(position);
        }

        void Show(string message)
        {
            if (toast != null) toast.Show(message);
            Debug.Log(message);
        }
    }
}
