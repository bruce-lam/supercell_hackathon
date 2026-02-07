using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Maps Synty Polygon Starter Pack prefabs to game item names.
/// Creates upgraded prefabs with Rigidbody, Collider, and labels.
/// Run from Hypnagogia > Map Synty Assets to Items
/// </summary>
public class SyntyAssetMapper : MonoBehaviour
{
    // ── MAPPING: item name → Synty prefab path ──
    // Items without a Synty match keep their existing primitive prefab.
    static readonly Dictionary<string, string> syntyMap = new Dictionary<string, string>()
    {
        // WEAPONS
        {"sword",   "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Sword_01.prefab"},
        {"shield",  "Assets/PolygonStarter/Prefabs/SM_Wep_Shield_04.prefab"},

        // TOOLS
        {"coin",    "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Icon_Coin_01.prefab"},
        {"ladder",  "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Ladder_1x2_01P.prefab"},

        // NATURE
        {"tree",    "Assets/PolygonStarter/Prefabs/SM_Generic_Tree_01.prefab"},
        {"rock",    "Assets/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_01.prefab"},
        {"cloud",   "Assets/PolygonStarter/Prefabs/SM_Generic_CloudRing_01.prefab"},

        // FURNITURE
        {"door",    "Assets/PolygonStarter/Prefabs/SM_Bld_Door_01.prefab"},
        {"crate",   "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Crate_03.prefab"},

        // SHAPES  
        {"ball",    "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Primitive_Sphere_01P.prefab"},
        {"box",     "Assets/PolygonStarter/Prefabs/SM_PolygonPrototype_Buildings_Block_1x1_01P.prefab"},
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

        int upgraded = 0;
        int kept = 0;

        foreach (var kvp in syntyMap)
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
                
                // Target size: ~0.3m for small items, ~0.7m for large items
                float targetSize = 0.4f;
                if (itemName == "tree" || itemName == "ladder" || itemName == "door")
                    targetSize = 0.8f;
                else if (itemName == "rock" || itemName == "coin")
                    targetSize = 0.25f;

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
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PolygonStarter/Prefabs" });
        Debug.Log($"[SyntyMapper] Found {guids.Length} Synty prefabs:");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"  → {path}");
        }
    }
}
