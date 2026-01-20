using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages ambient environmental audio loops for the town4new scene.
/// Supports multiple layered ambient sounds (wind, city hum, etc.) that cover the entire map.
/// </summary>
public class EnvironmentalAudio : MonoBehaviour
{
    [System.Serializable]
    public class AmbientLayer
    {
        public string layerName = "Ambient";
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 0.5f;
        public bool loop = true;
        [Tooltip("Fade in time when layer starts")]
        public float fadeInTime = 2f;
        [Tooltip("Random delay range before starting (min, max)")]
        public Vector2 startDelayRange = Vector2.zero;
        [Tooltip("If true, this layer is 2D (non-spatial)")]
        public bool is2D = true;
        [HideInInspector]
        public AudioSource audioSource;
    }

    [System.Serializable]
    public class PointAmbience
    {
        public string pointName = "Ambient Point";
        public Transform position;
        public AudioClip[] clips;
        [Range(0f, 1f)]
        public float volume = 0.6f;
        public float minDistance = 5f;
        public float maxDistance = 30f;
        [Tooltip("Min and max time between random plays")]
        public Vector2 intervalRange = new Vector2(5f, 15f);
        [HideInInspector]
        public AudioSource audioSource;
    }

    [Header("Global Ambient Layers")]
    [SerializeField] private AmbientLayer[] ambientLayers;

    [Header("Point-Based Ambience")]
    [SerializeField] private PointAmbience[] pointAmbiences;

    [Header("Wind System")]
    [SerializeField] private AudioClip[] windClips;
    [SerializeField] private float windBaseVolume = 0.3f;
    [SerializeField] private float windVariationSpeed = 0.5f;
    [SerializeField] private Vector2 windVolumeRange = new Vector2(0.2f, 0.5f);

    [Header("City Hum")]
    [SerializeField] private AudioClip cityHumClip;
    [SerializeField] private float cityHumVolume = 0.25f;

    [Header("Random Ambient Events")]
    [SerializeField] private AudioClip[] distantSoundsClips; // Distant cars, sirens, etc.
    [SerializeField] private float distantSoundVolume = 0.4f;
    [SerializeField] private Vector2 distantSoundInterval = new Vector2(10f, 30f);

