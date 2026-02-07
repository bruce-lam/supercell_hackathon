using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.InputSystem;

/// <summary>
/// Bridge between Unity and the FastAPI Genie backend.
/// Hold V to record a wish, release to send it.
/// The Genie responds with an item to spawn and voice audio.
/// </summary>
public class GenieClient : MonoBehaviour
{
    [Header("Backend")]
    public string serverUrl = "http://localhost:8000";

    [Header("References")]
    public PipeSpawner pipeSpawner;
    public AudioSource genieAudioSource;

    [Header("Door References")]
    [Tooltip("Assign your 3 door GameObjects here")]
    public GameObject[] doors;

    [Header("VFX Prefabs")]
    [Tooltip("Assign from Assets/Prefabs/VFX - run Hypnagogia > Generate VFX Prefabs first")]
    public GameObject vfxFire;
    public GameObject vfxSmoke;
    public GameObject vfxSparks;

    [Header("Recording")]
    public int recordingLengthSec = 10;
    public int sampleRate = 44100;

    [Header("Active Door")]
    [Tooltip("Which door the player is trying (1, 2, or 3)")]
    public int currentDoorId = 1;

    private AudioClip micClip;
    private bool isRecording = false;
    private string micDevice;

    void Start()
    {
        // Find PipeSpawner if not assigned
        if (pipeSpawner == null)
            pipeSpawner = FindObjectOfType<PipeSpawner>();

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
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Hold V to record
        if (keyboard.vKey.wasPressedThisFrame && !isRecording)
        {
            StartRecording();
        }

        if (keyboard.vKey.wasReleasedThisFrame && isRecording)
        {
            StopRecordingAndSend();
        }
    }

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

        // Build multipart form
        List<IMultipartFormSection> form = new List<IMultipartFormSection>();
        form.Add(new MultipartFormDataSection("door_id", currentDoorId.ToString()));
        form.Add(new MultipartFormFileSection("file", wavData, "wish.wav", "audio/wav"));

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

            // 1. Spawn the item from the pipe
            if (pipeSpawner != null && !string.IsNullOrEmpty(response.object_name))
            {
                GameObject spawned = pipeSpawner.SpawnItem(response.object_name);

                if (spawned != null)
                {
                    // Apply hex color from Genie
                    if (!string.IsNullOrEmpty(response.hex_color))
                    {
                        Color genieColor;
                        if (ColorUtility.TryParseHtmlString(response.hex_color, out genieColor))
                        {
                            Renderer rend = spawned.GetComponent<Renderer>();
                            if (rend != null)
                            {
                                Material mat = new Material(rend.sharedMaterial);
                                mat.color = genieColor;
                                mat.SetColor("_BaseColor", genieColor);
                                rend.material = mat;
                            }
                        }
                    }

                    // Apply scale from Genie
                    if (response.scale > 0)
                    {
                        spawned.transform.localScale *= response.scale;
                    }

                    // Apply VFX from Genie
                    AttachVFX(spawned, response.vfx_type);
                }
            }

            // 2. Play drop voice audio
            if (!string.IsNullOrEmpty(response.audio_url_drop))
            {
                StartCoroutine(PlayAudioFromUrl(serverUrl + response.audio_url_drop));
            }

            // 3. Handle door opening (after a delay for dramatic effect)
            if (response.door_open)
            {
                StartCoroutine(OpenDoorAfterDelay(response));
            }
        }
    }

    IEnumerator OpenDoorAfterDelay(GenieResponse response)
    {
        // Wait for drop voice to finish, then play congrats
        yield return new WaitForSeconds(4f);

        // Play congrats voice
        if (!string.IsNullOrEmpty(response.audio_url_congrats))
        {
            StartCoroutine(PlayAudioFromUrl(serverUrl + response.audio_url_congrats));
        }

        // Open the door
        int doorIndex = currentDoorId - 1;
        if (doors != null && doorIndex >= 0 && doorIndex < doors.Length && doors[doorIndex] != null)
        {
            // Simple door open: rotate 90 degrees
            StartCoroutine(AnimateDoorOpen(doors[doorIndex]));
            Debug.Log($"[GenieClient] 🚪 Door {currentDoorId} OPENED!");
        }
    }

    IEnumerator AnimateDoorOpen(GameObject door)
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Quaternion startRot = door.transform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 90f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            door.transform.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        door.transform.localRotation = endRot;
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

    // --- VFX ---
    void AttachVFX(GameObject target, string vfxType)
    {
        if (string.IsNullOrEmpty(vfxType) || vfxType == "none") return;

        GameObject vfxPrefab = null;
        switch (vfxType.ToLower())
        {
            case "fire":  vfxPrefab = vfxFire; break;
            case "smoke": vfxPrefab = vfxSmoke; break;
            case "sparks": vfxPrefab = vfxSparks; break;
        }

        if (vfxPrefab == null)
        {
            Debug.LogWarning($"[GenieClient] VFX type '{vfxType}' not found or prefab not assigned.");
            return;
        }

        GameObject vfx = Instantiate(vfxPrefab, target.transform);
        vfx.transform.localPosition = Vector3.up * 0.2f; // Slightly above center
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
