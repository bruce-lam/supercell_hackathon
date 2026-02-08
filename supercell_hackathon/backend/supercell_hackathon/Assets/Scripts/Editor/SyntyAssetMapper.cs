using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Maps Unity asset prefabs (Synty, ithappy, or any pack) to game item names (words).
/// Creates prefabs in Assets/Prefabs/Items with Rigidbody, Collider, and labels.
/// Run from Hypnagogia > Map Synty Assets to Items (processes both Synty and Custom maps).
/// </summary>
public class SyntyAssetMapper : MonoBehaviour
{
    // ── MAPPING: word (item name) → prefab path ──
    // These get turned into Assets/Prefabs/Items/{word}.prefab for PipeSpawner/SpawnItem(name).
    static readonly Dictionary<string, string> syntyMap = new Dictionary<string, string>()
    {
        // WEAPONS
        {"sword",   "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Sword_01.prefab"},
        {"shield",  "Assets/PolygonStarter/Prefabs/SM_Wep_Shield_04.prefab"},
        {"bomb",    "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Primitive_Sphere_01P.prefab"},

        // TOOLS
        {"coin",    "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Icon_Coin_01.prefab"},
        {"ladder",  "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Ladder_1x2_01P.prefab"},
        {"potion",  "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Primitive_Cone_01P.prefab"},

        // NATURE
        {"tree",    "Assets/PolygonStarter/Prefabs/SM_Generic_Tree_01.prefab"},
        {"rock",    "Assets/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_01.prefab"},
        {"cloud",   "Assets/PolygonStarter/Prefabs/SM_Generic_CloudRing_01.prefab"},

        // FURNITURE (Synty – overridden by ithappy in customAssetMap where we have both)
        {"door",    "Assets/PolygonStarter/Prefabs/SM_Bld_Door_01.prefab"},
        {"crate",   "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Crate_03.prefab"},

        // SHAPES
        {"ball",    "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Primitive_Sphere_01P.prefab"},
        {"box",     "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Buildings_Block_1x1_01P.prefab"},
    };

