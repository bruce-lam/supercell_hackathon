using UnityEngine;

/// <summary>
/// Spawns primitive geometry in the scene based on backend responses.
/// This is a starter — replace primitives with real prefabs as you build them.
/// 
/// SETUP: Drag this onto an empty GameObject called "ObjectSpawner".
///        Set spawnPoint to a Transform where objects should appear (e.g. center of room).
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [Tooltip("Where objects appear. If empty, spawns 2m in front of the camera.")]
    public Transform spawnPoint;

    [Tooltip("How far in front of the player to spawn if no spawnPoint is set")]
    public float defaultSpawnDistance = 2f;

    public static ObjectSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-subscribe to NetworkManager responses
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnWishResponse += OnWishResponse;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnWishResponse -= OnWishResponse;
        }
    }

    private void OnWishResponse(NetworkManager.WishResponse response)
    {
        if (!response.success) return;
        Spawn(response.objectType, response.description);
    }

    /// <summary>
    /// Spawns a placeholder object. Replace this with your real prefab logic.
    /// </summary>
    public void Spawn(string objectType, string description)
    {
        Vector3 position = GetSpawnPosition();

        // For the hackathon: spawn primitives based on the object type
        // Replace these with actual prefabs / procedural generation later
        GameObject spawned;

        switch (objectType.ToLower())
        {
            case "bridge":
                spawned = CreateBridge(position);
                break;
            case "wall":
                spawned = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spawned.transform.position = position;
                spawned.transform.localScale = new Vector3(3f, 2f, 0.3f);
                break;
            case "pillar":
            case "tower":
                spawned = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spawned.transform.position = position;
                spawned.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
                break;
            case "sphere":
            case "orb":
                spawned = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spawned.transform.position = position + Vector3.up;
                break;
            default:
                // Default: just spawn a cube with a label
                spawned = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spawned.transform.position = position + Vector3.up;
                break;
        }

        spawned.name = $"Wish_{objectType}";

        // Give it a random color so it's visually distinct
        Renderer rend = spawned.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.7f, 1f);
        }

        Debug.Log($"[ObjectSpawner] Spawned '{objectType}' at {position}. Description: {description}");
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }

        // Fallback: spawn in front of the main camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.position + cam.transform.forward * defaultSpawnDistance;
        }

        return Vector3.zero + Vector3.up;
    }

    /// <summary>
    /// Quick & dirty bridge out of 3 cubes (two pillars + a deck).
    /// Replace with a real model when you have one.
    /// </summary>
    private GameObject CreateBridge(Vector3 center)
    {
        GameObject bridge = new GameObject("Bridge");
        bridge.transform.position = center;

        // Left pillar
        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.transform.SetParent(bridge.transform);
        left.transform.localPosition = new Vector3(-1.5f, 0.5f, 0f);
        left.transform.localScale = new Vector3(0.4f, 1f, 1f);

        // Right pillar
        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.transform.SetParent(bridge.transform);
        right.transform.localPosition = new Vector3(1.5f, 0.5f, 0f);
        right.transform.localScale = new Vector3(0.4f, 1f, 1f);

        // Deck
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.transform.SetParent(bridge.transform);
        deck.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        deck.transform.localScale = new Vector3(3.5f, 0.2f, 1.2f);

        return bridge;
    }
}
