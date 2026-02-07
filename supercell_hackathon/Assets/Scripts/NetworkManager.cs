using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles all HTTP communication with the backend.
/// Zero plugins required — uses Unity's built-in UnityWebRequest.
/// 
/// SETUP: Drag this onto an empty GameObject called "NetworkManager".
///        Set the backendUrl in the Inspector to your server's address.
/// </summary>
public class NetworkManager : MonoBehaviour
{
    [Header("Backend Configuration")]
    [Tooltip("Base URL of your backend server (e.g. http://localhost:8000)")]
    public string backendUrl = "http://localhost:8000";

    [Tooltip("Endpoint path for wish requests")]
    public string wishEndpoint = "/api/wish";

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Singleton so other scripts can easily call NetworkManager.Instance
    public static NetworkManager Instance { get; private set; }

    // Event that fires when we get a response from the backend
    public event Action<WishResponse> OnWishResponse;
    public event Action<string> OnError;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Send a wish to the backend and get a response.
    /// Call this from your UI button handler.
    /// </summary>
    public void SendWish(string wishText)
    {
        if (string.IsNullOrWhiteSpace(wishText))
        {
            Debug.LogWarning("[NetworkManager] Wish text is empty, ignoring.");
            return;
        }

        if (debugMode) Debug.Log($"[NetworkManager] Sending wish: \"{wishText}\"");
        StartCoroutine(PostWish(wishText));
    }

    private IEnumerator PostWish(string wishText)
    {
        // Build the JSON payload
        string json = JsonUtility.ToJson(new WishRequest { wish = wishText });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        string url = backendUrl.TrimEnd('/') + wishEndpoint;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30; // 30 second timeout

            if (debugMode) Debug.Log($"[NetworkManager] POST {url}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                if (debugMode) Debug.Log($"[NetworkManager] Response: {responseText}");

                try
                {
                    WishResponse response = JsonUtility.FromJson<WishResponse>(responseText);
                    OnWishResponse?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkManager] Failed to parse response: {e.Message}");
                    OnError?.Invoke($"Parse error: {e.Message}");
                }
            }
            else
            {
                string error = $"HTTP {request.responseCode}: {request.error}";
                Debug.LogError($"[NetworkManager] Request failed: {error}");
                OnError?.Invoke(error);
            }
        }
    }

    // ──────────────────────────────────────────────
    // Data classes for JSON serialization
    // ──────────────────────────────────────────────

    [Serializable]
    public class WishRequest
    {
        public string wish;
    }

    /// <summary>
    /// Adjust these fields to match whatever your backend actually returns.
    /// Right now it expects: { "objectType": "bridge", "description": "A stone bridge", "success": true }
    /// </summary>
    [Serializable]
    public class WishResponse
    {
        public string objectType;
        public string description;
        public bool success;
        // Add more fields as your backend API evolves
    }
}
