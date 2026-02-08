using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the Pipe GameObject. Spawns random items from the pipe opening.
/// Press F to spawn an item during play mode.
/// Can also be triggered from other scripts via SpawnItem() or SpawnRandomItem().
/// </summary>
public class PipeSpawner : MonoBehaviour
{
    [Header("Item Prefabs")]
    [Tooltip("Drag all item prefabs from Assets/Prefabs/Items here")]
    public GameObject[] itemPrefabs;

    [Header("Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(0f, -0.5f, 0f); // Below pipe
    public float dropForce = 2f;
    public float spawnCooldown = 0.5f;
    public bool addRandomSpin = true;

    [Header("Cleanup")]
    public float destroyAfterSeconds = 30f;

    private float lastSpawnTime = 0f;

    void Start()
    {
        int count = (itemPrefabs != null) ? itemPrefabs.Length : 0;
        Debug.Log($"[PipeSpawner] Ready! {count} item prefabs loaded. Press F to spawn.");
        if (count == 0)
            Debug.LogError("[PipeSpawner] ERROR: No prefabs assigned! Run Hypnagogia > Assign Items to Pipe Spawner");
    }

    void Update()
    {
        // F-key debug spawning disabled — spawning is handled by GenieClient
        // which correctly targets only the active room's PipeSpawner.
        // var keyboard = Keyboard.current;
        // if (keyboard == null) return;
        // if (keyboard.fKey.wasPressedThisFrame && Time.time - lastSpawnTime > spawnCooldown)
        // {
        //     SpawnRandomItem();
        // }
    }

    /// <summary>
    /// Spawns a specific item by name (e.g., "sword", "key", "pizza")
    /// </summary>
    public GameObject SpawnItem(string itemName)
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogWarning("[PipeSpawner] No item prefabs assigned!");
            return null;
        }

        // Find the prefab by name
        foreach (var prefab in itemPrefabs)
        {
            try {
                if (prefab != null && prefab.name.ToLower() == itemName.ToLower())
                    return DoSpawn(prefab);
            } catch (MissingReferenceException) { continue; }
        }

        Debug.LogWarning($"[PipeSpawner] Item '{itemName}' not found in prefab list!");
        return null;
    }

    /// <summary>
    /// Spawns a random item from the prefab list
    /// </summary>
    public GameObject SpawnRandomItem()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogWarning("[PipeSpawner] No item prefabs assigned!");
            return null;
        }

        // Try up to 5 times to find a valid (non-null) prefab
        for (int attempt = 0; attempt < 5; attempt++)
        {
            int index = Random.Range(0, itemPrefabs.Length);
            try {
                if (itemPrefabs[index] != null)
                    return DoSpawn(itemPrefabs[index]);
            } catch (MissingReferenceException) { continue; }
        }
        Debug.LogWarning("[PipeSpawner] Could not find valid prefab after retries");
        return null;
    }

    private GameObject DoSpawn(GameObject prefab)
    {
        lastSpawnTime = Time.time;

        Vector3 spawnPos = transform.position + transform.TransformDirection(spawnOffset);
        GameObject item = Instantiate(prefab, spawnPos, Random.rotation);
        item.name = prefab.name; // Remove "(Clone)" suffix

        // Ensure item has a collider (auto-fit if missing)
        if (item.GetComponentInChildren<Collider>() == null)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                BoxCollider bc = item.AddComponent<BoxCollider>();
                bc.center = item.transform.InverseTransformPoint(bounds.center);
                bc.size = bounds.size;
            }
            else
            {
                item.AddComponent<BoxCollider>();
            }
        }

        // Apply physics
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null) rb = item.AddComponent<Rigidbody>();

        rb.linearVelocity = Vector3.down * dropForce;

        if (addRandomSpin)
        {
            rb.angularVelocity = Random.insideUnitSphere * 3f;
        }

        // Auto-destroy after time
        if (destroyAfterSeconds > 0)
        {
            Destroy(item, destroyAfterSeconds);
        }

        Debug.Log($"[PipeSpawner] Spawned: {prefab.name}");
        return item;
    }
}
