# Top-Down Survival

2D top-down survival hra vytvorená v Unity 6 (6000.3 LTS, URP 2D).

## Funkcie

- **Pohyb hráča** – pohyb v 8 smeroch založený na fyzike (klávesnica alebo gamepad), cez vodu, stromy a kamene sa neprejde.
- **Zbieranie surovín** – rúbanie stromov na drevo, ťaženie kameňov na kameň, zbieranie húb. Podrž tlačidlo zbierania pri zvýraznenej surovine.
- **Inventár** – 24 slotov so stohovaním predmetov. Prvý riadok je stále viditeľný hotbar; v okne inventára môžeš predmety presúvať, spájať a vymieňať.
- **Ukladanie / načítanie** – JSON uloženie v `Application.persistentDataPath/savegame.json`. Načíta sa pri spustení a uloží pri ukončení hry.
- **Procedurálny svet** – nekonečný svet generovaný zo seedu, načítavaný po chunkoch, s biómami: voda, pláže, lúky, les a skalnatý terén. Ukladá sa iba seed a vyťažené suroviny.

## Ovládanie

| Akcia | Klávesy |
|---|---|
| Pohyb | WASD / šípky / ľavá páčka |
| Zbieranie | E / medzerník / ľavé tlačidlo myši / gamepad West |
| Inventár | Tab / I (Esc zatvorí) |
| Uložiť / Načítať | F5 / F9 |
| Nový svet | F10 |

## Začíname

1. Otvor projekt v Unity **6000.3.28f1**.
2. Otvor `Assets/Scenes/Main.unity` a stlač Play.

Položka menu `Survival > Rebuild Content and Main Scene` znovu vygeneruje dlaždice, predmety, prefaby surovín a hlavnú scénu zo spritov v `Assets/Art/Sprites`.

## Štruktúra projektu

```
Assets/
  Art/Sprites     pixel art (16 px na jednotku)
  Art/Tiles       dlaždice pre tilemapy
  Data            definície predmetov + databáza predmetov
  Prefabs         prefaby surovín
  Scripts/        herný kód (assembly Survival)
    Core          GameManager (nová hra, uloženie/načítanie)
    Inventory     Inventory, InventorySlot
    Items         ItemDefinition, ItemDatabase
    Player        PlayerMovement, PlayerGatherer, CameraFollow
    Save          SaveData, SaveSystem
    UI            InventoryUI, MessageToast
    World         TerrainSampler, WorldGenerator, ResourceNode
    Editor        generátor obsahu/scény, nastavenia importu spritov
  Tests/EditMode  unit testy (inventár, generovanie sveta, ukladanie)
```

## Autori grafiky

Grafika: [Tiny Town](https://kenney.nl/assets/tiny-town) a [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon) od Kenneyho (CC0). Pozri `Assets/Art/LICENSE-*.txt`.
