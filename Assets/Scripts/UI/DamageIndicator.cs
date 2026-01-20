using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityShooter.UI
{
    /// <summary>
    /// Directional damage indicator system that shows where incoming attacks are coming from.
    /// Uses radial alpha-fading graphics pointing toward the damage source.
    /// </summary>
    public class DamageIndicator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform indicatorContainer;
        [SerializeField] private Image indicatorTemplate;
        [SerializeField] private int maxIndicators = 8;

        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;

        [Header("Colors")]
        [SerializeField] private Color indicatorColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Red
        [SerializeField] private Color criticalColor = new Color(1f, 0f, 0f, 1f); // Bright red
        [SerializeField] private Gradient fadeGradient;

        [Header("Position Settings")]
        [SerializeField] private float indicatorDistance = 150f;
        [SerializeField] private float indicatorWidth = 40f;
        [SerializeField] private float indicatorHeight = 80f;

        [Header("Animation Settings")]
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float fadeInDuration = 0.1f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float pulseSpeed = 5f;
        [SerializeField] private float pulseIntensity = 0.3f;

        [Header("Vignette Effect")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField] private Image vignetteImage;
        [SerializeField] private float vignetteIntensity = 0.3f;
        [SerializeField] private float vignetteFadeDuration = 0.5f;

        // Pooled indicators
        private List<DamageIndicatorInstance> indicatorPool;
        private List<DamageIndicatorInstance> activeIndicators;
        private float vignetteTimer;

        private void Awake()
        {
            InitializeIndicatorPool();
            SetupFadeGradient();
        }

        private void Start()
        {
            if (indicatorTemplate != null)
            {
                indicatorTemplate.gameObject.SetActive(false);
            }

            if (vignetteImage != null)
            {
                Color vignetteColor = vignetteImage.color;
                vignetteColor.a = 0f;
                vignetteImage.color = vignetteColor;
            }
        }

        private void Update()
        {
            UpdateActiveIndicators();
            UpdateVignetteEffect();
        }

        private void InitializeIndicatorPool()
        {
            indicatorPool = new List<DamageIndicatorInstance>();
            activeIndicators = new List<DamageIndicatorInstance>();

            for (int i = 0; i < maxIndicators; i++)
            {
                DamageIndicatorInstance instance = CreateIndicatorInstance();
                instance.Hide();
                indicatorPool.Add(instance);
            }
        }

        private DamageIndicatorInstance CreateIndicatorInstance()
        {
            GameObject indicatorGO;

            if (indicatorTemplate != null)
            {
                indicatorGO = Instantiate(indicatorTemplate.gameObject, indicatorContainer);
            }
            else
            {
                // Create a default indicator
                indicatorGO = new GameObject("DamageIndicator");
                indicatorGO.transform.SetParent(indicatorContainer, false);

                Image img = indicatorGO.AddComponent<Image>();
                img.color = indicatorColor;

                RectTransform rect = indicatorGO.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(indicatorWidth, indicatorHeight);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f); // Pivot at bottom for rotation around center
            }

            DamageIndicatorInstance instance = new DamageIndicatorInstance
            {
                gameObject = indicatorGO,
                image = indicatorGO.GetComponent<Image>(),
                rectTransform = indicatorGO.GetComponent<RectTransform>(),
                canvasGroup = indicatorGO.GetComponent<CanvasGroup>() ?? indicatorGO.AddComponent<CanvasGroup>()
            };

            return instance;
        }

        private void SetupFadeGradient()
        {
            if (fadeGradient == null || fadeGradient.colorKeys.Length == 0)
            {
                fadeGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[2]
                {
                    new GradientColorKey(indicatorColor, 0f),
                    new GradientColorKey(indicatorColor, 1f)
                };
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0f, 1f)
                };
                fadeGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        private void UpdateActiveIndicators()
        {
            for (int i = activeIndicators.Count - 1; i >= 0; i--)
            {
                DamageIndicatorInstance indicator = activeIndicators[i];
                indicator.timer += Time.deltaTime;

                // Update direction towards damage source
                if (playerTransform != null)
                {
                    UpdateIndicatorRotation(indicator);
                }

                // Update alpha based on lifetime
                float normalizedTime = indicator.timer / displayDuration;
                UpdateIndicatorAlpha(indicator, normalizedTime);

                // Check for removal
                if (indicator.timer >= displayDuration)
                {
                    indicator.Hide();
                    activeIndicators.RemoveAt(i);
                    indicatorPool.Add(indicator);
                }
            }
        }

        private void UpdateIndicatorRotation(DamageIndicatorInstance indicator)
        {
            // Calculate direction from player to damage source in local space
            Vector3 directionToSource = indicator.damageSourcePosition - playerTransform.position;
            directionToSource.y = 0f; // Flatten to horizontal plane

            // Get the angle relative to player's forward direction
            float angle = Vector3.SignedAngle(playerTransform.forward, directionToSource, Vector3.up);

            // Rotate indicator to point toward damage source
            // Negative because UI rotation is counter-clockwise
            indicator.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

            // Position indicator at edge of screen toward damage source
            float radians = (angle - 90f) * Mathf.Deg2Rad;
            Vector2 position = new Vector2(
                Mathf.Cos(radians) * indicatorDistance,
                Mathf.Sin(radians) * indicatorDistance
            );
            indicator.rectTransform.anchoredPosition = position;
        }

        private void UpdateIndicatorAlpha(DamageIndicatorInstance indicator, float normalizedTime)
        {
            float alpha = 1f;

            // Fade in
            if (normalizedTime < fadeInDuration / displayDuration)
            {
                alpha = normalizedTime / (fadeInDuration / displayDuration);
            }
            // Fade out
            else if (normalizedTime > (displayDuration - fadeOutDuration) / displayDuration)
            {
                float fadeStart = (displayDuration - fadeOutDuration) / displayDuration;
                alpha = 1f - ((normalizedTime - fadeStart) / (fadeOutDuration / displayDuration));
            }

            // Apply pulse effect
            float pulse = 1f + Mathf.Sin(indicator.timer * pulseSpeed * Mathf.PI * 2f) * pulseIntensity;
            alpha *= pulse;

            indicator.canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void UpdateVignetteEffect()
        {
            if (!enableVignette || vignetteImage == null) return;

            if (vignetteTimer > 0)
            {
                vignetteTimer -= Time.deltaTime;

                float normalizedTime = vignetteTimer / vignetteFadeDuration;
                float alpha = Mathf.Lerp(0f, vignetteIntensity, normalizedTime);

                Color vignetteColor = vignetteImage.color;
                vignetteColor.a = alpha;
                vignetteImage.color = vignetteColor;
            }
        }

        private DamageIndicatorInstance GetAvailableIndicator()
        {
            // Try to get from pool
            if (indicatorPool.Count > 0)
            {
                DamageIndicatorInstance indicator = indicatorPool[indicatorPool.Count - 1];
                indicatorPool.RemoveAt(indicatorPool.Count - 1);
                return indicator;
            }

            // If pool is empty, recycle oldest active indicator
            if (activeIndicators.Count > 0)
            {
                DamageIndicatorInstance oldest = activeIndicators[0];
                activeIndicators.RemoveAt(0);
                return oldest;
            }

            // Create new indicator as fallback
            return CreateIndicatorInstance();
        }

        // ==================== PUBLIC METHODS ====================

        /// <summary>
        /// Set the player transform for direction calculations.
        /// </summary>
        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        /// <summary>
        /// Show damage indicator pointing toward the damage source.
        /// </summary>
        public void ShowDamageDirection(Vector3 damageSourcePosition)
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("[DamageIndicator] Player transform not set!");
                return;
            }

            DamageIndicatorInstance indicator = GetAvailableIndicator();

            indicator.damageSourcePosition = damageSourcePosition;
            indicator.timer = 0f;
            indicator.image.color = indicatorColor;

            // Initial setup
            UpdateIndicatorRotation(indicator);
            indicator.canvasGroup.alpha = 0f;
            indicator.gameObject.SetActive(true);

            activeIndicators.Add(indicator);

            // Trigger vignette
            if (enableVignette)
            {
                vignetteTimer = vignetteFadeDuration;
            }
        }

        /// <summary>
        /// Show critical damage indicator with enhanced visuals.
        /// </summary>
        public void ShowCriticalDamageDirection(Vector3 damageSourcePosition)
        {
            ShowDamageDirection(damageSourcePosition);

            // Enhance the newest indicator
            if (activeIndicators.Count > 0)
            {
                DamageIndicatorInstance indicator = activeIndicators[activeIndicators.Count - 1];
                indicator.image.color = criticalColor;
                indicator.rectTransform.sizeDelta = new Vector2(indicatorWidth * 1.3f, indicatorHeight * 1.3f);
            }

            // Stronger vignette
            if (enableVignette)
            {
                vignetteTimer = vignetteFadeDuration * 1.5f;
            }
        }

        /// <summary>
        /// Clear all active damage indicators.
        /// </summary>
        public void ClearAllIndicators()
        {
            foreach (var indicator in activeIndicators)
            {
                indicator.Hide();
                indicatorPool.Add(indicator);
            }
            activeIndicators.Clear();

            if (vignetteImage != null)
            {
                Color vignetteColor = vignetteImage.color;
                vignetteColor.a = 0f;
                vignetteImage.color = vignetteColor;
            }
        }

        /// <summary>
        /// Set visibility of damage indicator system.
        /// </summary>
        public void SetVisible(bool visible)
        {
            indicatorContainer?.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Internal class to manage individual indicator instances.
        /// </summary>
        private class DamageIndicatorInstance
        {
            public GameObject gameObject;
            public Image image;
            public RectTransform rectTransform;
            public CanvasGroup canvasGroup;
            public Vector3 damageSourcePosition;
            public float timer;

            public void Hide()
            {
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
                timer = 0f;
            }
        }
    }
}
