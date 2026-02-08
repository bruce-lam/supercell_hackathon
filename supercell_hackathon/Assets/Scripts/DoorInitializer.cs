using UnityEngine;
using EasyDoorSystem;

/// <summary>
/// Forces all EasyDoor components to their CLOSED state on Start.
/// Also disables automaticPlayerDetection so doors only open
/// when the Genie says so (via GenieClient).
///
/// SETUP: Add this to any GameObject in the scene (e.g. the Camera).
/// </summary>
public class DoorInitializer : MonoBehaviour
{
    void Start()
    {
        EasyDoor[] doors = FindObjectsByType<EasyDoor>(FindObjectsSortMode.None);

        foreach (EasyDoor door in doors)
        {
            // Disable auto-detection — doors only open via wishes
            door.automaticPlayerDetection = false;

            // Force closed state (always call CloseDoor to handle any state)
            door.CloseDoor();

            Debug.Log($"[DoorInit] 🚪 {door.name} → locked, autoDetect OFF, isOpen={door.IsOpen}");
        }

        Debug.Log($"[DoorInit] Initialized {doors.Length} door(s) — all locked.");
    }
}
