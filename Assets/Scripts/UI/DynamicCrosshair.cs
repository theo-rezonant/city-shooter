using UnityEngine;
using UnityEngine.UI;

namespace CityShooter.UI
{
    /// <summary>
    /// Dynamic crosshair that responds to player actions with smooth animations.
    /// Expands during movement and firing for realistic weapon feedback.
    /// </summary>
    public class DynamicCrosshair : MonoBehaviour
    {
        [Header("Crosshair Elements")]
        [SerializeField] private RectTransform crosshairContainer;
        [SerializeField] private Image centerDot;
        [SerializeField] private Image topLine;
        [SerializeField] private Image bottomLine;
        [SerializeField] private Image leftLine;
        [SerializeField] private Image rightLine;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0f, 1f, 1f, 0.9f); // Cyan
        [SerializeField] private Color hitColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private Color criticalColor = new Color(1f, 1f, 0f, 1f); // Yellow

        [Header("Size Settings")]
        [SerializeField] private float baseGap = 8f;
        [SerializeField] private float maxGap = 30f;
        [SerializeField] private float lineLength = 12f;
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private float centerDotSize = 4f;

        [Header("Animation Settings")]
        [SerializeField] private float fireExpansionAmount = 10f;
        [SerializeField] private float movementExpansionAmount = 8f;
        [SerializeField] private float expansionSpeed = 15f;
        [SerializeField] private float contractionSpeed = 8f;

        [Header("Glow Effect")]
        [SerializeField] private bool enableGlow = true;
        [SerializeField] private float glowIntensity = 1.5f;
        [SerializeField] private float glowPulseSpeed = 2f;

        // State
        private float currentGap;
        private float targetGap;
        private bool isMoving;
        private bool isFiring;
        private float movementSpeed;
        private float glowTimer;
        private Color currentColor;

        private void Start()
        {
            InitializeCrosshair();
            currentGap = baseGap;
            targetGap = baseGap;
            currentColor = normalColor;
        }

        private void Update()
        {
            UpdateGap();
            UpdateGlowEffect();
            UpdateCrosshairPositions();
        }

        private void InitializeCrosshair()
        {
            if (centerDot != null)
            {
                centerDot.rectTransform.sizeDelta = new Vector2(centerDotSize, centerDotSize);
                centerDot.color = normalColor;
            }

            SetupLine(topLine, lineThickness, lineLength);
            SetupLine(bottomLine, lineThickness, lineLength);
            SetupLine(leftLine, lineLength, lineThickness);
            SetupLine(rightLine, lineLength, lineThickness);

            SetAllColors(normalColor);
        }

        private void SetupLine(Image line, float width, float height)
        {
            if (line != null)
            {
                line.rectTransform.sizeDelta = new Vector2(width, height);
                line.color = normalColor;
            }
        }

        private void UpdateGap()
        {
            // Calculate target gap based on state
            targetGap = baseGap;

            if (isMoving)
            {
                targetGap += movementExpansionAmount * movementSpeed;
            }

            // Clamp target gap
            targetGap = Mathf.Clamp(targetGap, baseGap, maxGap);

            // Smooth interpolation
            float speed = currentGap < targetGap ? expansionSpeed : contractionSpeed;
            currentGap = Mathf.Lerp(currentGap, targetGap, speed * Time.deltaTime);
        }

        private void UpdateGlowEffect()
        {
            if (!enableGlow) return;

            glowTimer += Time.deltaTime * glowPulseSpeed;
            float pulse = 1f + Mathf.Sin(glowTimer) * 0.1f;

            // Apply subtle pulse to alpha
            Color pulseColor = currentColor;
            pulseColor.a *= pulse;

            SetAllColors(pulseColor);
        }

        private void UpdateCrosshairPositions()
        {
            float offset = currentGap + lineLength * 0.5f;

            if (topLine != null)
                topLine.rectTransform.anchoredPosition = new Vector2(0, offset);
            if (bottomLine != null)
                bottomLine.rectTransform.anchoredPosition = new Vector2(0, -offset);
            if (leftLine != null)
                leftLine.rectTransform.anchoredPosition = new Vector2(-offset, 0);
            if (rightLine != null)
                rightLine.rectTransform.anchoredPosition = new Vector2(offset, 0);
        }

        private void SetAllColors(Color color)
        {
            if (centerDot != null) centerDot.color = color;
            if (topLine != null) topLine.color = color;
            if (bottomLine != null) bottomLine.color = color;
            if (leftLine != null) leftLine.color = color;
            if (rightLine != null) rightLine.color = color;
        }

        // ==================== PUBLIC METHODS ====================

        /// <summary>
        /// Called when the player fires. Triggers immediate expansion.
        /// </summary>
        public void TriggerFireExpansion()
        {
            currentGap += fireExpansionAmount;
            currentGap = Mathf.Clamp(currentGap, baseGap, maxGap);
        }

        /// <summary>
        /// Set the movement state of the player.
        /// </summary>
        public void SetMovementState(bool moving, float speed)
        {
            isMoving = moving;
            movementSpeed = Mathf.Clamp01(speed);
        }

        /// <summary>
        /// Set the firing state (for continuous fire weapons).
        /// </summary>
        public void SetFiringState(bool firing)
        {
            isFiring = firing;
        }

        /// <summary>
        /// Temporarily change crosshair color (e.g., on hit).
        /// </summary>
        public void FlashColor(Color color, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FlashColorCoroutine(color, duration));
        }

        private System.Collections.IEnumerator FlashColorCoroutine(Color flashColor, float duration)
        {
            Color originalColor = currentColor;
            currentColor = flashColor;
            SetAllColors(flashColor);

            yield return new WaitForSeconds(duration);

            currentColor = originalColor;
            SetAllColors(originalColor);
        }

        /// <summary>
        /// Flash hit color when hitting an enemy.
        /// </summary>
        public void ShowHitFeedback()
        {
            FlashColor(hitColor, 0.1f);
        }

        /// <summary>
        /// Set crosshair to critical/warning state.
        /// </summary>
        public void SetCriticalState(bool critical)
        {
            currentColor = critical ? criticalColor : normalColor;
            SetAllColors(currentColor);
        }

        /// <summary>
        /// Enable or disable crosshair visibility.
        /// </summary>
        public void SetVisible(bool visible)
        {
            crosshairContainer?.gameObject.SetActive(visible);
        }
    }
}
