using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Generates primitive-based prefabs for all game items.
/// Run from Hypnagogia > Generate Item Prefabs
/// </summary>
public class ItemPrefabGenerator : MonoBehaviour
{
    struct ItemDef
    {
        public string name;
        public Color color;
        public PrimitiveType shape;
        public Vector3 scale;

        public ItemDef(string n, Color c, PrimitiveType s, Vector3 sc)
        { name = n; color = c; shape = s; scale = sc; }
    }

    [MenuItem("Hypnagogia/Generate Item Prefabs")]
    static void GenerateAll()
    {
        string basePath = "Assets/Prefabs/Items";
        EnsureFolder(basePath);

        List<ItemDef> items = new List<ItemDef>
        {
            // ── WEAPONS ──
            new ItemDef("sword",   new Color(0.75f, 0.75f, 0.80f), PrimitiveType.Cube,     new Vector3(0.08f, 0.8f, 0.08f)),
            new ItemDef("shield",  new Color(0.55f, 0.35f, 0.15f), PrimitiveType.Cylinder,  new Vector3(0.5f, 0.05f, 0.5f)),
            new ItemDef("bomb",    new Color(0.15f, 0.15f, 0.15f), PrimitiveType.Sphere,    new Vector3(0.3f, 0.3f, 0.3f)),
            new ItemDef("hammer",  new Color(0.5f, 0.5f, 0.5f),    PrimitiveType.Cube,      new Vector3(0.2f, 0.15f, 0.15f)),
            new ItemDef("potion",  new Color(0.6f, 0.1f, 0.9f),    PrimitiveType.Capsule,   new Vector3(0.12f, 0.2f, 0.12f)),

            // ── FURNITURE ──
            new ItemDef("chair",   new Color(0.6f, 0.4f, 0.2f),    PrimitiveType.Cube,      new Vector3(0.4f, 0.4f, 0.4f)),
            new ItemDef("table",   new Color(0.5f, 0.35f, 0.15f),  PrimitiveType.Cube,      new Vector3(0.8f, 0.05f, 0.5f)),
            new ItemDef("bed",     new Color(0.9f, 0.85f, 0.8f),   PrimitiveType.Cube,      new Vector3(0.6f, 0.2f, 1.0f)),
            new ItemDef("toilet",  new Color(0.95f, 0.95f, 0.95f), PrimitiveType.Cylinder,  new Vector3(0.3f, 0.25f, 0.3f)),
            new ItemDef("lamp",    new Color(1.0f, 0.95f, 0.4f),   PrimitiveType.Capsule,   new Vector3(0.15f, 0.3f, 0.15f)),
            new ItemDef("door",    new Color(0.5f, 0.3f, 0.1f),    PrimitiveType.Cube,      new Vector3(0.6f, 1.2f, 0.08f)),
            new ItemDef("chest",   new Color(0.6f, 0.45f, 0.1f),   PrimitiveType.Cube,      new Vector3(0.5f, 0.35f, 0.35f)),

            // ── NATURE ──
            new ItemDef("tree",    new Color(0.2f, 0.6f, 0.15f),   PrimitiveType.Capsule,   new Vector3(0.3f, 0.6f, 0.3f)),
            new ItemDef("rock",    new Color(0.5f, 0.5f, 0.48f),   PrimitiveType.Sphere,    new Vector3(0.35f, 0.25f, 0.3f)),
            new ItemDef("mushroom",new Color(0.9f, 0.2f, 0.2f),    PrimitiveType.Capsule,   new Vector3(0.15f, 0.15f, 0.15f)),
            new ItemDef("flower",  new Color(0.95f, 0.4f, 0.6f),   PrimitiveType.Sphere,    new Vector3(0.15f, 0.15f, 0.15f)),
            new ItemDef("cloud",   new Color(0.95f, 0.95f, 1.0f),  PrimitiveType.Sphere,    new Vector3(0.6f, 0.3f, 0.4f)),
            new ItemDef("fire",    new Color(1.0f, 0.4f, 0.05f),   PrimitiveType.Capsule,   new Vector3(0.2f, 0.3f, 0.2f)),

            // ── FOOD ──
            new ItemDef("pizza",   new Color(0.95f, 0.75f, 0.2f),  PrimitiveType.Cylinder,  new Vector3(0.35f, 0.03f, 0.35f)),
            new ItemDef("burger",  new Color(0.7f, 0.45f, 0.15f),  PrimitiveType.Cylinder,  new Vector3(0.25f, 0.12f, 0.25f)),
            new ItemDef("banana",  new Color(1.0f, 0.9f, 0.2f),    PrimitiveType.Capsule,   new Vector3(0.08f, 0.15f, 0.08f)),
            new ItemDef("cheese",  new Color(1.0f, 0.85f, 0.1f),   PrimitiveType.Cube,      new Vector3(0.2f, 0.15f, 0.15f)),
            new ItemDef("cake",    new Color(0.95f, 0.7f, 0.75f),  PrimitiveType.Cylinder,  new Vector3(0.3f, 0.15f, 0.3f)),

            // ── ANIMALS ──
            new ItemDef("duck",    new Color(1.0f, 0.9f, 0.1f),    PrimitiveType.Sphere,    new Vector3(0.2f, 0.2f, 0.25f)),
            new ItemDef("spider",  new Color(0.1f, 0.1f, 0.1f),    PrimitiveType.Sphere,    new Vector3(0.15f, 0.1f, 0.15f)),
            new ItemDef("fish",    new Color(0.2f, 0.5f, 0.9f),    PrimitiveType.Capsule,   new Vector3(0.1f, 0.2f, 0.1f)),
            new ItemDef("cat",     new Color(0.9f, 0.6f, 0.2f),    PrimitiveType.Capsule,   new Vector3(0.15f, 0.2f, 0.15f)),

            // ── TOOLS ──
            new ItemDef("key",     new Color(0.85f, 0.75f, 0.15f), PrimitiveType.Cube,      new Vector3(0.05f, 0.2f, 0.1f)),
            new ItemDef("ladder",  new Color(0.6f, 0.45f, 0.2f),   PrimitiveType.Cube,      new Vector3(0.3f, 0.8f, 0.05f)),
            new ItemDef("coin",    new Color(1.0f, 0.85f, 0.0f),   PrimitiveType.Cylinder,  new Vector3(0.15f, 0.02f, 0.15f)),

            // ── SHAPES ──
            new ItemDef("box",     new Color(0.65f, 0.45f, 0.25f), PrimitiveType.Cube,      new Vector3(0.3f, 0.3f, 0.3f)),
            new ItemDef("ball",    new Color(0.9f, 0.15f, 0.15f),  PrimitiveType.Sphere,    new Vector3(0.25f, 0.25f, 0.25f)),
        };

        int count = 0;
        foreach (var item in items)
        {
            CreateItemPrefab(item, basePath);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Hypnagogia] Generated {count} item prefabs in {basePath}");

        // Auto-assign to any PipeSpawner in scene
        AssignToPipeSpawners();
    }

