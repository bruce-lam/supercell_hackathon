using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using EasyDoorSystem;

public class SetupMaterials : MonoBehaviour
{
    [MenuItem("Hypnagogia/Setup Room (Rebuild)")]
    static void SetupRoom()
    {
        // ── CREATE MATERIALS ──
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material floorMat = CreateMat("FloorMaterial", new Color(0.18f, 0.18f, 0.22f), 0.4f);
        Material wallMat = CreateMat("WallMaterial", new Color(0.75f, 0.73f, 0.70f), 0.15f);

        AssetDatabase.SaveAssets();

        // ── FLOOR: 10m x 10m ──
        GameObject plane = GameObject.Find("Plane");
        if (plane != null)
        {
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(1f, 1f, 1f);
            ApplyMaterial(plane, floorMat);
        }

        // ── WALLS: 3m tall, positioned at floor edges ──
        float roomHalf = 5f;
        float wallHeight = 3f;
        float wallThick = 0.15f;

        SetupWall("Cube", new Vector3(0f, wallHeight / 2f, roomHalf),
            new Vector3(roomHalf * 2f, wallHeight, wallThick), wallMat);

        SetupWall("Cube (1)", new Vector3(-roomHalf, wallHeight / 2f, 0f),
            new Vector3(wallThick, wallHeight, roomHalf * 2f), wallMat);

        SetupWall("Cube (2)", new Vector3(roomHalf, wallHeight / 2f, 0f),
            new Vector3(wallThick, wallHeight, roomHalf * 2f), wallMat);

        // ── DIRECTIONAL LIGHT ──
        Light[] lights = Object.FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(40f, -50f, 0f);
                light.intensity = 1.5f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.85f;
                break;
            }
        }

        AssetDatabase.SaveAssets();
        MarkDirty();
        Debug.Log("[Hypnagogia] Room setup complete!");
    }

    [MenuItem("Hypnagogia/Close All Doors + Apply Door Material")]
    static void CloseDoorsAndRestyle()
    {
        // Find all EasyDoor components in the scene
        EasyDoor[] doors = Object.FindObjectsOfType<EasyDoor>();

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
        // Find ALL .mat files in the project
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        int converted = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            // Check if it's using a Built-in shader (not URP)
            if (mat.shader != null && !mat.shader.name.Contains("Universal") && !mat.shader.name.Contains("URP"))
            {
                // Get the old color before converting
                Color oldColor = Color.white;
                if (mat.HasProperty("_Color")) oldColor = mat.color;

                Texture oldMainTex = null;
                if (mat.HasProperty("_MainTex")) oldMainTex = mat.mainTexture;

                Texture oldNormal = null;
                if (mat.HasProperty("_BumpMap")) oldNormal = mat.GetTexture("_BumpMap");

                // Switch to URP Lit shader
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");

                // Restore properties
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", oldColor);
                if (oldMainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", oldMainTex);
                if (oldNormal != null && mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", oldNormal);

                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Hypnagogia] Converted {converted} materials from Built-in to URP Lit.");
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
        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
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
        Renderer[] allRenderers = Object.FindObjectsOfType<Renderer>();
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
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);

        string path = $"Assets/Materials/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