    // ── CUSTOM ASSETS: all your packs (custom overrides synty for same word) ──
    static readonly Dictionary<string, string> customAssetMap = new Dictionary<string, string>()
    {
        // ── ithappy Furniture FREE ──
        {"bed",      "Assets/ithappy/Furniture_FREE/Prefabs/bed_001.prefab"},
        {"lamp",     "Assets/ithappy/Furniture_FREE/Prefabs/lamp_001.prefab"},
        {"flower",   "Assets/ithappy/Furniture_FREE/Prefabs/flower_001.prefab"},
        {"chair",    "Assets/ithappy/Furniture_FREE/Prefabs/lounge_chair_001.prefab"},
        {"table",    "Assets/ithappy/Furniture_FREE/Prefabs/kitchen_table_001.prefab"},
        {"toilet",   "Assets/ithappy/Furniture_FREE/Prefabs/bathroom_item_001.prefab"},
        {"door",     "Assets/ithappy/Furniture_FREE/Prefabs/door_001.prefab"},
        {"box",      "Assets/ithappy/Furniture_FREE/Prefabs/box_001.prefab"},
        {"chest",    "Assets/ithappy/Furniture_FREE/Prefabs/dresser_001.prefab"},
        {"cake",     "Assets/ithappy/Furniture_FREE/Prefabs/dish_001.prefab"},
        {"burger",   "Assets/ithappy/Furniture_FREE/Prefabs/dish_002.prefab"},
        {"drink",    "Assets/ithappy/Furniture_FREE/Prefabs/drink_001.prefab"},
        {"toy",      "Assets/ithappy/Furniture_FREE/Prefabs/toy_001.prefab"},
        {"camera",   "Assets/ithappy/Furniture_FREE/Prefabs/camera_001.prefab"},
        {"closet",   "Assets/ithappy/Furniture_FREE/Prefabs/closet_001.prefab"},
        {"sofa",     "Assets/ithappy/Furniture_FREE/Prefabs/sofa_001.prefab"},
        {"fridge",   "Assets/ithappy/Furniture_FREE/Prefabs/fridge_001.prefab"},
        {"microwave","Assets/ithappy/Furniture_FREE/Prefabs/microwave_oven_001.prefab"},
        {"tv",       "Assets/ithappy/Furniture_FREE/Prefabs/tv_wall_001.prefab"},
        {"coffee",   "Assets/ithappy/Furniture_FREE/Prefabs/coffee_machine_001.prefab"},
        {"sink",     "Assets/ithappy/Furniture_FREE/Prefabs/kitchen_sink_001.prefab"},
        {"washing_machine","Assets/ithappy/Furniture_FREE/Prefabs/washing_machine_001.prefab"},

        // ── BTM_Assets (gems, items) ──
        {"key",      "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Key.prefab"},
        {"coin",     "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Coin.prefab"},
        {"bomb",     "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Bomb.prefab"},
        {"shield",   "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Shield.prefab"},
        {"potion",   "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/BluePotion.prefab"},
        {"heart",    "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Heart.prefab"},
        {"trophy",   "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Trophy.prefab"},
        {"battery",  "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Battery.prefab"},
        {"star",     "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Star.prefab"},
        {"clock",    "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Clock.prefab"},
        {"money",    "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Money.prefab"},
        {"firstaid", "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/FirstAid.prefab"},
        {"skull",    "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Skull.prefab"},
        {"lock",     "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Lock.prefab"},
        {"gem",      "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/SphereGem.prefab"},

        // ── LowPolyMedievalPropsLite ──
        {"fire",     "Assets/LowPolyMedievalPropsLite/Prefabs/Fire_01.prefab"},
        {"rock",     "Assets/LowPolyMedievalPropsLite/Prefabs/Stone_01.prefab"},
        {"barrel",   "Assets/LowPolyMedievalPropsLite/Prefabs/Barrel_01.prefab"},
        {"candle",   "Assets/LowPolyMedievalPropsLite/Prefabs/Candle_01.prefab"},
        {"axe",      "Assets/LowPolyMedievalPropsLite/Prefabs/Axe_01.prefab"},
        {"hammer",   "Assets/LowPolyMedievalPropsLite/Prefabs/Axe_01.prefab"},
        {"jug",      "Assets/LowPolyMedievalPropsLite/Prefabs/Jug_01.prefab"},
        {"cup",      "Assets/LowPolyMedievalPropsLite/Prefabs/Cup_01.prefab"},
        {"bag",      "Assets/LowPolyMedievalPropsLite/Prefabs/Bag_01.prefab"},
        {"bucket",   "Assets/LowPolyMedievalPropsLite/Prefabs/Bucket_01.prefab"},
        {"food",     "Assets/LowPolyMedievalPropsLite/Prefabs/Food_01.prefab"},
        {"firewood", "Assets/LowPolyMedievalPropsLite/Prefabs/Firewood_01.prefab"},
        {"stairs",   "Assets/LowPolyMedievalPropsLite/Prefabs/Stairs_01.prefab"},
        {"fence",    "Assets/LowPolyMedievalPropsLite/Prefabs/Fence_01.prefab"},

        // ── StylizedMagicPotion (bottles) ──
        {"bottle",   "Assets/StylizedMagicPotion/Prefabs/Prefab4K/Bottle1_4K.prefab"},

        // ── polyperfect (Halloween / nature) ──
        {"mushroom", "Assets/polyperfect/Poly Halloween/Prefabs/Halloween/Mushroom_Magical_Amanita_Mature.prefab"},
        {"pumpkin",  "Assets/polyperfect/Poly Halloween/Prefabs/Halloween/Pumpkin_Carved_A.prefab"},
        {"lantern",  "Assets/polyperfect/Poly Halloween/Prefabs/Halloween/Lantern_Street_Fixed_Halloween.prefab"},
        {"book",     "Assets/polyperfect/Poly Halloween/Prefabs/Halloween/Book_Evil_Open.prefab"},
        {"broom",    "Assets/polyperfect/Poly Halloween/Prefabs/Halloween/Broom_Old.prefab"},
        {"cauldron", "Assets/polyperfect/Poly Halloween/Prefabs/Halloween/Cauldron_Potion_Full.prefab"},

        // ── Pandazole Kitchen Food ──
        {"cheese",   "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Food_Cheese.prefab"},
        {"pizza",    "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Prop_PizzaCutter.prefab"},
        {"apple",    "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Food_Apple.prefab"},
        {"bread",    "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Food_Bread.prefab"},
        {"banana",   "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Food_Apple.prefab"},
        {"stove",    "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Prop_Stove.prefab"},
        {"pan",      "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Prop_Pan_01.prefab"},
        {"pot",      "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Prop_Pot_03.prefab"},
        {"knife",    "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Prop_Knife_01.prefab"},
        {"plate",    "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs/Prop_Plate_01.prefab"},

        // ── Survival Tools ──
        {"flashlight","Assets/Survival Tools/Prefabs/flashlight.prefab"},
        {"waterbottle","Assets/Survival Tools/Prefabs/waterbottle.prefab"},
        {"firstaid", "Assets/Survival Tools/Prefabs/firstaid.prefab"},
        {"pills",    "Assets/Survival Tools/Prefabs/pills.prefab"},
        {"cannedfood","Assets/Survival Tools/Prefabs/cannedfood.prefab"},
        {"walkie",   "Assets/Survival Tools/Prefabs/walkie.prefab"},
        {"matchbox", "Assets/Survival Tools/Prefabs/matchbox.prefab"},
        {"tape",     "Assets/Survival Tools/Prefabs/tape.prefab"},

        // ── Washing Machines + Rubber Duck ──
        {"duck",     "Assets/Washing Machines/Prefabs/RubberDuck_Model01_C1_001.prefab"},
        {"washing_machine","Assets/Washing Machines/Prefabs/WashingMachine_Model01_C1_001.prefab"},

        // ── Rocks and Boulders 2 (alternate rock) ──
        {"boulder",  "Assets/Rocks and Boulders 2/Rocks/Prefabs/Rock2.prefab"},

        // ── nappin Office Essentials ──
        {"desk",     "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Desk.prefab"},
        {"wardrobe", "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Wardrobe.prefab"},
        {"stove",    "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Stove.prefab"},
        {"mirror",   "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mirror1.prefab"},
        {"plant",    "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)DeskPlant.prefab"},
        {"printer",  "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Printer.prefab"},
        {"vending",  "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)VendingMachine.prefab"},
        {"mug",      "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)Mug.prefab"},

        // ── Gogo Casual Pack (plants & lamps) ──
        {"vase",     "Assets/Gogo Casual Pack/Gogo Casual Free Plants Pack/Prefabs/Decoration_Vase_02_03.prefab"},
        {"tablelamp","Assets/Gogo Casual Pack/Gogo Casual Free Light Pack/Prefabs/Decoration_Light_TableLamp_01_01.prefab"},

        // ── Japanese Mask ──
        {"mask",     "Assets/Japanese Mask/Prefabs/Kitsune.prefab"},
    };

