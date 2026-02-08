using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates VFX particle system prefabs (fire, smoke, sparks).
/// Run from Hypnagogia > Generate VFX Prefabs
/// </summary>
public class VFXPrefabGenerator : MonoBehaviour
{
    [MenuItem("Hypnagogia/Generate VFX Prefabs")]
    static void GenerateVFX()
    {
        string basePath = "Assets/Prefabs/VFX";
        EnsureFolder(basePath);

        CreateFireVFX(basePath);
        CreateSmokeVFX(basePath);
        CreateSparksVFX(basePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Hypnagogia] Generated 3 VFX prefabs in " + basePath);
    }

    static void CreateFireVFX(string basePath)
    {
        GameObject obj = new GameObject("vfx_fire");
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 1.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0f, 1f),   // Orange
            new Color(1f, 0.15f, 0f, 1f)    // Red
        );
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // Float upward

        var emission = ps.emission;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.05f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Use default particle material
        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        SavePrefab(obj, basePath, "vfx_fire");
    }

    static void CreateSmokeVFX(string basePath)
    {
        GameObject obj = new GameObject("vfx_smoke");
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 2f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.5f, 0.5f, 0.5f, 0.4f),
            new Color(0.3f, 0.3f, 0.3f, 0.6f)
        );
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(1f, 2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 0f),
                new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.6f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        SavePrefab(obj, basePath, "vfx_smoke");
    }

    static void CreateSparksVFX(string basePath)
    {
        GameObject obj = new GameObject("vfx_sparks");
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 0.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.3f, 1f),  // Bright yellow
            new Color(1f, 0.7f, 0.1f, 1f)   // Gold
        );
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f; // Sparks fall

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        // Burst emission for spark feel
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 10, 20, 5, 0.2f)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.8f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        SavePrefab(obj, basePath, "vfx_sparks");
    }

    static Material GetParticleMaterial()
    {
        // Try to find URP particle material
        Material mat = Shader.Find("Universal Render Pipeline/Particles/Unlit") != null
            ? new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))
            : new Material(Shader.Find("Particles/Standard Unlit"));

        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);   // Alpha blend
        return mat;
    }

    static void SavePrefab(GameObject obj, string basePath, string name)
    {
        string path = $"{basePath}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