    [Header("Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float masterAmbienceVolume = 1f;

    private AudioSource windSource;
    private AudioSource cityHumSource;
    private AudioSource distantSoundsSource;
    private bool isPlaying = false;
    private Coroutine windVariationCoroutine;
    private List<Coroutine> pointAmbienceCoroutines = new List<Coroutine>();

    private void Awake()
    {
        InitializeAudioSources();
    }

    private void Start()
    {
        if (autoStart)
        {
            StartAmbience();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void InitializeAudioSources()
    {
        // Initialize ambient layer sources
        if (ambientLayers != null)
        {
            foreach (var layer in ambientLayers)
            {
                layer.audioSource = CreateAmbientSource(layer.layerName, layer.is2D);
            }
        }

        // Initialize wind source
        windSource = CreateAmbientSource("Wind", true);

        // Initialize city hum source
        cityHumSource = CreateAmbientSource("CityHum", true);

        // Initialize distant sounds source
        distantSoundsSource = CreateAmbientSource("DistantSounds", true);

        // Initialize point ambience sources
        if (pointAmbiences != null)
        {
            foreach (var point in pointAmbiences)
            {
                point.audioSource = CreatePointSource(point);
            }
        }
    }

    private AudioSource CreateAmbientSource(string name, bool is2D)
    {
        GameObject sourceObj = new GameObject($"AmbientSource_{name}");
        sourceObj.transform.SetParent(transform);
        sourceObj.transform.localPosition = Vector3.zero;

        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = is2D ? 0f : 1f;
        source.loop = true;

        // Assign to ambience mixer group
        if (AudioManager.Instance != null && AudioManager.Instance.AmbienceGroup != null)
        {
            source.outputAudioMixerGroup = AudioManager.Instance.AmbienceGroup;
        }

        return source;
    }

    private AudioSource CreatePointSource(PointAmbience point)
    {
        Transform sourceParent = point.position != null ? point.position : transform;
        GameObject sourceObj = new GameObject($"PointAmbience_{point.pointName}");
        sourceObj.transform.SetParent(sourceParent);
        sourceObj.transform.localPosition = Vector3.zero;

        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f; // 3D
        source.rolloffMode = AudioRolloffMode.Custom;
        source.minDistance = point.minDistance;
        source.maxDistance = point.maxDistance;
        source.loop = false;

        // Set custom falloff
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CreateAmbientFalloffCurve());

        // Assign to ambience mixer group
        if (AudioManager.Instance != null && AudioManager.Instance.AmbienceGroup != null)
        {
            source.outputAudioMixerGroup = AudioManager.Instance.AmbienceGroup;
        }

        return source;
    }

    #region Public Control Methods

    /// <summary>
    /// Starts all ambient audio layers.
    /// </summary>
    public void StartAmbience()
    {
        if (isPlaying) return;
        isPlaying = true;

        // Start ambient layers with fade in
        if (ambientLayers != null)
        {
            foreach (var layer in ambientLayers)
            {
                StartCoroutine(StartAmbientLayer(layer));
            }
        }

        // Start wind
        StartCoroutine(StartWind());

        // Start city hum
        StartCoroutine(StartCityHum());

        // Start distant sounds
        StartCoroutine(PlayDistantSoundsLoop());

        // Start point ambiences
        if (pointAmbiences != null)
        {
            foreach (var point in pointAmbiences)
            {
                Coroutine c = StartCoroutine(PlayPointAmbienceLoop(point));
                pointAmbienceCoroutines.Add(c);
            }
        }
    }

    /// <summary>
    /// Stops all ambient audio with optional fade out.
    /// </summary>
    public void StopAmbience(float fadeOutTime = 2f)
    {
        if (!isPlaying) return;

        StartCoroutine(FadeOutAllAmbience(fadeOutTime));
    }

    /// <summary>
    /// Sets the master ambience volume (0-1).
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterAmbienceVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    #endregion

    #region Ambient Layer Playback

    private IEnumerator StartAmbientLayer(AmbientLayer layer)
    {
        if (layer.clip == null || layer.audioSource == null) yield break;

        // Random start delay
        if (layer.startDelayRange.y > 0)
        {
            float delay = Random.Range(layer.startDelayRange.x, layer.startDelayRange.y);
            yield return new WaitForSeconds(delay);
        }

        layer.audioSource.clip = layer.clip;
        layer.audioSource.loop = layer.loop;
        layer.audioSource.volume = 0f;
        layer.audioSource.Play();

        // Fade in
        float elapsed = 0f;
        float targetVolume = layer.volume * masterAmbienceVolume;

        while (elapsed < layer.fadeInTime)
        {
            elapsed += Time.deltaTime;
            layer.audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / layer.fadeInTime);
            yield return null;
        }

        layer.audioSource.volume = targetVolume;
    }

    private IEnumerator StartWind()
    {
        if (windClips == null || windClips.Length == 0 || windSource == null) yield break;

        windSource.clip = windClips[Random.Range(0, windClips.Length)];
        windSource.loop = true;
        windSource.volume = 0f;
        windSource.Play();

        // Fade in
        float elapsed = 0f;
        float targetVolume = windBaseVolume * masterAmbienceVolume;

        while (elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            windSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / 3f);
            yield return null;
        }

        // Start wind variation
        windVariationCoroutine = StartCoroutine(WindVolumeVariation());
    }

    private IEnumerator WindVolumeVariation()
    {
        float time = Random.Range(0f, 100f);

        while (isPlaying)
        {
            time += Time.deltaTime * windVariationSpeed;

            // Use Perlin noise for natural variation
            float noise = Mathf.PerlinNoise(time, 0f);
            float targetVolume = Mathf.Lerp(windVolumeRange.x, windVolumeRange.y, noise) * masterAmbienceVolume;

            windSource.volume = Mathf.Lerp(windSource.volume, targetVolume, Time.deltaTime * 2f);

            yield return null;
        }
    }

    private IEnumerator StartCityHum()
    {
        if (cityHumClip == null || cityHumSource == null) yield break;

        cityHumSource.clip = cityHumClip;
        cityHumSource.loop = true;
        cityHumSource.volume = 0f;
        cityHumSource.Play();

        // Fade in
        float elapsed = 0f;
        float targetVolume = cityHumVolume * masterAmbienceVolume;

        while (elapsed < 4f)
        {
            elapsed += Time.deltaTime;
            cityHumSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / 4f);
            yield return null;
        }

        cityHumSource.volume = targetVolume;
    }

