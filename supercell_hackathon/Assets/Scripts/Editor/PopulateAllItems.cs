using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Scans ALL asset packs for game-ready prefabs and assigns them to every PipeSpawner.
/// Also generates the asset list for the backend SYSTEM_PROMPT.
///
/// Run from:  Hypnagogia > Populate All Items Into Pipes
/// </summary>
public class PopulateAllItems : MonoBehaviour
{
    // ── Paths to scan for prefabs ──────────────────────────────────────
    // Each entry: (search folder relative to Assets, human label)
    static readonly (string path, string label)[] PREFAB_SOURCES = new[]
    {
        // Existing items
        ("Prefabs/Items",                                  "Items"),

        // BTM Gems & Items
        ("BTM_Assets/BTM_Items_Gems/Prefabs",             "BTM Gems"),

        // Ball Pack
        ("Ball Pack/Prefabs",                              "Ball Pack"),

        // Stylized Magic Potions (use 2K for perf)
        ("StylizedMagicPotion/Prefabs/Prefab2K",          "Potions"),

        // Survival Tools
        ("Survival Tools/Prefabs",                         "Survival Tools"),

        // Low Poly Medieval Props
        ("LowPolyMedievalPropsLite/Prefabs",              "Medieval Props"),

        // Pandazole Kitchen Food
        ("Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Prefabs",  "Kitchen Food"),

        // Pandazole Kitchen Props
        ("Pandazole_Ultimate_Pack/Pandazole Kitchen Props/Prefabs", "Kitchen Props"),

        // ithappy Furniture
        ("ithappy/Furniture_FREE/Prefabs",                 "Furniture"),

        // nappin Office
        ("nappin/OfficeEssentialsPack/Prefabs",             "Office"),

        // Blink Sport Balls
        ("Blink/Models/Sport_Balls",                       "Sport Balls"),

        // Washing Machines & Rubber Ducks
        ("Washing Machines/Prefabs",                       "Washing Machines"),

        // Japanese Mask
        ("Japanese Mask/Prefabs",                          "Japanese Mask"),

        // Flashlight
        ("Flashlight/Model",                               "Flashlight"),
    };

    // ── Skip only non-visual utility objects ──
    static readonly HashSet<string> SKIP_NAMES = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Directional Light", "Main Camera",
    };

    [MenuItem("Hypnagogia/Populate All Items Into Pipes")]
    static void PopulateAll()
    {
        string assetsPath = Application.dataPath; // .../Assets

        List<GameObject> allPrefabs = new List<GameObject>();
        HashSet<string> seenNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var (relPath, label) in PREFAB_SOURCES)
        {
            string fullDir = Path.Combine(assetsPath, relPath);
            if (!Directory.Exists(fullDir))
            {
                Debug.LogWarning($"[PopulateItems] ⚠️ Skipping {label}: folder not found at {relPath}");
                continue;
            }

            string[] prefabFiles = Directory.GetFiles(fullDir, "*.prefab", SearchOption.TopDirectoryOnly);
            int added = 0;

            foreach (string file in prefabFiles)
            {
                // Convert to Unity asset path
                string assetPath = "Assets" + file.Substring(assetsPath.Length);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null) continue;

                string prefabName = prefab.name;

                // Skip only utility objects (lights, cameras)
                if (SKIP_NAMES.Contains(prefabName)) continue;

                // Skip duplicates (first one wins)
                if (seenNames.Contains(prefabName)) continue;

                // Verify it has renderers (visual mesh)
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;

                // Verify it has materials (not missing)
                bool hasMaterials = true;
                foreach (var r in renderers)
                {
                    if (r.sharedMaterials == null || r.sharedMaterials.Length == 0)
                    {
                        hasMaterials = false;
                        break;
                    }
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (mat == null)
                        {
                            hasMaterials = false;
                            break;
                        }
                    }
                    if (!hasMaterials) break;
                }
                if (!hasMaterials)
                {
                    Debug.LogWarning($"[PopulateItems] ❌ Skipping {prefabName} — missing material");
                    continue;
                }

                allPrefabs.Add(prefab);
                seenNames.Add(prefabName);
                added++;
            }

            Debug.Log($"[PopulateItems] ✅ {label}: {added} prefabs added from {relPath}");
        }

        // Sort alphabetically for consistency
        allPrefabs.Sort((a, b) => string.Compare(a.name, b.name, true));

        // Assign to all PipeSpawners in scene
        PipeSpawner[] spawners = Object.FindObjectsByType<PipeSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            Undo.RecordObject(spawner, "Populate Items");
            spawner.itemPrefabs = allPrefabs.ToArray();
            EditorUtility.SetDirty(spawner);
        }

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        // Generate asset name list for backend
        string nameList = string.Join("\", \"", allPrefabs.Select(p => p.name));
        Debug.Log($"[PopulateItems] 🎉 DONE! {allPrefabs.Count} prefabs → {spawners.Length} pipe spawner(s)");
        Debug.Log($"[PopulateItems] 📋 Asset names for backend SYSTEM_PROMPT:\n\"{nameList}\"");

        // Also write to a file for easy copy-paste
        string outputPath = Path.Combine(assetsPath, "Scripts", "Editor", "ASSET_LIST.txt");
        File.WriteAllText(outputPath, string.Join("\n", allPrefabs.Select(p => p.name)));
        Debug.Log($"[PopulateItems] 📝 Full list written to: {outputPath}");
    }

    [MenuItem("Hypnagogia/Print Current Pipe Items")]
    static void PrintPipeItems()
    {
        PipeSpawner[] spawners = Object.FindObjectsByType<PipeSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            string names = spawner.itemPrefabs != null
                ? string.Join(", ", spawner.itemPrefabs.Where(p => p != null).Select(p => p.name))
                : "(empty)";
            Debug.Log($"[PipeItems] {spawner.name}: {spawner.itemPrefabs?.Length ?? 0} items → {names}");
        }
    }
}
