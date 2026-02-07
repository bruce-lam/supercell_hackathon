using UnityEngine;

/// <summary>
/// Makes a GameObject always face the main camera (billboard effect).
/// Attach to text labels so they're always readable.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        // Find the main camera
        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main != null)
                cam = Camera.main.transform;
            else
                return;
        }

        // Face the camera
        transform.LookAt(transform.position + cam.forward);
    }
}