    [MenuItem("Hypnagogia/Map Synty Assets to Items")]
    static void MapSyntyAssets()
    {
        string itemsFolder = "Assets/Prefabs/Items";
        if (!AssetDatabase.IsValidFolder(itemsFolder))
        {
            Debug.LogError("[SyntyMapper] Items folder not found! Run 'Generate Item Prefabs' first.");
            return;
        }

        // Merge syntyMap + customAssetMap (custom overrides Synty for same word)
        var combined = new Dictionary<string, string>(syntyMap);
        foreach (var kvp in customAssetMap)
            combined[kvp.Key] = kvp.Value;

        int upgraded = 0;
        int kept = 0;

        foreach (var kvp in combined)
        {
            string itemName = kvp.Key;
            string syntyPath = kvp.Value;

            // Load the Synty prefab
            GameObject syntyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(syntyPath);
            if (syntyPrefab == null)
            {
                Debug.LogWarning($"[SyntyMapper] Synty prefab not found: {syntyPath}");
                kept++;
                continue;
            }

            // Create a wrapper prefab with the correct item name
            string outputPath = $"{itemsFolder}/{itemName}.prefab";

            // Instantiate the Synty model
            GameObject wrapper = (GameObject)PrefabUtility.InstantiatePrefab(syntyPrefab);
            wrapper.name = itemName;

            // ── PHYSICS ──
            // Add Rigidbody if missing
            if (wrapper.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = wrapper.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.linearDamping = 0.5f;
            }

            // Add collider if missing (box collider that fits the mesh)
            if (wrapper.GetComponent<Collider>() == null)
            {
                // Try to auto-fit a BoxCollider around the renderers
                Renderer[] renderers = wrapper.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);

                    BoxCollider bc = wrapper.AddComponent<BoxCollider>();
                    bc.center = wrapper.transform.InverseTransformPoint(bounds.center);
                    bc.size = bounds.size;
                }
                else
                {
                    wrapper.AddComponent<BoxCollider>();
                }
            }

