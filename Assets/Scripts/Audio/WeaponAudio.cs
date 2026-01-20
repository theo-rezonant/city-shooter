using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/// <summary>
/// Handles 3D spatial audio for the laser gun weapon.
/// Syncs with firing animations and implements clip pooling to prevent repetitive patterns.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WeaponAudio : MonoBehaviour
{
    [Header("Laser Fire Audio")]
    [SerializeField] private AudioClip[] laserFireClips;
    [SerializeField] private float fireVolume = 0.8f;
    [SerializeField] private float fireMinPitch = 0.95f;
    [SerializeField] private float fireMaxPitch = 1.05f;

    [Header("Weapon Handling Audio")]
    [SerializeField] private AudioClip weaponReadyClip;
    [SerializeField] private AudioClip weaponReloadClip;
    [SerializeField] private AudioClip weaponEmptyClip;
    [SerializeField] private float handlingVolume = 0.6f;

    [Header("Impact Audio")]
    [SerializeField] private AudioClip[] laserImpactClips;
    [SerializeField] private float impactVolume = 0.7f;

    [Header("Audio Settings")]
    [SerializeField] private float cooldownBetweenShots = 0.1f;
    [SerializeField] private bool enableDucking = true;
    [SerializeField] private float duckingDuration = 0.3f;

    [Header("3D Spatial Settings")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 40f;
    [SerializeField] private float dopplerLevel = 0.3f;

    private AudioSource audioSource;
    private int lastClipIndex = -1;
    private float lastFireTime;
    private Coroutine duckingCoroutine;

    private void Awake()
    {
        InitializeAudioSource();
    }

    private void InitializeAudioSource()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure for 3D spatial audio
        audioSource.spatialBlend = 1.0f;
        audioSource.dopplerLevel = dopplerLevel;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.playOnAwake = false;

        // Set custom falloff curve
        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CreateWeaponFalloffCurve());

        // Assign to SFX mixer group if available
        if (AudioManager.Instance != null && AudioManager.Instance.SFXGroup != null)
        {
            audioSource.outputAudioMixerGroup = AudioManager.Instance.SFXGroup;
        }
    }

    /// <summary>
    /// Plays the laser fire sound. Called from animation events or firing scripts.
    /// Uses clip pooling to avoid repetitive patterns.
    /// </summary>
    public void PlayFireSound()
    {
        // Cooldown check to prevent audio spam
        if (Time.time - lastFireTime < cooldownBetweenShots) return;
        lastFireTime = Time.time;

        AudioClip clip = GetRandomClipAvoidingRepeat(laserFireClips);
        if (clip == null) return;

        // Randomize pitch slightly for variation
        audioSource.pitch = Random.Range(fireMinPitch, fireMaxPitch);
        audioSource.PlayOneShot(clip, fireVolume);

        // Request ducking for ambience
        if (enableDucking && AudioManager.Instance != null)
        {
            if (duckingCoroutine != null)
            {
                StopCoroutine(duckingCoroutine);
            }
            duckingCoroutine = StartCoroutine(HandleDucking());
        }
    }

    /// <summary>
    /// Animation event callback for fire sync.
    /// </summary>
    public void OnFireAnimationEvent()
    {
        PlayFireSound();
    }

    /// <summary>
    /// Plays impact sound at a specific world position (for projectile hits).
    /// </summary>
    public void PlayImpactSound(Vector3 position)
    {
        if (laserImpactClips == null || laserImpactClips.Length == 0) return;

        AudioClip clip = laserImpactClips[Random.Range(0, laserImpactClips.Length)];

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOneShotAtPosition(clip, position, impactVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, position, impactVolume);
        }
    }

    /// <summary>
    /// Plays weapon ready sound (holster/equip).
    /// </summary>
    public void PlayWeaponReady()
    {
        if (weaponReadyClip != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(weaponReadyClip, handlingVolume);
        }
    }

    /// <summary>
    /// Plays reload sound.
    /// </summary>
    public void PlayReload()
    {
        if (weaponReloadClip != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(weaponReloadClip, handlingVolume);
        }
    }

    /// <summary>
    /// Plays empty clip/no ammo sound.
    /// </summary>
    public void PlayEmpty()
    {
        if (weaponEmptyClip != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(weaponEmptyClip, handlingVolume);
        }
    }

    /// <summary>
    /// Gets a random clip from the array, avoiding immediate repeats.
    /// </summary>
    private AudioClip GetRandomClipAvoidingRepeat(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        int index;
        do
        {
            index = Random.Range(0, clips.Length);
        } while (index == lastClipIndex && clips.Length > 1);

        lastClipIndex = index;
        return clips[index];
    }

    private IEnumerator HandleDucking()
    {
        AudioManager.Instance.RequestDucking();
        yield return new WaitForSeconds(duckingDuration);
        AudioManager.Instance.ReleaseDucking();
    }

    private AnimationCurve CreateWeaponFalloffCurve()
    {
        AnimationCurve curve = new AnimationCurve();

        // Weapon sounds should have a sharp initial presence then fall off
        curve.AddKey(0f, 1f);
        curve.AddKey(0.1f, 0.9f);
        curve.AddKey(0.3f, 0.6f);
        curve.AddKey(0.5f, 0.3f);
        curve.AddKey(0.7f, 0.15f);
        curve.AddKey(1f, 0f);

        return curve;
    }

    /// <summary>
    /// Sets custom fire clips at runtime (for different weapon modes).
    /// </summary>
    public void SetFireClips(AudioClip[] clips)
    {
        laserFireClips = clips;
        lastClipIndex = -1;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize audio range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
#endif
}
