using UnityEngine;

/// <summary>
/// Makes a UI element always face the main camera (billboard effect).
/// Attach to floating label GameObjects.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // Face the camera
        transform.LookAt(transform.position + cam.transform.forward);
    }
}
