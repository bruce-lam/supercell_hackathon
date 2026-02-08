using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Bridge between Unity and the FastAPI Genie backend.
/// Hold V to record a wish, release to send it.
/// On Start: fetches door rules from /get_rules.
/// After a door opens: advances to next room and plays transition voice.
/// </summary>
public class GenieClient : MonoBehaviour
{
    [Header("Backend")]
    public string serverUrl = "http://localhost:8000";

    [Header("References")]
    [Tooltip("One PipeSpawner per room, indexed by door (Element 0 = Room 1, etc.)")]
    public PipeSpawner[] pipeSpawners;
    public AudioSource genieAudioSource;

    [Header("UI")]
    [Tooltip("Same subtitle text used by GenieIntro — shows clues and wish responses")]
    public TextMeshProUGUI subtitleText;

    [Header("Door References")]
    [Tooltip("Assign your 3 door GameObjects here")]
    public GameObject[] doors;

    [Header("VFX Prefabs")]
    [Tooltip("Assign from Assets/Prefabs/VFX - run Hypnagogia > Generate VFX Prefabs first")]
    public GameObject vfxFire;
    public GameObject vfxSmoke;
    public GameObject vfxSparks;

    [Header("Door SFX")]
    [Tooltip("Loaded automatically from Assets/Audio/door_creak_open")]
    public AudioClip doorOpenSFX;

    [Header("Answer SFX")]
    [Tooltip("Played when the wish is correct/incorrect. Auto-loaded from Assets/Audio/")]
    public AudioClip successSFX;
    public AudioClip failSFX;

    [Header("Recording")]
    public int recordingLengthSec = 10;
    public int sampleRate = 44100;

    [Header("Active Door")]
    [Tooltip("Which door the player is trying (1, 2, or 3)")]
    public int currentDoorId = 1;

    private AudioClip micClip;
    private bool isRecording = false;
    private string micDevice;
    private bool isRequestingHint = false;

    // VR controller button state tracking (for edge detection)
    private bool vrXButtonWasDown = false;  // Left controller X button
    private bool vrBButtonWasDown = false;  // Right controller B button
    private bool vrAButtonWasDown = false;  // Right controller A button

    // --- Pickup / Door interaction state ---
    private ItemPickup heldItem = null;
    private ItemPickup nearestItem = null;
    private bool isNearDoor = false;
    private const float PICKUP_RANGE = 4.0f;
    private const float DOOR_RANGE = 5.0f;
    private Transform cachedPlayerCam = null;
    private float lastProximityLog = 0f;

    // --- Auto-created Interaction Prompt UI ---
    private TMPro.TextMeshProUGUI interactionPrompt;
    private TMPro.TextMeshProUGUI controlsHUD;
    private Canvas promptCanvas;

    // --- Session state: door rules from backend ---
    private DoorRule[] doorRules;
    public bool rulesLoaded { get; private set; } = false;

    /// <summary>Returns the PipeSpawner for the current room</summary>
    private PipeSpawner ActivePipeSpawner
    {
        get
        {
            if (pipeSpawners == null || pipeSpawners.Length == 0) return null;
            int idx = currentDoorId - 1;
            if (idx >= 0 && idx < pipeSpawners.Length) return pipeSpawners[idx];
            return pipeSpawners[0]; // fallback
        }
    }

    /// <summary>Returns the door_rules string for the current door to send to the backend</summary>
    private string CurrentDoorRulesString
    {
        get
        {
            if (doorRules == null || doorRules.Length == 0) return "No rules available";
            int idx = currentDoorId - 1;
            if (idx >= 0 && idx < doorRules.Length)
                return $"Door {currentDoorId} Law: {doorRules[idx].law}";
            return "No rules for this door";
        }
    }

    void Start()
    {
        // Auto-find PipeSpawners if not assigned
        if (pipeSpawners == null || pipeSpawners.Length == 0)
            pipeSpawners = FindObjectsByType<PipeSpawner>(FindObjectsSortMode.None);

        // Create AudioSource for Genie voice if not assigned
        if (genieAudioSource == null)
        {
            genieAudioSource = gameObject.AddComponent<AudioSource>();
            genieAudioSource.spatialBlend = 0f; // 2D audio
        }

        // Get default mic
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            Debug.Log($"[GenieClient] Mic found: {micDevice}");
        }
        else
        {
            Debug.LogWarning("[GenieClient] No microphone found! Voice input disabled.");
        }

        // Fetch door rules from backend
        StartCoroutine(FetchDoorRules());

