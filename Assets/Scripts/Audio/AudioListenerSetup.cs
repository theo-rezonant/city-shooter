using UnityEngine;

/// <summary>
/// Configures the AudioListener on the FPS camera for first-person spatial audio.
/// Ensures proper audio listening perspective for immersive gameplay.
/// </summary>
public class AudioListenerSetup : MonoBehaviour
{
    [Header("Audio Listener Settings")]
    [SerializeField] private bool autoAttachToMainCamera = true;
    [SerializeField] private bool removeOtherListeners = true;

    [Header("Occlusion Settings")]
    [SerializeField] private bool enableSimpleOcclusion = false;
    [SerializeField] private LayerMask occlusionLayers;
    [SerializeField] private float occlusionCheckInterval = 0.1f;

    private AudioListener audioListener;
    private Camera mainCamera;

    private void Awake()
    {
        SetupAudioListener();
    }

    private void Start()
    {
        if (autoAttachToMainCamera)
        {
            AttachToMainCamera();
        }
    }

    private void SetupAudioListener()
    {
        // Check if we already have an AudioListener
        audioListener = GetComponent<AudioListener>();

        if (audioListener == null)
        {
            audioListener = gameObject.AddComponent<AudioListener>();
        }

        // Remove other AudioListeners if specified
        if (removeOtherListeners)
        {
            RemoveOtherAudioListeners();
        }

        // Configure audio settings
        ConfigureAudioSettings();
    }

    private void AttachToMainCamera()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[AudioListenerSetup] No main camera found. AudioListener remains on current object.");
            return;
        }

        // If not already on main camera, move the listener
        if (mainCamera.gameObject != gameObject)
        {
            // Check if main camera already has a listener
            AudioListener existingListener = mainCamera.GetComponent<AudioListener>();

            if (existingListener != null)
            {
                // Use existing listener
                audioListener = existingListener;
            }
            else
            {
                // Transfer listener to camera
                if (audioListener != null)
                {
                    Destroy(audioListener);
                }
                audioListener = mainCamera.gameObject.AddComponent<AudioListener>();
            }
        }
    }

    private void RemoveOtherAudioListeners()
    {
        AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        foreach (var listener in allListeners)
        {
            if (listener != audioListener && listener.gameObject != gameObject)
            {
                Debug.Log($"[AudioListenerSetup] Removing duplicate AudioListener from {listener.gameObject.name}");
                Destroy(listener);
            }
        }
    }

    private void ConfigureAudioSettings()
    {
        // Configure global audio settings
        AudioSettings.Reset(AudioSettings.GetConfiguration());

        // Set volume rolloff scale for consistent 3D audio
        AudioListener.volume = 1.0f;

        // Configure DSP buffer for low latency (for action games)
        var config = AudioSettings.GetConfiguration();

        // Smaller buffer for lower latency (but more CPU usage)
        // Options: 256, 512, 1024
        // Default is usually 1024
        if (config.dspBufferSize > 512)
        {
            config.dspBufferSize = 512;

            // Note: This requires a reset which can cause audio glitches
            // Only uncomment if low latency is critical
            // AudioSettings.Reset(config);
        }
    }

    /// <summary>
    /// Attaches the audio listener to a specific camera transform.
    /// </summary>
    public void AttachToCamera(Camera camera)
    {
        if (camera == null) return;

        // Remove listener from current location
        if (audioListener != null && audioListener.gameObject != camera.gameObject)
        {
            Destroy(audioListener);
        }

        // Add to new camera
        audioListener = camera.GetComponent<AudioListener>();
        if (audioListener == null)
        {
            audioListener = camera.gameObject.AddComponent<AudioListener>();
        }

        mainCamera = camera;
    }

    /// <summary>
    /// Gets the current audio listener component.
    /// </summary>
    public AudioListener GetAudioListener()
    {
        return audioListener;
    }

    /// <summary>
    /// Sets the global audio volume (0-1).
    /// </summary>
    public static void SetGlobalVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Pauses/unpauses all audio.
    /// </summary>
    public static void SetAudioPaused(bool paused)
    {
        AudioListener.pause = paused;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize listener position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw direction indicator
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
#endif
}
