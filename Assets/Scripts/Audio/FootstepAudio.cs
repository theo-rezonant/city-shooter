using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Animation-driven footstep audio system.
/// Designed to be triggered by Animation Events in Strafe.fbx.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceFootstepSet
    {
        public string surfaceTag = "Default";
        public AudioClip[] footstepClips;
        public float volumeMultiplier = 1f;
    }

    [Header("Footstep Clips by Surface")]
    [SerializeField] private SurfaceFootstepSet[] surfaceSets;
    [SerializeField] private AudioClip[] defaultFootsteps;

    [Header("Audio Settings")]
    [SerializeField] private float baseVolume = 0.5f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float footstepCooldown = 0.2f;

    [Header("Movement Detection")]
    [SerializeField] private float minVelocityForSound = 0.1f;
    [SerializeField] private bool useVelocityBasedVolume = true;
    [SerializeField] private float maxVelocityForVolume = 6f;

    [Header("3D Spatial Settings")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 25f;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private Transform groundCheckOrigin;

    private AudioSource audioSource;
    private Dictionary<string, SurfaceFootstepSet> surfaceLookup;
    private float lastFootstepTime;
    private int lastClipIndex = -1;
    private Rigidbody characterRigidbody;
    private CharacterController characterController;

    private void Awake()
    {
        InitializeAudioSource();
        BuildSurfaceLookup();

        // Cache movement components
        characterRigidbody = GetComponentInParent<Rigidbody>();
        characterController = GetComponentInParent<CharacterController>();

        if (groundCheckOrigin == null)
        {
            groundCheckOrigin = transform;
        }
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
        audioSource.dopplerLevel = 0f; // Footsteps don't need Doppler
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.playOnAwake = false;

        // Set custom falloff curve
        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CreateFootstepFalloffCurve());

        // Assign to SFX mixer group if available
        if (AudioManager.Instance != null && AudioManager.Instance.SFXGroup != null)
        {
            audioSource.outputAudioMixerGroup = AudioManager.Instance.SFXGroup;
        }
    }

    private void BuildSurfaceLookup()
    {
        surfaceLookup = new Dictionary<string, SurfaceFootstepSet>();

        if (surfaceSets != null)
        {
            foreach (var set in surfaceSets)
            {
                if (!surfaceLookup.ContainsKey(set.surfaceTag))
                {
                    surfaceLookup.Add(set.surfaceTag, set);
                }
            }
        }
    }

    /// <summary>
    /// Called by Animation Events in Strafe.fbx at foot contact frames.
    /// </summary>
    public void PlayFootstep()
    {
        // Cooldown check
        if (Time.time - lastFootstepTime < footstepCooldown) return;
        lastFootstepTime = Time.time;

        // Velocity check
        float velocity = GetCurrentVelocity();
        if (velocity < minVelocityForSound) return;

        // Get surface type
        string surfaceTag = DetectGroundSurface();
        AudioClip clip = GetFootstepClip(surfaceTag, out float volumeMultiplier);

        if (clip == null) return;

        // Calculate volume based on velocity if enabled
        float finalVolume = baseVolume * volumeMultiplier;
        if (useVelocityBasedVolume)
        {
            float velocityFactor = Mathf.Clamp01(velocity / maxVelocityForVolume);
            finalVolume *= Mathf.Lerp(0.5f, 1f, velocityFactor);
        }

        // Randomize pitch for variation
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, finalVolume);
    }

    /// <summary>
    /// Animation Event callback - Left foot.
    /// </summary>
    public void OnLeftFootStep()
    {
        PlayFootstep();
    }

    /// <summary>
    /// Animation Event callback - Right foot.
    /// </summary>
    public void OnRightFootStep()
    {
        PlayFootstep();
    }

    /// <summary>
    /// Generic Animation Event callback.
    /// </summary>
    public void OnFootstepEvent()
    {
        PlayFootstep();
    }

    private float GetCurrentVelocity()
    {
        if (characterController != null)
        {
            return characterController.velocity.magnitude;
        }

        if (characterRigidbody != null)
        {
            return characterRigidbody.linearVelocity.magnitude;
        }

        return float.MaxValue; // If no velocity source, always play
    }

    private string DetectGroundSurface()
    {
        RaycastHit hit;
        if (Physics.Raycast(groundCheckOrigin.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayers))
        {
            // Check for surface tag
            if (!string.IsNullOrEmpty(hit.collider.tag) && hit.collider.tag != "Untagged")
            {
                return hit.collider.tag;
            }

            // Check for terrain
            if (hit.collider.GetComponent<Terrain>() != null)
            {
                return "Terrain";
            }
        }

        return "Default";
    }

    private AudioClip GetFootstepClip(string surfaceTag, out float volumeMultiplier)
    {
        volumeMultiplier = 1f;
        AudioClip[] clips = defaultFootsteps;

        if (surfaceLookup.TryGetValue(surfaceTag, out SurfaceFootstepSet set))
        {
            if (set.footstepClips != null && set.footstepClips.Length > 0)
            {
                clips = set.footstepClips;
                volumeMultiplier = set.volumeMultiplier;
            }
        }

        if (clips == null || clips.Length == 0) return null;

        // Avoid immediate repeats
        int index;
        if (clips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                index = Random.Range(0, clips.Length);
            } while (index == lastClipIndex);
        }

        lastClipIndex = index;
        return clips[index];
    }

    private AnimationCurve CreateFootstepFalloffCurve()
    {
        AnimationCurve curve = new AnimationCurve();

        // Footsteps should be audible nearby but fade quickly
        curve.AddKey(0f, 1f);
        curve.AddKey(0.2f, 0.7f);
        curve.AddKey(0.4f, 0.4f);
        curve.AddKey(0.6f, 0.2f);
        curve.AddKey(0.8f, 0.05f);
        curve.AddKey(1f, 0f);

        return curve;
    }

    /// <summary>
    /// Manually trigger footstep (for non-animation driven movement).
    /// </summary>
    public void TriggerFootstep()
    {
        PlayFootstep();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize audio range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        // Visualize ground check
        Transform origin = groundCheckOrigin != null ? groundCheckOrigin : transform;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin.position + Vector3.up * 0.1f, origin.position + Vector3.down * groundCheckDistance);
    }
#endif
}
