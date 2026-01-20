using UnityEngine;
using System.Collections;

/// <summary>
/// Handles 3D spatial audio for enemy (Soldier) characters.
/// Includes movement sounds and hit reaction audio that syncs with Reaction.fbx animation.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    [Header("Movement Audio")]
    [SerializeField] private AudioClip[] movementFootsteps;
    [SerializeField] private AudioClip[] armorRustleClips;
    [SerializeField] private float movementVolume = 0.4f;
    [SerializeField] private float footstepInterval = 0.4f;

    [Header("Hit Reaction Audio")]
    [SerializeField] private AudioClip[] hitReactionClips;
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField] private float hitReactionVolume = 0.8f;

    [Header("Combat Audio")]
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip[] alertClips;
    [SerializeField] private float combatVolume = 0.7f;

    [Header("Vocal Audio")]
    [SerializeField] private AudioClip[] idleVocalClips;
    [SerializeField] private AudioClip[] painVocalClips;
    [SerializeField] private AudioClip[] deathVocalClips;
    [SerializeField] private float vocalVolume = 0.6f;

    [Header("3D Spatial Settings")]
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 35f;
    [SerializeField] private float dopplerLevel = 0.5f;

    [Header("Performance Settings")]
    [SerializeField] private float cullDistance = 50f;
    [SerializeField] private bool isMoving = false;

    private AudioSource audioSource;
    private AudioSource secondaryAudioSource; // For layered sounds
    private Transform playerTransform;
    private float lastFootstepTime;
    private bool isCulled = false;
    private int lastClipIndex = -1;

    private void Awake()
    {
        InitializeAudioSources();
    }

    private void Start()
    {
        // Find player for distance-based culling
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        UpdateAudioCulling();

        if (!isCulled && isMoving)
        {
            UpdateMovementAudio();
        }
    }

    private void InitializeAudioSources()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        Configure3DAudioSource(audioSource);

        // Create secondary audio source for layering
        secondaryAudioSource = gameObject.AddComponent<AudioSource>();
        Configure3DAudioSource(secondaryAudioSource);
    }

    private void Configure3DAudioSource(AudioSource source)
    {
        source.spatialBlend = 1.0f;
        source.dopplerLevel = dopplerLevel;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.playOnAwake = false;

        // Set custom falloff curve
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CreateEnemyFalloffCurve());

        // Assign to SFX mixer group if available
        if (AudioManager.Instance != null && AudioManager.Instance.SFXGroup != null)
        {
            source.outputAudioMixerGroup = AudioManager.Instance.SFXGroup;
        }
    }

    private void UpdateAudioCulling()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool shouldBeCulled = distance > cullDistance;

        if (shouldBeCulled != isCulled)
        {
            isCulled = shouldBeCulled;

            if (isCulled)
            {
                // Stop ongoing audio when culled
                audioSource.Stop();
                secondaryAudioSource.Stop();
            }
        }
    }

    private void UpdateMovementAudio()
    {
        if (Time.time - lastFootstepTime < footstepInterval) return;
        lastFootstepTime = Time.time;

        PlayFootstep();
    }

    #region Movement Audio

    /// <summary>
    /// Sets the movement state for automatic footstep playback.
    /// </summary>
    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    /// <summary>
    /// Plays a footstep sound. Can be called from animation events.
    /// </summary>
    public void PlayFootstep()
    {
        if (isCulled) return;

        AudioClip clip = GetRandomClipAvoidingRepeat(movementFootsteps);
        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip, movementVolume);
        }

        // Occasionally play armor rustle
        if (armorRustleClips != null && armorRustleClips.Length > 0 && Random.value > 0.7f)
        {
            AudioClip rustleClip = armorRustleClips[Random.Range(0, armorRustleClips.Length)];
            secondaryAudioSource.PlayOneShot(rustleClip, movementVolume * 0.5f);
        }
    }

    /// <summary>
    /// Animation event callback for enemy footsteps.
    /// </summary>
    public void OnEnemyFootstep()
    {
        PlayFootstep();
    }

    #endregion

    #region Hit Reaction Audio

    /// <summary>
    /// Plays hit reaction audio. Should sync with Reaction.fbx animation.
    /// </summary>
    public void PlayHitReaction()
    {
        if (isCulled) return;

        // Play physical hit sound
        AudioClip hitClip = GetRandomClip(hitReactionClips);
        if (hitClip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(hitClip, hitReactionVolume);
        }

        // Play pain vocal
        AudioClip painClip = GetRandomClip(painVocalClips);
        if (painClip != null)
        {
            secondaryAudioSource.pitch = Random.Range(0.9f, 1.1f);
            secondaryAudioSource.PlayOneShot(painClip, vocalVolume);
        }
    }

    /// <summary>
    /// Animation event callback for hit reaction.
    /// </summary>
    public void OnHitReactionEvent()
    {
        PlayHitReaction();
    }

    /// <summary>
    /// Plays death audio.
    /// </summary>
    public void PlayDeath()
    {
        if (isCulled) return;

        // Play death sound effect
        AudioClip deathClip = GetRandomClip(deathClips);
        if (deathClip != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(deathClip, hitReactionVolume);
        }

        // Play death vocal
        AudioClip vocalClip = GetRandomClip(deathVocalClips);
        if (vocalClip != null)
        {
            secondaryAudioSource.pitch = Random.Range(0.85f, 1f);
            secondaryAudioSource.PlayOneShot(vocalClip, vocalVolume);
        }

        // Stop movement sounds
        isMoving = false;
    }

    #endregion

    #region Combat Audio

    /// <summary>
    /// Plays attack sound when enemy fires/attacks.
    /// </summary>
    public void PlayAttack()
    {
        if (isCulled) return;

        AudioClip clip = GetRandomClip(attackClips);
        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, combatVolume);
        }
    }

    /// <summary>
    /// Plays alert sound when enemy spots player.
    /// </summary>
    public void PlayAlert()
    {
        if (isCulled) return;

        AudioClip clip = GetRandomClip(alertClips);
        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip, combatVolume);
        }
    }

    /// <summary>
    /// Plays idle vocal (random chatter while patrolling).
    /// </summary>
    public void PlayIdleVocal()
    {
        if (isCulled) return;

        AudioClip clip = GetRandomClip(idleVocalClips);
        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, vocalVolume * 0.7f);
        }
    }

    #endregion

    #region Utility Methods

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    private AudioClip GetRandomClipAvoidingRepeat(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        int index;
        do
        {
            index = Random.Range(0, clips.Length);
        } while (index == lastClipIndex);

        lastClipIndex = index;
        return clips[index];
    }

    private AnimationCurve CreateEnemyFalloffCurve()
    {
        AnimationCurve curve = new AnimationCurve();

        // Enemy sounds should be clearly audible at medium range for gameplay awareness
        curve.AddKey(0f, 1f);
        curve.AddKey(0.15f, 0.85f);
        curve.AddKey(0.35f, 0.5f);
        curve.AddKey(0.55f, 0.25f);
        curve.AddKey(0.75f, 0.1f);
        curve.AddKey(1f, 0f);

        return curve;
    }

    /// <summary>
    /// Set custom spatial settings (for different enemy types).
    /// </summary>
    public void SetSpatialSettings(float minDist, float maxDist, float doppler)
    {
        minDistance = minDist;
        maxDistance = maxDist;
        dopplerLevel = doppler;

        audioSource.minDistance = minDist;
        audioSource.maxDistance = maxDist;
        audioSource.dopplerLevel = doppler;

        secondaryAudioSource.minDistance = minDist;
        secondaryAudioSource.maxDistance = maxDist;
        secondaryAudioSource.dopplerLevel = doppler;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize audio range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        // Visualize cull distance
        Gizmos.color = new Color(0.5f, 0f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, cullDistance);
    }
#endif
}
