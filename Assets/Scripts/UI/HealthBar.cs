using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CityShooter.UI
{
    /// <summary>
    /// Sci-fi styled health bar with curved aesthetic and animated feedback.
    /// Uses a segmented/modular design to match high-tech visual style.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform healthBarContainer;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthBackgroundImage;
        [SerializeField] private Image damageFillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthIcon;
        [SerializeField] private Image[] healthSegments;

        [Header("Colors")]
        [SerializeField] private Color healthyColor = new Color(0f, 1f, 1f, 0.9f); // Cyan
        [SerializeField] private Color warnColor = new Color(1f, 0.8f, 0f, 0.9f); // Yellow
        [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f, 0.9f); // Red
        [SerializeField] private Color damageColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        [SerializeField] private Color glowColor = new Color(0f, 1f, 1f, 0.3f); // Cyan glow

        [Header("Thresholds")]
        [SerializeField] [Range(0f, 1f)] private float warnThreshold = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float criticalThreshold = 0.25f;

        [Header("Animation Settings")]
        [SerializeField] private float fillSpeed = 5f;
        [SerializeField] private float damageDelayTime = 0.5f;
        [SerializeField] private float damageFadeSpeed = 2f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseIntensity = 0.2f;

        [Header("Visual Effects")]
        [SerializeField] private bool enablePulseOnCritical = true;
        [SerializeField] private bool enableGlow = true;
        [SerializeField] private Image glowImage;

        // State
        private float currentHealth;
        private float maxHealth;
        private float displayedHealth;
        private float targetFill;
        private float damageFill;
        private float damageDelayTimer;
        private bool isCritical;
        private float pulseTimer;

        private void Start()
        {
            InitializeVisuals();
        }

        private void Update()
        {
            UpdateHealthFill();
            UpdateDamageFill();
            UpdatePulseEffect();
            UpdateGlowEffect();
        }

        private void InitializeVisuals()
        {
            if (healthBackgroundImage != null)
            {
                healthBackgroundImage.color = backgroundColor;
            }

            if (damageFillImage != null)
            {
                damageFillImage.color = damageColor;
            }

            if (glowImage != null)
            {
                glowImage.color = glowColor;
            }

            UpdateHealthColor();
        }

        private void UpdateHealthFill()
        {
            if (healthFillImage == null) return;

            // Smooth fill animation
            displayedHealth = Mathf.Lerp(displayedHealth, targetFill, fillSpeed * Time.deltaTime);
            healthFillImage.fillAmount = displayedHealth;

            // Update text
            if (healthText != null)
            {
                int displayValue = Mathf.RoundToInt(displayedHealth * maxHealth);
                healthText.text = $"{displayValue}<size=60%>/{Mathf.RoundToInt(maxHealth)}</size>";
            }
        }

        private void UpdateDamageFill()
        {
            if (damageFillImage == null) return;

            if (damageDelayTimer > 0)
            {
                damageDelayTimer -= Time.deltaTime;
            }
            else
            {
                // Fade damage indicator towards current health
                damageFill = Mathf.Lerp(damageFill, displayedHealth, damageFadeSpeed * Time.deltaTime);
                damageFillImage.fillAmount = damageFill;
            }
        }

        private void UpdatePulseEffect()
        {
            if (!enablePulseOnCritical || !isCritical) return;

            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = 1f + Mathf.Sin(pulseTimer * Mathf.PI) * pulseIntensity;

            if (healthFillImage != null)
            {
                Color pulseColor = criticalColor;
                pulseColor.a *= pulse;
                healthFillImage.color = pulseColor;
            }

            // Also pulse the container slightly
            if (healthBarContainer != null)
            {
                float scale = 1f + Mathf.Sin(pulseTimer * Mathf.PI) * 0.02f;
                healthBarContainer.localScale = Vector3.one * scale;
            }
        }

        private void UpdateGlowEffect()
        {
            if (!enableGlow || glowImage == null) return;

            // Glow intensity based on health level
            float glowAlpha = isCritical ? 0.5f + Mathf.Sin(pulseTimer * Mathf.PI) * 0.3f : 0.3f;
            Color glow = GetHealthColor();
            glow.a = glowAlpha;
            glowImage.color = glow;
        }

        private void UpdateHealthColor()
        {
            Color targetColor = GetHealthColor();

            if (healthFillImage != null)
            {
                healthFillImage.color = targetColor;
            }

            if (healthIcon != null)
            {
                healthIcon.color = targetColor;
            }

            if (healthText != null)
            {
                healthText.color = targetColor;
            }

            // Update segments if using segmented style
            if (healthSegments != null && healthSegments.Length > 0)
            {
                UpdateSegments();
            }
        }

        private Color GetHealthColor()
        {
            float healthPercent = currentHealth / maxHealth;

            if (healthPercent <= criticalThreshold)
            {
                return criticalColor;
            }
            else if (healthPercent <= warnThreshold)
            {
                return warnColor;
            }
            return healthyColor;
        }

        private void UpdateSegments()
        {
            float healthPercent = currentHealth / maxHealth;
            int activeSegments = Mathf.CeilToInt(healthPercent * healthSegments.Length);

            for (int i = 0; i < healthSegments.Length; i++)
            {
                if (healthSegments[i] != null)
                {
                    bool isActive = i < activeSegments;
                    healthSegments[i].color = isActive ? GetHealthColor() : backgroundColor;

                    // Fade transition for partially filled segment
                    if (i == activeSegments - 1 && activeSegments > 0)
                    {
                        float segmentFill = (healthPercent * healthSegments.Length) - (activeSegments - 1);
                        Color segmentColor = GetHealthColor();
                        segmentColor.a *= segmentFill;
                        healthSegments[i].color = segmentColor;
                    }
                }
            }
        }

        // ==================== PUBLIC METHODS ====================

        /// <summary>
        /// Update the health display with new values.
        /// </summary>
        public void UpdateHealth(float current, float max)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(current, 0f, max);
            maxHealth = max;

            targetFill = currentHealth / maxHealth;

            // Track damage for delayed fill effect
            if (current < previousHealth)
            {
                damageFill = displayedHealth;
                damageDelayTimer = damageDelayTime;
            }

            // Update critical state
            bool wasCritical = isCritical;
            isCritical = (currentHealth / maxHealth) <= criticalThreshold;

            if (isCritical && !wasCritical)
            {
                pulseTimer = 0f;
            }

            UpdateHealthColor();
        }

        /// <summary>
        /// Trigger a damage flash effect.
        /// </summary>
        public void TriggerDamageFlash()
        {
            if (healthBarContainer != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            Vector3 originalScale = healthBarContainer.localScale;
            float flashDuration = 0.1f;
            float elapsed = 0f;

            // Quick shake effect
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 50f) * 0.05f;
                healthBarContainer.localScale = originalScale + new Vector3(shake, shake, 0);
                yield return null;
            }

            healthBarContainer.localScale = originalScale;
        }

        /// <summary>
        /// Set the health bar visibility.
        /// </summary>
        public void SetVisible(bool visible)
        {
            healthBarContainer?.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Get current health percentage.
        /// </summary>
        public float GetHealthPercent()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }
    }
}