    #endregion

    #region Random Sound Events

    private IEnumerator PlayDistantSoundsLoop()
    {
        if (distantSoundsClips == null || distantSoundsClips.Length == 0) yield break;

        // Initial delay
        yield return new WaitForSeconds(Random.Range(5f, 10f));

        while (isPlaying)
        {
            // Play random distant sound
            AudioClip clip = distantSoundsClips[Random.Range(0, distantSoundsClips.Length)];
            distantSoundsSource.pitch = Random.Range(0.9f, 1.1f);
            distantSoundsSource.PlayOneShot(clip, distantSoundVolume * masterAmbienceVolume);

            // Wait for next sound
            float interval = Random.Range(distantSoundInterval.x, distantSoundInterval.y);
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator PlayPointAmbienceLoop(PointAmbience point)
    {
        if (point.clips == null || point.clips.Length == 0 || point.audioSource == null) yield break;

        // Initial delay
        yield return new WaitForSeconds(Random.Range(2f, 5f));

        while (isPlaying)
        {
            // Play random clip from point
            AudioClip clip = point.clips[Random.Range(0, point.clips.Length)];
            point.audioSource.pitch = Random.Range(0.95f, 1.05f);
            point.audioSource.PlayOneShot(clip, point.volume * masterAmbienceVolume);

            // Wait for next sound
            float interval = Random.Range(point.intervalRange.x, point.intervalRange.y);
            yield return new WaitForSeconds(interval);
        }
    }

    #endregion

    #region Utility Methods

    private IEnumerator FadeOutAllAmbience(float fadeTime)
    {
        isPlaying = false;

        // Stop coroutines
        if (windVariationCoroutine != null)
        {
            StopCoroutine(windVariationCoroutine);
        }

        foreach (var c in pointAmbienceCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        pointAmbienceCoroutines.Clear();

        // Collect all active sources
        List<AudioSource> activeSources = new List<AudioSource>();
        Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

        if (ambientLayers != null)
        {
            foreach (var layer in ambientLayers)
            {
                if (layer.audioSource != null && layer.audioSource.isPlaying)
                {
                    activeSources.Add(layer.audioSource);
                    originalVolumes[layer.audioSource] = layer.audioSource.volume;
                }
            }
        }

        if (windSource != null && windSource.isPlaying)
        {
            activeSources.Add(windSource);
            originalVolumes[windSource] = windSource.volume;
        }

        if (cityHumSource != null && cityHumSource.isPlaying)
        {
            activeSources.Add(cityHumSource);
            originalVolumes[cityHumSource] = cityHumSource.volume;
        }

        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            foreach (var source in activeSources)
            {
                if (originalVolumes.TryGetValue(source, out float original))
                {
                    source.volume = Mathf.Lerp(original, 0f, t);
                }
            }

            yield return null;
        }

        // Stop all sources
        foreach (var source in activeSources)
        {
            source.Stop();
        }
    }

    private void UpdateAllVolumes()
    {
        if (ambientLayers != null)
        {
            foreach (var layer in ambientLayers)
            {
                if (layer.audioSource != null && layer.audioSource.isPlaying)
                {
                    layer.audioSource.volume = layer.volume * masterAmbienceVolume;
                }
            }
        }

        if (windSource != null && windSource.isPlaying)
        {
            windSource.volume = windBaseVolume * masterAmbienceVolume;
        }

        if (cityHumSource != null && cityHumSource.isPlaying)
        {
            cityHumSource.volume = cityHumVolume * masterAmbienceVolume;
        }
    }

    private AnimationCurve CreateAmbientFalloffCurve()
    {
        AnimationCurve curve = new AnimationCurve();

        // Gentle falloff for ambient sounds
        curve.AddKey(0f, 1f);
        curve.AddKey(0.3f, 0.7f);
        curve.AddKey(0.6f, 0.35f);
        curve.AddKey(0.85f, 0.1f);
        curve.AddKey(1f, 0f);

        return curve;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize point ambience positions
        if (pointAmbiences != null)
        {
            foreach (var point in pointAmbiences)
            {
                if (point.position != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(point.position.position, point.minDistance);

                    Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                    Gizmos.DrawWireSphere(point.position.position, point.maxDistance);

                    // Draw label
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(point.position.position + Vector3.up * 2f, point.pointName);
#endif
                }
            }
        }
    }
#endif
}
