using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Connects the UI (Button + InputField) to the NetworkManager.
/// 
/// SETUP:
///   1. Drag this onto your Canvas (or any GameObject).
///   2. In the Inspector, wire up:
///      - wishInputField  → your TMP_InputField
///      - wishButton      → your "MAKE WISH" Button
///      - statusText      → (optional) a TextMeshProUGUI to show feedback
/// </summary>
public class WishManager : MonoBehaviour
{
    [Header("UI References — Drag these in the Inspector")]
    [Tooltip("The TMP InputField where the user types their wish")]
    public TMP_InputField wishInputField;

    [Tooltip("The 'MAKE WISH' button")]
    public Button wishButton;

    [Tooltip("(Optional) Text element to show status/feedback")]
    public TextMeshProUGUI statusText;

    [Header("Settings")]
    [Tooltip("Clear the input field after sending?")]
    public bool clearAfterSend = true;

    private void Start()
    {
        // Wire up the button click
        if (wishButton != null)
        {
            wishButton.onClick.AddListener(OnWishButtonClicked);
        }
        else
        {
            Debug.LogError("[WishManager] wishButton is not assigned in the Inspector!");
        }

        // Subscribe to backend responses
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnWishResponse += HandleWishResponse;
            NetworkManager.Instance.OnError += HandleError;
        }
        else
        {
            Debug.LogError("[WishManager] NetworkManager.Instance is null! " +
                "Make sure a GameObject with NetworkManager exists in the scene and is set to awaken first.");
        }

        SetStatus("Ready. Type a wish and press the button.");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnWishResponse -= HandleWishResponse;
            NetworkManager.Instance.OnError -= HandleError;
        }
    }

    private void OnWishButtonClicked()
    {
        if (wishInputField == null)
        {
            Debug.LogError("[WishManager] wishInputField is not assigned!");
            return;
        }

        string wish = wishInputField.text;

        if (string.IsNullOrWhiteSpace(wish))
        {
            SetStatus("Please type a wish first!");
            return;
        }

        // Disable button while processing to prevent spam
        wishButton.interactable = false;
        SetStatus($"Sending wish: \"{wish}\"...");

        // Send it to the backend
        NetworkManager.Instance.SendWish(wish);

        if (clearAfterSend)
        {
            wishInputField.text = "";
        }
    }

    private void HandleWishResponse(NetworkManager.WishResponse response)
    {
        wishButton.interactable = true;

        if (response.success)
        {
            SetStatus($"Wish granted! Creating: {response.objectType}");
            Debug.Log($"[WishManager] Success! Object: {response.objectType}, Desc: {response.description}");

            // TODO: Tell your ObjectSpawner to create the object
            // ObjectSpawner.Instance.Spawn(response);
        }
        else
        {
            SetStatus("The wish could not be granted. Try again!");
        }
    }

    private void HandleError(string error)
    {
        wishButton.interactable = true;
        SetStatus($"Error: {error}");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[WishManager] {message}");
    }
}
