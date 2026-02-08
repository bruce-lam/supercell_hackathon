using UnityEngine;

/// <summary>
/// Attached to every spawned object. Stores the Genie's verdict and handles
/// pickup (float in front of player) and door interaction.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("Stored Verdict (set by GenieClient)")]
    public bool doorOpen;
    public string congratsVoice;
    public string audioUrlCongrats;
    public string dropVoice;

    [Header("Pickup State")]
    public bool isHeld = false;
    public bool isPickupable = true;  // false after being used on a door

    [Header("Float Settings")]
    public float floatHeight = -0.15f;     // Y offset relative to camera (slightly below eye level)
    public float floatDistance = 1.8f;      // Distance in front of camera (far enough to see clearly)
    public float followSpeed = 10f;        // Lerp speed (snappy following)
    public float bobAmplitude = 0.04f;     // Gentle bob
    public float bobFrequency = 2f;

    private Transform playerCamera;
    private Rigidbody rb;
    private Collider[] colliders;
    private float bobTimer;

    // Glow effect
    private Renderer[] renderers;
    private bool isHighlighted = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void LateUpdate()
    {
        if (!isHeld) return;
        if (playerCamera == null)
        {
            // Try to find camera — use Camera.main first, fallback to any camera
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;
            playerCamera = cam.transform;
        }

        // Calculate target position: in front of the player's view, slightly below eye level
        bobTimer += Time.deltaTime * bobFrequency;
        float bob = Mathf.Sin(bobTimer) * bobAmplitude;

        Vector3 targetPos = playerCamera.position
            + playerCamera.forward * floatDistance
            + playerCamera.up * (floatHeight + bob);

        // Smoothly move toward target
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Face the player (so you always see the front of the object)
        Vector3 lookDir = playerCamera.position - transform.position;
        lookDir.y = 0; // Keep upright
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(-lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        // Slowly rotate for visual interest
        transform.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Called when the player picks up this item
    /// </summary>
    public void Pickup()
    {
        isHeld = true;

        // Find the camera now
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null) playerCamera = cam.transform;

        // Disable physics so it floats
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable colliders so it doesn't block movement
        foreach (var col in colliders)
            col.enabled = false;

        // Immediately snap to a position in front of the player (no lerp delay)
        if (playerCamera != null)
        {
            transform.position = playerCamera.position
                + playerCamera.forward * floatDistance
                + playerCamera.up * floatHeight;
        }

        Debug.Log($"[ItemPickup] 🤚 Picked up: {gameObject.name}");
    }

    /// <summary>
    /// Called when the player drops this item
    /// </summary>
    public void Drop()
    {
        isHeld = false;

        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Re-enable colliders
        foreach (var col in colliders)
            col.enabled = true;

        Debug.Log($"[ItemPickup] 📦 Dropped: {gameObject.name}");
    }

    /// <summary>
    /// Highlight this object when the player is close enough to pick it up
    /// </summary>
    public void SetHighlight(bool on)
    {
        if (isHighlighted == on) return;
        isHighlighted = on;

        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            // Boost emission for glow effect
            if (on)
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 1f) * 0.5f);
            }
            else
            {
                rend.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
