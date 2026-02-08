using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

/// <summary>
/// Plays a Genie intro monologue when the scene loads.
/// Fetches audio from the backend's /intro endpoint and shows subtitles.
/// 
/// SETUP:
///   1. Add this to the same GameObject as GenieClient
///   2. Create a world-space Canvas with a TextMeshProUGUI for subtitles
///   3. Drag the subtitle text into the "subtitleText" field
///   4. Optionally create a persistent hint TextMeshProUGUI for "Hold trigger to wish"
/// </summary>
public class GenieIntro : MonoBehaviour
{
    [Header("Backend")]
    [Tooltip("Must match GenieClient's serverUrl")]
    public string serverUrl = "http://localhost:8000";

    [Header("UI References")]
    [Tooltip("World-space TextMeshProUGUI for subtitles")]
    public TextMeshProUGUI subtitleText;

    [Tooltip("Persistent hint text (e.g. 'Hold trigger to wish')")]
    public TextMeshProUGUI hintText;

    [Header("Timing")]
    public float introDelay = 0f;
    public float subtitleFadeSpeed = 2f;

    private AudioSource audioSource;
    private bool introPlayed = false;

    // JSON response from /intro
    [System.Serializable]
    private class IntroResponse
    {
        public string audio_url;
        public string subtitle;
    }

    void Start()
    {
        // Reuse GenieClient's AudioSource or create one
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Hide UI initially
        if (subtitleText != null)
        {
            subtitleText.alpha = 0f;
            subtitleText.text = "";
        }

        if (hintText != null)
        {
            hintText.alpha = 0f;
            hintText.text = "🎤 Hold trigger to wish";
        }

        // Start the intro sequence
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // Wait before starting (let player orient themselves)
        yield return new WaitForSeconds(introDelay);

        // Don't play if already played (e.g. scene reload)
        if (introPlayed) yield break;
        introPlayed = true;

        // Fetch intro from backend
        string url = $"{serverUrl}/intro";
        Debug.Log($"[GenieIntro] 🎬 Fetching intro from {url}...");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GenieIntro] Failed to fetch intro: {www.error}");
                // Show hint anyway even if intro fails
                yield return FadeInHint();
                yield break;
            }

            // Parse JSON response
            IntroResponse response = JsonUtility.FromJson<IntroResponse>(www.downloadHandler.text);

            // Show subtitle
            if (subtitleText != null && !string.IsNullOrEmpty(response.subtitle))
            {
                subtitleText.text = response.subtitle;
                yield return FadeText(subtitleText, 0f, 1f, 0.5f);
            }

            // Fetch and play audio
            string audioUrl = $"{serverUrl}{response.audio_url}";
            Debug.Log($"[GenieIntro] 🔊 Playing intro audio from: {audioUrl}");

            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
            {
                yield return audioRequest.SendWebRequest();

                if (audioRequest.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                    audioSource.clip = clip;
                    audioSource.Play();

                    // Wait for audio to finish
                    yield return new WaitForSeconds(clip.length);
                }
                else
                {
                    Debug.LogWarning($"[GenieIntro] Failed to play intro audio: {audioRequest.error}");
                    // Still show subtitle for a reading duration
                    yield return new WaitForSeconds(8f);
                }
            }

            // Fade out subtitle
            if (subtitleText != null)
            {
                yield return FadeText(subtitleText, 1f, 0f, 1f);
            }
        }

        // Show persistent hint
        yield return FadeInHint();
    }

    IEnumerator FadeInHint()
    {
        if (hintText != null)
        {
            yield return new WaitForSeconds(0.5f);
            yield return FadeText(hintText, 0f, 1f, 1f);
        }
    }

    IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        text.alpha = to;
    }
}