    [MenuItem("Hypnagogia/Assign Items to Pipe Spawner")]
    static void AssignToPipeSpawners()
    {
        string basePath = "Assets/Prefabs/Items";
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { basePath });

        List<GameObject> prefabList = new List<GameObject>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) prefabList.Add(prefab);
        }

        if (prefabList.Count == 0)
        {
            Debug.LogWarning("[Hypnagogia] No item prefabs found. Run 'Generate Item Prefabs' first!");
            return;
        }

        PipeSpawner[] spawners = Object.FindObjectsOfType<PipeSpawner>();
        if (spawners.Length == 0)
        {
            Debug.LogWarning("[Hypnagogia] No PipeSpawner found in scene! Add PipeSpawner component to your pipe first.");
            return;
        }

        foreach (var spawner in spawners)
        {
            spawner.itemPrefabs = prefabList.ToArray();
            EditorUtility.SetDirty(spawner);
        }

        Debug.Log($"[Hypnagogia] Assigned {prefabList.Count} prefabs to {spawners.Length} PipeSpawner(s).");
    }

    static void CreateItemPrefab(ItemDef item, string basePath)
    {
        // Create the primitive
        GameObject obj = GameObject.CreatePrimitive(item.shape);
        obj.name = item.name;
        obj.transform.localScale = item.scale;

        // Create material by CLONING the pipeline default (fixes grey/pink in Unity 6)
        Material mat;
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (pipeline != null && pipeline.defaultMaterial != null)
        {
            mat = new Material(pipeline.defaultMaterial);
        }
        else
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        mat.color = item.color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", item.color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", item.color);
        mat.SetFloat("_Smoothness", 0.5f);

        string matPath = $"{basePath}/Materials";
        EnsureFolder(matPath);
        string matAssetPath = $"{matPath}/Mat_{item.name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matAssetPath) != null)
            AssetDatabase.DeleteAsset(matAssetPath);
        AssetDatabase.CreateAsset(mat, matAssetPath);

        obj.GetComponent<Renderer>().sharedMaterial = mat;

        // Add Rigidbody for physics dropping
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.mass = 1f;

        // ── ADD NAMEPLATE LABEL (Built-in TextMesh - always works) ──
        float worldLabelHeight = item.scale.y * 0.5f + 0.35f;

        // Create a container that counters parent scale
        GameObject labelRoot = new GameObject("LabelRoot");
        labelRoot.transform.SetParent(obj.transform, false);
        labelRoot.transform.localPosition = new Vector3(0f, worldLabelHeight / item.scale.y, 0f);
        labelRoot.transform.localScale = new Vector3(
            1f / item.scale.x,
            1f / item.scale.y,
            1f / item.scale.z
        ) * 0.15f; // Controls overall label size in world space

        // Add TextMesh (built-in, zero dependencies)
        TextMesh textMesh = labelRoot.AddComponent<TextMesh>();
        textMesh.text = item.name.ToUpper();
        textMesh.fontSize = 100;
        textMesh.characterSize = 0.1f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyle.Bold;

        // Make sure the text renderer is visible
        MeshRenderer textRenderer = labelRoot.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            textRenderer.receiveShadows = false;
        }

        // Billboard script so label always faces camera
        labelRoot.AddComponent<BillboardLabel>();

        // Save as prefab
        string prefabPath = $"{basePath}/{item.name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);

        PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        Object.DestroyImmediate(obj);
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