        // Auto-load SFX if not assigned
        if (doorOpenSFX == null)
            doorOpenSFX = Resources.Load<AudioClip>("door_creak_open");
        if (successSFX == null)
            successSFX = Resources.Load<AudioClip>("success_chime");
        if (failSFX == null)
            failSFX = Resources.Load<AudioClip>("fail_buzzer");

        // Auto-create interaction prompt UI
        CreateInteractionUI();
    }

    void CreateInteractionUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("InteractionCanvas");
        promptCanvas = canvasObj.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // --- Centered interaction prompt (big, visible) ---
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(canvasObj.transform, false);

        var promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.3f);
        promptRect.anchorMax = new Vector2(0.5f, 0.3f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = Vector2.zero;
        promptRect.sizeDelta = new Vector2(500, 60);

        // Background
        var bgPanel = promptObj.AddComponent<UnityEngine.UI.Image>();
        bgPanel.color = new Color(0, 0, 0, 0.7f);

        // Text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 5);
        textRect.offsetMax = new Vector2(-15, -5);

        interactionPrompt = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        interactionPrompt.fontSize = 22;
        interactionPrompt.color = Color.white;
        interactionPrompt.alignment = TMPro.TextAlignmentOptions.Center;
        interactionPrompt.enableAutoSizing = false;
        promptObj.SetActive(false); // Hidden by default

        // --- Top-left controls HUD ---
        GameObject hudObj = new GameObject("ControlsHUD");
        hudObj.transform.SetParent(canvasObj.transform, false);

        var hudRect = hudObj.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 1);
        hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(20, -20);
        hudRect.sizeDelta = new Vector2(260, 120);

        var hudBg = hudObj.AddComponent<UnityEngine.UI.Image>();
        hudBg.color = new Color(0, 0, 0, 0.5f);

        GameObject hudTextObj = new GameObject("HUDText");
        hudTextObj.transform.SetParent(hudObj.transform, false);
        var hudTextRect = hudTextObj.AddComponent<RectTransform>();
        hudTextRect.anchorMin = Vector2.zero;
        hudTextRect.anchorMax = Vector2.one;
        hudTextRect.offsetMin = new Vector2(10, 5);
        hudTextRect.offsetMax = new Vector2(-10, -5);

        controlsHUD = hudTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        controlsHUD.fontSize = 15;
        controlsHUD.color = Color.white;
        controlsHUD.alignment = TMPro.TextAlignmentOptions.TopLeft;
        controlsHUD.enableAutoSizing = false;
        controlsHUD.lineSpacing = 6;

        bool isVR = UnityEngine.XR.XRSettings.isDeviceActive;
        if (isVR)
            controlsHUD.text = "<b>CONTROLS</b>\n<color=#FFD700>[X]</color> Hold to Wish\n<color=#FFD700>[A]</color> Pick Up / Use\n<color=#FFD700>[B]</color> Hint";
        else
            controlsHUD.text = "<b>CONTROLS</b>\n<color=#FFD700>[V]</color> Hold to Wish\n<color=#FFD700>[E]</color> Pick Up / Use\n<color=#FFD700>[H]</color> Hint";

        Debug.Log("[GenieClient] ✅ Interaction UI created");
    }

    // =====================================================
    // FETCH DOOR RULES from /get_rules on session start
    // =====================================================
    IEnumerator FetchDoorRules()
    {
        string url = $"{serverUrl}/get_rules";
        Debug.Log("[GenieClient] 🎲 Fetching door rules from backend...");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GenieClient] ❌ Failed to fetch rules: {request.error}");
                // Use fallback rules
                doorRules = new DoorRule[] {
                    new DoorRule { law = "Must be red", clues = new string[] { "Bring me the color of passion.", "Something red.", "RED. Just make it red." } },
                    new DoorRule { law = "Must be metal", clues = new string[] { "Only cold iron opens this path.", "Something metallic.", "METAL. Give me metal." } },
                    new DoorRule { law = "Must be round", clues = new string[] { "I seek something with no end.", "Something that rolls.", "A SPHERE. Just a sphere." } }
                };
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"[GenieClient] ✅ Door rules received: {json}");

            // Parse the response
            RulesResponse response = JsonUtility.FromJson<RulesResponse>(json);
            if (response != null && response.doors != null)
            {
                doorRules = response.doors;

                for (int i = 0; i < doorRules.Length; i++)
                {
                    int clueCount = doorRules[i].clues != null ? doorRules[i].clues.Length : 0;
                    Debug.Log($"[GenieClient] 🚪 Door {i + 1} | Law: {doorRules[i].law} | Clues: {clueCount}");
                }
            }

            rulesLoaded = true;
            Debug.Log("[GenieClient] ✅ Rules loaded and ready.");
        }
    }

    void Update()
    {
        // === KEYBOARD INPUT (Desktop / Editor) ===
        var keyboard = Keyboard.current;
        bool keyboardRecordPressed = keyboard != null && keyboard.vKey.wasPressedThisFrame;
        bool keyboardRecordReleased = keyboard != null && keyboard.vKey.wasReleasedThisFrame;
        bool keyboardHintPressed = keyboard != null && keyboard.hKey.wasPressedThisFrame;
        bool keyboardDebugPressed = keyboard != null && keyboard.fKey.wasPressedThisFrame;

        // === VR CONTROLLER INPUT (Meta Quest) ===
        bool vrRecordPressed = false;
        bool vrRecordReleased = false;
        bool vrHintPressed = false;

        // Check left controller X button (primaryButton) for push-to-talk
        UnityEngine.XR.InputDevice leftHand = default;
        var leftHandDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0)
        {
            leftHand = leftHandDevices[0];
            bool xButtonDown = false;
            if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out xButtonDown))
            {
                if (xButtonDown && !vrXButtonWasDown)
                    vrRecordPressed = true;     // Just pressed
                if (!xButtonDown && vrXButtonWasDown)
                    vrRecordReleased = true;    // Just released
                vrXButtonWasDown = xButtonDown;
            }
        }

        // Check right controller B button (primaryButton) for hints
        var rightHandDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
        {
            bool bButtonDown = false;
            if (rightHandDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bButtonDown))
            {
                if (bButtonDown && !vrBButtonWasDown)
                    vrHintPressed = true;
                vrBButtonWasDown = bButtonDown;
            }
        }

        // Check right controller A button (secondaryButton) for pickup/use
        bool vrActionPressed = false;
        if (rightHandDevices.Count > 0)
        {
            bool aButtonDown = false;
            if (rightHandDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out aButtonDown))
            {
                if (aButtonDown && !vrAButtonWasDown)
                    vrActionPressed = true;
                vrAButtonWasDown = aButtonDown;
            }
        }

        bool keyboardActionPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;

        // === PROXIMITY DETECTION ===
        UpdateProximity();

        // === COMBINED INPUT ===
        // Hold to record (V key or X button)
        if ((keyboardRecordPressed || vrRecordPressed) && !isRecording)
        {
            StartRecording();
        }

        if ((keyboardRecordReleased || vrRecordReleased) && isRecording)
        {
            StopRecordingAndSend();
        }

        // Press E or A button for pickup / door interaction
        if (keyboardActionPressed || vrActionPressed)
        {
            HandleActionButton();
        }

        // Press H or B button to request a progressive hint
        if ((keyboardHintPressed || vrHintPressed) && !isRequestingHint)
        {
            StartCoroutine(RequestHint());
        }

        // Press F to debug-spawn a random item from the ACTIVE room's pipe only
        if (keyboardDebugPressed)
        {
            PipeSpawner pipe = ActivePipeSpawner;
            if (pipe != null)
            {
                pipe.SpawnRandomItem();
                Debug.Log($"[GenieClient] 🧪 Debug spawn from pipe for Door {currentDoorId}");

                // Auto-add ItemPickup to debug-spawned objects so they're pickupable
                StartCoroutine(AddItemPickupToNewObjects());
            }
            else
            {
                Debug.LogWarning("[GenieClient] No active pipe found for debug spawn!");
            }
        }
    }

    // Ensure all spawned objects eventually get ItemPickup (safety net)
    System.Collections.IEnumerator AddItemPickupToNewObjects()
    {
        yield return new WaitForSeconds(0.5f); // Wait for physics to settle
        // Find any Rigidbody without ItemPickup and add one
        foreach (var rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            if (rb.GetComponent<ItemPickup>() == null && rb.GetComponent<PipeSpawner>() == null
                && rb.GetComponent<CharacterController>() == null)
            {
                var pickup = rb.gameObject.AddComponent<ItemPickup>();
                Debug.Log($"[GenieClient] 🔧 Auto-added ItemPickup to '{rb.gameObject.name}'");
            }
        }
    }

    // =====================================================
    // PROXIMITY DETECTION — find nearby items and doors
    // =====================================================
    void UpdateProximity()
    {
        // Robust camera finding — Camera.main requires 'MainCamera' tag!
        if (cachedPlayerCam == null || !cachedPlayerCam.gameObject.activeInHierarchy)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                // Fallback: find any active camera in the scene
                mainCam = FindAnyObjectByType<Camera>();
                if (mainCam != null)
                    Debug.LogWarning($"[GenieClient] Camera.main is null! Using fallback camera: '{mainCam.gameObject.name}'. Consider adding 'MainCamera' tag.");
            }
            if (mainCam == null)
            {
                if (Time.time - lastProximityLog > 5f)
                {
                    Debug.LogError("[GenieClient] ❌ No camera found in scene! Pickup detection disabled.");
                    lastProximityLog = Time.time;
                }
                return;
            }
            cachedPlayerCam = mainCam.transform;
        }

        // Use XZ distance (ignore height) for ground-level items
        Vector3 playerXZ = new Vector3(cachedPlayerCam.position.x, 0, cachedPlayerCam.position.z);

        // Find nearest pickupable item
        ItemPickup closest = null;
        float closestDist = PICKUP_RANGE;
        int totalItems = 0;
        int pickupableItems = 0;
        float nearestAnyDist = float.MaxValue;

        foreach (var item in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
        {
            totalItems++;
            if (item == null) continue;
            if (item.isHeld || !item.isPickupable) continue;
            pickupableItems++;

            // XZ distance so objects on the floor are reachable from standing height
            Vector3 itemXZ = new Vector3(item.transform.position.x, 0, item.transform.position.z);
            float dist = Vector3.Distance(playerXZ, itemXZ);

            if (dist < nearestAnyDist) nearestAnyDist = dist;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = item;
            }
        }

        // Periodic debug logging (every 2 seconds)
        if (Time.time - lastProximityLog > 2f && totalItems > 0)
        {
            Debug.Log($"[GenieClient] 📍 Proximity: {totalItems} ItemPickup found, {pickupableItems} pickupable, nearest={nearestAnyDist:F1}m (range={PICKUP_RANGE}m), cam='{cachedPlayerCam.gameObject.name}' pos={cachedPlayerCam.position}");
            lastProximityLog = Time.time;
        }

        // Update highlight on nearest item
        if (nearestItem != null && nearestItem != closest)
            nearestItem.SetHighlight(false);

        ItemPickup prevNearest = nearestItem;
        nearestItem = closest;

        if (nearestItem != null)
        {
            nearestItem.SetHighlight(true);
            if (prevNearest != nearestItem)
                Debug.Log($"[GenieClient] 👁️ Near item: '{nearestItem.gameObject.name}' (dist={closestDist:F1}m)");
        }

        // Check if near a door (also XZ distance)
        isNearDoor = false;
        int doorIndex = currentDoorId - 1;
        if (doors != null && doorIndex >= 0 && doorIndex < doors.Length && doors[doorIndex] != null)
        {
            Vector3 doorXZ = new Vector3(doors[doorIndex].transform.position.x, 0, doors[doorIndex].transform.position.z);
            float doorDist = Vector3.Distance(playerXZ, doorXZ);
            isNearDoor = doorDist < DOOR_RANGE;
        }

        // Update interaction prompt
        UpdateInteractionPrompt();
    }

    void UpdateInteractionPrompt()
    {
        if (interactionPrompt == null) return;

        bool isVR = UnityEngine.XR.XRSettings.isDeviceActive;
        string key = isVR ? "A" : "E";
        GameObject promptPanel = interactionPrompt.transform.parent.gameObject;

        // Clean item names (strip "(Clone)" suffix)
        string heldName = heldItem != null ? heldItem.gameObject.name.Replace("(Clone)", "").Trim() : "";
        string nearName = nearestItem != null ? nearestItem.gameObject.name.Replace("(Clone)", "").Trim() : "";

        if (heldItem != null && isNearDoor)
        {
            promptPanel.SetActive(true);
            interactionPrompt.text = $"Press <color=#00FF88><b>[{key}]</b></color> to try <color=#FFD700>{heldName}</color> on the door";
        }
        else if (heldItem != null)
        {
            promptPanel.SetActive(true);
            interactionPrompt.text = $"Holding <color=#FFD700>{heldName}</color>  —  Press <color=#FF6666><b>[{key}]</b></color> to drop\n<color=#888888><size=70%>Walk to a door to try it!</size></color>";
        }
        else if (nearestItem != null)
        {
            promptPanel.SetActive(true);
            interactionPrompt.text = $"Press <color=#00FF88><b>[{key}]</b></color> to pick up <color=#FFD700>{nearName}</color>";
        }
        else
        {
            promptPanel.SetActive(false);
        }
    }

    // =====================================================
    // ACTION BUTTON — pickup item or use on door
    // =====================================================
    void HandleActionButton()
    {
        Debug.Log($"[GenieClient] 🎮 Action pressed! heldItem={heldItem?.gameObject.name ?? "none"}, nearestItem={nearestItem?.gameObject.name ?? "none"}, nearDoor={isNearDoor}");

        // Priority 1: If holding an item and near a door → try on door
        if (heldItem != null && isNearDoor)
        {
            Debug.Log($"[GenieClient] 🚪 Trying '{heldItem.gameObject.name}' on door!");
            TryObjectOnDoor();
            return;
        }

        // Priority 2: If holding an item but NOT near door → drop it
        if (heldItem != null)
        {
            Debug.Log($"[GenieClient] 📦 Dropping '{heldItem.gameObject.name}'");
            heldItem.Drop();
            heldItem = null;
            return;
        }

        // Priority 3: If near a pickupable item → pick it up
        if (nearestItem != null)
        {
            heldItem = nearestItem;
            heldItem.Pickup();
            Debug.Log($"[GenieClient] 🤚 Picked up: '{heldItem.gameObject.name}'");
            nearestItem = null;
            return;
        }

        Debug.Log("[GenieClient] ❌ Action pressed but nothing nearby to interact with");
    }

    // =====================================================
    // TRY OBJECT ON DOOR — play stored verdict
    // =====================================================
    void TryObjectOnDoor()
    {
        if (heldItem == null) return;

        Debug.Log($"[GenieClient] 🚪 Trying '{heldItem.gameObject.name}' on Door {currentDoorId}");

        if (heldItem.doorOpen)
        {
            // SUCCESS!
            StartCoroutine(PlayDoorSuccess(heldItem));
        }
        else
        {
            // WRONG — play rejection
            StartCoroutine(PlayDoorFail(heldItem));
        }
    }

    IEnumerator PlayDoorSuccess(ItemPickup item)
    {
        // Play success chime
        if (successSFX != null && genieAudioSource != null)
            genieAudioSource.PlayOneShot(successSFX, 0.8f);

        // Play congrats voice
        if (!string.IsNullOrEmpty(item.audioUrlCongrats))
        {
            ShowSubtitle(item.congratsVoice);
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(PlayAudioFromUrl(serverUrl + item.audioUrlCongrats));
        }

        yield return new WaitForSeconds(1f);

        // Destroy the held item
        Destroy(item.gameObject);
        heldItem = null;

        // Open the door
        int doorIndex = currentDoorId - 1;
        if (doors != null && doorIndex >= 0 && doorIndex < doors.Length && doors[doorIndex] != null)
        {
            StartCoroutine(AnimateDoorOpen(doors[doorIndex]));
            Debug.Log($"[GenieClient] 🚪 Door {currentDoorId} OPENED!");
        }

        // Wait for congrats to finish, then advance
        yield return new WaitForSeconds(5f);

        if (currentDoorId < 3)
        {
            currentDoorId++;
            Debug.Log($"[GenieClient] ➡️ Advanced to Door {currentDoorId}");
            StartCoroutine(PlayRoomTransition(currentDoorId));
        }
        else
        {
            Debug.Log("[GenieClient] 🏆 ALL DOORS OPENED! Player wins!");
        }
    }

    IEnumerator PlayDoorFail(ItemPickup item)
    {
        // Play fail buzzer
        if (failSFX != null && genieAudioSource != null)
            genieAudioSource.PlayOneShot(failSFX, 0.7f);

        // Play rejection voice
        if (!string.IsNullOrEmpty(item.audioUrlCongrats))
        {
            ShowSubtitle(item.congratsVoice);
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(PlayAudioFromUrl(serverUrl + item.audioUrlCongrats));
        }

        // Drop the item back to the ground
        item.Drop();
        item.isPickupable = true; // Can try again or pick up another
        heldItem = null;
    }

    // --- Public accessors for InputOverlay ---
    public ItemPickup GetHeldItem() => heldItem;
    public bool IsNearDoor() => isNearDoor;
    public bool IsNearPickupableItem() => nearestItem != null;

    void StartRecording()
    {
        if (string.IsNullOrEmpty(micDevice)) return;

        isRecording = true;
        micClip = Microphone.Start(micDevice, false, recordingLengthSec, sampleRate);
        Debug.Log("[GenieClient] 🎙️ Recording... (release V to send)");
    }

    void StopRecordingAndSend()
    {
        if (!isRecording) return;
        isRecording = false;

        int lastSample = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);

        if (lastSample <= 0)
        {
            Debug.LogWarning("[GenieClient] Recording was empty.");
            return;
        }

        // Trim the clip to actual recorded length
        float[] samples = new float[lastSample * micClip.channels];
        micClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create("wish", lastSample, micClip.channels, sampleRate, false);
        trimmedClip.SetData(samples, 0);

        Debug.Log($"[GenieClient] 🎙️ Recorded {lastSample / (float)sampleRate:F1}s of audio. Sending to Genie...");

        // Convert to WAV and send
        byte[] wavData = AudioClipToWav(trimmedClip);
        StartCoroutine(SendWishToBackend(wavData));
    }

    IEnumerator SendWishToBackend(byte[] wavData)
    {
        string url = $"{serverUrl}/process_wish";

        // Build multipart form — now includes door_rules from session
        List<IMultipartFormSection> form = new List<IMultipartFormSection>();
        form.Add(new MultipartFormDataSection("door_id", currentDoorId.ToString()));
        form.Add(new MultipartFormDataSection("door_rules", CurrentDoorRulesString));
        form.Add(new MultipartFormFileSection("file", wavData, "wish.wav", "audio/wav"));

        Debug.Log($"[GenieClient] 📤 Sending wish for Door {currentDoorId} with rules: {CurrentDoorRulesString}");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GenieClient] ❌ Backend error: {request.error}");
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"[GenieClient] ✅ Genie response: {json}");

            // Parse the response
            GenieResponse response = JsonUtility.FromJson<GenieResponse>(json);

            // 1. Spawn the item from the CURRENT ROOM's pipe only
            PipeSpawner pipeSpawner = ActivePipeSpawner;
            if (pipeSpawner != null && !string.IsNullOrEmpty(response.object_name))
            {
                // Parse hex color first (needed for both prefab and fallback)
                Color genieColor = Color.white;
                if (!string.IsNullOrEmpty(response.hex_color))
                    ColorUtility.TryParseHtmlString(response.hex_color, out genieColor);

                // Try to spawn from prefab library
                GameObject spawned = pipeSpawner.SpawnItem(response.object_name);

                // Fallback: create a labeled cube if no matching prefab
                if (spawned == null)
                {
                    string label = !string.IsNullOrEmpty(response.display_name)
                        ? response.display_name
                        : response.object_name;
                    spawned = pipeSpawner.SpawnFallbackCube(label, genieColor);
                }

                if (spawned != null)
                {
                    // Apply hex color from Genie (only for real prefabs; fallback already has it)
                    if (!string.IsNullOrEmpty(response.hex_color))
                    {
                        Renderer rend = spawned.GetComponent<Renderer>();
                        if (rend != null && rend.sharedMaterial != null)
                        {
                            Material mat = new Material(rend.sharedMaterial);
                            mat.color = genieColor;
                            mat.SetColor("_BaseColor", genieColor);
                            rend.material = mat;
                        }
                    }

                    // Apply scale from Genie
                    if (response.scale > 0)
                    {
                        spawned.transform.localScale *= response.scale;
                    }

                    // Apply VFX from Genie
                    AttachVFX(spawned, response.vfx_type);

                    // Add floating label with display_name (if not already a fallback cube with label)
                    if (!string.IsNullOrEmpty(response.display_name)
                        && spawned.GetComponentInChildren<TMPro.TextMeshPro>() == null)
                    {
                        AddFloatingLabel(spawned, response.display_name);
                    }

                    // === FIX: Prevent objects falling through floor ===
                    Rigidbody rb = spawned.GetComponent<Rigidbody>();
                    if (rb == null) rb = spawned.GetComponentInChildren<Rigidbody>();
                    if (rb != null)
                    {
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                    // Ensure colliders exist and are properly sized
                    if (spawned.GetComponentInChildren<Collider>() == null)
                    {
                        BoxCollider bc = spawned.AddComponent<BoxCollider>();
                        Renderer[] renderers = spawned.GetComponentsInChildren<Renderer>();
                        if (renderers.Length > 0)
                        {
                            Bounds bounds = renderers[0].bounds;
                            for (int i = 1; i < renderers.Length; i++)
                                bounds.Encapsulate(renderers[i].bounds);
                            bc.center = spawned.transform.InverseTransformPoint(bounds.center);
                            bc.size = bounds.size;
                        }
                    }
                    // Add floor safety net: clamp Y position so it can't go below floor
                    var safety = spawned.AddComponent<FloorSafety>();

                    // === STORE VERDICT on ItemPickup (deferred until door interaction) ===
                    ItemPickup pickup = spawned.AddComponent<ItemPickup>();
                    pickup.doorOpen = response.door_open;
                    pickup.congratsVoice = response.congrats_voice;
                    pickup.audioUrlCongrats = response.audio_url_congrats;
                    pickup.dropVoice = response.drop_voice;
                }
            }

            // 2. Play drop voice audio with subtitle (plays immediately — just the sarcastic object reaction)
            if (!string.IsNullOrEmpty(response.audio_url_drop))
            {
                ShowSubtitle(response.drop_voice);
                StartCoroutine(PlayAudioFromUrl(serverUrl + response.audio_url_drop));
            }

            // 3. Verdict is DEFERRED — no success/fail here!
            // The player must pick up the object and try it on the door.
            // See TryObjectOnDoor() / PlayDoorSuccess() / PlayDoorFail()
        }
    }

    // NOTE: PlayFailAfterDelay and OpenDoorAfterDelay have been replaced by
    // PlayDoorFail() and PlayDoorSuccess() — triggered via TryObjectOnDoor()
    // when the player physically uses an object on the door.

    // =====================================================
    // ROOM TRANSITION — play congrats + next clue
    // =====================================================
    IEnumerator PlayRoomTransition(int doorId)
    {
        string url = $"{serverUrl}/room_transition?door_id={doorId}";
        Debug.Log($"[GenieClient] 🎙️ Fetching room transition for Door {doorId}...");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GenieClient] ⚠️ Transition audio failed: {request.error}");
                yield break;
            }

            string json = request.downloadHandler.text;
            TransitionResponse transition = JsonUtility.FromJson<TransitionResponse>(json);

            if (!string.IsNullOrEmpty(transition.audio_url))
            {
                ShowSubtitle(transition.subtitle, 60f); // Very long duration — keep visible during audio + reading
                yield return StartCoroutine(PlayAudioFromUrl(serverUrl + transition.audio_url));
                // Keep subtitle visible for extra reading time after audio finishes
                yield return new WaitForSeconds(20f);
                HideSubtitle();
            }

            Debug.Log($"[GenieClient] 🎉 Room transition: {transition.subtitle}");
        }
    }

    IEnumerator AnimateDoorOpen(GameObject door)
    {
        // Use the Easy Door System's built-in open (respects saved open/closed states)
        // Search children too — EasyDoor component is on the "Door" child, not the parent
        var easyDoor = door.GetComponentInChildren<EasyDoorSystem.EasyDoor>();

        // Play door creak SFX
        if (doorOpenSFX != null)
        {
            AudioSource doorAudio = door.GetComponentInChildren<AudioSource>();
            if (doorAudio != null)
            {
                doorAudio.clip = doorOpenSFX;
                doorAudio.spatialBlend = 0f; // 2D so player always hears it
                doorAudio.Play();
            }
            else if (genieAudioSource != null)
            {
                genieAudioSource.PlayOneShot(doorOpenSFX);
            }
        }

        if (easyDoor != null)
        {
            easyDoor.OpenDoor();
            Debug.Log($"[GenieClient] 🚪 Opening {easyDoor.gameObject.name} via EasyDoor system (parent: {door.name})");
        }
        else
        {
            // Fallback: simple Y rotation if no EasyDoor component
            float duration = 1.5f;
            float elapsed = 0f;
            Quaternion startRot = door.transform.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(0f, -90f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                door.transform.localRotation = Quaternion.Lerp(startRot, endRot, t);
                yield return null;
            }

            door.transform.localRotation = endRot;
        }
        yield return null;
    }

    IEnumerator PlayAudioFromUrl(string url)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                genieAudioSource.clip = clip;
                genieAudioSource.Play();
                Debug.Log($"[GenieClient] 🔊 Playing audio from: {url}");
            }
            else
            {
                Debug.LogWarning($"[GenieClient] Failed to load audio: {www.error}");
            }
        }
    }

    // --- SUBTITLES ---
    void ShowSubtitle(string text, float duration = -1f)
    {
        if (subtitleText == null || string.IsNullOrEmpty(text)) return;
        StopCoroutine("AutoHideSubtitle");
        subtitleText.text = text;
        subtitleText.alpha = 1f;

        // Calculate duration: ~80ms per character, minimum 10s
        if (duration < 0)
            duration = Mathf.Max(10f, text.Length * 0.08f);

        autoHideDuration = duration;
        StartCoroutine("AutoHideSubtitle");
    }

    void HideSubtitle()
    {
        StopCoroutine("AutoHideSubtitle");
        if (subtitleText != null)
        {
            subtitleText.alpha = 0f;
            subtitleText.text = "";
        }
    }

    private float autoHideDuration = 10f;

    IEnumerator AutoHideSubtitle()
    {
        yield return new WaitForSeconds(autoHideDuration);
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            if (subtitleText != null)
                subtitleText.alpha = Mathf.Lerp(1f, 0f, elapsed);
            yield return null;
        }
        if (subtitleText != null)
        {
            subtitleText.alpha = 0f;
            subtitleText.text = "";
        }
    }
    // --- FLOATING LABELS ---
    void AddFloatingLabel(GameObject target, string displayName)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(target.transform);

        // Position above the object based on its bounds
        Renderer rend = target.GetComponentInChildren<Renderer>();
        float height = 1.0f;
        if (rend != null)
            height = rend.bounds.size.y + 0.3f;
        labelObj.transform.localPosition = Vector3.up * height;

        var tmp = labelObj.AddComponent<TMPro.TextMeshPro>();
        tmp.text = displayName;
        tmp.fontSize = 2.5f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = false;
        tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);

        // Billboard effect
        labelObj.AddComponent<BillboardLabel>();
    }

    // --- VFX ---
    void AttachVFX(GameObject target, string vfxType)
    {
        if (string.IsNullOrEmpty(vfxType) || vfxType == "none") return;

        Color startColor, endColor;
        float startSize = 0.3f;
        float lifetime = 1.5f;
        float rate = 15f;
        float speed = 1f;

        switch (vfxType.ToLower())
        {
            case "fire":
                startColor = new Color(1f, 0.6f, 0f, 1f); // Orange
                endColor = new Color(1f, 0f, 0f, 0f);      // Fade to red
                startSize = 0.3f; lifetime = 0.8f; rate = 25f; speed = 1.5f;
                break;
            case "smoke":
                startColor = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Gray
                endColor = new Color(0.3f, 0.3f, 0.3f, 0f);
                startSize = 0.5f; lifetime = 2f; rate = 10f; speed = 0.5f;
                break;
            case "sparks":
                startColor = new Color(1f, 0.9f, 0.3f, 1f); // Yellow
                endColor = new Color(1f, 0.5f, 0f, 0f);
                startSize = 0.1f; lifetime = 0.5f; rate = 30f; speed = 3f;
                break;
            default:
                Debug.LogWarning($"[GenieClient] Unknown VFX type: {vfxType}");
                return;
        }

        // Create particle system at runtime — no prefab needed
        GameObject vfxObj = new GameObject($"VFX_{vfxType}");
        vfxObj.transform.SetParent(target.transform);
        vfxObj.transform.localPosition = Vector3.up * 0.2f;

        var ps = vfxObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = startSize;
        main.startColor = startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Use URP-compatible particle material
        var renderer = vfxObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.material.color = startColor;

        // Auto-destroy after 15 seconds
        Destroy(vfxObj, 15f);

        Debug.Log($"[GenieClient] ✨ Attached {vfxType} VFX to {target.name}");
    }

    // --- WAV Encoding ---
    byte[] AudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int sampleCount = samples.Length;
            int channels = clip.channels;
            int freq = clip.frequency;
            int bitsPerSample = 16;

            // WAV header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(freq);
            writer.Write(freq * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write((short)bitsPerSample);

            // data chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(sampleCount * 2);

            foreach (float sample in samples)
            {
                short s = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(s);
            }

            return stream.ToArray();
        }
    }

    // =====================================================
    // REQUEST HINT — H key asks for progressive hint
    // =====================================================
    IEnumerator RequestHint()
    {
        isRequestingHint = true;
        string url = $"{serverUrl}/get_hint?door_id={currentDoorId}";
        Debug.Log($"[GenieClient] 💡 Requesting hint for Door {currentDoorId}...");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GenieClient] ⚠️ Hint request failed: {request.error}");
                isRequestingHint = false;
                yield break;
            }

            string json = request.downloadHandler.text;
            HintResponse hint = JsonUtility.FromJson<HintResponse>(json);

            // Show subtitle
            ShowSubtitle(hint.hint);

            // Play voiced hint
            if (!string.IsNullOrEmpty(hint.audio_url))
            {
                yield return StartCoroutine(PlayAudioFromUrl(serverUrl + hint.audio_url));
            }

            Debug.Log($"[GenieClient] 💡 Hint level {hint.hint_level} | Remaining: {hint.hints_remaining}");
        }

        isRequestingHint = false;
    }

    // --- Data classes for JSON parsing ---

    [System.Serializable]
    public class DoorRule
    {
        public string law;
        public string[] clues;  // 3 progressive clues: Hard, Medium, Easy
    }

    [System.Serializable]
    public class RulesResponse
    {
        public DoorRule[] doors;
    }

    [System.Serializable]
    public class TransitionResponse
    {
        public string audio_url;
        public string subtitle;
    }

    [System.Serializable]
    public class HintResponse
    {
        public string hint;
        public string audio_url;
        public int hint_level;
        public int hints_remaining;
    }

    [System.Serializable]
    public class GenieResponse
    {
        public string object_name;
        public string display_name;
        public string hex_color;
        public float scale;
        public string vfx_type;
        public bool door_open;
        public string drop_voice;
        public string congrats_voice;
        public string audio_url_drop;
        public string audio_url_congrats;
        public string audio_url;
    }
}
