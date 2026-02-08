using UnityEngine;

/// <summary>
/// Plays a single audio clip as background music for the entire game.
/// Loops automatically when the clip ends.
/// Either assign a clip in the Inspector, or put a file in Assets/Audio/Resources/ and name it to match "Resource Name" (e.g. BackgroundMusic.mp3).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Header("Background Music")]
    [Tooltip("Assign here, or leave empty to load from Resources by name below")]
    public AudioClip backgroundClip;

    [Tooltip("If Background Clip is empty, load from Resources/Audio/ by this name (no extension). Put your file in Assets/Audio/Resources/ and name it e.g. BackgroundMusic")]
    public string resourceName = "BackgroundMusic";

    [Range(0f, 1f)]
    public float volume = 0.7f;

    [Tooltip("If true, this GameObject (and the music) persists when loading new scenes")]
    public bool persistAcrossScenes = true;

    private AudioSource _audioSource;
    private static BackgroundMusic _instance;

    private void Awake()
    {
        if (persistAcrossScenes)
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;   // Loop from start when clip ends
        _audioSource.spatialBlend = 0f; // 2D (no 3D positioning)
        _audioSource.volume = volume;
    }

    private void Start()
    {
        if (backgroundClip == null && !string.IsNullOrEmpty(resourceName))
        {
            backgroundClip = Resources.Load<AudioClip>(resourceName);
            if (backgroundClip == null)
                Debug.LogWarning($"[BackgroundMusic] No clip in Inspector and could not load from Resources by name \"{resourceName}\". Put your audio in Assets/Audio/Resources/ and name it {resourceName} (e.g. {resourceName}.mp3).");
        }

        if (backgroundClip != null)
        {
            _audioSource.clip = backgroundClip;
            _audioSource.volume = volume;
            _audioSource.Play();
        }
        else
        {
            Debug.LogWarning("[BackgroundMusic] No clip assigned and no Resources fallback. Assign an AudioClip in the Inspector or add a file to Assets/Audio/Resources/.");
        }
    }

    private void OnValidate()
    {
        if (_audioSource != null && backgroundClip != null && _audioSource.clip != backgroundClip)
        {
            _audioSource.clip = backgroundClip;
            _audioSource.volume = volume;
        }
    }

    /// <summary>Call to change volume at runtime (e.g. from settings).</summary>
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_audioSource != null)
            _audioSource.volume = volume;
    }
}