            // ── LABEL ──
            // Add a TextMesh label (floating above the object)
            Transform existingLabel = wrapper.transform.Find("Label");
            if (existingLabel == null)
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(wrapper.transform);
                labelObj.transform.localPosition = Vector3.up * 0.5f;
                labelObj.transform.localRotation = Quaternion.identity;

                TextMesh tm = labelObj.AddComponent<TextMesh>();
                tm.text = itemName.ToUpper();
                tm.fontSize = 48;
                tm.characterSize = 0.05f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;
                tm.fontStyle = FontStyle.Bold;

                // Billboard component so text always faces the camera
                labelObj.AddComponent<BillboardLabel>();
            }

            // ── SCALE ADJUSTMENT ──
            // Some Synty models are huge, normalize them
            Renderer[] allRenderers = wrapper.GetComponentsInChildren<Renderer>();
            if (allRenderers.Length > 0)
            {
                Bounds totalBounds = allRenderers[0].bounds;
                for (int i = 1; i < allRenderers.Length; i++)
                    totalBounds.Encapsulate(allRenderers[i].bounds);

                float maxDim = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);
                
                // Target size: normalize so items look good when spawned
                float targetSize = 0.4f;
                if (itemName == "tree" || itemName == "ladder" || itemName == "door" || itemName == "fridge" || itemName == "closet" || itemName == "tv" || itemName == "wardrobe" || itemName == "washing_machine" || itemName == "vending" || itemName == "stairs" || itemName == "fence")
                    targetSize = 0.8f;
                else if (itemName == "bed" || itemName == "sofa" || itemName == "table" || itemName == "desk" || itemName == "barrel" || itemName == "boulder" || itemName == "cauldron")
                    targetSize = 0.6f;
                else if (itemName == "rock" || itemName == "coin" || itemName == "flower" || itemName == "lamp" || itemName == "drink" || itemName == "toy" || itemName == "camera" || itemName == "key" || itemName == "heart" || itemName == "star" || itemName == "gem" || itemName == "skull" || itemName == "candle" || itemName == "mug" || itemName == "cup" || itemName == "pills" || itemName == "lock" || itemName == "battery" || itemName == "duck" || itemName == "mask")
                    targetSize = 0.25f;
                else if (itemName == "chair" || itemName == "chest" || itemName == "toilet" || itemName == "box" || itemName == "sink" || itemName == "microwave" || itemName == "coffee" || itemName == "bucket" || itemName == "jug" || itemName == "axe" || itemName == "hammer" || itemName == "bottle" || itemName == "potion" || itemName == "flashlight" || itemName == "waterbottle" || itemName == "firstaid" || itemName == "trophy" || itemName == "clock" || itemName == "lantern" || itemName == "book" || itemName == "broom" || itemName == "vase" || itemName == "tablelamp" || itemName == "plant" || itemName == "printer" || itemName == "mirror")
                    targetSize = 0.35f;
                else if (itemName == "fire" || itemName == "pumpkin" || itemName == "food" || itemName == "bag" || itemName == "plate" || itemName == "pan" || itemName == "pot" || itemName == "knife" || itemName == "cannedfood" || itemName == "walkie" || itemName == "matchbox" || itemName == "tape" || itemName == "firewood" || itemName == "stove" || itemName == "money")
                    targetSize = 0.3f;

                if (maxDim > 0.01f)
                {
                    float scaleFactor = targetSize / maxDim;
                    wrapper.transform.localScale *= scaleFactor;
                }
            }

            // ── SAVE PREFAB ──
            // Delete old prefab first
            if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
                AssetDatabase.DeleteAsset(outputPath);

            PrefabUtility.SaveAsPrefabAsset(wrapper, outputPath);
            Object.DestroyImmediate(wrapper);

            upgraded++;
            Debug.Log($"[SyntyMapper] ✅ {itemName} → {syntyPrefab.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SyntyMapper] Done! Upgraded {upgraded} items to Synty models. {kept} kept as primitives.");
        Debug.Log("[SyntyMapper] Now run 'Hypnagogia > Assign Items to Pipe Spawner' to update the PipeSpawner.");
    }

    [MenuItem("Hypnagogia/List All Synty Prefabs")]
    static void ListSyntyPrefabs()
    {
        ListPrefabsInFolder("Assets/PolygonStarter/Prefabs", "Synty");
    }

    [MenuItem("Hypnagogia/List Prefabs in ithappy")]
    static void ListIthappyPrefabs()
    {
        ListPrefabsInFolder("Assets/ithappy", "ithappy");
    }

    [MenuItem("Hypnagogia/List Prefabs in Assets (all)")]
    static void ListAllPrefabs()
    {
        ListPrefabsInFolder("Assets", "Assets");
    }

    [MenuItem("Hypnagogia/List Prefabs in BTM_Assets")]
    static void ListBTMPrefabs() { ListPrefabsInFolder("Assets/BTM_Assets", "BTM_Assets"); }
    [MenuItem("Hypnagogia/List Prefabs in LowPolyMedieval")]
    static void ListLowPolyPrefabs() { ListPrefabsInFolder("Assets/LowPolyMedievalPropsLite", "LowPolyMedieval"); }
    [MenuItem("Hypnagogia/List Prefabs in nappin")]
    static void ListNappinPrefabs() { ListPrefabsInFolder("Assets/nappin", "nappin"); }
    [MenuItem("Hypnagogia/List Prefabs in Pandazole")]
    static void ListPandazolePrefabs() { ListPrefabsInFolder("Assets/Pandazole_Ultimate_Pack", "Pandazole"); }
    [MenuItem("Hypnagogia/List Prefabs in Survival Tools")]
    static void ListSurvivalPrefabs() { ListPrefabsInFolder("Assets/Survival Tools", "Survival Tools"); }
    [MenuItem("Hypnagogia/List Prefabs in polyperfect")]
    static void ListPolyperfectPrefabs() { ListPrefabsInFolder("Assets/polyperfect", "polyperfect"); }

    static void ListPrefabsInFolder(string folder, string label)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        Debug.Log($"[AssetMapper] Found {guids.Length} prefabs under {label}:");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"  → {path}");
        }
    }
}
