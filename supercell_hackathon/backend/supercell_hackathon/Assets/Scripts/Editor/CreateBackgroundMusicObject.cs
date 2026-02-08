using UnityEngine;
using UnityEditor;

/// <summary>
/// Creates a GameObject with the BackgroundMusic component in the active scene.
/// Run from Hypnagogia > Create Background Music GameObject.
/// </summary>
public static class CreateBackgroundMusicObject
{
    [MenuItem("Hypnagogia/Create Background Music GameObject")]
    static void Create()
    {
        if (Object.FindObjectOfType<BackgroundMusic>() != null)
        {
            bool replace = EditorUtility.DisplayDialog("Background Music exists", "A BackgroundMusic object already exists in the scene. Create another anyway?", "Create Another", "Cancel");
            if (!replace) return;
        }

        GameObject go = new GameObject("BackgroundMusic");
        go.AddComponent<AudioSource>();
        go.AddComponent<BackgroundMusic>();

        Undo.RegisterCreatedObjectUndo(go, "Create Background Music");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[Hypnagogia] Created BackgroundMusic GameObject. Put BackgroundMusic.wav in Assets/Audio/Resources/ to play automatically.");
    }
}
