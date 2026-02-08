using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool: Hypnagogia > Generate Gem Prefabs
/// Creates named gem variant prefabs (diamond, ruby, emerald, etc.) from
/// existing RPG CRYSTALS prefabs with gem-appropriate material colors.
/// </summary>
public class GenerateGemPrefabs : MonoBehaviour
{
    // Each gem definition: name, base crystal prefab index, color, emission color, metallic, smoothness
    struct GemDef
    {
        public string name;
        public int crystalIndex;  // Which Crystal_N to use as base shape
        public Color color;
        public Color emission;
        public float metallic;
        public float smoothness;

        public GemDef(string n, int idx, Color c, Color e, float m, float s)
        {
            name = n; crystalIndex = idx; color = c; emission = e; metallic = m; smoothness = s;
        }
    }

    [MenuItem("Hypnagogia/Generate Gem Prefabs")]
    static void Generate()
    {
        string crystalPath = "Assets/RPG CRYSTALS/URP/Prefab URP";
        string outputPath = "Assets/Prefabs/Items";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        // Gem definitions — carefully chosen crystal shapes + real gem colors
        GemDef[] gems = new GemDef[]
        {
            // Classic precious gems
            new GemDef("diamond",       1,  new Color(0.85f, 0.92f, 1.0f, 0.8f),   new Color(0.7f, 0.85f, 1.0f) * 1.5f,    0.9f, 1.0f),
            new GemDef("ruby",          3,  new Color(0.85f, 0.05f, 0.1f, 0.85f),   new Color(1.0f, 0.1f, 0.15f) * 1.2f,    0.7f, 0.95f),
            new GemDef("emerald",       5,  new Color(0.05f, 0.7f, 0.2f, 0.85f),    new Color(0.1f, 0.9f, 0.3f) * 1.0f,     0.6f, 0.9f),
            new GemDef("sapphire",      7,  new Color(0.05f, 0.1f, 0.85f, 0.85f),   new Color(0.1f, 0.15f, 1.0f) * 1.2f,    0.7f, 0.95f),
            new GemDef("amethyst",      9,  new Color(0.55f, 0.15f, 0.8f, 0.85f),   new Color(0.6f, 0.2f, 0.9f) * 1.0f,     0.5f, 0.85f),
            new GemDef("topaz",         2,  new Color(1.0f, 0.7f, 0.1f, 0.85f),     new Color(1.0f, 0.8f, 0.2f) * 1.0f,     0.6f, 0.9f),

            // Semi-precious & exotic
            new GemDef("obsidian",      4,  new Color(0.05f, 0.05f, 0.08f, 0.95f),  new Color(0.15f, 0.1f, 0.2f) * 0.5f,    0.95f, 0.98f),
            new GemDef("opal",          6,  new Color(0.9f, 0.85f, 0.95f, 0.7f),    new Color(0.8f, 0.6f, 1.0f) * 1.3f,     0.4f, 0.85f),
            new GemDef("jade",          8,  new Color(0.2f, 0.55f, 0.25f, 0.9f),    new Color(0.3f, 0.6f, 0.3f) * 0.7f,     0.3f, 0.7f),
            new GemDef("onyx",          10, new Color(0.03f, 0.03f, 0.03f, 0.95f),  new Color(0.05f, 0.05f, 0.05f) * 0.3f,  0.85f, 0.95f),
            new GemDef("citrine",       11, new Color(1.0f, 0.6f, 0.05f, 0.85f),    new Color(1.0f, 0.65f, 0.1f) * 1.0f,    0.5f, 0.88f),
            new GemDef("garnet",        14, new Color(0.5f, 0.02f, 0.08f, 0.9f),    new Color(0.6f, 0.05f, 0.1f) * 0.8f,    0.6f, 0.9f),
            new GemDef("turquoise",     12, new Color(0.2f, 0.8f, 0.75f, 0.85f),    new Color(0.25f, 0.9f, 0.85f) * 1.0f,   0.3f, 0.7f),
            new GemDef("aquamarine",    15, new Color(0.3f, 0.75f, 0.9f, 0.8f),     new Color(0.35f, 0.8f, 1.0f) * 1.1f,    0.5f, 0.88f),
            new GemDef("moonstone",     13, new Color(0.8f, 0.82f, 0.88f, 0.7f),    new Color(0.7f, 0.75f, 0.95f) * 1.2f,   0.4f, 0.8f),

            // Rare & magical
            new GemDef("bloodstone",    16, new Color(0.15f, 0.25f, 0.1f, 0.9f),    new Color(0.6f, 0.05f, 0.05f) * 0.6f,   0.5f, 0.75f),
            new GemDef("sunstone",      17, new Color(1.0f, 0.45f, 0.1f, 0.85f),    new Color(1.0f, 0.5f, 0.15f) * 1.5f,    0.6f, 0.85f),
            new GemDef("lapis_lazuli",  18, new Color(0.1f, 0.15f, 0.6f, 0.9f),     new Color(0.15f, 0.2f, 0.7f) * 0.8f,    0.3f, 0.7f),
            new GemDef("rose_quartz",   19, new Color(0.95f, 0.6f, 0.7f, 0.8f),     new Color(1.0f, 0.6f, 0.75f) * 1.0f,    0.3f, 0.8f),
            new GemDef("alexandrite",   20, new Color(0.2f, 0.6f, 0.5f, 0.85f),     new Color(0.4f, 0.2f, 0.7f) * 1.2f,     0.7f, 0.92f),
        };

        int created = 0;
        int skipped = 0;

        foreach (var gem in gems)
        {
            string outputFile = $"{outputPath}/{gem.name}.prefab";

            // Skip if already exists
            if (File.Exists(outputFile))
            {
                Debug.Log($"[GemGen] Skipping {gem.name} — already exists");
                skipped++;
                continue;
            }

            // Load source crystal prefab
            string crystalFile = $"{crystalPath}/Crystal_{gem.crystalIndex}.prefab";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(crystalFile);
            if (source == null)
            {
                Debug.LogWarning($"[GemGen] Crystal_{gem.crystalIndex} not found for {gem.name}, trying Crystal_1");
                source = AssetDatabase.LoadAssetAtPath<GameObject>($"{crystalPath}/Crystal_1.prefab");
                if (source == null)
                {
                    Debug.LogError($"[GemGen] Could not find any crystal prefab for {gem.name}!");
                    continue;
                }
            }

            // Instantiate, rename, and create material variant
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = gem.name;

            // Create gem-colored material
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                // Create a unique material for this gem
                Material gemMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (rend.sharedMaterial != null)
                    gemMat = new Material(rend.sharedMaterial);

                // Apply gem colors
                gemMat.name = $"{gem.name}_Material";

                // Base color
                gemMat.color = gem.color;
                if (gemMat.HasProperty("_BaseColor"))
                    gemMat.SetColor("_BaseColor", gem.color);

                // Emission (inner glow)
                gemMat.EnableKeyword("_EMISSION");
                gemMat.SetColor("_EmissionColor", gem.emission);

                // Surface properties
                if (gemMat.HasProperty("_Metallic"))
                    gemMat.SetFloat("_Metallic", gem.metallic);
                if (gemMat.HasProperty("_Smoothness"))
                    gemMat.SetFloat("_Smoothness", gem.smoothness);

                // Transparency for gem-like appearance
                if (gem.color.a < 1.0f)
                {
                    gemMat.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
                    gemMat.SetFloat("_Blend", 0);   // Alpha blend
                    gemMat.SetOverrideTag("RenderType", "Transparent");
                    gemMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    gemMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    gemMat.SetInt("_ZWrite", 0);
                    gemMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }

                // Save material as asset
                string matDir = $"{outputPath}/Materials";
                if (!Directory.Exists(matDir))
                    Directory.CreateDirectory(matDir);

                string matPath = $"{matDir}/{gem.name}_Material.mat";
                AssetDatabase.CreateAsset(gemMat, matPath);

                // Apply to renderer
                Material savedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Material[] mats = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = savedMat;
                rend.sharedMaterials = mats;
            }

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(instance, outputFile);
            DestroyImmediate(instance);

            Debug.Log($"[GemGen] ✨ Created: {gem.name} (from Crystal_{gem.crystalIndex})");
            created++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[GemGen] Done! Created {created} gem prefabs, skipped {skipped} existing.");
        EditorUtility.DisplayDialog("Gem Prefabs Generated",
            $"Created {created} gem prefabs\nSkipped {skipped} (already existed)\n\nGems are in:\n{outputPath}/",
            "OK");
    }
}
