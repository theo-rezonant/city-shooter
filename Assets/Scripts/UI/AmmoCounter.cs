using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CityShooter.UI
{
    /// <summary>
    /// Sci-fi ammo/energy counter with fuel gauge aesthetic.
    /// Designed to complement the laser gun's emissive fuel tank visual.
    /// </summary>
    public class AmmoCounter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform ammoContainer;
        [SerializeField] private Image ammoFillImage;
        [SerializeField] private Image ammoBackgroundImage;
        [SerializeField] private Image ammoGlowImage;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI maxAmmoText;
        [SerializeField] private Image energyIcon;
        [SerializeField] private Image[] energySegments;

        [Header("Reload Indicator")]
        [SerializeField] private RectTransform reloadIndicator;
        [SerializeField] private Image reloadFillImage;
        [SerializeField] private TextMeshProUGUI reloadText;

        [Header("Colors")]
        [SerializeField] private Color fullColor = new Color(0f, 1f, 1f, 0.9f); // Cyan
        [SerializeField] private Color mediumColor = new Color(0f, 0.8f, 1f, 0.9f); // Light cyan
        [SerializeField] private Color lowColor = new Color(1f, 0.6f, 0f, 0.9f); // Orange
        [SerializeField] private Color emptyColor = new Color(1f, 0.2f, 0.2f, 0.9f); // Red
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        [SerializeField] private Color glowColor = new Color(0f, 1f, 1f, 0.4f);

        [Header("Thresholds")]
        [SerializeField] [Range(0f, 1f)] private float lowThreshold = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float mediumThreshold = 0.6f;

        [Header("Animation Settings")]
        [SerializeField] private float fillSpeed = 10f;
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float pulseIntensity = 0.15f;
        [SerializeField] private float fireKickAmount = 0.1f;

        [Header("Visual Effects")]
        [SerializeField] private bool enablePulseOnLow = true;
        [SerializeField] private bool enableGlow = true;
        [SerializeField] private bool useSegmentedDisplay = false;

        // State
        private int currentAmmo;
        private int maxAmmo;
        private float displayedFill;
        private float targetFill;
        private bool isLow;
        private bool isReloading;
        private float pulseTimer;
        private float reloadProgress;

        private void Start()
        {
            InitializeVisuals();
        }

        private void Update()
        {
            UpdateFillAnimation();
            UpdatePulseEffect();
            UpdateGlowEffect();
            UpdateReloadIndicator();
        }

        private void InitializeVisuals()
        {
            if (ammoBackgroundImage != null)
            {
                ammoBackgroundImage.color = backgroundColor;
            }

            if (glowColor != null && ammoGlowImage != null)
            {
                ammoGlowImage.color = glowColor;
            }

            if (reloadIndicator != null)
            {
                reloadIndicator.gameObject.SetActive(false);
            }

            UpdateAmmoColor();
        }

        private void UpdateFillAnimation()
        {
            // Smooth fill animation
            displayedFill = Mathf.Lerp(displayedFill, targetFill, fillSpeed * Time.deltaTime);

            if (ammoFillImage != null)
            {
                ammoFillImage.fillAmount = displayedFill;
            }

            // Update segments if using segmented display
            if (useSegmentedDisplay && energySegments != null)
            {
                UpdateSegments();
            }
        }

        private void UpdatePulseEffect()
        {
            if (!enablePulseOnLow || !isLow) return;

            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = 1f + Mathf.Sin(pulseTimer * Mathf.PI) * pulseIntensity;

            if (ammoFillImage != null)
            {
                Color pulseColor = GetAmmoColor();
                pulseColor.a *= pulse;
                ammoFillImage.color = pulseColor;
            }

            if (ammoContainer != null)
            {
                float scale = 1f + Mathf.Sin(pulseTimer * Mathf.PI) * 0.01f;
                ammoContainer.localScale = Vector3.one * scale;
            }
        }

        private void UpdateGlowEffect()
        {
            if (!enableGlow || ammoGlowImage == null) return;

            float glowAlpha = isLow ? 0.5f + Mathf.Sin(pulseTimer * Mathf.PI) * 0.3f : 0.3f;
            Color glow = GetAmmoColor();
            glow.a = glowAlpha;
            ammoGlowImage.color = glow;
        }

        private void UpdateReloadIndicator()
        {
            if (reloadIndicator == null || !isReloading) return;

            if (reloadFillImage != null)
            {
                reloadFillImage.fillAmount = reloadProgress;
            }
        }

        private void UpdateSegments()
        {
            if (energySegments == null || energySegments.Length == 0) return;

            float ammoPercent = maxAmmo > 0 ? (float)currentAmmo / maxAmmo : 0f;
            int activeSegments = Mathf.CeilToInt(ammoPercent * energySegments.Length);

            for (int i = 0; i < energySegments.Length; i++)
            {
                if (energySegments[i] != null)
                {
                    bool isActive = i < activeSegments;
                    energySegments[i].color = isActive ? GetAmmoColor() : backgroundColor;
                }
            }
        }

        private void UpdateAmmoColor()
        {
            Color targetColor = GetAmmoColor();

            if (ammoFillImage != null)
            {
                ammoFillImage.color = targetColor;
            }

            if (energyIcon != null)
            {
                energyIcon.color = targetColor;
            }

            if (ammoText != null)
            {
                ammoText.color = targetColor;
            }
        }

        private Color GetAmmoColor()
        {
            if (maxAmmo <= 0) return emptyColor;

            float ammoPercent = (float)currentAmmo / maxAmmo;

            if (ammoPercent <= 0f)
            {
                return emptyColor;
            }
            else if (ammoPercent <= lowThreshold)
            {
                return lowColor;
            }
            else if (ammoPercent <= mediumThreshold)
            {
                return mediumColor;
            }
            return fullColor;
        }

        // ==================== PUBLIC METHODS ====================

        /// <summary>
        /// Update the ammo display with new values.
        /// </summary>
        public void UpdateAmmo(int current, int max)
        {
            int previousAmmo = currentAmmo;
            currentAmmo = Mathf.Clamp(current, 0, max);
            maxAmmo = max;

            targetFill = maxAmmo > 0 ? (float)currentAmmo / maxAmmo : 0f;

            // Update text displays
            if (ammoText != null)
            {
                ammoText.text = currentAmmo.ToString("D3");
            }

            if (maxAmmoText != null)
            {
                maxAmmoText.text = $"/{max}";
            }

            // Check low state
            bool wasLow = isLow;
            isLow = maxAmmo > 0 && ((float)currentAmmo / maxAmmo) <= lowThreshold;

            if (isLow && !wasLow)
            {
                pulseTimer = 0f;
            }

            // Trigger fire kick effect if ammo decreased
            if (current < previousAmmo)
            {
                TriggerFireKick();
            }

            UpdateAmmoColor();
        }

        /// <summary>
        /// Trigger visual feedback when firing.
        /// </summary>
        public void TriggerFireKick()
        {
            if (ammoContainer != null)
            {
                StartCoroutine(FireKickCoroutine());
            }
        }

        private System.Collections.IEnumerator FireKickCoroutine()
        {
            Vector3 originalScale = ammoContainer.localScale;
            Vector3 kickScale = originalScale * (1f - fireKickAmount);

            ammoContainer.localScale = kickScale;
            yield return new WaitForSeconds(0.05f);

            float elapsed = 0f;
            float duration = 0.1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                ammoContainer.localScale = Vector3.Lerp(kickScale, originalScale, t);
                yield return null;
            }

            ammoContainer.localScale = originalScale;
        }

        /// <summary>
        /// Show the reload indicator with progress.
        /// </summary>
        public void ShowReloadProgress(float progress)
        {
            if (reloadIndicator == null) return;

            isReloading = progress < 1f;
            reloadProgress = progress;

            reloadIndicator.gameObject.SetActive(isReloading);

            if (reloadText != null)
            {
                reloadText.text = isReloading ? "RECHARGING" : "";
            }
        }

        /// <summary>
        /// Set visibility of the ammo counter.
        /// </summary>
        public void SetVisible(bool visible)
        {
            ammoContainer?.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Check if ammo is critically low.
        /// </summary>
        public bool IsAmmoLow()
        {
            return isLow;
        }

        /// <summary>
        /// Check if ammo is empty.
        /// </summary>
        public bool IsAmmoEmpty()
        {
            return currentAmmo <= 0;
        }
    }
}
