# Implementing and Mapping Your Unity Assets to Words

Your game spawns objects by **word** (e.g. `"sword"`, `"lamp"`, `"bed"`). Those words are mapped to prefabs in `Assets/Prefabs/Items`. Here’s how to use assets from your Unity account and map them to words.

---

## Quick flow (3 steps)

1. **Create item prefabs by word**  
   Menu: **Hypnagogia → Generate Item Prefabs**  
   This creates one prefab per word in `Assets/Prefabs/Items` (e.g. `sword.prefab`, `lamp.prefab`) with primitives as placeholders.

2. **Map your assets to words**  
   Menu: **Hypnagogia → Map Synty Assets to Items**  
   This replaces those prefabs with your real assets (Synty + any custom mappings you added). See “Where to add mappings” below.

3. **Assign prefabs to the spawner**  
   Menu: **Hypnagogia → Assign Items to Pipe Spawner**  
   This assigns all prefabs in `Assets/Prefabs/Items` to the PipeSpawner so spawning by word works at runtime.

---

## Where to add your asset → word mappings

Open **Assets/Scripts/Editor/SyntyAssetMapper.cs**.

- **Synty (Polygon Starter)**: Edit the `syntyMap` dictionary. Each entry is:
  - **Key**: word (e.g. `"sword"`, `"lamp"`). Must match the item names from **Generate Item Prefabs** (or add the word to `ItemPrefabGenerator` first).
  - **Value**: path to the prefab under `Assets`, e.g.  
    `"Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Sword_01.prefab"`

- **Your own packs (ithappy, Hovl, etc.)**: Edit the `customAssetMap` dictionary in the same file. Same idea: **word → prefab path**.

Example for ithappy furniture:

```csharp
static readonly Dictionary<string, string> customAssetMap = new Dictionary<string, string>()
{
    {"bed",     "Assets/ithappy/Furniture_FREE/Prefabs/bed_001.prefab"},
    {"lamp",    "Assets/ithappy/Furniture_FREE/Prefabs/lamp_001.prefab"},
    {"flower",  "Assets/ithappy/Furniture_FREE/Prefabs/flower_001.prefab"},
    {"sofa",    "Assets/ithappy/Furniture_FREE/Prefabs/sofa_001.prefab"},
    // add more word → path as needed
};
```

If a word is in both `syntyMap` and `customAssetMap`, the **custom** path wins.

---

## Finding prefab paths

- In Unity: select the prefab in the Project window; the path is shown at the bottom or in the Inspector.
- Or use the menu (paths are printed in the Console):
  - **Hypnagogia → List All Synty Prefabs** – PolygonStarter
  - **Hypnagogia → List Prefabs in ithappy**
  - **Hypnagogia → List Prefabs in BTM_Assets**
  - **Hypnagogia → List Prefabs in LowPolyMedieval**
  - **Hypnagogia → List Prefabs in nappin**
  - **Hypnagogia → List Prefabs in Pandazole**
  - **Hypnagogia → List Prefabs in Survival Tools**
  - **Hypnagogia → List Prefabs in polyperfect**
  - **Hypnagogia → List Prefabs in Assets (all)**

---

## Asset packs currently mapped (word → pack)

| Pack | Examples of words mapped |
|------|---------------------------|
| **PolygonStarter** (syntyMap) | sword, shield, coin, ladder, tree, rock, cloud, door, crate, ball, box, bomb, potion |
| **ithappy** | bed, lamp, flower, chair, table, toilet, door, box, chest, cake, burger, drink, toy, camera, closet, sofa, fridge, microwave, tv, coffee, sink, washing_machine |
| **BTM_Assets** | key, coin, bomb, shield, potion, heart, trophy, battery, star, clock, money, firstaid, skull, lock, gem |
| **LowPolyMedievalPropsLite** | fire, rock, barrel, candle, axe, hammer, jug, cup, bag, bucket, food, firewood, stairs, fence |
| **StylizedMagicPotion** | bottle |
| **polyperfect** (Halloween) | mushroom, pumpkin, lantern, book, broom, cauldron |
| **Pandazole Kitchen Food** | cheese, pizza, apple, bread, banana, stove, pan, pot, knife, plate |
| **Survival Tools** | flashlight, waterbottle, firstaid, pills, cannedfood, walkie, matchbox, tape |
| **Washing Machines** | duck, washing_machine |
| **Rocks and Boulders 2** | boulder |
| **nappin Office Essentials** | desk, wardrobe, stove, mirror, plant, printer, vending, mug |
| **Gogo Casual Pack** | vase, tablelamp |
| **Japanese Mask** | mask |

---

## Adding a new word

If the word doesn’t exist yet (e.g. `"sofa"`):

1. In **ItemPrefabGenerator.cs**, add a new `ItemDef` in the `items` list, e.g.  
   `new ItemDef("sofa", color, shape, scale),`
2. Run **Hypnagogia → Generate Item Prefabs** (creates `sofa.prefab` with a primitive).
3. In **SyntyAssetMapper.cs**, add `{"sofa", "Assets/.../sofa_001.prefab"}` to `syntyMap` or `customAssetMap`.
4. Run **Hypnagogia → Map Synty Assets to Items** (replaces the primitive with your asset).
5. Run **Hypnagogia → Assign Items to Pipe Spawner**.

---

## How it’s used at runtime

- **PipeSpawner.SpawnItem("sword")** – spawns the prefab named `sword` from `Assets/Prefabs/Items/sword.prefab`.
- **GenieClient** uses the same list when fulfilling wishes by word.

So: **word** = prefab name = file `Assets/Prefabs/Items/<word>.prefab`. The mapper’s job is to build those prefabs from your asset pack paths.
