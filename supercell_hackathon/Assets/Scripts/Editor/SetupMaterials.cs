using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using EasyDoorSystem;

public class SetupMaterials : MonoBehaviour
{
    [MenuItem("Hypnagogia/Setup Room (Rebuild)")]
    static void SetupRoom()
    {
        // ── MATERIALS ──
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // 1. CHECKERED FLOOR — High contrast dungeon tiles
        Texture2D checkTex = GenerateCheckeredTexture(512, 
            new Color(0.06f, 0.06f, 0.10f),  // Almost black
            new Color(0.25f, 0.20f, 0.30f));  // Purple-grey (visible contrast!)
        SaveTexturePNG(checkTex, "Assets/Materials/T_Checkered.png");

        Material floorMat = CreateMat("FloorMaterial", Color.white, 0.7f);
        Texture2D savedFloorTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/T_Checkered.png");
        if (savedFloorTex != null)
        {
            if (floorMat.HasProperty("_BaseMap")) floorMat.SetTexture("_BaseMap", savedFloorTex);
            floorMat.mainTexture = savedFloorTex;
            floorMat.mainTextureScale = new Vector2(6f, 6f);
            if (floorMat.HasProperty("_BaseMap"))
                floorMat.SetTextureScale("_BaseMap", new Vector2(6f, 6f));
        }
        // Make floor slightly metallic for reflections
        if (floorMat.HasProperty("_Metallic")) floorMat.SetFloat("_Metallic", 0.15f);

        // 2. STONE WALL — Bold dungeon bricks with deep mortar
        Texture2D wallTex = GenerateStoneWallTexture(512);
        SaveTexturePNG(wallTex, "Assets/Materials/T_StoneWall.png");

        Material wallMat = CreateMat("WallMaterial", new Color(0.55f, 0.45f, 0.6f), 0.1f); // Brighter purple stone
        Texture2D savedWallTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/T_StoneWall.png");
        if (savedWallTex != null)
        {
            if (wallMat.HasProperty("_BaseMap")) wallMat.SetTexture("_BaseMap", savedWallTex);
            wallMat.mainTexture = savedWallTex;
            wallMat.mainTextureScale = new Vector2(2f, 2f); // Bigger bricks
            if (wallMat.HasProperty("_BaseMap"))
                wallMat.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
        }

        // 3. CEILING — Dark stone with faint texture
        Material ceilMat = CreateMat("CeilingMaterial", new Color(0.08f, 0.06f, 0.10f), 0.02f);
        if (savedWallTex != null)
        {
            if (ceilMat.HasProperty("_BaseMap")) ceilMat.SetTexture("_BaseMap", savedWallTex);
            ceilMat.mainTexture = savedWallTex;
        }

        AssetDatabase.SaveAssets();

        // ── APPLY MATERIALS TO ALL ROOM OBJECTS ──
        int floorCount = 0, wallCount = 0;
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name.ToLower();

            if (n.Contains("floor") || n == "plane")
            {
                r.sharedMaterial = floorMat;
                floorCount++;
            }
            else if (n.StartsWith("wall") || n.StartsWith("cube"))
            {
                r.sharedMaterial = wallMat;
                wallCount++;
            }
        }
        Debug.Log($"[Hypnagogia] Applied materials: {floorCount} floor, {wallCount} wall renderers");

        // ── CEILING ──
        GameObject existingCeil = GameObject.Find("Ceiling");
        if (existingCeil != null) Object.DestroyImmediate(existingCeil);
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.position = new Vector3(0f, 4.5f, 0f);
        ceiling.transform.localScale = new Vector3(10.5f, 0.15f, 10.5f);
        ceiling.GetComponent<Renderer>().sharedMaterial = ceilMat;

        // ── GLOWING RUG (circle under pipe) ──
        GameObject existingRug = GameObject.Find("MagicRug");
        if (existingRug != null) Object.DestroyImmediate(existingRug);
        GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rug.name = "MagicRug";
        rug.transform.position = new Vector3(0f, 0.02f, 0f);
        rug.transform.localScale = new Vector3(3f, 0.02f, 3f);
        // Remove collider so player can walk on it
        Object.DestroyImmediate(rug.GetComponent<Collider>());
        Material rugMat = CreateMat("RugMaterial", new Color(0.4f, 0.15f, 0.6f), 0.8f); // Purple glow
        if (rugMat.HasProperty("_EmissionColor"))
        {
            rugMat.EnableKeyword("_EMISSION");
            rugMat.SetColor("_EmissionColor", new Color(0.3f, 0.1f, 0.5f) * 1.5f);
        }
        rug.GetComponent<Renderer>().sharedMaterial = rugMat;

