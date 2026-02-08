using UnityEngine;
using TMPro;

/// <summary>
/// Top-left on-screen HUD showing control hints.
/// Auto-detects Quest vs Desktop and shows appropriate icons.
/// Context-sensitive: changes labels based on game state.
/// </summary>
public class InputOverlay : MonoBehaviour
{
    private TextMeshProUGUI controlsText;
    private Canvas canvas;
    private bool isVR = false;

    // Game state references
    private GenieClient genieClient;
    private ItemPickup currentlyHeld;
    private bool nearDoor = false;
    private bool nearItem = false;

    void Start()
    {
        // Detect VR
        isVR = UnityEngine.XR.XRSettings.isDeviceActive;

        // Create canvas
        GameObject canvasObj = new GameObject("InputOverlayCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Background panel (top-left)
        GameObject panel = new GameObject("ControlsPanel");
        panel.transform.SetParent(canvasObj.transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(20, -20);
        panelRect.sizeDelta = new Vector2(280, 140);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0, 0, 0, 0.6f);

        // Round corners aren't built-in, so we just use the semi-transparent box

        // Controls text
        GameObject textObj = new GameObject("ControlsText");
        textObj.transform.SetParent(panel.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12, 8);
        textRect.offsetMax = new Vector2(-12, -8);

        controlsText = textObj.AddComponent<TextMeshProUGUI>();
        controlsText.fontSize = 16;
        controlsText.color = Color.white;
        controlsText.alignment = TextAlignmentOptions.TopLeft;
        controlsText.enableAutoSizing = false;
        controlsText.lineSpacing = 8;

        // Find GenieClient
        genieClient = FindObjectOfType<GenieClient>();

        UpdateDisplay();
    }

    void Update()
    {
        // Re-detect VR each frame (in case headset connects/disconnects)
        bool wasVR = isVR;
        isVR = UnityEngine.XR.XRSettings.isDeviceActive;

        // Check game state for context-sensitive labels
        if (genieClient != null)
        {
            currentlyHeld = genieClient.GetHeldItem();
            nearDoor = genieClient.IsNearDoor();
            nearItem = genieClient.IsNearPickupableItem();
        }

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (controlsText == null) return;

        string wish, pickup, hint;

        if (isVR)
        {
            wish = "<color=#FFD700>[X]</color>  Hold to Wish";
            hint = "<color=#FFD700>[B]</color>  Request Hint";

            if (currentlyHeld != null && nearDoor)
                pickup = "<color=#00FF88>[A]</color>  <color=#00FF88>Use on Door</color>";
            else if (currentlyHeld != null)
                pickup = "<color=#FFD700>[A]</color>  Drop Item";
            else if (nearItem)
                pickup = "<color=#00FF88>[A]</color>  <color=#00FF88>Pick Up</color>";
            else
                pickup = "<color=#888888>[A]</color>  <color=#888888>Pick Up</color>";
        }
        else
        {
            wish = "<color=#FFD700>[V]</color>  Hold to Wish";
            hint = "<color=#FFD700>[H]</color>  Request Hint";

            if (currentlyHeld != null && nearDoor)
                pickup = "<color=#00FF88>[E]</color>  <color=#00FF88>Use on Door</color>";
            else if (currentlyHeld != null)
                pickup = "<color=#FFD700>[E]</color>  Drop Item";
            else if (nearItem)
                pickup = "<color=#00FF88>[E]</color>  <color=#00FF88>Pick Up</color>";
            else
                pickup = "<color=#888888>[E]</color>  <color=#888888>Pick Up</color>";
        }

        controlsText.text = $"<b>CONTROLS</b>\n{wish}\n{pickup}\n{hint}";
    }
}
