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
    public float floatHeight = -0.3f;     // Y offset relative to camera
    public float floatDistance = 1.2f;    // Distance in front of camera
    public float followSpeed = 8f;       // Lerp speed
    public float bobAmplitude = 0.05f;   // Gentle bob
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

    void Update()
    {
        if (!isHeld) return;
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
            if (playerCamera == null) return;
        }

        // Calculate target position: in front of the player's view, slightly below eye level
        bobTimer += Time.deltaTime * bobFrequency;
        float bob = Mathf.Sin(bobTimer) * bobAmplitude;

        Vector3 targetPos = playerCamera.position
            + playerCamera.forward * floatDistance
            + playerCamera.up * (floatHeight + bob);

        // Smoothly move toward target
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Slowly rotate for visual interest
        transform.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Called when the player picks up this item
    /// </summary>
    public void Pickup()
    {
        isHeld = true;
        playerCamera = Camera.main?.transform;

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

        Debug.Log($"[ItemPickup] 🤚 Picked up: {gameObject.name}");
    }

    /// <summary>
    /// Called when the player drops this item (wrong answer, or picked up a different one)
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
