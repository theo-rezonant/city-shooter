using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central audio management system for the FPS game.
/// Handles AudioMixer groups, ducking, and global audio settings.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("Ducking Settings")]
    [SerializeField] private float duckingAmount = -10f; // dB reduction
    [SerializeField] private float duckingFadeTime = 0.1f;
    [SerializeField] private float duckingRecoveryTime = 0.3f;

    [Header("Global Audio Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float ambienceVolume = 0.7f;
    [SerializeField] private float uiVolume = 1f;

    private float originalAmbienceVolume;
    private Coroutine duckingCoroutine;
    private bool isDucking = false;
    private int activeDuckingRequests = 0;

    // Mixer parameter names
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";
    private const string AMBIENCE_VOLUME_PARAM = "AmbienceVolume";
    private const string UI_VOLUME_PARAM = "UIVolume";

    public AudioMixerGroup SFXGroup => sfxGroup;
    public AudioMixerGroup AmbienceGroup => ambienceGroup;
    public AudioMixerGroup UIGroup => uiGroup;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSettings();
    }

    private void InitializeAudioSettings()
    {
        SetMasterVolume(masterVolume);
        SetSFXVolume(sfxVolume);
        SetAmbienceVolume(ambienceVolume);
        SetUIVolume(uiVolume);

        // Store original ambience volume for ducking
        if (mainMixer != null)
        {
            mainMixer.GetFloat(AMBIENCE_VOLUME_PARAM, out originalAmbienceVolume);
        }
    }

    #region Volume Controls

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SetMixerVolume(MASTER_VOLUME_PARAM, masterVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SetMixerVolume(SFX_VOLUME_PARAM, sfxVolume);
    }

    public void SetAmbienceVolume(float volume)
    {
        ambienceVolume = Mathf.Clamp01(volume);
        originalAmbienceVolume = VolumeToDecibels(ambienceVolume);
        if (!isDucking)
        {
            SetMixerVolume(AMBIENCE_VOLUME_PARAM, ambienceVolume);
        }
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        SetMixerVolume(UI_VOLUME_PARAM, uiVolume);
    }

    private void SetMixerVolume(string parameter, float normalizedVolume)
    {
        if (mainMixer != null)
        {
            // Convert 0-1 range to decibels (-80 to 0)
            float dB = VolumeToDecibels(normalizedVolume);
            mainMixer.SetFloat(parameter, dB);
        }
    }

    private float VolumeToDecibels(float volume)
    {
        return volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
    }

    #endregion

    #region Ducking System

    /// <summary>
    /// Requests ambience ducking (used when weapon fires or loud SFX plays).
    /// Multiple concurrent requests are tracked to prevent early recovery.
    /// </summary>
    public void RequestDucking()
    {
        activeDuckingRequests++;

        if (!isDucking)
        {
            if (duckingCoroutine != null)
            {
                StopCoroutine(duckingCoroutine);
            }
            duckingCoroutine = StartCoroutine(DuckAmbience());
        }
    }

    /// <summary>
    /// Releases a ducking request. Ambience recovers when all requests are released.
    /// </summary>
    public void ReleaseDucking()
    {
        activeDuckingRequests = Mathf.Max(0, activeDuckingRequests - 1);

        if (activeDuckingRequests == 0 && isDucking)
        {
            if (duckingCoroutine != null)
            {
                StopCoroutine(duckingCoroutine);
            }
            duckingCoroutine = StartCoroutine(RecoverAmbience());
        }
    }

    private IEnumerator DuckAmbience()
    {
        if (mainMixer == null) yield break;

        isDucking = true;
        float currentVolume;
        mainMixer.GetFloat(AMBIENCE_VOLUME_PARAM, out currentVolume);
        float targetVolume = originalAmbienceVolume + duckingAmount;

        float elapsed = 0f;
        while (elapsed < duckingFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duckingFadeTime;
            float newVolume = Mathf.Lerp(currentVolume, targetVolume, t);
            mainMixer.SetFloat(AMBIENCE_VOLUME_PARAM, newVolume);
            yield return null;
        }

        mainMixer.SetFloat(AMBIENCE_VOLUME_PARAM, targetVolume);
    }

    private IEnumerator RecoverAmbience()
    {
        if (mainMixer == null) yield break;

        float currentVolume;
        mainMixer.GetFloat(AMBIENCE_VOLUME_PARAM, out currentVolume);

        float elapsed = 0f;
        while (elapsed < duckingRecoveryTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duckingRecoveryTime;
            float newVolume = Mathf.Lerp(currentVolume, originalAmbienceVolume, t);
            mainMixer.SetFloat(AMBIENCE_VOLUME_PARAM, newVolume);
            yield return null;
        }

        mainMixer.SetFloat(AMBIENCE_VOLUME_PARAM, originalAmbienceVolume);
        isDucking = false;
    }

    #endregion

    #region Audio Source Factory

    /// <summary>
    /// Creates a 3D spatial audio source with proper settings for FPS gameplay.
    /// </summary>
    public AudioSource Create3DAudioSource(GameObject parent, AudioMixerGroup mixerGroup = null)
    {
        AudioSource source = parent.AddComponent<AudioSource>();
        Configure3DAudioSource(source, mixerGroup);
        return source;
    }

    /// <summary>
    /// Configures an existing AudioSource for 3D spatial audio.
    /// </summary>
    public void Configure3DAudioSource(AudioSource source, AudioMixerGroup mixerGroup = null)
    {
        source.spatialBlend = 1.0f; // Full 3D
        source.dopplerLevel = 0.5f;
        source.spread = 0f;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.minDistance = 1f;
        source.maxDistance = 50f;

        // Set custom logarithmic falloff curve for smooth transitions
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CreateLogarithmicCurve());

        if (mixerGroup != null)
        {
            source.outputAudioMixerGroup = mixerGroup;
        }
        else if (sfxGroup != null)
        {
            source.outputAudioMixerGroup = sfxGroup;
        }
    }

    /// <summary>
    /// Creates a logarithmic falloff curve for natural sound attenuation.
    /// </summary>
    private AnimationCurve CreateLogarithmicCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        int steps = 20;

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            // Logarithmic falloff formula
            float value = 1f - Mathf.Log10(1f + 9f * t) / Mathf.Log10(10f);
            curve.AddKey(t, Mathf.Max(0f, value));
        }

        return curve;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Plays a one-shot 3D sound at a world position.
    /// </summary>
    public void PlayOneShotAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = position;

        AudioSource source = tempAudio.AddComponent<AudioSource>();
        Configure3DAudioSource(source);
        source.clip = clip;
        source.volume = volume;
        source.Play();

        Destroy(tempAudio, clip.length + 0.1f);
    }

    /// <summary>
    /// Plays a random clip from an array of clips.
    /// </summary>
    public AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    #endregion
}