        // ── LIGHTING ──
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
            if (l.gameObject.name.Contains("Mood")) Object.DestroyImmediate(l.gameObject);
        }

        // Spotlight under pipe (Drama!)
        GameObject spotObj = new GameObject("MoodLight_Spot");
        spotObj.transform.position = new Vector3(0f, 4.4f, 0f);
        spotObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Light spot = spotObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 12f;
        spot.spotAngle = 50f;
        spot.intensity = 25f;
        spot.color = new Color(0.9f, 0.8f, 1f); // Slight purple tint
        spot.shadows = LightShadows.Soft;

        // Ambient Corner Lights (Magic feel)
        CreatePointLight("MoodLight_Cyan", new Vector3(-4f, 2.5f, 4f), new Color(0f, 0.7f, 1f), 5f);
        CreatePointLight("MoodLight_Pink", new Vector3(4f, 2.5f, 4f), new Color(1f, 0.2f, 0.8f), 5f);
        CreatePointLight("MoodLight_Warm", new Vector3(0f, 3f, -4f), new Color(1f, 0.6f, 0.2f), 4f);
        CreatePointLight("MoodLight_Floor", new Vector3(0f, 0.5f, 0f), new Color(0.5f, 0.2f, 1f), 3f); // Under-rug glow

        // ── PROPS ──
        PlaceProp("Table", "table", new Vector3(-3.5f, 0f, 3.5f), new Vector3(0, 45, 0));
        PlaceProp("Chair", "chair", new Vector3(-2.5f, 0f, 2.5f), new Vector3(0, -135, 0));
        PlaceProp("Lamp", "lamp", new Vector3(-3.8f, 1.2f, 3.8f), Vector3.zero);
        PlaceProp("Chest", "chest", new Vector3(3.5f, 0f, 3.5f), new Vector3(0, -45, 0));

        MarkDirty();
        AssetDatabase.SaveAssets();
        Debug.Log("[Hypnagogia] ✨ Room complete: Checkered floor, Stone walls, Ceiling, Magic rug, Mood lighting, Props!");
    }

    // ── TEXTURE GENERATORS ──

    static Texture2D GenerateCheckeredTexture(int size, Color dark, Color light)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        int tileSize = size / 8; // 8x8 grid
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                bool isLight = ((x / tileSize) + (y / tileSize)) % 2 == 0;
                tex.SetPixel(x, y, isLight ? light : dark);
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenerateStoneWallTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color mortarColor = new Color(0.05f, 0.03f, 0.07f); // Very dark mortar (deep grooves)
        int mortarWidth = 6; // Thick visible mortar lines
        int brickH = size / 6; // Bigger bricks (6 rows)
        int brickW = size / 3; // 3 columns

        // Per-brick color seeds (make each brick unique)
        System.Random rng = new System.Random(42);
        float[,] brickHue = new float[12, 6];
        for (int r = 0; r < 12; r++)
            for (int c = 0; c < 6; c++)
                brickHue[r, c] = 0.7f + (float)rng.NextDouble() * 0.5f;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                int row = y / brickH;
                int offset = (row % 2 == 0) ? 0 : brickW / 2;
                int localX = (x + offset) % brickW;
                int localY = y % brickH;
                int col = ((x + offset) / brickW) % 6;

                // Thick mortar grooves
                if (localX < mortarWidth || localY < mortarWidth) {
                    // Add slight dimple variation to mortar
                    float mv = 0.9f + ((x * 3 + y * 7) % 20) / 200f;
                    tex.SetPixel(x, y, mortarColor * mv);
                } else {
                    // Each brick gets a unique base color
                    float bh = brickHue[row % 12, col % 6];
                    // Stone colors: muted purples/greys/warm browns
                    float r2 = 0.35f * bh;
                    float g = 0.28f * bh;
                    float b = 0.40f * bh;
                    // Per-pixel noise for stone texture
                    float noise = 0.88f + ((x * 17 + y * 31) % 40) / 160f;
                    Color c = new Color(r2 * noise, g * noise, b * noise, 1f);
                    tex.SetPixel(x, y, c);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    static void SaveTexturePNG(Texture2D tex, string path)
    {
        byte[] pngData = tex.EncodeToPNG();
        string fullPath = System.IO.Path.Combine(Application.dataPath, "..", path);
        System.IO.File.WriteAllBytes(fullPath, pngData);
        AssetDatabase.ImportAsset(path);
        Debug.Log($"[Hypnagogia] Saved texture: {path}");
    }

    static void CreatePointLight(string name, Vector3 pos, Color col, float intensity)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = pos;
        Light l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 8f;
        l.intensity = intensity;
        l.color = col;
        l.shadows = LightShadows.Soft;
    }

    static void PlaceProp(string objName, string prefabName, Vector3 pos, Vector3 rot)
    {
        // Don't duplicate if exists
        if (GameObject.Find(objName) != null) return;

        // Load prefab generated by ItemPrefabGenerator
        string path = $"Assets/Prefabs/Items/{prefabName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab != null)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = objName;
            inst.transform.position = pos + Vector3.up * 0.05f; // Slightly above floor
            inst.transform.rotation = Quaternion.Euler(rot);

            // Make ALL Rigidbodies kinematic (including children)
            foreach (Rigidbody rb in inst.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Ensure collider exists somewhere
            if (inst.GetComponentInChildren<Collider>() == null)
            {
                MeshFilter mf = inst.GetComponentInChildren<MeshFilter>();
                if (mf != null)
                    mf.gameObject.AddComponent<MeshCollider>();
            }
        }
        else
        {
            Debug.LogWarning($"[Hypnagogia] Could not find prop prefab: {path}. Run 'Generate Item Prefabs' first.");
        }
    }

    [MenuItem("Hypnagogia/Close All Doors + Apply Door Material")]
    static void CloseDoorsAndRestyle()
    {
        // Find all EasyDoor components in the scene
        EasyDoor[] doors = Object.FindObjectsByType<EasyDoor>(FindObjectsSortMode.None);

        if (doors.Length == 0)
        {
            Debug.LogWarning("[Hypnagogia] No EasyDoor components found in the scene!");
            return;
        }

        // Create a distinct dark wood door material
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material doorMat = CreateMat("DoorMaterial", new Color(0.35f, 0.2f, 0.08f), 0.35f);
        AssetDatabase.SaveAssets();

        foreach (EasyDoor door in doors)
        {
            // ── CLOSE: Set rotation/position to the closed state ──
            SerializedObject so = new SerializedObject(door);
            Vector3 closedRot = so.FindProperty("closedRotation").vector3Value;
            Vector3 closedPos = so.FindProperty("closedPosition").vector3Value;

            door.transform.localEulerAngles = closedRot;
            door.transform.localPosition = closedPos;

            Debug.Log($"[Hypnagogia] Closed door: {door.gameObject.name}");

            // ── CHANGE MATERIAL: Apply dark wood to all renderers ──
            Renderer[] renderers = door.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                Material[] mats = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = doorMat;
                rend.sharedMaterials = mats;
            }

            Debug.Log($"[Hypnagogia] Applied door material to: {door.gameObject.name} ({renderers.Length} renderers)");
        }

        MarkDirty();
        Debug.Log($"[Hypnagogia] Done! Closed and restyled {doors.Length} door(s).");
    }

    [MenuItem("Hypnagogia/Fix All Pink Materials (Convert to URP)")]
    static void ConvertAllToURP()
    {
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (pipeline == null || pipeline.defaultMaterial == null)
        {
            Debug.LogError("[Hypnagogia] No URP pipeline asset found in Graphics Settings!");
            return;
        }

        Material templateMat = pipeline.defaultMaterial;
        Shader urpShader = templateMat.shader;
        int converted = 0;

        // Fix ALL materials in Assets/
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // SKIP Synty / third-party asset materials (they have their own textures)
            if (path.Contains("PolygonStarter") || path.Contains("Synty") || path.Contains("XR Interaction Toolkit")
                || path.Contains("Azerilo") || path.Contains("ithappy") || path.Contains("Free Rug"))
                continue;
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            bool isBroken = mat.shader == null
                || mat.shader.name.Contains("Error")
                || mat.shader.name.Contains("Hidden")
                || mat.shader.name == ""
                || mat.shader.name == "Standard"
                || (!mat.shader.name.Contains("Universal") && !mat.shader.name.Contains("URP") && !mat.shader.name.Contains("Particle") && !mat.shader.name.Contains("TextMesh"));

            if (isBroken)
            {
                Color oldColor = Color.white;
                if (mat.HasProperty("_Color")) oldColor = mat.color;
                else if (mat.HasProperty("_BaseColor")) oldColor = mat.GetColor("_BaseColor");

                // Copy ALL properties from the working default material
                mat.shader = urpShader;
                mat.CopyMatchingPropertiesFromMaterial(templateMat);

                // Re-apply the color
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", oldColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", oldColor);
                mat.color = oldColor;

                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"[Hypnagogia] Fixed: {path}");
            }
        }

        // Also replace broken materials on all scene renderers
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (mats[i].shader == null || mats[i].shader.name.Contains("Error") || mats[i].shader.name.Contains("Hidden"))
                {
                    Color c = mats[i].HasProperty("_Color") ? mats[i].color : Color.grey;
                    mats[i] = new Material(templateMat);
                    if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", c);
                    mats[i].color = c;
                    changed = true;
                    converted++;
                }
            }
            if (changed) r.sharedMaterials = mats;
        }

        AssetDatabase.SaveAssets();
        MarkDirty();
        Debug.Log($"[Hypnagogia] Fixed {converted} materials using pipeline default");
    }

    [MenuItem("Hypnagogia/Apply Metal Material to Pipes")]
    static void ApplyMetalToPipes()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // Try to load the pipe's existing metal texture
        Texture2D colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Pipes/Source/Textures/T_Metal003_2K_Color.TGA");
        Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Pipes/Source/Textures/T_Metal003_2K_Normal.TGA");
        Texture2D roughTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Pipes/Source/Textures/T_Metal003_2K_Roughness.TGA");
        Texture2D metalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Pipes/Source/Textures/T_Metal003_2K_Metalness.TGA");

        Material pipeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        pipeMat.name = "PipeMetalMaterial";
        pipeMat.SetFloat("_Metallic", 0.85f);
        pipeMat.SetFloat("_Smoothness", 0.6f);
        pipeMat.SetColor("_BaseColor", new Color(0.6f, 0.63f, 0.65f)); // Steel grey

        if (colorTex != null) pipeMat.SetTexture("_BaseMap", colorTex);
        if (normalTex != null) { pipeMat.SetTexture("_BumpMap", normalTex); pipeMat.EnableKeyword("_NORMALMAP"); }
        if (metalTex != null) { pipeMat.SetTexture("_MetallicGlossMap", metalTex); pipeMat.EnableKeyword("_METALLICSPECGLOSSMAP"); }

        string path = "Assets/Materials/PipeMetalMaterial.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(pipeMat, path);
        AssetDatabase.SaveAssets();

        // Apply to all pipe objects in scene
        int count = 0;
        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.name.Contains("Pipe"))
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = pipeMat;
                    r.sharedMaterials = mats;
                    count++;
                }
            }
        }

        MarkDirty();
        Debug.Log($"[Hypnagogia] Applied metal material to {count} pipe renderers.");
    }

    [MenuItem("Hypnagogia/Add Colliders to Everything")]
    static void AddColliders()
    {
        int added = 0;
        // Find every renderer in the scene
        Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer r in allRenderers)
        {
            GameObject obj = r.gameObject;
            // Skip if already has any collider
            if (obj.GetComponent<Collider>() != null) continue;

            // Add a MeshCollider if it has a MeshFilter, otherwise BoxCollider
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = obj.AddComponent<MeshCollider>();
                mc.convex = false; // Static walls don't need convex
                added++;
            }
            else
            {
                obj.AddComponent<BoxCollider>();
                added++;
            }
        }

        MarkDirty();
        Debug.Log($"[Hypnagogia] Added colliders to {added} objects.");
    }

    // ── HELPERS ──

    static void SetupWall(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.Find(name);
        if (wall == null) { Debug.LogWarning($"[Hypnagogia] '{name}' not found."); return; }
        wall.transform.position = pos;
        wall.transform.rotation = Quaternion.identity;
        wall.transform.localScale = scale;
        ApplyMaterial(wall, mat);
    }

    static void ApplyMaterial(GameObject obj, Material mat)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static Material CreateMat(string name, Color color, float smoothness)
    {
        // Clone the pipeline default material (preserves ALL URP keywords/properties)
        Material mat = null;
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (pipeline != null && pipeline.defaultMaterial != null)
        {
            mat = new Material(pipeline.defaultMaterial); // Copy from working material!
            Debug.Log($"[Hypnagogia] Cloned pipeline default material ({pipeline.defaultMaterial.shader.name}) for {name}");
        }
        else
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        mat.name = name;

        // Set color everywhere it could exist
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);

        string path = $"Assets/Materials/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Shader FindURPLitShader()
    {
        // Method 1: Get shader from the active Render Pipeline (GUARANTEED in URP projects)
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (pipeline != null && pipeline.defaultMaterial != null && pipeline.defaultMaterial.shader != null)
        {
            Debug.Log($"[Hypnagogia] Got URP shader from pipeline: {pipeline.defaultMaterial.shader.name}");
            return pipeline.defaultMaterial.shader;
        }

        // Method 2: Try Shader.Find with various names
        string[] shaderNames = new string[] {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Shader Graphs/Lit"
        };
        foreach (string sn in shaderNames)
        {
            Shader s = Shader.Find(sn);
            if (s != null) return s;
        }

        // Method 3: Last resort
        Debug.LogWarning("[Hypnagogia] Could not find URP Lit shader! Using Standard as fallback.");
        return Shader.Find("Standard");
    }

    static void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
