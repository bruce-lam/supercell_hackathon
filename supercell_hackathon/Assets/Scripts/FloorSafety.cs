using UnityEngine;

/// <summary>
/// Safety net for spawned objects: prevents falling through the floor.
/// Clamps Y position to never go below a minimum, and teleports back up
/// if the object somehow clips through geometry.
/// </summary>
public class FloorSafety : MonoBehaviour
{
    [Tooltip("Minimum Y position (world space) — objects below this get teleported up")]
    public float minimumY = -5f;

    [Tooltip("Y position to teleport to when rescued")]
    public float rescueY = 2f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Extra insurance: set solver iterations higher for this object
        if (rb != null)
        {
            rb.solverIterations = 10;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    void FixedUpdate()
    {
        // If object falls below the safety threshold, rescue it
        if (transform.position.y < minimumY)
        {
            Debug.LogWarning($"[FloorSafety] 🛟 Rescued '{gameObject.name}' from falling through floor (Y={transform.position.y:F1})");

            Vector3 rescuePos = transform.position;
            rescuePos.y = rescueY;
            transform.position = rescuePos;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
