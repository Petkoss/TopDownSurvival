# Top-Down Survival

A 2D top-down survival game made with Unity 6 (6000.3 LTS, URP 2D).

## Features

- **Player movement** – 8-directional physics movement (keyboard or gamepad), blocked by water, trees and rocks.
- **Resource gathering** – chop trees for wood, mine rocks for stone, pick mushrooms. Hold the gather button near a highlighted resource.
- **Inventory** – 24 stacking slots. The first row is the always-visible hotbar; open the full window to move, merge and swap stacks.
- **Save / load** – JSON save in `Application.persistentDataPath/savegame.json`. Loads on start, saves on quit.
- **Procedural world** – infinite, seeded, chunk-streamed world with water, beaches, grassland, forest and rocky biomes. Only the seed and harvested resources are saved.

## Controls

| Action | Keys |
|---|---|
| Move | WASD / Arrow keys / Left stick |
| Gather | E / Space / Left mouse / Gamepad West |
| Inventory | Tab / I (Esc closes) |
| Save / Load | F5 / F9 |
| New world | F10 |

## Getting started

1. Open the project with Unity **6000.3.28f1**.
2. Open `Assets/Scenes/Main.unity` and press Play.

`Survival > Rebuild Content and Main Scene` regenerates tiles, items, resource prefabs and the main scene from the sprites in `Assets/Art/Sprites`.

## Project layout

```
Assets/
  Art/Sprites     pixel art (16 px per unit)
  Art/Tiles       tile assets for the tilemaps
  Data            item definitions + item database
  Prefabs         resource node prefabs
  Scripts/        game code (Survival assembly)
    Core          GameManager (new game, save/load)
    Inventory     Inventory, InventorySlot
    Items         ItemDefinition, ItemDatabase
    Player        PlayerMovement, PlayerGatherer, CameraFollow
    Save          SaveData, SaveSystem
    UI            InventoryUI, MessageToast
    World         TerrainSampler, WorldGenerator, ResourceNode
    Editor        content/scene builder, sprite import settings
  Tests/EditMode  unit tests (inventory, world generation, save system)
```

## Credits

Art: [Tiny Town](https://kenney.nl/assets/tiny-town) and [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon) by Kenney (CC0). See `Assets/Art/LICENSE-*.txt`.
