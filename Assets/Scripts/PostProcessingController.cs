using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityShooter.PostProcessing
{
    /// <summary>
    /// Runtime controller for URP Post-Processing effects.
    /// Manages Bloom, SSAO, Tonemapping, and Vignette effects for cinematic quality.
    /// Specifically tuned for laser gun emissive effects and urban SSAO depth.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class PostProcessingController : MonoBehaviour
    {
        [Header("Volume Reference")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private VolumeProfile volumeProfile;

        [Header("Bloom Settings (Laser Gun Emissives)")]
        [Tooltip("Threshold for bloom to trigger on emissive materials")]
        [Range(0f, 2f)]
        [SerializeField] private float bloomThreshold = 0.9f;

        [Tooltip("Bloom intensity for sci-fi weapon effects")]
        [Range(0f, 3f)]
        [SerializeField] private float bloomIntensity = 1.2f;

        [Tooltip("Bloom scatter for soft glow")]
        [Range(0f, 1f)]
        [SerializeField] private float bloomScatter = 0.7f;

        [Header("SSAO Settings (Urban Environment)")]
        [Tooltip("SSAO intensity for building corners and alleys")]
        [Range(0f, 4f)]
        [SerializeField] private float ssaoIntensity = 2.5f;

        [Tooltip("SSAO radius for urban scale")]
        [Range(0f, 1f)]
        [SerializeField] private float ssaoRadius = 0.5f;

        [Header("Tonemapping")]
        [Tooltip("Tonemapping mode (2 = ACES)")]
        [SerializeField] private TonemappingMode tonemappingMode = TonemappingMode.ACES;

        [Header("Vignette Settings")]
        [Tooltip("Vignette intensity for focus")]
        [Range(0f, 1f)]
        [SerializeField] private float vignetteIntensity = 0.25f;

        [Tooltip("Vignette smoothness")]
        [Range(0f, 1f)]
        [SerializeField] private float vignetteSmoothness = 0.4f;

        [Header("Color Adjustments")]
        [Range(-2f, 2f)]
        [SerializeField] private float postExposure = 0.2f;
        [Range(-100f, 100f)]
        [SerializeField] private float contrast = 10f;
        [Range(-100f, 100f)]
        [SerializeField] private float saturation = 10f;

        // Effect references
        private Bloom bloom;
        private Tonemapping tonemapping;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private FilmGrain filmGrain;

        // Weapon fire bloom boost
        private float currentBloomIntensity;
        private float targetBloomIntensity;
        private bool isWeaponFiring = false;

        private void Awake()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = GetComponent<Volume>();
            }

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                volumeProfile = postProcessVolume.profile;
            }

            InitializeEffects();
        }

        private void InitializeEffects()
        {
            if (volumeProfile == null)
            {
                Debug.LogWarning("[PostProcessingController] No volume profile assigned!");
                return;
            }

            // Get or create Bloom effect
            if (!volumeProfile.TryGet(out bloom))
            {
                bloom = volumeProfile.Add<Bloom>(true);
            }

            // Get or create Tonemapping effect
            if (!volumeProfile.TryGet(out tonemapping))
            {
                tonemapping = volumeProfile.Add<Tonemapping>(true);
            }

            // Get or create Vignette effect
            if (!volumeProfile.TryGet(out vignette))
            {
                vignette = volumeProfile.Add<Vignette>(true);
            }

            // Get or create Color Adjustments effect
            if (!volumeProfile.TryGet(out colorAdjustments))
            {
                colorAdjustments = volumeProfile.Add<ColorAdjustments>(true);
            }

            // Get or create Film Grain effect
            if (!volumeProfile.TryGet(out filmGrain))
            {
                filmGrain = volumeProfile.Add<FilmGrain>(true);
            }

            // Apply initial settings
            ApplySettings();
        }

        private void Start()
        {
            currentBloomIntensity = bloomIntensity;
            targetBloomIntensity = bloomIntensity;
        }

        private void Update()
        {
            // Smoothly interpolate bloom intensity for weapon fire effects
            if (Mathf.Abs(currentBloomIntensity - targetBloomIntensity) > 0.01f)
            {
                currentBloomIntensity = Mathf.Lerp(currentBloomIntensity, targetBloomIntensity, Time.deltaTime * 10f);
                if (bloom != null)
                {
                    bloom.intensity.value = currentBloomIntensity;
                }
            }

            // Reset bloom after weapon fire
            if (isWeaponFiring)
            {
                isWeaponFiring = false;
                targetBloomIntensity = bloomIntensity;
            }
        }

        /// <summary>
        /// Apply all post-processing settings from inspector values.
        /// </summary>
        public void ApplySettings()
        {
            if (bloom != null)
            {
                bloom.active = true;
                bloom.threshold.overrideState = true;
                bloom.threshold.value = bloomThreshold;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = bloomIntensity;
                bloom.scatter.overrideState = true;
                bloom.scatter.value = bloomScatter;
                bloom.highQualityFiltering.overrideState = true;
                bloom.highQualityFiltering.value = true;
            }

            if (tonemapping != null)
            {
                tonemapping.active = true;
                tonemapping.mode.overrideState = true;
                tonemapping.mode.value = tonemappingMode;
            }

            if (vignette != null)
            {
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = vignetteIntensity;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = vignetteSmoothness;
                vignette.color.overrideState = true;
                vignette.color.value = Color.black;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.active = true;
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = postExposure;
                colorAdjustments.contrast.overrideState = true;
                colorAdjustments.contrast.value = contrast;
                colorAdjustments.saturation.overrideState = true;
                colorAdjustments.saturation.value = saturation;
            }

            if (filmGrain != null)
            {
                filmGrain.active = true;
                filmGrain.intensity.overrideState = true;
                filmGrain.intensity.value = 0.15f;
            }

            Debug.Log("[PostProcessingController] Post-processing settings applied successfully.");
        }

        /// <summary>
        /// Boost bloom intensity when the laser gun fires.
        /// Called by the weapon system when firing.
        /// </summary>
        /// <param name="boostMultiplier">Multiplier for bloom intensity (default 1.5x)</param>
        public void OnWeaponFire(float boostMultiplier = 1.5f)
        {
            isWeaponFiring = true;
            targetBloomIntensity = bloomIntensity * boostMultiplier;
            currentBloomIntensity = targetBloomIntensity; // Instant boost
        }

        /// <summary>
        /// Set bloom threshold dynamically for emissive material tuning.
        /// </summary>
        public void SetBloomThreshold(float threshold)
        {
            bloomThreshold = Mathf.Clamp(threshold, 0f, 2f);
            if (bloom != null)
            {
                bloom.threshold.value = bloomThreshold;
            }
        }

        /// <summary>
        /// Set SSAO intensity for scene depth control.
        /// </summary>
        public void SetSSAOIntensity(float intensity)
        {
            ssaoIntensity = Mathf.Clamp(intensity, 0f, 4f);
            // Note: SSAO is controlled via Renderer Feature in URP, not Volume
            Debug.Log($"[PostProcessingController] SSAO intensity set to {ssaoIntensity}. Apply in Renderer Feature settings.");
        }

        /// <summary>
        /// Enable/disable all post-processing for performance testing.
        /// </summary>
        public void SetPostProcessingEnabled(bool enabled)
        {
            if (postProcessVolume != null)
            {
                postProcessVolume.enabled = enabled;
            }
        }

        /// <summary>
        /// Apply cinematic preset for cutscenes/dramatic moments.
        /// </summary>
        public void ApplyCinematicPreset()
        {
            if (vignette != null)
            {
                vignette.intensity.value = 0.4f;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.contrast.value = 15f;
                colorAdjustments.saturation.value = 5f;
            }

            if (filmGrain != null)
            {
                filmGrain.intensity.value = 0.25f;
            }
        }

        /// <summary>
        /// Apply action preset for intense gameplay.
        /// </summary>
        public void ApplyActionPreset()
        {
            if (bloom != null)
            {
                bloom.intensity.value = bloomIntensity * 1.2f;
            }

            if (vignette != null)
            {
                vignette.intensity.value = 0.2f;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = 15f;
            }
        }

        /// <summary>
        /// Reset to default settings.
        /// </summary>
        public void ResetToDefault()
        {
            ApplySettings();
        }

        private void OnValidate()
        {
            // Apply settings when values change in inspector
            if (Application.isPlaying)
            {
                ApplySettings();
            }
        }
    }
}
