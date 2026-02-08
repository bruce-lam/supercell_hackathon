using UnityEngine;
using UnityEditor;

/// <summary>
/// Repairs Synty Polygon Starter Pack materials by re-linking their texture atlas.
/// The "Fix All Pink Materials" script may have stripped textures from these materials.
/// Run from Hypnagogia > Repair Synty Materials
/// </summary>
public class SyntyMaterialRepair : MonoBehaviour
{
    [MenuItem("Hypnagogia/Repair Synty Materials")]
    static void RepairSyntyMaterials()
    {
        // Synty uses 4 color variants, each mat references its corresponding texture
        string[] matPaths = new string[] {
            "Assets/PolygonStarter/Materials/PolygonStarter_Mat_01.mat",
            "Assets/PolygonStarter/Materials/PolygonStarter_Mat_02.mat",
            "Assets/PolygonStarter/Materials/PolygonStarter_Mat_03.mat",
            "Assets/PolygonStarter/Materials/PolygonStarter_Mat_04.mat",
        };

        string[] texPaths = new string[] {
            "Assets/PolygonStarter/Textures/PolygonStarter_Texture_01.png",
            "Assets/PolygonStarter/Textures/PolygonStarter_Texture_02.png",
            "Assets/PolygonStarter/Textures/PolygonStarter_Texture_03.png",
            "Assets/PolygonStarter/Textures/PolygonStarter_Texture_04.png",
        };

        // Get pipeline default for a clean URP Lit starting point
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        Shader urpShader = null;
        if (pipeline != null && pipeline.defaultMaterial != null)
            urpShader = pipeline.defaultMaterial.shader;

        int repaired = 0;

        for (int i = 0; i < matPaths.Length; i++)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPaths[i]);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPaths[i]);

            if (mat == null)
            {
                Debug.LogWarning($"[SyntyRepair] Material not found: {matPaths[i]}");
                continue;
            }
            if (tex == null)
            {
                Debug.LogWarning($"[SyntyRepair] Texture not found: {texPaths[i]}");
                continue;
            }

            // Ensure it uses URP Lit shader
            if (urpShader != null && (mat.shader == null || mat.shader.name.Contains("Error") || mat.shader.name == "Standard"))
            {
                mat.shader = urpShader;
            }

            // Re-link the texture atlas
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);
            mat.mainTexture = tex;

            // Set color to white so texture shows through (not tinted)
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            mat.color = Color.white;

            EditorUtility.SetDirty(mat);
            repaired++;
            Debug.Log($"[SyntyRepair] ✅ Repaired: {matPaths[i]} → texture: {texPaths[i]}");
        }

        // Also repair the misc/plane materials
        string[] miscMatPaths = AssetDatabase.FindAssets("t:Material", new[] { 
            "Assets/PolygonStarter/Materials/Misc",
            "Assets/PolygonStarter/Materials/Plane"
        });
        foreach (string guid in miscMatPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Fix shader if broken
            if (urpShader != null && mat.shader != null && 
                (mat.shader.name.Contains("Error") || mat.shader.name == "Standard"))
            {
                mat.shader = urpShader;
                EditorUtility.SetDirty(mat);
                repaired++;
                Debug.Log($"[SyntyRepair] ✅ Shader fixed: {path}");
            }
        }

        // ── REPAIR FREE RUG PACK MATERIALS ──
        string[] rugMatPaths = new string[] {
            "Assets/Azerilo/Free Rug Pack/Material/Material1.mat",
            "Assets/Azerilo/Free Rug Pack/Material/Material2.mat",
            "Assets/Azerilo/Free Rug Pack/Material/Material3.mat",
        };
        string[] rugTexPaths = new string[] {
            "Assets/Azerilo/Free Rug Pack/Material/rugTexture1.png",
            "Assets/Azerilo/Free Rug Pack/Material/rugTexture2.png",
            "Assets/Azerilo/Free Rug Pack/Material/rugTexture3.png",
        };

        for (int i = 0; i < rugMatPaths.Length; i++)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(rugMatPaths[i]);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rugTexPaths[i]);
            if (mat == null || tex == null) continue;

            if (urpShader != null)
                mat.shader = urpShader;

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            mat.color = Color.white;

            EditorUtility.SetDirty(mat);
            repaired++;
            Debug.Log($"[SyntyRepair] ✅ Rug repaired: {rugMatPaths[i]}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SyntyRepair] Done! Repaired {repaired} third-party materials (Synty + Rugs).");
    }
}
