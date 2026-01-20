using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls atmospheric effects for the City Shooter game environment.
/// Manages fog, lighting, and skybox parameters for a cinematic cyberpunk aesthetic.
/// </summary>
public class AtmosphericController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The main directional light (Sun) in the scene")]
    public Light sunLight;

    [Tooltip("The global volume for post-processing effects")]
    public Volume globalVolume;

    [Header("Sun Settings")]
    [Tooltip("Sun rotation angle (Y-axis) for time of day simulation")]
    [Range(0f, 360f)]
    public float sunAngle = 45f;

    [Tooltip("Sun elevation angle")]
    [Range(-10f, 90f)]
    public float sunElevation = 30f;

    [Tooltip("Sun intensity multiplier")]
    [Range(0f, 5f)]
    public float sunIntensity = 2f;

    [Tooltip("Sun color temperature")]
    [Range(1000f, 10000f)]
    public float sunColorTemperature = 6570f;

    [Header("Fog Settings")]
    [Tooltip("Enable/disable fog")]
    public bool fogEnabled = true;

    [Tooltip("Fog color - affects the overall atmosphere")]
    public Color fogColor = new Color(0.65f, 0.55f, 0.5f, 1f);

    [Tooltip("Fog density - higher values create thicker fog")]
    [Range(0.001f, 0.1f)]
    public float fogDensity = 0.005f;

    [Tooltip("Linear fog start distance")]
    [Range(0f, 100f)]
    public float fogStartDistance = 10f;

    [Tooltip("Linear fog end distance - controls maximum view distance")]
    [Range(50f, 1000f)]
    public float fogEndDistance = 300f;

    [Header("Ambient Settings")]
    [Tooltip("Ambient sky color - affects overall scene brightness")]
    public Color ambientSkyColor = new Color(0.6f, 0.5f, 0.45f, 1f);

    [Tooltip("Ambient equator color - mid-tones")]
    public Color ambientEquatorColor = new Color(0.35f, 0.3f, 0.28f, 1f);

    [Tooltip("Ambient ground color - shadows and dark areas")]
    public Color ambientGroundColor = new Color(0.15f, 0.12f, 0.1f, 1f);

    [Tooltip("Ambient intensity multiplier")]
    [Range(0f, 2f)]
    public float ambientIntensity = 1f;

    [Header("Post-Processing Settings")]
    [Tooltip("Bloom intensity")]
    [Range(0f, 2f)]
    public float bloomIntensity = 0.5f;

    [Tooltip("Vignette intensity")]
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.25f;

    [Tooltip("Post exposure adjustment")]
    [Range(-2f, 2f)]
    public float postExposure = 0.5f;

    [Header("Presets")]
    [Tooltip("Current atmospheric preset")]
    public AtmospherePreset currentPreset = AtmospherePreset.CinematicSunset;

    // Volume profile components
    private Bloom _bloom;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;
    private Fog _volumeFog;

    public enum AtmospherePreset
    {
        CinematicSunset,
        CyberpunkNight,
        GoldenHour,
        OvercastMoody,
        ClearDay
    }

    private void Start()
    {
        InitializeComponents();
        ApplyPreset(currentPreset);
    }

    private void InitializeComponents()
    {
        // Auto-find sun light if not assigned
        if (sunLight == null)
        {
            sunLight = FindObjectOfType<Light>();
            if (sunLight != null && sunLight.type != LightType.Directional)
            {
                var lights = FindObjectsOfType<Light>();
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sunLight = light;
                        break;
                    }
                }
            }
        }

        // Auto-find global volume if not assigned
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
        }

        // Get volume profile components
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _bloom);
            globalVolume.profile.TryGet(out _vignette);
            globalVolume.profile.TryGet(out _colorAdjustments);
            globalVolume.profile.TryGet(out _volumeFog);
        }
    }

    private void Update()
    {
        ApplySettings();
    }

    /// <summary>
    /// Applies all atmospheric settings to the scene.
    /// </summary>
    public void ApplySettings()
    {
        ApplySunSettings();
        ApplyFogSettings();
        ApplyAmbientSettings();
        ApplyPostProcessingSettings();
    }

    private void ApplySunSettings()
    {
        if (sunLight == null) return;

        // Apply rotation
        sunLight.transform.rotation = Quaternion.Euler(sunElevation, sunAngle, 0f);

        // Apply intensity
        sunLight.intensity = sunIntensity;

        // Apply color temperature
        sunLight.colorTemperature = sunColorTemperature;
        sunLight.useColorTemperature = true;
    }

    private void ApplyFogSettings()
    {
        // Standard Unity fog
        RenderSettings.fog = fogEnabled;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;

        // URP Volume fog (if available)
        if (_volumeFog != null)
        {
            _volumeFog.enabled.Override(fogEnabled);
            _volumeFog.color.Override(fogColor);
        }
    }

    private void ApplyAmbientSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientIntensity = ambientIntensity;
    }

    private void ApplyPostProcessingSettings()
    {
        if (_bloom != null)
        {
            _bloom.intensity.Override(bloomIntensity);
        }

        if (_vignette != null)
        {
            _vignette.intensity.Override(vignetteIntensity);
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.Override(postExposure);
        }
    }

    /// <summary>
    /// Applies a preset atmospheric configuration.
    /// </summary>
    /// <param name="preset">The preset to apply</param>
    public void ApplyPreset(AtmospherePreset preset)
    {
        currentPreset = preset;

        switch (preset)
        {
            case AtmospherePreset.CinematicSunset:
                ApplyCinematicSunsetPreset();
                break;
            case AtmospherePreset.CyberpunkNight:
                ApplyCyberpunkNightPreset();
                break;
            case AtmospherePreset.GoldenHour:
                ApplyGoldenHourPreset();
                break;
            case AtmospherePreset.OvercastMoody:
                ApplyOvercastMoodyPreset();
                break;
            case AtmospherePreset.ClearDay:
                ApplyClearDayPreset();
                break;
        }

        ApplySettings();
    }

    private void ApplyCinematicSunsetPreset()
    {
        // Sun
        sunAngle = 45f;
        sunElevation = 15f;
        sunIntensity = 2.2f;
        sunColorTemperature = 4500f;

        // Fog - warm sunset haze
        fogEnabled = true;
        fogColor = new Color(0.75f, 0.55f, 0.45f, 1f);
        fogDensity = 0.008f;
        fogStartDistance = 10f;
        fogEndDistance = 250f;

        // Ambient - warm tones
        ambientSkyColor = new Color(0.7f, 0.5f, 0.4f, 1f);
        ambientEquatorColor = new Color(0.5f, 0.35f, 0.3f, 1f);
        ambientGroundColor = new Color(0.2f, 0.12f, 0.1f, 1f);
        ambientIntensity = 1.1f;

        // Post-processing
        bloomIntensity = 0.6f;
        vignetteIntensity = 0.3f;
        postExposure = 0.6f;
    }

    private void ApplyCyberpunkNightPreset()
    {
        // Sun (moon)
        sunAngle = 180f;
        sunElevation = 10f;
        sunIntensity = 0.5f;
        sunColorTemperature = 9000f;

        // Fog - cool purple/blue haze
        fogEnabled = true;
        fogColor = new Color(0.15f, 0.12f, 0.25f, 1f);
        fogDensity = 0.015f;
        fogStartDistance = 5f;
        fogEndDistance = 150f;

        // Ambient - cool tones with purple tint
        ambientSkyColor = new Color(0.1f, 0.08f, 0.2f, 1f);
        ambientEquatorColor = new Color(0.15f, 0.1f, 0.25f, 1f);
        ambientGroundColor = new Color(0.05f, 0.03f, 0.08f, 1f);
        ambientIntensity = 0.6f;

        // Post-processing - high contrast
        bloomIntensity = 0.8f;
        vignetteIntensity = 0.45f;
        postExposure = 0.2f;
    }

    private void ApplyGoldenHourPreset()
    {
        // Sun - low and golden
        sunAngle = 60f;
        sunElevation = 8f;
        sunIntensity = 2.5f;
        sunColorTemperature = 3500f;

        // Fog - warm golden haze
        fogEnabled = true;
        fogColor = new Color(0.9f, 0.7f, 0.4f, 1f);
        fogDensity = 0.006f;
        fogStartDistance = 15f;
        fogEndDistance = 300f;

        // Ambient - warm golden
        ambientSkyColor = new Color(0.8f, 0.65f, 0.4f, 1f);
        ambientEquatorColor = new Color(0.6f, 0.45f, 0.3f, 1f);
        ambientGroundColor = new Color(0.3f, 0.2f, 0.1f, 1f);
        ambientIntensity = 1.2f;

        // Post-processing
        bloomIntensity = 0.7f;
        vignetteIntensity = 0.2f;
        postExposure = 0.8f;
    }

    private void ApplyOvercastMoodyPreset()
    {
        // Sun - diffuse
        sunAngle = 90f;
        sunElevation = 45f;
        sunIntensity = 1.0f;
        sunColorTemperature = 6500f;

        // Fog - grey desaturated
        fogEnabled = true;
        fogColor = new Color(0.5f, 0.5f, 0.52f, 1f);
        fogDensity = 0.012f;
        fogStartDistance = 5f;
        fogEndDistance = 200f;

        // Ambient - desaturated
        ambientSkyColor = new Color(0.45f, 0.45f, 0.5f, 1f);
        ambientEquatorColor = new Color(0.35f, 0.35f, 0.38f, 1f);
        ambientGroundColor = new Color(0.15f, 0.15f, 0.17f, 1f);
        ambientIntensity = 0.8f;

        // Post-processing - muted
        bloomIntensity = 0.3f;
        vignetteIntensity = 0.35f;
        postExposure = 0.0f;
    }

    private void ApplyClearDayPreset()
    {
        // Sun - bright midday
        sunAngle = 45f;
        sunElevation = 60f;
        sunIntensity = 2.0f;
        sunColorTemperature = 6570f;

        // Fog - light atmospheric
        fogEnabled = true;
        fogColor = new Color(0.7f, 0.75f, 0.85f, 1f);
        fogDensity = 0.003f;
        fogStartDistance = 50f;
        fogEndDistance = 500f;

        // Ambient - neutral bright
        ambientSkyColor = new Color(0.55f, 0.6f, 0.7f, 1f);
        ambientEquatorColor = new Color(0.45f, 0.48f, 0.55f, 1f);
        ambientGroundColor = new Color(0.2f, 0.22f, 0.25f, 1f);
        ambientIntensity = 1.0f;

        // Post-processing - clean
        bloomIntensity = 0.4f;
        vignetteIntensity = 0.15f;
        postExposure = 0.3f;
    }

    /// <summary>
    /// Smoothly transitions between two presets over time.
    /// Call this from a coroutine for animated transitions.
    /// </summary>
    /// <param name="targetPreset">The target preset to transition to</param>
    /// <param name="duration">Transition duration in seconds</param>
    public System.Collections.IEnumerator TransitionToPreset(AtmospherePreset targetPreset, float duration)
    {
        // Store current values
        float startSunAngle = sunAngle;
        float startSunElevation = sunElevation;
        float startSunIntensity = sunIntensity;
        float startSunColorTemp = sunColorTemperature;
        Color startFogColor = fogColor;
        float startFogDensity = fogDensity;
        float startFogStart = fogStartDistance;
        float startFogEnd = fogEndDistance;
        Color startAmbientSky = ambientSkyColor;
        Color startAmbientEquator = ambientEquatorColor;
        Color startAmbientGround = ambientGroundColor;
        float startAmbientIntensity = ambientIntensity;
        float startBloomIntensity = bloomIntensity;
        float startVignetteIntensity = vignetteIntensity;
        float startPostExposure = postExposure;

        // Apply target preset to get target values
        ApplyPreset(targetPreset);

        float targetSunAngle = sunAngle;
        float targetSunElevation = sunElevation;
        float targetSunIntensity = sunIntensity;
        float targetSunColorTemp = sunColorTemperature;
        Color targetFogColor = fogColor;
        float targetFogDensity = fogDensity;
        float targetFogStart = fogStartDistance;
        float targetFogEnd = fogEndDistance;
        Color targetAmbientSky = ambientSkyColor;
        Color targetAmbientEquator = ambientEquatorColor;
        Color targetAmbientGround = ambientGroundColor;
        float targetAmbientIntensity = ambientIntensity;
        float targetBloomIntensity = bloomIntensity;
        float targetVignetteIntensity = vignetteIntensity;
        float targetPostExposure = postExposure;

        // Lerp over duration
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smoothstep

            sunAngle = Mathf.Lerp(startSunAngle, targetSunAngle, t);
            sunElevation = Mathf.Lerp(startSunElevation, targetSunElevation, t);
            sunIntensity = Mathf.Lerp(startSunIntensity, targetSunIntensity, t);
            sunColorTemperature = Mathf.Lerp(startSunColorTemp, targetSunColorTemp, t);
            fogColor = Color.Lerp(startFogColor, targetFogColor, t);
            fogDensity = Mathf.Lerp(startFogDensity, targetFogDensity, t);
            fogStartDistance = Mathf.Lerp(startFogStart, targetFogStart, t);
            fogEndDistance = Mathf.Lerp(startFogEnd, targetFogEnd, t);
            ambientSkyColor = Color.Lerp(startAmbientSky, targetAmbientSky, t);
            ambientEquatorColor = Color.Lerp(startAmbientEquator, targetAmbientEquator, t);
            ambientGroundColor = Color.Lerp(startAmbientGround, targetAmbientGround, t);
            ambientIntensity = Mathf.Lerp(startAmbientIntensity, targetAmbientIntensity, t);
            bloomIntensity = Mathf.Lerp(startBloomIntensity, targetBloomIntensity, t);
            vignetteIntensity = Mathf.Lerp(startVignetteIntensity, targetVignetteIntensity, t);
            postExposure = Mathf.Lerp(startPostExposure, targetPostExposure, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are exact
        ApplyPreset(targetPreset);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor helper to apply preset in edit mode.
    /// </summary>
    [ContextMenu("Apply Current Preset")]
    public void EditorApplyPreset()
    {
        InitializeComponents();
        ApplyPreset(currentPreset);
    }
#endif
}
