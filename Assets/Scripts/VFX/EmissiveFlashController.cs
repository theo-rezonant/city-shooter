using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityShooter.Weapons
{
    /// <summary>
    /// Controls emissive material flashes on specific sub-objects of the laser gun.
    /// Specifically targets the 'fuel' and 'barrel' mesh renderers for emission spikes.
    /// Uses URP Lit shader compatible emission properties.
    /// </summary>
    public class EmissiveFlashController : MonoBehaviour
    {
        [Header("Target Configuration")]
        [Tooltip("Auto-detect fuel and barrel sub-objects by name")]
        [SerializeField] private bool autoDetectTargets = true;

        [Tooltip("Manually assign target renderers for emission flash")]
        [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

        [Tooltip("Names of sub-objects to target (case-insensitive search)")]
        [SerializeField] private string[] targetSubObjectNames = { "fuel", "barrel", "barell" };

        [Header("Flash Settings")]
        [SerializeField] private Color emissionColor = new Color(0f, 1f, 1f, 1f); // Cyan
        [SerializeField] private float peakIntensity = 8f;
        [SerializeField] private float baseIntensity = 0f;
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.25f;

        [Header("Animation Curve")]
        [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("URP Shader Properties")]
        [SerializeField] private string emissionColorProperty = "_EmissionColor";
        [SerializeField] private string emissionKeyword = "_EMISSION";

        private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private Dictionary<Renderer, Material[]> _instancedMaterials = new Dictionary<Renderer, Material[]>();
        private Coroutine _flashCoroutine;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            // Clean up instanced materials
            CleanupInstancedMaterials();
        }

        /// <summary>
        /// Initializes the controller by finding target renderers and setting up materials.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (autoDetectTargets)
            {
                AutoDetectTargetRenderers();
            }

            SetupMaterials();
            _isInitialized = true;
        }

        /// <summary>
        /// Auto-detects fuel and barrel sub-objects from the weapon hierarchy.
        /// </summary>
        private void AutoDetectTargetRenderers()
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in allRenderers)
            {
                string objName = renderer.gameObject.name.ToLower();

                foreach (string targetName in targetSubObjectNames)
                {
                    if (objName.Contains(targetName.ToLower()))
                    {
                        if (!targetRenderers.Contains(renderer))
                        {
                            targetRenderers.Add(renderer);
                            Debug.Log($"[EmissiveFlashController] Auto-detected target: {renderer.gameObject.name}");
                        }
                        break;
                    }
                }
            }

            if (targetRenderers.Count == 0)
            {
                Debug.LogWarning("[EmissiveFlashController] No target sub-objects found. Please assign them manually or check naming.");
            }
        }

        /// <summary>
        /// Sets up instanced materials for emission control without affecting shared materials.
        /// </summary>
        private void SetupMaterials()
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer == null)
                    continue;

                // Store original materials
                Material[] originals = renderer.sharedMaterials;
                _originalMaterials[renderer] = originals;

                // Create instanced copies
                Material[] instances = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    if (originals[i] != null)
                    {
                        instances[i] = new Material(originals[i]);
                        // Enable emission keyword for URP Lit shader
                        instances[i].EnableKeyword(emissionKeyword);

                        // Ensure emission is enabled in material properties
                        if (instances[i].HasProperty("_EmissiveColor"))
                        {
                            // HDRP uses _EmissiveColor
                            emissionColorProperty = "_EmissiveColor";
                        }
                    }
                }

                _instancedMaterials[renderer] = instances;
                renderer.materials = instances;
            }
        }

        /// <summary>
        /// Cleans up instanced materials to prevent memory leaks.
        /// </summary>
        private void CleanupInstancedMaterials()
        {
            foreach (var kvp in _instancedMaterials)
            {
                if (kvp.Value != null)
                {
                    foreach (Material mat in kvp.Value)
                    {
                        if (mat != null)
                        {
                            Destroy(mat);
                        }
                    }
                }
            }

            _instancedMaterials.Clear();
        }

        /// <summary>
        /// Triggers the emissive flash effect on all target renderers.
        /// </summary>
        public void TriggerFlash()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        /// <summary>
        /// Triggers a flash with custom color and intensity.
        /// </summary>
        /// <param name="color">Flash color.</param>
        /// <param name="intensity">Peak intensity multiplier.</param>
        public void TriggerFlash(Color color, float intensity)
        {
            Color originalColor = emissionColor;
            float originalIntensity = peakIntensity;

            emissionColor = color;
            peakIntensity = intensity;

            TriggerFlash();

            // Restore original settings after starting coroutine
            // The coroutine captures the current values
            emissionColor = originalColor;
            peakIntensity = originalIntensity;
        }

        private IEnumerator FlashCoroutine()
        {
            float totalDuration = flashDuration + fadeOutDuration;
            float elapsed = 0f;

            // Capture current settings for this flash
            Color flashColor = emissionColor;
            float peak = peakIntensity;
            float baseVal = baseIntensity;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / totalDuration;

                // Calculate intensity using animation curve
                float curveValue = flashCurve.Evaluate(normalizedTime);

                // During flash phase, spike to peak
                float intensity;
                if (elapsed <= flashDuration)
                {
                    // Quick rise to peak
                    float flashT = elapsed / flashDuration;
                    intensity = Mathf.Lerp(baseVal, peak, Mathf.Sin(flashT * Mathf.PI * 0.5f));
                }
                else
                {
                    // Fade out phase
                    float fadeT = (elapsed - flashDuration) / fadeOutDuration;
                    intensity = Mathf.Lerp(peak, baseVal, fadeT);
                }

                // Apply intensity to all target materials
                SetEmissionIntensity(intensity, flashColor);

                yield return null;
            }

            // Ensure we end at base intensity
            SetEmissionIntensity(baseVal, flashColor);
            _flashCoroutine = null;
        }

        /// <summary>
        /// Sets the emission intensity on all target materials.
        /// </summary>
        private void SetEmissionIntensity(float intensity, Color color)
        {
            Color hdrColor = color * intensity;

            foreach (var kvp in _instancedMaterials)
            {
                Material[] materials = kvp.Value;
                if (materials == null)
                    continue;

                foreach (Material mat in materials)
                {
                    if (mat != null && mat.HasProperty(emissionColorProperty))
                    {
                        mat.SetColor(emissionColorProperty, hdrColor);
                    }
                }
            }
        }

        /// <summary>
        /// Manually adds a renderer to the target list.
        /// </summary>
        public void AddTargetRenderer(Renderer renderer)
        {
            if (renderer == null || targetRenderers.Contains(renderer))
                return;

            targetRenderers.Add(renderer);

            // Setup material for new renderer
            Material[] originals = renderer.sharedMaterials;
            _originalMaterials[renderer] = originals;

            Material[] instances = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                if (originals[i] != null)
                {
                    instances[i] = new Material(originals[i]);
                    instances[i].EnableKeyword(emissionKeyword);
                }
            }

            _instancedMaterials[renderer] = instances;
            renderer.materials = instances;
        }

        /// <summary>
        /// Removes a renderer from the target list.
        /// </summary>
        public void RemoveTargetRenderer(Renderer renderer)
        {
            if (renderer == null || !targetRenderers.Contains(renderer))
                return;

            // Restore original materials
            if (_originalMaterials.TryGetValue(renderer, out Material[] originals))
            {
                renderer.sharedMaterials = originals;
                _originalMaterials.Remove(renderer);
            }

            // Clean up instanced materials
            if (_instancedMaterials.TryGetValue(renderer, out Material[] instances))
            {
                foreach (Material mat in instances)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
                _instancedMaterials.Remove(renderer);
            }

            targetRenderers.Remove(renderer);
        }

        /// <summary>
        /// Sets the emission color for future flashes.
        /// </summary>
        public void SetEmissionColor(Color color)
        {
            emissionColor = color;
        }

        /// <summary>
        /// Gets or sets the peak emission intensity.
        /// </summary>
        public float PeakIntensity
        {
            get => peakIntensity;
            set => peakIntensity = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets the number of target renderers.
        /// </summary>
        public int TargetCount => targetRenderers.Count;

        /// <summary>
        /// Gets whether a flash is currently active.
        /// </summary>
        public bool IsFlashing => _flashCoroutine != null;

#if UNITY_EDITOR
        [ContextMenu("Re-detect Targets")]
        private void EditorRedetectTargets()
        {
            targetRenderers.Clear();
            AutoDetectTargetRenderers();
        }

        [ContextMenu("Test Flash")]
        private void EditorTestFlash()
        {
            if (Application.isPlaying)
            {
                TriggerFlash();
            }
            else
            {
                Debug.Log("[EmissiveFlashController] Flash test only available in Play Mode.");
            }
        }
#endif
    }
}
