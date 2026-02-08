using UnityEngine;
using UnityEditor;
using EasyDoorSystem;

/// <summary>
/// Copies open/closed rotation+position from one EasyDoor to another.
/// Run from Hypnagogia > Copy Door 2 States to Door 3
/// </summary>
public class DoorStateCopier : MonoBehaviour
{
    [MenuItem("Hypnagogia/Copy Door 2 States to Door 3")]
    static void CopyDoor2ToDoor3()
    {
        EasyDoor[] doors = Object.FindObjectsByType<EasyDoor>(FindObjectsSortMode.None);

        EasyDoor door2 = null, door3 = null;
        foreach (var d in doors)
        {
            // Match by parent name
            string parentName = d.transform.parent != null ? d.transform.parent.name : d.name;
            if (parentName.Contains("Easy_Door 1 (2)") || parentName.Contains("Hinge") && d.transform.root.name.Contains("(2)"))
                door2 = d;
            if (parentName.Contains("Easy_Door 1 (3)") || parentName.Contains("Hinge") && d.transform.root.name.Contains("(3)"))
                door3 = d;
            
            // Also try matching by the door's own hierarchy
            Transform t = d.transform;
            while (t.parent != null) { t = t.parent; }
            if (t.name == "Easy_Door 1 (2)" || t.name.Contains("(2)")) door2 = d;
            if (t.name == "Easy_Door 1 (3)" || t.name.Contains("(3)")) door3 = d;
        }

        if (door2 == null) { Debug.LogError("[DoorCopy] Could not find Door 2!"); return; }
        if (door3 == null) { Debug.LogError("[DoorCopy] Could not find Door 3!"); return; }

        Debug.Log($"[DoorCopy] Found Door 2: {door2.name} (under {door2.transform.parent?.name})");
        Debug.Log($"[DoorCopy] Found Door 3: {door3.name} (under {door3.transform.parent?.name})");

        SerializedObject src = new SerializedObject(door2);
        SerializedObject dst = new SerializedObject(door3);

        // Copy all four state values
        Vector3 srcOpenRot = src.FindProperty("openedRotation").vector3Value;
        Vector3 srcCloseRot = src.FindProperty("closedRotation").vector3Value;
        Vector3 srcOpenPos = src.FindProperty("openedPosition").vector3Value;
        Vector3 srcClosePos = src.FindProperty("closedPosition").vector3Value;

        Debug.Log($"[DoorCopy] Door 2 openRot={srcOpenRot} closeRot={srcCloseRot}");
        Debug.Log($"[DoorCopy] Door 2 openPos={srcOpenPos} closePos={srcClosePos}");

        dst.FindProperty("openedRotation").vector3Value = srcOpenRot;
        dst.FindProperty("closedRotation").vector3Value = srcCloseRot;
        dst.FindProperty("openedPosition").vector3Value = srcOpenPos;
        dst.FindProperty("closedPosition").vector3Value = srcClosePos;

        dst.ApplyModifiedProperties();
        EditorUtility.SetDirty(door3);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[DoorCopy] ✅ Copied Door 2 open/close states to Door 3! Save your scene.");
    }

    [MenuItem("Hypnagogia/Swap Door 3 Open-Close States")]
    static void SwapDoor3States()
    {
        EasyDoor[] doors = Object.FindObjectsByType<EasyDoor>(FindObjectsSortMode.None);

        EasyDoor door3 = null;
        foreach (var d in doors)
        {
            Transform t = d.transform;
            while (t.parent != null) { t = t.parent; }
            if (t.name == "Easy_Door 1 (3)" || t.name.Contains("(3)")) door3 = d;
        }

        if (door3 == null) { Debug.LogError("[DoorCopy] Could not find Door 3!"); return; }

        SerializedObject so = new SerializedObject(door3);
        
        Vector3 openRot = so.FindProperty("openedRotation").vector3Value;
        Vector3 closeRot = so.FindProperty("closedRotation").vector3Value;
        Vector3 openPos = so.FindProperty("openedPosition").vector3Value;
        Vector3 closePos = so.FindProperty("closedPosition").vector3Value;

        // Swap
        so.FindProperty("openedRotation").vector3Value = closeRot;
        so.FindProperty("closedRotation").vector3Value = openRot;
        so.FindProperty("openedPosition").vector3Value = closePos;
        so.FindProperty("closedPosition").vector3Value = openPos;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(door3);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[DoorCopy] ✅ Swapped Door 3 states! Open↔Close. Save your scene.");
    }

    [MenuItem("Hypnagogia/Print All Door States")]
    static void PrintDoorStates()
    {
        EasyDoor[] doors = Object.FindObjectsByType<EasyDoor>(FindObjectsSortMode.None);
        foreach (var d in doors)
        {
            SerializedObject so = new SerializedObject(d);
            Transform root = d.transform;
            while (root.parent != null) root = root.parent;
            Debug.Log($"[DoorStates] {root.name}/{d.name}: " +
                $"openRot={so.FindProperty("openedRotation").vector3Value} " +
                $"closeRot={so.FindProperty("closedRotation").vector3Value} " +
                $"openPos={so.FindProperty("openedPosition").vector3Value} " +
                $"closePos={so.FindProperty("closedPosition").vector3Value} " +
                $"isOpen={d.IsOpen}");
        }
    }
}
