using UnityEngine;
using UnityEditor;
using EasyDoorSystem;

/// <summary>
/// Fixes door hinge pivot issues for the Easy Door System.
/// The EasyDoor script rotates ITSELF using transform.local*, so its
/// position IS the pivot point. This script:
/// 1. Creates a "Hinge" empty parent at the door's hinge edge
/// 2. Re-parents the EasyDoor object under it
/// 3. Offsets the EasyDoor so the door visually stays in place
/// 4. Re-saves the open/closed states
///
/// Run from Hypnagogia > Fix Door Hinges
/// </summary>
public class DoorHingeFixer : MonoBehaviour
{
    [MenuItem("Hypnagogia/Fix Door Hinges")]
    static void FixAllDoorHinges()
    {
        EasyDoor[] doors = Object.FindObjectsByType<EasyDoor>(FindObjectsSortMode.None);

        if (doors.Length == 0)
        {
            Debug.LogWarning("[HingeFix] No EasyDoor components found!");
            return;
        }

        int fixed_count = 0;

        foreach (EasyDoor door in doors)
        {
            Transform doorTransform = door.transform;

            // Skip if already has a Hinge parent (already fixed)
            if (doorTransform.parent != null && doorTransform.parent.name.Contains("Hinge"))
            {
                Debug.Log($"[HingeFix] Skipping {door.name} — already has Hinge parent");
                continue;
            }

            // Get renderers from this object or its children
            Renderer[] renderers = doorTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[HingeFix] {door.name} has no renderers — skipping");
                continue;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Determine hinge side (use the LEFT edge of the door in local X)
            float halfWidth = bounds.extents.x;
            
            // Hinge position: current door position but shifted to the left edge
            Vector3 hingeWorldPos = doorTransform.position - doorTransform.right * halfWidth;

            // Remember the current world position and rotation of the door
            Vector3 originalWorldPos = doorTransform.position;
            Quaternion originalWorldRot = doorTransform.rotation;
            Transform originalParent = doorTransform.parent;

            // Create the Hinge parent at the edge
            GameObject hinge = new GameObject($"Hinge_{door.name}");
            hinge.transform.position = hingeWorldPos;
            hinge.transform.rotation = originalWorldRot;

            // If the door had a parent, put the hinge under it
            if (originalParent != null)
                hinge.transform.SetParent(originalParent, true);

            // Re-parent the EasyDoor under the hinge
            doorTransform.SetParent(hinge.transform, true);

            // Now update the EasyDoor's saved states
            SerializedObject so = new SerializedObject(door);
            
            // Save closed state (current position)
            so.FindProperty("closedRotation").vector3Value = doorTransform.localEulerAngles;
            so.FindProperty("closedPosition").vector3Value = doorTransform.localPosition;

            // Calculate open state: 90-degree rotation around the hinge
            Vector3 closedLocalPos = doorTransform.localPosition;
            Quaternion closedLocalRot = doorTransform.localRotation;

            // Rotate 90 degrees around hinge (the parent's Y axis)
            doorTransform.RotateAround(hinge.transform.position, Vector3.up, -90f);
            
            so.FindProperty("openedRotation").vector3Value = doorTransform.localEulerAngles;
            so.FindProperty("openedPosition").vector3Value = doorTransform.localPosition;

            // Restore to closed
            doorTransform.localPosition = closedLocalPos;
            doorTransform.localRotation = closedLocalRot;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(door);

            fixed_count++;
            Debug.Log($"[HingeFix] ✅ Fixed {door.name} — hinge at {hingeWorldPos}, offset: {doorTransform.localPosition}");
        }

        if (fixed_count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"[HingeFix] Done! Fixed {fixed_count} door(s). Save your scene!");
    }

    [MenuItem("Hypnagogia/Test Door Open-Close (All)")]
    static void TestDoors()
    {
        EasyDoor[] doors = Object.FindObjectsByType<EasyDoor>(FindObjectsSortMode.None);
        foreach (EasyDoor door in doors)
        {
            door.ToggleDoor();
            Debug.Log($"[HingeFix] Toggled: {door.name}");
        }
    }
}
